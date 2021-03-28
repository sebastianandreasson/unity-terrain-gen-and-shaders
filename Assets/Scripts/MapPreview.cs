using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour {
  public Renderer textureRenderer;
  public MeshFilter meshFilter;
  public MeshRenderer meshRenderer;

  public enum DrawMode {
    NoiseMap,
    Mesh,
    FalloffMap,
  }
  public DrawMode drawMode;

  public MeshSettings meshSettings;
  public HeightMapSettings heightMapSettings;
  public TextureData textureData;

  public Material terrainMaterial;

  [Range(0, MeshSettings.numSupportedLODs - 1)]
  public int editorLOD;
  public bool autoUpdate;

  float[,] falloffMap;
  void Start() {
    gameObject.SetActive(false);
  }
  public void DrawMapInEditor() {
    HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);
    VegetationSpawner vegSpawn = FindObjectOfType<VegetationSpawner>();

    if (drawMode == DrawMode.NoiseMap) {
      DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
    } else if (drawMode == DrawMode.Mesh) {
      DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorLOD));
    } else if (drawMode == DrawMode.FalloffMap) {
      DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
    }

    // vegSpawn.Spawn(transform, heightMap.heightMap); 
  }

  // HeightMap GenerateHeightMap(Vector2 center) {
  //   float[,] noiseMap = Noise.GenerateNoiseMap(mapSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);

  //   if (terrainData.useFalloff) {
  //     if (falloffMap == null) {
  //       falloffMap = FalloffGenerator.GenerateFalloffMap(mapSize + 2);
  //     }

  //     for (int y = 0; y < mapSize + 2; y++) {
  //       for (int x = 0; x < mapSize + 2; x++) {
  //         if (terrainData.useFalloff) {
  //           noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
  //         }
  //       }
  //     }
  //   }

  //   return new HeightMap(noiseMap);
  // }

  public void DrawTexture(Texture2D texture) {
    textureRenderer.sharedMaterial.SetTexture("_MainTex", texture);
    textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

    textureRenderer.gameObject.SetActive(true);
    meshFilter.gameObject.SetActive(false);
  }
  public void DrawMesh(MeshData meshData) {
    meshFilter.sharedMesh = meshData.CreateMesh();

    textureRenderer.gameObject.SetActive(false);
    meshFilter.gameObject.SetActive(true);
  }

  void OnValuesUpdated() {
    if (!Application.isPlaying) {
      DrawMapInEditor();
    }
  }

  void OnTextureValuesUpdated() {
    textureData.ApplyToMaterial(terrainMaterial);
  }

  void OnValidate() {
    if (meshSettings != null) {
      meshSettings.OnValuesUpdated -= OnValuesUpdated;
      meshSettings.OnValuesUpdated += OnValuesUpdated;
    }
    if (heightMapSettings != null) {
      heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
      heightMapSettings.OnValuesUpdated += OnValuesUpdated;
    }
    if (textureData != null) {
      textureData.OnValuesUpdated -= OnTextureValuesUpdated;
      textureData.OnValuesUpdated += OnTextureValuesUpdated;
    }
  }
}
