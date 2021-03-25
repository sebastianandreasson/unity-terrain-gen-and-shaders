using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

  public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int LOD) {
    AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

    int meshSimplificationIncrement = (LOD == 0) ? 1 : LOD * 2;

    int size = heightMap.GetLength(0);
    int meshSize = size - 2 * meshSimplificationIncrement;
    int meshSizeWithoutLOD = size - 2;

    float topLeftX = (meshSizeWithoutLOD - 1) / -2f;
    float topLeftZ = (meshSizeWithoutLOD - 1) / 2f;

    int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

    MeshData meshData = new MeshData(verticesPerLine);
    int[,] vertexIndicesMap = new int[size, size];
    int meshVertexIndex = 0;
    int borderVertexIndex = -1;

    for (int y = 0; y < size; y += meshSimplificationIncrement) {
      for (int x = 0; x < size; x += meshSimplificationIncrement) {
        bool isBorderVertex = y == 0 || y == size - 1 || x == 0 || x == size - 1;

        if (isBorderVertex) {
          vertexIndicesMap[x, y] = borderVertexIndex;
          borderVertexIndex--;
        } else {
          vertexIndicesMap[x, y] = meshVertexIndex;
          meshVertexIndex++;
        }
      }
    }

    for (int y = 0; y < size; y += meshSimplificationIncrement) {
      for (int x = 0; x < size; x += meshSimplificationIncrement) {
        int vertexIndex = vertexIndicesMap[x, y];
        Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
        float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
        Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeWithoutLOD, height, topLeftZ - percent.y * meshSizeWithoutLOD);

        meshData.AddVertex(vertexPosition, percent, vertexIndex);

        if (x < size - 1 && y < size - 1) {
          int a = vertexIndicesMap[x, y];
          int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
          int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
          int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
          meshData.AddTriangle(a, d, c);
          meshData.AddTriangle(d, a, b);
        }
        vertexIndex++;
      }
    }

    meshData.BakeNormals();

    return meshData;
  }
}

public class MeshData {
  Vector3[] vertices;
  int[] triangles;
  Vector2[] uvs;
  Vector3[] bakedNormals;

  Vector3[] borderVertices;
  int[] borderTriangles;

  int triangleIndex;
  int borderTriangleIndex;

  public MeshData(int meshSize) {
    vertices = new Vector3[meshSize * meshSize];
    uvs = new Vector2[meshSize * meshSize];
    triangles = new int[(meshSize - 1) * (meshSize - 1) * 6];
    triangleIndex = 0;

    borderVertices = new Vector3[meshSize * 4 + 4];
    borderTriangles = new int[24 * meshSize];
  }

  public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
    if (vertexIndex < 0) {
      borderVertices[-vertexIndex - 1] = vertexPosition;
    } else {
      vertices[vertexIndex] = vertexPosition;
      uvs[vertexIndex] = uv;
    }
  }

  public void AddTriangle(int a, int b, int c) {
    if (a < 0 || b < 0 || c < 0) {
      borderTriangles[borderTriangleIndex] = a;
      borderTriangles[borderTriangleIndex + 1] = b;
      borderTriangles[borderTriangleIndex + 2] = c;
      borderTriangleIndex += 3;
    } else {
      triangles[triangleIndex] = a;
      triangles[triangleIndex + 1] = b;
      triangles[triangleIndex + 2] = c;
      triangleIndex += 3;
    }
  }

  Vector3[] CalculateNormals() {
    Vector3[] vertexNormals = new Vector3[vertices.Length];
    int triangleCount = triangles.Length / 3;
    for (int i = 0; i < triangleCount; i++) {
      int normalTriangleIndex = i * 3;
      int vertexIndexA = triangles[normalTriangleIndex];
      int vertexIndexB = triangles[normalTriangleIndex + 1];
      int vertexIndexC = triangles[normalTriangleIndex + 2];

      Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
      vertexNormals[vertexIndexA] += triangleNormal;
      vertexNormals[vertexIndexB] += triangleNormal;
      vertexNormals[vertexIndexC] += triangleNormal;
    }


    int borderTriangleCount = borderTriangles.Length / 3;
    for (int i = 0; i < borderTriangleCount; i++) {
      int normalTriangleIndex = i * 3;
      int vertexIndexA = borderTriangles[normalTriangleIndex];
      int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
      int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

      Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
      if (vertexIndexA >= 0) vertexNormals[vertexIndexA] += triangleNormal;
      if (vertexIndexB >= 0) vertexNormals[vertexIndexB] += triangleNormal;
      if (vertexIndexC >= 0) vertexNormals[vertexIndexC] += triangleNormal;
    }

    for (int i = 0; i < vertexNormals.Length; i++) {
      vertexNormals[i].Normalize();
    }

    return vertexNormals;
  }

  Vector3 SurfaceNormalFromIndices(int a, int b, int c) {
    Vector3 pointA = (a < 0 ? borderVertices[-a - 1] : vertices[a]);
    Vector3 pointB = (b < 0 ? borderVertices[-b - 1] : vertices[b]);
    Vector3 pointC = (c < 0 ? borderVertices[-c - 1] : vertices[c]);

    Vector3 sideAB = pointB - pointA;
    Vector3 sideAC = pointC - pointA;
    return Vector3.Cross(sideAB, sideAC).normalized;
  }

  public void BakeNormals() {
    bakedNormals = CalculateNormals();
  }

  public Mesh CreateMesh() {
    Mesh mesh = new Mesh();
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.uv = uvs;
    mesh.normals = bakedNormals;
    return mesh;
  }
}