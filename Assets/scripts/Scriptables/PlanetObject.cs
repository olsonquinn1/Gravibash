using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Planet", menuName = "Planet/New Planet")]
public class PlanetObject : ScriptableObject
{
    [Header("Mesh")]
    [SerializeField] Material planetMaterial;
    [SerializeField] public float radius = 20;
    [SerializeField][Min(3)] public int segments = 200;
    [SerializeField] public int textureTiers = 3;
    [SerializeField][Min(2)] public int uvWidth = 3;
    [SerializeField] public bool uvMirror = true;
    [SerializeField] public float heightPerLevel = 1;
    [SerializeField] public int surfaceLevel = 1;

    [Header("Lighting")]
    [SerializeField] public GameObject planetLighting;

    [Header("Terrain Generation")]
    [Min(0)] [SerializeField] public float smoothness = 35.6f;
    [Range(0, 10)][SerializeField] public float minSmoothPercent = 9.9896f;
    [Min(0.001f)][SerializeField] public float smoothOffset = 10;

    [Min(0)][SerializeField] public float scale = 4;
    [Range(0, 10)][SerializeField] public float minScalePercent = 8;
    [Min(0.001f)][SerializeField] public float scaleOffset = 1;

    [SerializeField] public float seed = 50;

    [Header("Surface Object Generation")]
    [SerializeField] public bool enableScatter = true;
    [SerializeField] public List<ScatterGroup> scatterGroups;
    [SerializeField] public float patchScale = 1;
    [SerializeField] public float treeDensity = 0;
    [SerializeField] public float probabilityShift = 0;
    [SerializeField] public float TreeScaleMin = 0.2f;
    [SerializeField] public float TreeScaleMax = 0.4f;
    [SerializeField] public float TreeHeightOffset = 0;


    [Header("Physics")]
    [SerializeField] public float mass = 5000000000;
    [SerializeField] public float gravityFalloff = 1.2f; //how distance affects gravity falloff: 0 = not at all, 2 = realistic
}
