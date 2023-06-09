using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetEditor_CamController : MonoBehaviour
{
    private float panSpeed = 10;
    [SerializeField] private PlanetManagerEditor planetMan;

    void Start() {
    }
    void Update()
    {
        Vector3 translate = new Vector3(0, 0, 0);
        if(Input.GetKey(KeyCode.UpArrow)) translate.y += 1;
        if(Input.GetKey(KeyCode.DownArrow)) translate.y -= 1;
        if(Input.GetKey(KeyCode.LeftArrow)) translate.x -= 1;
        if(Input.GetKey(KeyCode.RightArrow)) translate.x += 1;

        Camera.main.orthographicSize -= Input.mouseScrollDelta.y;

        Camera.main.transform.position += panSpeed * translate * Time.deltaTime * Camera.main.orthographicSize / 10;

        planetMan.updateAllPlanetGen();
    }
}
