//
// Created by @Forkercat on 03/04/2021.
//
// A URP compute shader renderer to upload data (created by grass painter tool)
// to compute shader. Check out shader files for mode definitions and comments.
//
// [Added Features]
// 1. Override Material Properties (color, ambient strength)
// 2. Cast Shadow Checkbox (since we are not using MeshRenderer anymore, we can do it here)
// 
// [Usage]
// 1. Create an empty object and create the material
// 2. Put GrassPainterEditor.cs to Asset/Editor
// 3. Drag GeometryGrassPainter.cs to the object  (you can use the old one as well)
// 4. Drag this script (GrassComputeRenderer.cs) to the object
// 5. Set up material and compute shader in the inspector
//
// Please check out NedMakesGames for learning compute shaders and MinionsArt for
// the logic of generating grass, although the scripts are pretty different though.
// Let me know if you have any question!
//
// Note that this shader works with the grass painter tool created by MinionsArt.
// Checkout the website for the tool scripts. I also made an updated version that
// introduces shortcuts just for convenience.
// https://www.patreon.com/posts/geometry-grass-46836032
//
// References & Credits:
// 1. ProceduralGrassRenderer.cs (NedMakesGames, https://gist.github.com/NedMakesGames/3e67fabe49e2e3363a657ef8a6a09838)
// 2. Geometry Grass Shader Tool (MinionsArt, https://www.patreon.com/posts/grass-geometry-1-40090373)
//

using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class GrassComputeRenderer : MonoBehaviour {
  [Header("Components")]
  [SerializeField] private GeometryGrassPainter grassPainter = default;
  [SerializeField] private Mesh sourceMesh = default;
  [SerializeField] private Material material = default;
  [SerializeField] private ComputeShader computeShader = default;

  // Blade
  [Header("Blade")]
  public float grassHeight = 1;
  public float grassWidth = 0.06f;
  public float grassRandomHeight = 0.25f;
  [Range(0, 1)] public float bladeRadius = 0.6f;
  public float bladeForwardAmount = 0.38f;
  [Range(1, 4)] public float bladeCurveAmount = 2;
  // Wind
  [Header("Wind")]
  public float windSpeed = 10;
  public float windStrength = 0.05f;
  // Interactor
  [Header("Interactor")]
  public float affectRadius = 0.3f;
  public float affectStrength = 5;
  // LOD
  [Header("LOD")]
  public float minFadeDistance = 40;
  public float maxFadeDistance = 60;
  // Material
  [Header("Material")]
  public bool overrideMaterial;
  public Color topColor = new Color(0, 1, 0);
  public Color baseColor = new Color(1, 1, 0);
  public float ambientStrength = 0.1f;
  // Other
  [Header("Other")]
  public bool castShadow;

  private Camera m_MainCamera;

  private readonly int m_AllowedBladesPerVertex = 4;
  private readonly int m_AllowedSegmentsPerBlade = 5;

  // The structure to send to the compute shader
  // This layout kind assures that the data is laid out sequentially
  [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind
      .Sequential)]
  private struct SourceVertex {
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;
    public Vector3 color;
  }

  // A state variable to help keep track of whether compute buffers have been set up
  private bool m_Initialized;
  // A compute buffer to hold vertex data of the source mesh
  private ComputeBuffer m_SourceVertBuffer;
  // A compute buffer to hold vertex data of the generated mesh
  private ComputeBuffer m_DrawBuffer;
  // A compute buffer to hold indirect draw arguments
  private ComputeBuffer m_ArgsBuffer;
  // Instantiate the shaders so data belong to their unique compute buffers
  private ComputeShader m_InstantiatedComputeShader;
  private Material m_InstantiatedMaterial;
  // The id of the kernel in the grass compute shader
  private int m_IdGrassKernel;
  // The x dispatch size for the grass compute shader
  private int m_DispatchSize;
  // The local bounds of the generated mesh
  private Bounds m_LocalBounds;

  // The size of one entry in the various compute buffers
  private const int SOURCE_VERT_STRIDE = sizeof(float) * (3 + 3 + 2 + 3);
  private const int DRAW_STRIDE = sizeof(float) * (3 + (3 + 2 + 3) * 3);
  private const int INDIRECT_ARGS_STRIDE = sizeof(int) * 4;

  // The data to reset the args buffer with every frame
  // 0: vertex count per draw instance. We will only use one instance
  // 1: instance count. One
  // 2: start vertex location if using a Graphics Buffer
  // 3: and start instance location if using a Graphics Buffer
  private int[] argsBufferReset = new int[] { 0, 1, 0, 0 };

  private void OnValidate() {
    // Set up components
    m_MainCamera = Camera.main;
    grassPainter = GetComponent<GeometryGrassPainter>();
    sourceMesh = grassPainter.mesh;
    // sourceMesh = GetComponent<MeshFilter>().sharedMesh;  // generated by GeometryGrassPainter
  }

  private void OnEnable() {
    // If initialized, call on disable to clean things up
    if (m_Initialized) {
      OnDisable();
    }

    // Setup compute shader and material manually

    // Don't do anything if resources are not found,
    // or no vertex is put on the mesh.
    if (grassPainter == null || sourceMesh == null || computeShader == null || material == null) {
      return;
    }

    sourceMesh = grassPainter.mesh; // update mesh

    if (sourceMesh.vertexCount == 0) {
      return;
    }

    m_Initialized = true;

    // Instantiate the shaders so they can point to their own buffers
    m_InstantiatedComputeShader = Instantiate(computeShader);
    m_InstantiatedMaterial = Instantiate(material);

    // Grab data from the source mesh
    Vector3[] positions = sourceMesh.vertices;
    Vector3[] normals = sourceMesh.normals;
    Vector2[] uvs = sourceMesh.uv;
    Color[] colors = sourceMesh.colors;

    // Create the data to upload to the source vert buffer
    SourceVertex[] vertices = new SourceVertex[positions.Length];
    for (int i = 0; i < vertices.Length; i++) {
      Color color = colors[i];
      vertices[i] = new SourceVertex() {
        position = positions[i],
        normal = normals[i],
        uv = uvs[i],
        color = new Vector3(color.r, color.g, color.b) // Color --> Vector3
      };
    }

    int numSourceVertices = vertices.Length;

    // Each segment has two points
    int maxBladesPerVertex = Mathf.Max(1, m_AllowedBladesPerVertex);
    int maxSegmentsPerBlade = Mathf.Max(1, m_AllowedSegmentsPerBlade);
    int maxBladeTriangles = maxBladesPerVertex * ((maxSegmentsPerBlade - 1) * 2 + 1);

    // Create compute buffers
    // The stride is the size, in bytes, each object in the buffer takes up
    m_SourceVertBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERT_STRIDE,
        ComputeBufferType.Structured, ComputeBufferMode.Immutable);
    m_SourceVertBuffer.SetData(vertices);

    m_DrawBuffer = new ComputeBuffer(numSourceVertices * maxBladeTriangles, DRAW_STRIDE,
        ComputeBufferType.Append);
    m_DrawBuffer.SetCounterValue(0);

    m_ArgsBuffer =
        new ComputeBuffer(1, INDIRECT_ARGS_STRIDE, ComputeBufferType.IndirectArguments);

    // Cache the kernel IDs we will be dispatching
    m_IdGrassKernel = m_InstantiatedComputeShader.FindKernel("Main");

    // Set buffer data
    m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_SourceVertices",
        m_SourceVertBuffer);
    m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_DrawTriangles", m_DrawBuffer);
    m_InstantiatedComputeShader.SetBuffer(m_IdGrassKernel, "_IndirectArgsBuffer",
        m_ArgsBuffer);
    // Set vertex data
    m_InstantiatedComputeShader.SetInt("_NumSourceVertices", numSourceVertices);
    m_InstantiatedComputeShader.SetInt("_MaxBladesPerVertex", maxBladesPerVertex);
    m_InstantiatedComputeShader.SetInt("_MaxSegmentsPerBlade", maxSegmentsPerBlade);

    m_InstantiatedMaterial.SetBuffer("_DrawTriangles", m_DrawBuffer);
    m_InstantiatedMaterial.SetShaderPassEnabled("ShadowCaster", castShadow);

    if (overrideMaterial) {
      m_InstantiatedMaterial.SetColor("_TopColor", topColor);
      m_InstantiatedMaterial.SetColor("_BaseColor", baseColor);
      m_InstantiatedMaterial.SetFloat("_AmbientStrength", ambientStrength);
    }

    // Calculate the number of threads to use. Get the thread size from the kernel
    // Then, divide the number of triangles by that size
    m_InstantiatedComputeShader.GetKernelThreadGroupSizes(m_IdGrassKernel,
        out uint threadGroupSize, out _, out _);
    m_DispatchSize = Mathf.CeilToInt((float)numSourceVertices / threadGroupSize);

    // Get the bounds of the source mesh and then expand by the maximum blade width and height
    m_LocalBounds = sourceMesh.bounds;
    m_LocalBounds.Expand(Mathf.Max(grassHeight + grassRandomHeight, grassWidth));
    // localBounds.Expand(1);  // default
  }

  private void OnDisable() {
    // Dispose of buffers and copied shaders here
    if (m_Initialized) {
      // If the application is not in play mode, we have to call DestroyImmediate
      if (Application.isPlaying) {
        Destroy(m_InstantiatedComputeShader);
        Destroy(m_InstantiatedMaterial);
      } else {
        DestroyImmediate(m_InstantiatedComputeShader);
        DestroyImmediate(m_InstantiatedMaterial);
      }

      // Release each buffer
      m_SourceVertBuffer?.Release();
      m_DrawBuffer?.Release();
      m_ArgsBuffer?.Release();
    }

    m_Initialized = false;
  }

  // LateUpdate is called after all Update calls
  private void LateUpdate() {
    // If in edit mode, we need to update the shaders each Update to make sure settings changes are applied
    // Don't worry, in edit mode, Update isn't called each frame
    if (Application.isPlaying == false) {
      OnDisable();
      OnEnable();
    }

    // Debug.Log("LateUpdate : " + m_Initialized + " " + sourceMesh.vertexCount);

    // If not initialized, do nothing (creating zero-length buffer will crash)
    if (!m_Initialized) {
      // Initialization is not done, please check if there are null components
      // or just because there is not vertex being painted.
      return;
    }

    // Clear the draw and indirect args buffers of last frame's data
    m_DrawBuffer.SetCounterValue(0);
    m_ArgsBuffer.SetData(argsBufferReset);

    // Transform the bounds to world space
    Bounds bounds = TransformBounds(m_LocalBounds);

    // Update the shader with frame specific data
    SetGrassData();

    // Dispatch the grass shader. It will run on the GPU
    m_InstantiatedComputeShader.Dispatch(m_IdGrassKernel, m_DispatchSize, 1, 1);

    // DrawProceduralIndirect queues a draw call up for our generated mesh
    Graphics.DrawProceduralIndirect(m_InstantiatedMaterial, bounds, MeshTopology.Triangles,
        m_ArgsBuffer, 0, null, null, ShadowCastingMode.On, true, gameObject.layer);
  }

  private void SetGrassData() {
    // Compute Shader
    m_InstantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
    m_InstantiatedComputeShader.SetFloat("_Time", Time.time);
    m_InstantiatedComputeShader.SetVector("_CameraPositionWS",
        m_MainCamera.transform.position);

    m_InstantiatedComputeShader.SetFloat("_GrassHeight", grassHeight);
    m_InstantiatedComputeShader.SetFloat("_GrassWidth", grassWidth);
    m_InstantiatedComputeShader.SetFloat("_GrassRandomHeight", grassRandomHeight);

    m_InstantiatedComputeShader.SetFloat("_WindSpeed", windSpeed);
    m_InstantiatedComputeShader.SetFloat("_WindStrength", windStrength);

    m_InstantiatedComputeShader.SetFloat("_InteractorRadius", affectRadius);
    m_InstantiatedComputeShader.SetFloat("_InteractorStrength", affectStrength);

    m_InstantiatedComputeShader.SetFloat("_BladeRadius", bladeRadius);
    m_InstantiatedComputeShader.SetFloat("_BladeForward", bladeForwardAmount);
    m_InstantiatedComputeShader.SetFloat("_BladeCurve", Mathf.Max(0, bladeCurveAmount));

    m_InstantiatedComputeShader.SetFloat("_MinFadeDist", minFadeDistance);
    m_InstantiatedComputeShader.SetFloat("_MaxFadeDist", maxFadeDistance);

    // Material
    m_InstantiatedMaterial.SetFloat("_FogStartDistance", RenderSettings.fogStartDistance);
    m_InstantiatedMaterial.SetFloat("_FogEndDistance", RenderSettings.fogEndDistance);
  }

  // This applies the game object's transform to the local bounds
  // Code by benblo from https://answers.unity.com/questions/361275/cant-convert-bounds-from-world-coordinates-to-loca.html
  private Bounds TransformBounds(Bounds boundsOS) {
    var center = transform.TransformPoint(boundsOS.center);

    // transform the local extents' axes
    var extents = boundsOS.extents;
    var axisX = transform.TransformVector(extents.x, 0, 0);
    var axisY = transform.TransformVector(0, extents.y, 0);
    var axisZ = transform.TransformVector(0, 0, extents.z);

    // sum their absolute value to get the world extents
    extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
    extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
    extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

    return new Bounds { center = center, extents = extents };
  }
}