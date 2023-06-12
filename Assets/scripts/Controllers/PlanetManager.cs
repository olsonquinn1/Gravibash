using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using TMPro;
using Mirror;

public class PlanetManager : NetworkBehaviour
{

    [SerializeField] public List<PlanetSceneObject> planets;
    [SerializeField] GameObject planetPrefab;
    private PlanetSceneObject[] sc;
    private GameObject[] obj;
    private PlanetController[] ctrl;
    [HideInInspector] public TMP_Text debugText;
    
    private float[] velNorm;
    private float[] velAngle;
    private float G = 6.67f * Pow(10, -6);
    [SerializeField] private bool editor = false;

    // Start is called before the first frame update
    void Start()
    {

        //create a planet prefab for each planet
        ctrl = new PlanetController[planets.Count];
        sc = new PlanetSceneObject[planets.Count];
        obj = new GameObject[planets.Count];
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
            if(!p.isStatic) {
                obj[id].GetComponent<Rigidbody2D>().isKinematic = false;
                pObj.transform.position = new Vector3(
                    p.origin.x + p.majorAxis * Cos((p.reverse ? -1.0f : 1.0f) * PI * 2.0f * (float) NetworkTime.time / sc[id].period),
                    p.origin.y + p.minorAxis * Sin((p.reverse ? -1.0f : 1.0f) * PI * 2.0f * (float) NetworkTime.time / sc[id].period),
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

    public bool gravVectorSumAtDeltaTime(float x, float y, float m, float dt, ref Vector2 sum) {
        sum = new Vector2(0, 0);
        for(int i = 0; i < planets.Count; i++) if(ctrl[i] != null) {
            Vector2 gVec = new Vector2(0, 0);
            if(gravVectorAtDeltaTime(x, y, m, dt, i, ref gVec)) return true;
            sum += gVec;
        }
        return false;
    }

    public bool gravVectorAtDeltaTime(float x, float y, float m, float dt, int id, ref Vector2 v) {
        Vector2 pos = posAtDeltaTime(id, dt);
        float dist = Sqrt(
            Pow(pos.x - x, 2) + Pow(pos.y - y, 2)
        );
        if(dist <= ctrl[id].radius) {
            drawDebugCircle(pos, ctrl[id].radius, Color.yellow);
            return true;
        }
        float accel = G * ctrl[id].mass / Pow(dist, ctrl[id].gravityFalloff);

        float angle =  Atan2(pos.y - y, pos.x - x);

        v = new Vector2(
            accel * Cos(angle),
            accel * Sin(angle)
        );
        
        return false;
    }

    private Vector2 posAtDeltaTime(int id, float dt) {
        PlanetSceneObject p = sc[id];
        if(p.isStatic) return new Vector3(p.origin.x, p.origin.y, 0);
        Vector2 pos = Quaternion.AngleAxis(sc[id].rotation, Vector3.forward) * new Vector3(
            p.origin.x + p.majorAxis * Cos((p.reverse ? -1.0f : 1.0f) * PI * 2.0f * ((float) NetworkTime.time + dt) / p.period + (PI / 2.0f)),
            p.origin.y + p.minorAxis * Sin((p.reverse ? -1.0f : 1.0f) * PI * 2.0f * ((float) NetworkTime.time + dt) / p.period + (PI / 2.0f)),
            0
        );
        
        return pos;
    }

    private void drawDebugCircle(Vector3 pos, float r, Color c) {
        int res = 20;
        Vector3[] points = new Vector3[res];
        points[0] = new Vector3(
            pos.x + r,
            pos.y
        );

        for(int i = 1; i < res; i++) {
            points[i] = new Vector3(
                pos.x + r * Cos(PI * 2 * i / res),
                pos.y + r * Sin(PI * 2 * i / res)
            );
            Debug.DrawLine(points[i - 1], points[i], c, 0);
        }
        Debug.DrawLine(points[res - 1], points[0], c, 0);
        
    }

    public Vector2 getVel(int planetId) {
        return obj[planetId].GetComponent<Rigidbody2D>().velocity;
    }

    public Vector3 getSpawnLocation() {
        return ctrl[UnityEngine.Random.Range(0, ctrl.Length)].getSpawnLocation();
    }
    void FixedUpdate() {
        string s = "";
        for(int i = 0; i < planets.Count; i++) {
            if(!sc[i].isStatic) {
                Rigidbody2D rb = obj[i].GetComponent<Rigidbody2D>();
                Quaternion rot = Quaternion.AngleAxis(sc[i].rotation, Vector3.forward);
                float velMagX = -1.0f * Sin( velAngle[i] * (float) NetworkTime.time );
                float velMagY = Cos( velAngle[i] * (float) NetworkTime.time ) ;
                rb.velocity = rot * new Vector3(
                     velNorm[i] * velMagX,
                     velNorm[i] * velMagY
                );
                Vector2 posOffset = rot * new Vector3(
                    sc[i].origin.x + sc[i].majorAxis * velMagX - obj[i].transform.position.x,
                    sc[i].origin.y + sc[i].minorAxis * velMagY - obj[i].transform.position.y,
                    0
                );
                rb.velocity += 15 * posOffset;
                s += "" + i + ": " + (float) NetworkTime.time + "\n";
            }
        }
        debugText.text = s;
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
