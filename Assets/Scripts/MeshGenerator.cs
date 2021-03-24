using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

  public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int LOD) {
    AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
    int size = heightMap.GetLength(0);
    float topLeftX = size - 1 / -2f;
    float topLeftZ = size - 1 / 2f;

    int meshSimplificationIncrement = (LOD == 0) ? 1 : LOD * 2;
    int verticesPerLine = (size - 1) / meshSimplificationIncrement + 1;

    MeshData meshData = new MeshData(verticesPerLine);
    int vertexIndex = 0;

    for (int y = 0; y < size; y += meshSimplificationIncrement) {
      for (int x = 0; x < size; x += meshSimplificationIncrement) {
        meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
        meshData.uvs[vertexIndex] = new Vector2(x / (float)size, y / (float)size);

        if (x < size - 1 && y < size - 1) {
          meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
          meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
        }
        vertexIndex++;
      }
    }

    return meshData;
  }
}

public class MeshData {
  public Vector3[] vertices;
  public int[] triangles;
  public Vector2[] uvs;

  int triangleIndex;

  public MeshData(int meshSize) {
    vertices = new Vector3[meshSize * meshSize];
    uvs = new Vector2[meshSize * meshSize];
    triangles = new int[(meshSize - 1) * (meshSize - 1) * 6];
    triangleIndex = 0;
  }

  public void AddTriangle(int a, int b, int c) {
    triangles[triangleIndex] = a;
    triangles[triangleIndex + 1] = b;
    triangles[triangleIndex + 2] = c;
    triangleIndex += 3;
  }

  public Mesh CreateMesh() {
    Mesh mesh = new Mesh();
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.uv = uvs;
    mesh.RecalculateNormals();
    return mesh;
  }
}