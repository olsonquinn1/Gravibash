using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using UnityEngine.Rendering.Universal;

public class NormalLightTest : MonoBehaviour
{
    [SerializeField] private float distance = 1;
    [SerializeField] private float period = 5;
    private float time = 0;
    [SerializeField] private GameObject lightobj;

    void Update()
    {   
        time += Time.deltaTime;
        lightobj.transform.localPosition = distance * new Vector3(
            Cos(PI * 2 * time / period),
            Sin(PI * 2 * time / period)
        );
    }
}
