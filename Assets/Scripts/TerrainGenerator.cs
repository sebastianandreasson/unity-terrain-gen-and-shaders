using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
  const float viewerMoveThresholdForChunkUpdate = 25f;
  const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

  public HeightMapGenerator heightMapGenerator;
  public MeshSettings meshSettings;
  public HeightMapSettings heightMapSettings;
  public ErosionSettings erosionSettings;
  public int colliderLODIndex;
  public LODInfo[] detailLevels;
  public static float maxViewDist;
  public Transform viewer;
  static Vector2 viewerPosition;
  Vector2 viewerPositionOld;
  public Material mapMaterial;
  float meshWorldSize;
  int chunksVisibleInViewDist;

  Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
  List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

  void Start() {
    meshWorldSize = meshSettings.meshWorldSize;
    float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
    chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / meshWorldSize);

    UpdateVisibleChunks();
  }

  void Update() {
    viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

    if (viewerPosition != viewerPositionOld) {
      foreach (TerrainChunk chunk in visibleTerrainChunks) {
        chunk.UpdateCollisionMesh();
      }
    }

    if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
      viewerPositionOld = viewerPosition;
      UpdateVisibleChunks();
    }
  }

  void UpdateVisibleChunks() {
    HashSet<Vector2> alreadyUpdatedChunks = new HashSet<Vector2>();

    for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
      alreadyUpdatedChunks.Add(visibleTerrainChunks[i].coord);
      visibleTerrainChunks[i].UpdateTerrainChunk();
    }

    int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
    int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

    for (int yOff = -chunksVisibleInViewDist; yOff <= chunksVisibleInViewDist; yOff++) {
      for (int xOff = -chunksVisibleInViewDist; xOff <= chunksVisibleInViewDist; xOff++) {
        Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOff, currentChunkCoordY + yOff);

        if (!alreadyUpdatedChunks.Contains(viewedChunkCoord)) {
          if (terrainChunks.ContainsKey(viewedChunkCoord)) {
            TerrainChunk chunk = terrainChunks[viewedChunkCoord];
            chunk.UpdateTerrainChunk();
          } else {
            Erosion erosion = FindObjectOfType<Erosion>();
            TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, erosion, erosionSettings, heightMapGenerator, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, viewer, transform, mapMaterial);
            terrainChunks.Add(viewedChunkCoord, newChunk);
            newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
            newChunk.Load();
          }
        }
      }
    }
  }

  void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
    if (isVisible) {
      visibleTerrainChunks.Add(chunk);
    } else {
      visibleTerrainChunks.Remove(chunk);
    }
  }
}

[System.Serializable]
public struct LODInfo {
  [Range(0, MeshSettings.numSupportedLODs - 1)]
  public int lod;
  public float visibleDistThreshold;

  public float sqrVisibleDstThreshold {
    get {
      return visibleDistThreshold * visibleDistThreshold;
    }
  }
}
