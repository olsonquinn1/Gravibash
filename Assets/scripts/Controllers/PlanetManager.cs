using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;

public class PlanetManager : MonoBehaviour
{

    [SerializeField] public List<PlanetSceneObject> planets;
    [SerializeField] GameObject planetPrefab;
    private PlanetSceneObject[] sc;
    private GameObject[] obj;
    private PlanetController[] ctrl;
    private float[] timing;
    private float[] velNorm;
    private float[] velAngle;
    [SerializeField] private bool editor = false;
    /* private float testX;
    private bool testXRev = false;
    private float testY;
    private bool testYRev = false; */
    
    // Start is called before the first frame update
    void Start()
    {
        //create a planet prefab for each planet
        ctrl = new PlanetController[planets.Count];
        sc = new PlanetSceneObject[planets.Count];
        obj = new GameObject[planets.Count];
        timing = new float[planets.Count];
        velNorm = new float[planets.Count];
        velAngle = new float[planets.Count];
        int id = 0;
        foreach(PlanetSceneObject p in planets) {
            GameObject pObj = Instantiate(
                planetPrefab,
                p.origin,
                transform.rotation 
            );
            PlanetController newPlanetController = pObj.GetComponent<PlanetController>();
            newPlanetController.planet = p.planet;
            newPlanetController.id = id;
            obj[id] = pObj;
            sc[id] = p;
            ctrl[id] = pObj.GetComponent<PlanetController>();
            timing[id] = 0;
            if(!p.isStatic) {
                timing[id] = (p.offset) * p.period;
                obj[id].GetComponent<Rigidbody2D>().isKinematic = false;

                pObj.transform.position = new Vector3(
                    p.origin.x + p.majorAxis * Cos((p.reverse ? -1 : 1) * PI * 2 * (p.offset * p.period)),
                    p.origin.y + p.minorAxis * Sin((p.reverse ? -1 : 1) * PI * 2 * (p.offset * p.period)),
                    0
                );
                //testX = pObj.transform.position.x;
                //testY = pObj.transform.position.y;
            }

            velNorm[id] = sc[id].majorAxis * 2.0f * PI / sc[id].period;
            
            velAngle[id] = PI * 2.0f / sc[id].period;

            id++;
            newPlanetController.Init();
        }
        if(!editor) GameObject.Find("HUD").GetComponent<HudController>().initPlanets(obj);
    }

    public Vector2 gravVectorSum(float x, float y, float m) {
        Vector2 sum = new Vector2(0, 0);
        for(int i = 0; i < planets.Count; i++) if(ctrl[i] != null)
            sum += ctrl[i].gravVector(x, y, m);
        return sum;
    }

    Vector2 getVel(int planetId) {
        return obj[planetId].GetComponent<Rigidbody2D>().velocity;
    }

    public Vector3 getSpawnLocation() {
        return ctrl[UnityEngine.Random.Range(0, ctrl.Length)].getSpawnLocation();
    }
    void FixedUpdate() {
        for(int i = 0; i < planets.Count; i++) {
            if(!sc[i].isStatic) {
                Rigidbody2D rb = obj[i].GetComponent<Rigidbody2D>();
                Quaternion rot = Quaternion.AngleAxis(sc[i].rotation, Vector3.forward);
                float velMagX = -1.0f * Sin( velAngle[i] * timing[i] );
                float velMagY = Cos( velAngle[i] * timing[i] ) ;
                rb.velocity = rot * new Vector3(
                     velNorm[i] * velMagX,
                     velNorm[i] * velMagY
                );
                Vector2 posOffset = rot * new Vector3(
                    sc[i].origin.x + sc[i].majorAxis * velMagX - obj[i].transform.position.x,
                    sc[i].origin.y + sc[i].minorAxis * velMagY - obj[i].transform.position.y,
                    0
                );
                rb.velocity += posOffset;
                timing[i] += Time.fixedDeltaTime;
            }
        }
    }
    /* if(!testXRev) {
        if(obj[i].transform.position.x < testX) testX = obj[i].transform.position.x;
        else {
            Debug.Log("minX: " + testX);
            testXRev = true;
        }
    } else {
        if(obj[i].transform.position.x > testX) testX = obj[i].transform.position.x;
        else {
            Debug.Log("maxX: " + testX);
            testXRev = false;
        }
    }
    if(!testYRev) {
        if(obj[i].transform.position.y < testY) testY = obj[i].transform.position.y;
        else {
            Debug.Log("minY: " + testY);
            testYRev = true;
        }
    } else {
        if(obj[i].transform.position.y > testY) testY = obj[i].transform.position.y;
        else {
            Debug.Log("maxY: " + testY);
            testYRev = false;
        }
    } */
    
    //Debug.DrawLine(obj[i].transform.position, obj[i].transform.position + Time.fixedDeltaTime * new Vector3(rb.velocity.x, rb.velocity.y), Color.green, 60);

    public void updateAllPlanetGen() {
        for(int i = 0; i < planets.Count; i++) {
            ctrl[i].updatePlanet();
        }
    }

    public float atmoDensity(float x, float y) {
        float DensitySum = 0;
        for(int i = 0; i < planets.Count; i++) if(ctrl[i] != null)
            DensitySum += ctrl[i].LocalDensity(x, y);
        return DensitySum;
    }
}
