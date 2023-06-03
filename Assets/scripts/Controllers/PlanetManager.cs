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
    
    // Start is called before the first frame update
    void Start()
    {
        //create a planet prefab for each planet
        ctrl = new PlanetController[planets.Count];
        sc = new PlanetSceneObject[planets.Count];
        obj = new GameObject[planets.Count];
        timing = new float[planets.Count];
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
                pObj.transform.position = new Vector3(
                    p.origin.x + p.majorAxis * Cos((p.reverse ? -1 : 1) * PI * 2 * (p.offset * p.period)),
                    p.origin.y + p.minorAxis * Sin((p.reverse ? -1 : 1) * PI * 2 * (p.offset * p.period)),
                    0
                );
            }
            id++;
            newPlanetController.Init();
        }
        GameObject.Find("HUD").GetComponent<HudController>().initPlanets(obj);
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

    void Update()
    {
        for(int i = 0; i < planets.Count; i++) {
            if(!sc[i].isStatic) {
                //translate planet along orbit path
                Quaternion rot = Quaternion.AngleAxis(sc[i].rotation, Vector3.forward);
                timing[i] += Time.deltaTime;
                obj[i].transform.position = rot * new Vector3(
                    sc[i].origin.x + sc[i].majorAxis * Cos(PI * 2 * (timing[i] / sc[i].period)),
                    sc[i].origin.y + sc[i].minorAxis * Sin(PI * 2 * (timing[i] / sc[i].period)),
                    0
                );
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
