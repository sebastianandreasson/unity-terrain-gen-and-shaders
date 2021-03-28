using UnityEngine;

public static class HeightMapGenerator {
  public static HeightMap GenerateHeightMap(int size, HeightMapSettings settings, Vector2 sampleCenter) {
    float[,] values = Noise.GenerateNoiseMap(size, settings.noiseSettings, sampleCenter);
    AnimationCurve heightCurve_threadsafe = new AnimationCurve(settings.heightCurve.keys);

    float minValue = float.MaxValue;
    float maxValue = float.MinValue;
    for (int i = 0; i < size; i++) {
      for (int j = 0; j < size; j++) {
        values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

        if (values[i, j] > maxValue) {
          maxValue = values[i, j];
        }
        if (values[i, j] < minValue) {
          minValue = values[i, j];
        }
      }
    }

    return new HeightMap(values, minValue, maxValue);
  }

}

public struct HeightMap {
  public readonly float[,] values;
  public readonly float minValue;
  public readonly float maxValue;

  public HeightMap(float[,] values, float minValue, float maxValue) {
    this.values = values;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }
}
// public class HeightMapGenerator : MonoBehaviour {
//   public ComputeShader heightMapComputeShader;

//   public float[] GenerateHeightMap(int mapSize, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset) {
//     var prng = new System.Random(seed);

//     Vector2[] offsets = new Vector2[octaves];
//     for (int i = 0; i < octaves; i++) {
//       offsets[i] = new Vector2(prng.Next(-10000, 10000) + offset.x, prng.Next(-10000, 10000) + offset.y);
//     }
//     ComputeBuffer offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 2);
//     offsetsBuffer.SetData(offsets);
//     heightMapComputeShader.SetBuffer(0, "offsets", offsetsBuffer);

//     int floatToIntMultiplier = 1000;
//     float[] map = new float[mapSize * mapSize];

//     ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(int));
//     mapBuffer.SetData(map);
//     heightMapComputeShader.SetBuffer(0, "heightMap", mapBuffer);

//     int[] minMaxHeight = { floatToIntMultiplier * octaves, 0 };
//     ComputeBuffer minMaxBuffer = new ComputeBuffer(minMaxHeight.Length, sizeof(int));
//     minMaxBuffer.SetData(minMaxHeight);
//     heightMapComputeShader.SetBuffer(0, "minMax", minMaxBuffer);

//     heightMapComputeShader.SetInt("mapSize", mapSize);
//     heightMapComputeShader.SetInt("octaves", octaves);
//     heightMapComputeShader.SetFloat("lacunarity", lacunarity);
//     heightMapComputeShader.SetFloat("persistence", persistence);
//     heightMapComputeShader.SetFloat("scaleFactor", scale);
//     heightMapComputeShader.SetInt("floatToIntMultiplier", floatToIntMultiplier);

//     heightMapComputeShader.Dispatch(0, Mathf.CeilToInt(map.Length / 64f), 1, 1);

//     mapBuffer.GetData(map);
//     minMaxBuffer.GetData(minMaxHeight);
//     mapBuffer.Release();
//     minMaxBuffer.Release();
//     offsetsBuffer.Release();

//     float minValue = (float)minMaxHeight[0] / (float)floatToIntMultiplier;
//     float maxValue = (float)minMaxHeight[1] / (float)floatToIntMultiplier;

//     for (int i = 0; i < map.Length; i++) {
//       map[i] = Mathf.InverseLerp(minValue, maxValue, map[i]);
//     }

//     return map;
//   }
// }