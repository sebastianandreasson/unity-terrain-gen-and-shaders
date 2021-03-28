using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData {
  const int textureSize = 512;
  const TextureFormat textureFormat = TextureFormat.RGB565;
  public Layer[] layers;
  float savedMinHeight;
  float savedMaxHeight;
  public void ApplyToMaterial(Material material) {

    material.SetInt("layerCount", layers.Length);
    material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
    material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
    material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
    material.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
    material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
  }

  [System.Serializable]
  public class Layer {
    public Texture2D texture;
    public Color tint;
    [Range(0, 1)]
    public float tintStrength;
    [Range(0, 1)]
    public float startHeight;
    [Range(0, 1)]
    public float blendStrength;
    public float textureScale;
  }
}
