using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk {
  const float colliderGenerationDistanceThreshold = 5;
  public event System.Action<TerrainChunk, bool> onVisibilityChanged;
  public Vector2 coord;
  Vector2 sampleCenter;
  GameObject meshObject;
  Bounds bounds;

  MeshRenderer meshRenderer;
  MeshFilter meshFilter;
  MeshCollider meshCollider;
  LODInfo[] detailLevels;
  LODMesh[] lodMeshes;
  int colliderLODIndex;

  float[] heightMapData;
  HeightMap heightMap;
  bool heightMapReceived;
  bool hasVegetation;
  int prevLODIndex = -1;
  bool hasSetCollider;
  float maxViewDist;

  Erosion erosion;
  ErosionSettings erosionSettings;
  HeightMapGenerator heightMapGenerator;
  HeightMapSettings heightMapSettings;
  MeshSettings meshSettings;
  Transform viewer;
  public TerrainChunk(Vector2 coord, Erosion erosion, ErosionSettings erosionSettings, HeightMapGenerator heightMapGenerator, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform viewer, Transform parent, Material material) {
    this.erosion = erosion;
    this.erosionSettings = erosionSettings;
    this.heightMapGenerator = heightMapGenerator;
    this.heightMapSettings = heightMapSettings;
    this.meshSettings = meshSettings;
    this.coord = coord;
    this.detailLevels = detailLevels;
    this.colliderLODIndex = colliderLODIndex;
    this.viewer = viewer;
    sampleCenter = (coord * meshSettings.meshWorldSize) / meshSettings.meshScale;
    Vector2 position = coord * meshSettings.meshWorldSize;
    bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);
    Vector3 positionV3 = new Vector3(position.x, 0, position.y);

    meshObject = new GameObject("Terrain Chunk");
    meshObject.layer = LayerMask.NameToLayer("Terrain");
    meshRenderer = meshObject.AddComponent<MeshRenderer>();
    meshRenderer.material = material;
    meshFilter = meshObject.AddComponent<MeshFilter>();
    meshCollider = meshObject.AddComponent<MeshCollider>();

    meshObject.transform.position = new Vector3(position.x, 0, position.y);
    meshObject.transform.parent = parent;
    SetVisible(false);

    lodMeshes = new LODMesh[detailLevels.Length];

    for (int i = 0; i < lodMeshes.Length; i++) {
      lodMeshes[i] = new LODMesh(detailLevels[i].lod);
      lodMeshes[i].updateCallback += UpdateTerrainChunk;
      if (i == colliderLODIndex) {
        lodMeshes[i].updateCallback += UpdateCollisionMesh;
      }
    }

    maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
  }

  public void Load() {
    ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, erosionSettings, heightMapSettings, sampleCenter), OnHeightMapReceived);
  }

  Vector2 viewerPosition {
    get {
      return new Vector2(viewer.position.x, viewer.position.z);
    }
  }

  void OnHeightMapReceived(object heightMapObject) {
    this.heightMapData = (float[])heightMapObject;

    this.heightMap = HeightMapGenerator.HeightMapForValues(erosion.Erode(this.heightMapData, meshSettings.numVertsPerLine, erosionSettings), meshSettings.numVertsPerLine + erosionSettings.brushRadius * 2);
    heightMapReceived = true;

    UpdateTerrainChunk();
  }

  void OnMeshDataReceived(MeshData meshData) {
    meshFilter.mesh = meshData.CreateMesh();
  }

  public void UpdateTerrainChunk() {
    if (!heightMapReceived) return;

    float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
    bool wasVisible = IsVisible();
    bool visible = viewerDistFromNearestEdge <= maxViewDist;

    if (visible) {
      int lodIndex = 0;
      for (int i = 0; i < detailLevels.Length - 1; i++) {
        if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold) {
          lodIndex = i + 1;
        } else {
          break;
        }
      }

      if (lodIndex != prevLODIndex) {
        LODMesh lodMesh = lodMeshes[lodIndex];
        if (lodMesh.hasMesh) {
          prevLODIndex = lodIndex;
          meshFilter.mesh = lodMesh.mesh;
        } else if (!lodMesh.hasRequestedMesh) {
          lodMesh.RequestMesh(heightMap, meshSettings);
        }
      }
    }

    if (wasVisible != visible) {
      SetVisible(visible);
      if (onVisibilityChanged != null) {
        onVisibilityChanged(this, visible);
      }
    }
  }

  public void UpdateCollisionMesh() {
    if (hasSetCollider) return;

    float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

    if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
      if (!lodMeshes[colliderLODIndex].hasRequestedMesh) {
        lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
      }
    }

    if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
      if (lodMeshes[colliderLODIndex].hasMesh) {
        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
        hasSetCollider = true;

        LODMesh lodMesh = lodMeshes[colliderLODIndex];
        // if (!hasVegetation) {
        //   VegetationSpawner vegSpawner = FindObjectOfType<VegetationSpawner>();
        //   vegSpawner.Spawn(meshObject.transform, heightMap.values);
        //   hasVegetation = true;
        // }
      }
    }
  }

  public void SetVisible(bool visible) {
    meshObject.SetActive(visible);
  }

  public bool IsVisible() {
    return meshObject.activeSelf;
  }
}

class LODMesh {
  public Mesh mesh;
  public bool hasRequestedMesh;
  public bool hasMesh;
  int lod;
  public event System.Action updateCallback;

  public LODMesh(int lod) {
    this.lod = lod;
  }

  void OnMeshDataReceived(object meshDataObject) {
    mesh = ((MeshData)meshDataObject).CreateMesh();
    hasMesh = true;

    updateCallback();
  }

  public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
    hasRequestedMesh = true;
    ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
  }
}