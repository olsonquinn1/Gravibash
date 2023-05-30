using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScatterGroup", menuName = "Scatter/New ScatterGroup")]
public class ScatterGroup : ScriptableObject {
    [SerializeField] public List<ScatterObject> items;

    public float getTotalWeight() {
        float sum = 0;
        foreach(ScatterObject i in items)
            sum += i.weight;
        return sum;
    }
}
