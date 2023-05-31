using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetManager : MonoBehaviour
{

    [SerializeField] List<PlanetSceneObject> planets;
    private List<GameObject> planetObjects;
    [SerializeField] GameObject planetPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        //create a planet prefab for each planet
        planetObjects = new List<GameObject>();
        int id = 0;
        foreach(PlanetSceneObject p in planets) {
            GameObject pObj = Instantiate(
                planetPrefab,
                p.origin,
                transform.rotation 
            );
            if(p.isStatic) {
                PlanetController newPlanetController = pObj.GetComponent<PlanetController>();
                newPlanetController.planet = p.planet;
                newPlanetController.id = id;
                newPlanetController.Init();
                planetObjects.Add(pObj);
            } else {

            }
        }
    }

    public Vector2 gravVectorSum(float x, float y, float m) {
        Vector2 sum = new Vector2(0, 0);
        foreach(GameObject p in planetObjects) 
            sum += p.GetComponent<PlanetController>().gravVector(x, y, m);
        return sum;
    }

    public Vector3 getSpawnLocation() {
        return planetObjects[0].GetComponent<PlanetController>().getSpawnLocation();
    }

    // Update is called once per frame
    void Update()
    {
        foreach(PlanetSceneObject p in planets) {
            if(!p.isStatic) {
                //translate planet along orbit path TODO
            }
        }
    }
}
