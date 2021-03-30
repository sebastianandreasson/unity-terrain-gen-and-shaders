using UnityEngine;
using System.Collections.Generic;

public class Erosion : MonoBehaviour {
  public ComputeShader erosion;
  public int numErosionIterations = 50000;
  public int erosionBrushRadius = 3;

  public int maxLifetime = 30;
  public float sedimentCapacityFactor = 3;
  public float minSedimentCapacity = .01f;
  public float depositSpeed = 0.3f;
  public float erodeSpeed = 0.3f;

  public float evaporateSpeed = .01f;
  public float gravity = 4;
  public float startSpeed = 1;
  public float startWater = 1;
  [Range(0, 1)]
  public float inertia = 0.3f;
  int mapSizeWithBorder;

  public float[] Erode(float[] map, int mapSize) {
    mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
    int numThreads = numErosionIterations / 1024;

    // Create brush
    List<int> brushIndexOffsets = new List<int>();
    List<float> brushWeights = new List<float>();

    float weightSum = 0;
    for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++) {
      for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++) {
        float sqrDst = brushX * brushX + brushY * brushY;
        if (sqrDst < erosionBrushRadius * erosionBrushRadius) {
          brushIndexOffsets.Add(brushY * mapSize + brushX);
          float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
          weightSum += brushWeight;
          brushWeights.Add(brushWeight);
        }
      }
    }
    for (int i = 0; i < brushWeights.Count; i++) {
      brushWeights[i] /= weightSum;
    }

    // Send brush data to compute shader
    ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
    ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
    brushIndexBuffer.SetData(brushIndexOffsets);
    brushWeightBuffer.SetData(brushWeights);
    erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
    erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

    // Generate random indices for droplet placement
    int[] randomIndices = new int[numErosionIterations];
    for (int i = 0; i < numErosionIterations; i++) {
      int randomX = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
      int randomY = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
      randomIndices[i] = randomY * mapSize + randomX;
    }

    // Send random indices to compute shader
    ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
    randomIndexBuffer.SetData(randomIndices);
    erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

    // Heightmap buffer
    ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
    mapBuffer.SetData(map);
    erosion.SetBuffer(0, "map", mapBuffer);

    // Settings
    erosion.SetInt("borderSize", erosionBrushRadius);
    erosion.SetInt("mapSize", mapSizeWithBorder);
    erosion.SetInt("brushLength", brushIndexOffsets.Count);
    erosion.SetInt("maxLifetime", maxLifetime);
    erosion.SetFloat("inertia", inertia);
    erosion.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
    erosion.SetFloat("minSedimentCapacity", minSedimentCapacity);
    erosion.SetFloat("depositSpeed", depositSpeed);
    erosion.SetFloat("erodeSpeed", erodeSpeed);
    erosion.SetFloat("evaporateSpeed", evaporateSpeed);
    erosion.SetFloat("gravity", gravity);
    erosion.SetFloat("startSpeed", startSpeed);
    erosion.SetFloat("startWater", startWater);

    // Run compute shader
    erosion.Dispatch(0, numThreads, 1, 1);
    mapBuffer.GetData(map);

    // Release buffers
    mapBuffer.Release();
    randomIndexBuffer.Release();
    brushIndexBuffer.Release();
    brushWeightBuffer.Release();

    return map;
  }
}