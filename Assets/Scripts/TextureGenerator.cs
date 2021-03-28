using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {
  public static Texture2D TextureFromColorMap(Color[] colorMap, int size) {
    Texture2D texture = new Texture2D(size, size);
    texture.filterMode = FilterMode.Point;
    texture.wrapMode = TextureWrapMode.Clamp;
    texture.SetPixels(colorMap);
    texture.Apply();
    return texture;
  }

  public static Texture2D TextureFromHeightMap(HeightMap heightMap) {
    int size = heightMap.values.GetLength(0);

    Color[] colorMap = new Color[size * size];
    for (int y = 0; y < size; y++) {
      for (int x = 0; x < size; x++) {
        int index = (y * size) + x;
        colorMap[index] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y]));
      }
    }

    return TextureFromColorMap(colorMap, size);
  }
}
