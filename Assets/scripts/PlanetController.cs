using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static UnityEngine.Mathf;
using UnityEngine.Rendering.Universal;

public class PlanetController : MonoBehaviour
{
    [Header("Mesh")]
    [SerializeField] float radius;
    [SerializeField][Min(3)] int segments;
    [SerializeField] int textureTiers;
    [SerializeField][Min(2)] int uvWidth;
    [SerializeField] bool uvMirror;
    [SerializeField] float heightPerLevel;
    [SerializeField] int surfaceLevel;

    [Header("Lighting")]
    [SerializeField] GameObject planetLighting;
    [SerializeField] float sunAngle;
    private Light2D sunlight;
    private Light2D dayGlow;
    private Light2D nightGlow;

    [Header("Terrain Generation")]
    [Min(0)] [SerializeField] float smoothness;
    [Range(0, 10)][SerializeField] float minSmoothPercent;
    [Min(0.001f)][SerializeField] float smoothOffset;

    [Min(0)][SerializeField] float scale;
    [Range(0, 10)][SerializeField] float minScalePercent;
    [Min(0.001f)][SerializeField] float scaleOffset;

    [SerializeField] float seed;

    [Header("Surface Object Generation")]
    [SerializeField] bool enableScatter = true;
    [SerializeField] List<ScatterGroup> scatterGroups;
    [SerializeField] float patchScale = 15;
    [SerializeField] float treeDensity = 4;
    [SerializeField] float probabilityShift = 1;
    [SerializeField] float TreeScaleMin = 0.2f;
    [SerializeField] float TreeScaleMax = 0.4f;
    [SerializeField] float TreeHeightOffset = 0;


    [Header("Physics")]
    [SerializeField] float mass;
    [SerializeField] float gravityFalloff; //how distance affects gravity falloff: 0 = not at all, 2 = realistic

    [Header("Multiplayer")]
    [SerializeField] GameObject spawnLocationObj;
    private List<GameObject> spawnLocations;
    private GameObject ScatterBase;

    private float G = 6.67f * Pow(10, -6); // 6.67 x 10^-11 (adjusted to make lower mass values work better)
    private System.Random rand;
    
    //returns gravity acceleration vector for given object position and mass from this planet
    public Vector2 gravVector(float x1, float y1, float m1) {
        //F = G * m1 * m2 / r^2
        //F = ma
        float distance = Sqrt(
            Pow(transform.position.x - x1, 2) + Pow(transform.position.y - y1, 2)
        );
        float force = G * m1 * mass / Pow(distance, gravityFalloff);
        float accel = force / m1;
        float angle =  Atan2(transform.position.y - y1, transform.position.x - x1);

        return new Vector2(
            accel * Cos(angle),
            accel * Sin(angle)
        );
    }
    public void updatePlanet() {
        MeshFilter mf = transform.GetComponentInChildren<MeshFilter>();
        PolygonCollider2D pc = GetComponent<PolygonCollider2D>();
        generatePlanetMesh(ref mf, ref pc);
    }

    private float randomFloat() {
        return (float) UnityEngine.Random.Range(0, 1000000000) / 1000000000.0f;
    }

    void updateLighting() {
        planetLighting.transform.rotation = Quaternion.Euler(0, 0, sunAngle);
    }

    void generateSurfaceScatter(List<Vector2> colliderVertices) {
        UnityEngine.Random.InitState((int) Floor(seed));
        
        foreach(Transform child in ScatterBase.transform)
            GameObject.Destroy(child.gameObject);

        int vertCount = colliderVertices.Count;

        int vertIndex = 0;
        float seedLocal = (float)((seed + .2313));
        
        for(vertIndex = 0; vertIndex < vertCount; vertIndex++)
        {
            float location = vertIndex/(patchScale);
            float PerlinVal = PerlinNoise(location, seedLocal);
            float probability  = PerlinVal * treeDensity - treeDensity/2;
            probability = 1/(1+(float)Pow(2.72f,probability+probabilityShift));

            float Rplacement = randomFloat();

            if((probability>Rplacement)||false) {
                Vector2 pos = colliderVertices[vertIndex];
                Vector2 gravVec = gravVector(pos.x, pos.y, 10);
                float angle = Atan2(gravVec.y,gravVec.x)*180/Mathf.PI;
                float acceleration = gravVec.magnitude;
                gravVec.Normalize();
                float Rfloat = randomFloat();
                Vector3 treeScale = new Vector3(TreeScaleMin+Rfloat*(TreeScaleMax-TreeScaleMin),TreeScaleMin+Rfloat*(TreeScaleMax-TreeScaleMin),0);
                pos = TreeHeightOffset*treeScale.x*gravVec+pos;

                Vector3 pos3 = new Vector3(pos.x,pos.y,10);

                GameObject TempObj = Instantiate( //make tree
                    chooseScatterFromGroup(0),
                    pos3,
                    Quaternion.Euler(new Vector3(0, 0, angle+90)),
                    ScatterBase.transform
                );
                TempObj.transform.localScale = treeScale;
            }
        }
    }

    GameObject chooseScatterFromGroup(int groupIndex) {
        int index = UnityEngine.Random.Range(0, scatterGroups[groupIndex].items.Count);
        return scatterGroups[groupIndex].items[index].prefab;
    }

    void generatePlanetMesh(ref MeshFilter mf, ref PolygonCollider2D pc) {
        
        int trianglesPerSegment = textureTiers * 2;
        float half = 0.5f / segments;
        int uvLevels = textureTiers + 1;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector2> colliderVertices = new List<Vector2>();
        List<int> triangles = new List<int>();
        Mesh mesh = new Mesh();

        int uvOffset = segments % uvWidth;
        if(uvOffset != 0) segments += uvWidth - uvOffset;

        float perlinSmoothSum = 0;
        float[] perlinSmooth = new float[segments];
        for(int i = 0; i < segments; i++) {
            float angle = PI * 2 * ((float) i / segments);
            perlinSmooth[i] = 
                PerlinNoise(
                    (seed + 1 + Cos(angle)) / (smoothOffset / 1000),
                    (seed + 1 + Sin(angle)) / (smoothOffset / 1000)
                ) * ((1 - (minSmoothPercent / 10))) + ((minSmoothPercent / 10));
            perlinSmoothSum += 1/perlinSmooth[i];
        }

        float[] perlinScale = new float[segments];
        for(int i = 0; i < segments; i++) {
            float angle = PI * 2 * ((float) i / segments);
            perlinScale[i] = 
                PerlinNoise(
                    (seed + 2 + Cos(angle)) / scaleOffset,
                    (seed + 2 + Sin(angle)) / scaleOffset
                ) * (scale * (1 - (minScalePercent / 10))) + (scale * (minScalePercent / 10));
        }

        float angleIntegral = 0;
        float angleScaler = PI * 2 / perlinSmoothSum;
        float[] perlinHeights = new float[segments];
        for(int i = 0; i < segments; i++) {
            perlinHeights[i] = 
                PerlinNoise(
                    (seed + Cos(angleIntegral)) * 100 / smoothness,
                    (seed + Sin(angleIntegral)) * 100 / smoothness
                ) * perlinScale[i];
            angleIntegral += angleScaler/perlinSmooth[i];
        }

        Vector2[] uvVals = new Vector2[uvLevels * uvWidth];
        for(int i = 0; i < uvLevels; i++) {
            for(int j = 0; j < uvWidth; j++) {
                uvVals[uvWidth * i + j] = new Vector2(
                    (float) j / (uvWidth - 1),
                    1.0f - ((float) i / textureTiers)
                );
            }
        }

        //generate vertices and corresponding uv
        int uvIndex = 0;
        bool uvIndexReverse = false;
        for(int i = 0; i < segments; i++) {

            for(int j = 0; j < uvLevels; j++) {

                float angle;
                if(j == 0)
                    angle = PI * 2 * ((float) i / segments);
                else
                    angle = PI * 2 * (((float) i / segments) + (half / Pow(2, j - 1)));

                Vector3 point = new Vector3(
                    (radius - (j * heightPerLevel) + perlinHeights[i]) * Cos(angle),
                    (radius - (j * heightPerLevel) + perlinHeights[i]) * Sin(angle),
                    0
                );

                vertices.Add(point);

                uv.Add(uvVals[j * uvWidth + uvIndex]);
                
                if(j == surfaceLevel)
                    colliderVertices.Add(point);
            }
            if(uvIndexReverse) {
                if(uvIndex == 0) {
                    uvIndexReverse = false;
                    uvIndex++;
                } else uvIndex--;
            }  
            else {
                if(uvIndex == uvWidth - 1) {
                    if(uvMirror) {
                        uvIndexReverse = true;
                        uvIndex--;
                    } else uvIndex = 0;
                    
                } else uvIndex++;
            }
        }
        //origin vertex
        vertices.Add(new Vector3(0, 0, 0));
        uv.Add(new Vector2(0, 0));

        //generate triangles
        int vertCount = segments * uvLevels;
        for(int i = 0; i < segments; i++) {
            int index = i * uvLevels;
            //front cap
            triangles.Add((index) % vertCount);
            triangles.Add((index + 1) % vertCount);
            triangles.Add((index + uvLevels) % vertCount);

            //middle
            for(int j = 0; j < textureTiers - 1; j++) {
                int[] vals = new int[] {
                    (i * uvLevels + j + 1) % vertCount, (i * uvLevels + j + 2) % vertCount,
                    ((i + 1) * uvLevels + j) % vertCount, ((i + 1) * uvLevels + j + 1) % vertCount
                };
                //upper
                triangles.Add(vals[0]);
                triangles.Add(vals[1]);
                triangles.Add(vals[2]);
                //lower
                triangles.Add(vals[1]);
                triangles.Add(vals[3]);
                triangles.Add(vals[2]);
            }

            //end cap
            triangles.Add((index + textureTiers) % vertCount);
            triangles.Add((index + uvLevels + textureTiers) % vertCount);
            triangles.Add((index + uvLevels + textureTiers - 1) % vertCount);
            
            //fill center
            triangles.Add(vertCount);
            triangles.Add((index + uvLevels + textureTiers) % vertCount);
            triangles.Add((index + textureTiers) % vertCount);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mf.mesh = mesh;
        pc.SetPath(0, colliderVertices.ToArray());

        if(enableScatter) generateSurfaceScatter(colliderVertices);
    }

    public Vector3 getSpawnLocation() {
        int index = UnityEngine.Random.Range(0, spawnLocations.Count);
        return spawnLocations[index].transform.position;
    }

    void Start()
    {
        UnityEngine.Random.InitState((int) Floor(seed));

        ScatterBase = new GameObject("ScatterBase");
        ScatterBase.transform.SetParent(transform);

        //generate the planet's mesh / scatter
        updatePlanet();
        
        //generate player spawn locations
        spawnLocations = new List<GameObject>();
        for(int i = 0; i < 4; i++) {
            Vector3 pos = (radius + 2.5f + scale) * new Vector3(
                Cos(PI * 2 * ((float) i / 4)), Sin(PI * 2 * ((float) i / 4)), 0
            );
            spawnLocations.Add(Instantiate(
                spawnLocationObj,
                pos,
                transform.rotation
            ));
        }

        //create lighting
        Instantiate(planetLighting, transform);
        sunlight = planetLighting.transform.GetChild(0).GetComponent<Light2D>();
        dayGlow = planetLighting.transform.GetChild(1).GetComponent<Light2D>();
        nightGlow = planetLighting.transform.GetChild(2).GetComponent<Light2D>();
    }

    void Update() {
        //regenerate planet
        if(Input.GetKeyDown(KeyCode.G)) {
            updatePlanet();
        }
        
        updateLighting();
    }
}