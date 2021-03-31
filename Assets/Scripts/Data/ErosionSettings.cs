using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ErosionSettings : UpdatableData {
  public int numIterations = 50000;
  public int brushRadius = 3;

  public int maxLifetime = 30;
  public float sedimentCapacityFactor = 3;
  public float minSedimentCapacity = .01f;
  public float depositSpeed = 0.3f;
  public float erodeSpeed = 0.3f;

  public float evaporateSpeed = .01f;
  public float gravity = 4;
  public float startSpeed = 1;
  public float startWater = 1;
  [Range(0, 1)]
  public float inertia = 0.3f;
}
