using UnityEngine;
using System.Collections.Generic;

public class Erosion : MonoBehaviour {
  public ComputeShader erosion;
  int mapSizeWithBorder;

  public float[] Erode(float[] map, int mapSize, ErosionSettings settings) {
    mapSizeWithBorder = mapSize + settings.brushRadius * 2;
    int numThreads = settings.numIterations / 1024;

    // Create brush
    List<int> brushIndexOffsets = new List<int>();
    List<float> brushWeights = new List<float>();

    float weightSum = 0;
    for (int brushY = -settings.brushRadius; brushY <= settings.brushRadius; brushY++) {
      for (int brushX = -settings.brushRadius; brushX <= settings.brushRadius; brushX++) {
        float sqrDst = brushX * brushX + brushY * brushY;
        if (sqrDst < settings.brushRadius * settings.brushRadius) {
          brushIndexOffsets.Add(brushY * mapSize + brushX);
          float brushWeight = 1 - Mathf.Sqrt(sqrDst) / settings.brushRadius;
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
    int[] randomIndices = new int[settings.numIterations];
    for (int i = 0; i < settings.numIterations; i++) {
      int randomX = Random.Range(settings.brushRadius, mapSize + settings.brushRadius);
      int randomY = Random.Range(settings.brushRadius, mapSize + settings.brushRadius);
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
    erosion.SetInt("borderSize", settings.brushRadius);
    erosion.SetInt("mapSize", mapSizeWithBorder);
    erosion.SetInt("brushLength", brushIndexOffsets.Count);
    erosion.SetInt("maxLifetime", settings.maxLifetime);
    erosion.SetFloat("inertia", settings.inertia);
    erosion.SetFloat("sedimentCapacityFactor", settings.sedimentCapacityFactor);
    erosion.SetFloat("minSedimentCapacity", settings.minSedimentCapacity);
    erosion.SetFloat("depositSpeed", settings.depositSpeed);
    erosion.SetFloat("erodeSpeed", settings.erodeSpeed);
    erosion.SetFloat("evaporateSpeed", settings.evaporateSpeed);
    erosion.SetFloat("gravity", settings.gravity);
    erosion.SetFloat("startSpeed", settings.startSpeed);
    erosion.SetFloat("startWater", settings.startWater);

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