﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

  public enum NormalizeMode { Local, Global }
  public static float[,] GenerateNoiseMap(int mapSize, NoiseSettings settings, Vector2 sampleCenter) {
    float[,] noiseMap = new float[mapSize, mapSize];

    System.Random prng = new System.Random(settings.seed);
    Vector2[] octaveOffsets = new Vector2[settings.octaves];

    float maxPossibleHeight = 0;
    float amplitude = 1;
    float frequency = 1;

    for (int i = 0; i < settings.octaves; i++) {
      float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
      float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;
      octaveOffsets[i] = new Vector2(offsetX, offsetY);

      maxPossibleHeight += amplitude;
      amplitude *= settings.persistance;
    }

    float maxNoiseHeight = float.MinValue;
    float minNoiseHeight = float.MaxValue;

    float half = mapSize / 2f;

    for (int y = 0; y < mapSize; y++) {
      for (int x = 0; x < mapSize; x++) {
        amplitude = 1;
        frequency = 1;
        float noiseHeight = 0;

        for (int i = 0; i < settings.octaves; i++) {
          float sampleX = (x - half + octaveOffsets[i].x) / settings.scale * frequency;
          float sampleY = (y - half + octaveOffsets[i].y) / settings.scale * frequency;

          float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

          noiseHeight += perlinValue * amplitude;

          amplitude *= settings.persistance;
          frequency *= settings.lacunarity;
        }

        if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
        if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;

        noiseMap[x, y] = noiseHeight;

        if (settings.normalizeMode == NormalizeMode.Global) {
          float normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight;
          noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
        }
      }
    }

    if (settings.normalizeMode == NormalizeMode.Local) {
      for (int y = 0; y < mapSize; y++) {
        for (int x = 0; x < mapSize; x++) {
          noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
        }
      }
    }

    return noiseMap;
  }

}

[System.Serializable]
public class NoiseSettings {
  public Noise.NormalizeMode normalizeMode;
  public float scale = 50;
  public int octaves = 6;
  [Range(0, 1)]
  public float persistance = 0.5f;
  public float lacunarity = 2;
  public int seed;
  public Vector2 offset;

  public void ValidateValues() {
    scale = Mathf.Max(scale, 0.1f);
    octaves = Mathf.Max(octaves, 1);
    lacunarity = Mathf.Max(lacunarity, 1);
    persistance = Mathf.Clamp01(persistance);
  }
}