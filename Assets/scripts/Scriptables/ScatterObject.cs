using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScatterObject", menuName = "Scatter/New ScatterObject")]
public class ScatterObject : ScriptableObject {
    [SerializeField] public float weight = 1;
    [HideInInspector] public float totalWeight;
    [SerializeField] public GameObject prefab;
    [SerializeField] public float scaleOffset = 1;
}
