using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

[ExecuteInEditMode]
public class PlanetEditorGizmo : MonoBehaviour
{
    private PlanetManager pm;
    private PlanetManagerEditor pme;

    private bool isEditor = false;

    void Awake()
    {
        pm = GetComponent<PlanetManager>();
        if(pm == null) {
            pme = GetComponent<PlanetManagerEditor>();
            isEditor = true;
        }

    }

    private void gizmoDraw(Vector3[] points) {
        for(int i = 0, j = 1; j < points.Length; i++, j++) {
            Gizmos.DrawLine(points[i], points[j]);
        }
        Gizmos.DrawLine(points[points.Length - 1], points[0]);
    }

    private Vector3[] ellipsePoints(int resolution, float x, float y, float maj, float min, float rotation) {
        Vector3[] positions = new Vector3[resolution];
        Quaternion rot = Quaternion.AngleAxis(rotation, Vector3.forward);
        for(int i = 0; i < resolution; i++) {
            float angle = (float) i / resolution * 2 * PI;
            positions[i] = new Vector3(
                maj * Cos(angle) + x,
                min * Sin(angle) + y,
                0
            );
            positions[i] = rot * positions[i];
        }
        return positions;
    }

    void OnDrawGizmos() {
        List<PlanetSceneObject> planets;
        if(!isEditor) {
            planets = pm.planets;
        } else {
            planets = pme.planets;
        }
        foreach(PlanetSceneObject p in planets) {
            if(p.isStatic) {
                gizmoDraw(ellipsePoints(100, p.origin.x, p.origin.y, p.planet.radius, p.planet.radius, 0));
            } else {
                gizmoDraw(ellipsePoints(100, p.origin.x, p.origin.y, p.majorAxis, p.minorAxis, p.rotation));
                gizmoDraw(ellipsePoints(
                    100,
                    p.origin.x + p.majorAxis * Cos((p.reverse ? -1 : 1) * PI * 2 * (p.offset * p.period)),
                    p.origin.y + p.minorAxis * Sin((p.reverse ? -1 : 1) * PI * 2 * (p.offset * p.period)),
                    p.planet.radius, p.planet.radius, p.rotation
                ));
            }
        }
    }
}
