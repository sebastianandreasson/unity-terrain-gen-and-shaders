using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {

  public enum NormalizeMode { Local, Global }
  public static float[] GenerateNoiseMap(int mapSize, NoiseSettings settings, Vector2 sampleCenter) {
    float[] noiseMap = new float[mapSize * mapSize];

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

        int index = y * mapSize + x;
        noiseMap[index] = noiseHeight;

        if (settings.normalizeMode == NormalizeMode.Global) {
          float normalizedHeight = (noiseMap[index] + 1) / maxPossibleHeight;
          noiseMap[index] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
        }
      }
    }

    if (settings.normalizeMode == NormalizeMode.Local) {
      for (int y = 0; y < mapSize; y++) {
        for (int x = 0; x < mapSize; x++) {
          int index = y * mapSize + x;
          noiseMap[index] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[index]);
        }
      }
    }

    return noiseMap;
  }

  public static bool ShouldPlaceAtPosition(System.Random prng, int x, int y, float weight) {
    float sampleX = prng.Next(-200000, 200000) + x * weight;
    float sampleY = prng.Next(-200000, 200000) + y * weight;
    return Mathf.PerlinNoise(sampleX, sampleY) > weight;
  }

  public static Vector2 ValueForPos(System.Random prng, Vector2 pos) {
    float sampleX = prng.Next(-200000, 200000) + pos.x * 1000;
    float sampleY = prng.Next(-200000, 200000) + pos.y * 1000;
    return new Vector2(Mathf.PerlinNoise(sampleX, sampleY), Mathf.PerlinNoise(sampleX, sampleY));
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