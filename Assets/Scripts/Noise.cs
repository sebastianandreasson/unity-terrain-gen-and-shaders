using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

  public enum NormalizeMode { Local, Global }
  public static float[,] GenerateNoiseMap(int mapSize, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
    float[,] noiseMap = new float[mapSize, mapSize];

    System.Random prng = new System.Random(seed);
    Vector2[] octaveOffsets = new Vector2[octaves];

    float maxPossibleHeight = 0;
    float amplitude = 1;
    float frequency = 1;

    for (int i = 0; i < octaves; i++) {
      float offsetX = prng.Next(-100000, 100000) + offset.x;
      float offsetY = prng.Next(-100000, 100000) - offset.y;
      octaveOffsets[i] = new Vector2(offsetX, offsetY);

      maxPossibleHeight += amplitude;
      amplitude *= persistence;
    }

    if (scale <= 0) {
      scale = 0.0001f;
    }

    float maxNoiseHeight = float.MinValue;
    float minNoiseHeight = float.MaxValue;

    float half = mapSize / 2f;

    for (int y = 0; y < mapSize; y++) {
      for (int x = 0; x < mapSize; x++) {
        amplitude = 1;
        frequency = 1;
        float noiseHeight = 0;

        for (int i = 0; i < octaves; i++) {
          float sampleX = (x - half + octaveOffsets[i].x) / scale * frequency;
          float sampleY = (y - half + octaveOffsets[i].y) / scale * frequency;

          float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

          noiseHeight += perlinValue * amplitude;

          amplitude *= persistence;
          frequency *= lacunarity;
        }

        if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
        else if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;

        noiseMap[x, y] = noiseHeight;
      }
    }

    for (int y = 0; y < mapSize; y++) {
      for (int x = 0; x < mapSize; x++) {
        if (normalizeMode == NormalizeMode.Local) {
          noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
        } else {
          float normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight;
          noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
        }
      }
    }

    return noiseMap;
  }

}