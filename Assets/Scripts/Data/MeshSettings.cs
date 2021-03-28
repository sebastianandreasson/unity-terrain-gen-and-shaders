using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]
public class MeshSettings : UpdatableData {
  public float meshScale = 2.5f;
  public const int numSupportedLODs = 5;
  public const int numSupportedChunkSizes = 9;
  public static readonly int[] supportedChunkSizes = {
    48,72,96,120,144,168,192,216,240
  };

  [Range(0, numSupportedChunkSizes - 1)]
  public int chunkSizeIndex;

  public int numVertsPerLine {
    get {
      return supportedChunkSizes[chunkSizeIndex] + 5;
    }
  }

  public float meshWorldSize {
    get {
      return (numVertsPerLine - 3) * meshScale;
    }
  }
}
