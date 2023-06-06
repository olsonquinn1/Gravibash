using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public class PlanetManager : MonoBehaviour
{

    [SerializeField] public List<PlanetSceneObject> planets;
    [SerializeField] GameObject planetPrefab;
    private PlanetSceneObject[] sc;
    private GameObject[] obj;
    private PlanetController[] ctrl;
    private float[] timing;
    private float[] perim;
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
        perim = new float[planets.Count];
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

            float h = Pow(p.majorAxis - p.minorAxis, 2) / Pow(p.majorAxis + p.minorAxis, 2);
            perim[id] = PI * (p.majorAxis + p.minorAxis) * (1 + (3 * h) / (10 + Sqrt(4 - 3 * h)));

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

    public Vector3 getSpawnLocation() {
        return ctrl[0].getSpawnLocation();
    }
    void FixedUpdate() {
        for(int i = 0; i < planets.Count; i++) {
            if(!sc[i].isStatic) {
                Rigidbody2D rb = obj[i].GetComponent<Rigidbody2D>();
                Quaternion rot = Quaternion.AngleAxis(sc[i].rotation, Vector3.forward);
                rb.velocity = rot * new Vector3(
                    sc[i].majorAxis * -2.0f * PI * Sin(PI * 2.0f * timing[i] / sc[i].period) / sc[i].period,
                    sc[i].minorAxis * 2.0f * PI * Cos(PI * 2.0f * timing[i] / sc[i].period) / sc[i].period
                );
                Vector2 posOffset = rot * new Vector3(
                    (sc[i].origin.x + sc[i].majorAxis * Cos(PI * 2 * (timing[i] / sc[i].period))) - obj[i].transform.position.x,
                    (sc[i].origin.y + sc[i].minorAxis * Sin(PI * 2 * (timing[i] / sc[i].period))) - obj[i].transform.position.y,
                    0
                );
                rb.velocity += posOffset;
                timing[i] += Time.fixedDeltaTime;
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
            }
        }
    }

    void planetsInEditor() {

    }

    public void updateAllPlanetGen() {
        for(int i = 0; i < planets.Count; i++) {
            ctrl[i].updatePlanet();
        }
    }

    
}
