using UnityEngine;

public class HeightMapGenerator {
  static public HeightMap HeightMapForValues(float[] values, int size) {
    float minValue = float.MaxValue;
    float maxValue = float.MinValue;
    float[,] newMap = new float[size, size];
    for (int i = 0; i < values.Length; i++) {
      int x = i % size;
      int y = i / size;
      if (values[i] > maxValue) {
        maxValue = values[i];
      }
      if (values[i] < minValue) {
        minValue = values[i];
      }
      newMap[x, y] = values[i];
    }
    return new HeightMap(newMap, minValue, maxValue);
  }

  static public float[] GenerateHeightMap(int mapSize, ErosionSettings erosionSettings, HeightMapSettings settings, Vector2 sampleCenter) {
    int mapSizeWithBorder = mapSize + erosionSettings.brushRadius * 2;

    float[] values = Noise.GenerateNoiseMap(mapSizeWithBorder, settings.noiseSettings, sampleCenter);
    return values;
  }

  static public float[] ApplyErosionAndHeightMultiplier(float[] values, int mapSize, Erosion erosion, ErosionSettings erosionSettings, HeightMapSettings settings) {
    float[] erodedValues = erosion.Erode(values, mapSize, erosionSettings);
    AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);
    for (int i = 0; i < erodedValues.Length; i++) {
      erodedValues[i] *= heightCurve_threadsafe.Evaluate(erodedValues[i]) * settings.heightMultiplier;
    }
    return erodedValues;
  }
}

public struct HeightMap {
  public readonly float[,] values;
  public readonly float minValue;
  public readonly float maxValue;

  public float[] values1d {
    get {
      int size = values.GetLength(0);
      float[] vls = new float[size * size];
      for (int y = 0; y < size - 1; y++) {
        for (int x = 0; x < size - 1; x++) {
          vls[x + (size - 1) * y] = values[x, y];
        }
      }
      return vls;
    }
  }

  public HeightMap(float[,] values, float minValue, float maxValue) {
    this.values = values;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }
}
