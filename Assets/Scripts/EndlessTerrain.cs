﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
  const float scale = 1f;
  const float viewerMoveThresholdForChunkUpdate = 25f;
  const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
  public LODInfo[] detailLevels;
  public static float maxViewDist;
  public Transform viewer;
  public static Vector2 viewerPosition;
  Vector2 viewerPositionOld;
  static MapGenerator mapGenerator;
  public Material mapMaterial;
  int chunkSize;
  int chunksVisibleInViewDist;

  Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
  static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

  void Start() {
    mapGenerator = FindObjectOfType<MapGenerator>();
    chunkSize = MapGenerator.mapSize - 1;
    maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
    chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);

    UpdateVisibleChunks();
  }

  void Update() {
    viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
    if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
      viewerPositionOld = viewerPosition;
      UpdateVisibleChunks();
    }
  }

  void UpdateVisibleChunks() {
    foreach (TerrainChunk chunk in terrainChunksVisibleLastUpdate) {
      chunk.SetVisible(false);
    }
    terrainChunksVisibleLastUpdate.Clear();

    int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
    int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

    for (int yOff = -chunksVisibleInViewDist; yOff <= chunksVisibleInViewDist; yOff++) {
      for (int xOff = -chunksVisibleInViewDist; xOff <= chunksVisibleInViewDist; xOff++) {
        Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOff, currentChunkCoordY + yOff);

        if (terrainChunks.ContainsKey(viewedChunkCoord)) {
          TerrainChunk chunk = terrainChunks[viewedChunkCoord];
          chunk.UpdateTerrainChunk();
        } else {
          terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
        }
      }
    }
  }

  public class TerrainChunk {
    Vector2 position;
    GameObject meshObject;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;

    MapData mapData;
    bool mapDataReceived;
    int prevLODIndex = -1;
    public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
      this.detailLevels = detailLevels;
      position = coord * size;
      bounds = new Bounds(position, Vector2.one * size);
      Vector3 positionV3 = new Vector3(position.x, 0, position.y);

      meshObject = new GameObject("Terrain Chunk");
      meshRenderer = meshObject.AddComponent<MeshRenderer>();
      meshRenderer.material = material;
      meshFilter = meshObject.AddComponent<MeshFilter>();

      meshObject.transform.position = positionV3 * scale;
      meshObject.transform.parent = parent;
      meshObject.transform.localScale = Vector3.one * scale;
      SetVisible(false);

      lodMeshes = new LODMesh[detailLevels.Length];

      for (int i = 0; i < lodMeshes.Length; i++) {
        lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
      }

      mapGenerator.RequestMapData(position, OnMapDataReceived);
    }

    void OnMapDataReceived(MapData mapData) {
      this.mapData = mapData;
      mapDataReceived = true;

      Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapSize);
      meshRenderer.material.SetTexture("_MainTex", texture);

      UpdateTerrainChunk();
    }

    void OnMeshDataReceived(MeshData meshData) {
      meshFilter.mesh = meshData.CreateMesh();
    }

    public void UpdateTerrainChunk() {
      if (!mapDataReceived) return;

      float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
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
            lodMesh.RequestMesh(mapData);
          }
        }

        terrainChunksVisibleLastUpdate.Add(this);
      }

      SetVisible(visible);
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
    System.Action updateCallback;

    public LODMesh(int lod, System.Action updateCallback) {
      this.lod = lod;
      this.updateCallback = updateCallback;
    }

    void OnMeshDataReceived(MeshData meshData) {
      mesh = meshData.CreateMesh();
      hasMesh = true;

      updateCallback();
    }

    public void RequestMesh(MapData mapData) {
      hasRequestedMesh = true;
      mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
    }
  }

  [System.Serializable]
  public struct LODInfo {
    public int lod;
    public float visibleDistThreshold;
  }
}