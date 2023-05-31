using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlanetSceneObject", menuName = "Planet/New Planet Scene Object")]
public class PlanetSceneObject : ScriptableObject
{

    //      elipse equation
    //  ( (x-h)^2 / a^2 ) +
    //  ( (y-k)^2 / b^2 )
    //  = 1

    //  x = a cos(angle), y = b sin(angle)
    //  x(t) = a cos(t), y(t) = b sin(t)
    //  t is period to complete revolution
    //  t = 2pi is 1 revolution per time interval
    //  ie: period is 8pi, thus 1/4 of a full rotation per time interval (2pi / period), find x position when t = 2
    //  x(t) = a cos(t/4)
    //  x(2) = a cos(2/4)

    [SerializeField] public PlanetObject planet;
    [SerializeField] public bool isStatic = true;
    [Tooltip("If static, center of planet, if non-static, center of orbit")] [SerializeField] public Vector3 origin;
    [SerializeField][Min(0)] public float majorAxis = 1;
    [SerializeField][Min(0)] public float minorAxis = 1;
    [SerializeField] public float rotation = 0;
    [SerializeField] public float period = 60;
    [SerializeField] public float offset = 0;

}
