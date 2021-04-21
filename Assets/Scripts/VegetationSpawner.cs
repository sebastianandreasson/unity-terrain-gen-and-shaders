using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationSpawner : MonoBehaviour {
  public GameObject[] treePrefabs;
  public GameObject grassPrefab;
  public NoiseSettings noiseSettings;
  private GameObject vegetation;

  GameObject GetTree(float n) {
    if (n % 7 == 0 || n % 6 == 0) {
      return treePrefabs[3];
    }
    if (n % 5 == 0 || n % 4 == 0) {
      return treePrefabs[2];
    }
    if (n % 3 == 0 || n % 2 == 0) {
      return treePrefabs[1];
    }
    return treePrefabs[0];
  }
  void SpawnTree(System.Random randomSeed, Transform parent, Vector3 offset, Vector2 pos) {
    GameObject tree = Instantiate(GetTree(pos.x));
    tree.transform.parent = parent;
    tree.isStatic = true;
    tree.name = "Tree - x: " + pos.x + ", y: " + pos.y;
    Vector3 origin = new Vector3(offset.x + pos.x * 2, 50, offset.y + pos.y * 2);
    tree.transform.localPosition = origin;

    Ray ray = new Ray(tree.transform.position, Vector3.down);
    if (Physics.Raycast(ray, out RaycastHit info, 100, 1 << LayerMask.NameToLayer("Terrain"))) {
      tree.transform.position = new Vector3(tree.transform.position.x, info.point.y, tree.transform.position.z);
      tree.transform.localRotation = Quaternion.Euler(info.normal.x, info.normal.y, info.normal.z);
      tree.transform.localPosition += new Vector3(Random.Range(-.5f, .5f), 0, Random.Range(-.5f, .5f));
    }
  }

  GameObject SpawnGrass(System.Random randomSeed, Transform parent, Vector3 parentOrigin, Vector2 pos) {
    GameObject grassPlot = new GameObject("Grass - x: " + pos.x + ", y: " + pos.y);
    grassPlot.transform.parent = parent;
    GameObject grass = Instantiate(grassPrefab);
    grass.transform.parent = grassPlot.transform;
    Vector3 origin = new Vector3(parentOrigin.x + pos.x * 2, 50, parentOrigin.y + pos.y * 2);

    grass.transform.localPosition = origin;

    Ray ray = new Ray(origin, Vector3.down);
    if (Physics.Raycast(ray, out RaycastHit info, 100, 1 << LayerMask.NameToLayer("Terrain"))) {
      grass.transform.position = new Vector3(grass.transform.position.x, info.point.y, grass.transform.position.z);
      grass.transform.localRotation = Quaternion.Euler(-90 + info.normal.x, info.normal.y, info.normal.z);
      grass.transform.localScale = new Vector3(1, 1, 5);
    }

    return grassPlot;
  }

  public void Spawn(Transform parent, float[,] heightMap) {
    int size = heightMap.GetLength(0) - 10;
    Vector2 origin = new Vector2(-size, -size);

    vegetation = new GameObject("Vegetation");
    vegetation.transform.parent = parent.transform;
    vegetation.transform.localPosition = Vector3.zero;
    GameObject trees = new GameObject("Trees");
    trees.transform.parent = vegetation.transform;
    trees.transform.localPosition = Vector3.zero;
    trees.transform.localScale = Vector3.one;

    GameObject grass = new GameObject("Grass");
    grass.transform.parent = vegetation.transform;
    grass.transform.localPosition = Vector3.zero;
    grass.transform.localScale = Vector3.one;

    float[] treeNoise = Noise.GenerateNoiseMap(size, noiseSettings, Vector2.zero);
    System.Random randomSeed = new System.Random(noiseSettings.seed);

    for (int x = 1; x < size - 1; x++) {
      for (int y = 1; y < size - 1; y++) {
        int index = x * size + y;
        if (treeNoise[index] > 0.5 && heightMap[x, y] > 2 && heightMap[x, y] < 6 && Noise.ShouldPlaceAtPosition(randomSeed, x, y, 0.56f)) {
          SpawnTree(randomSeed, trees.transform, origin, new Vector2(x, (size - 1) - y));
        }
        // if (heightMap[x, y] > 1.25 && heightMap[x, y] < 4 && Noise.ShouldPlaceAtPosition(randomSeed, x, y, 0.95f)) {
        //   GameObject g = SpawnGrass(randomSeed, grass.transform, origin, new Vector2(x, (size - 1) - y));
        //   g.transform.parent = grass.transform;
        // }
      }
    }
  }
}
