using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleLight2D : MonoBehaviour
{
    [SerializeField] private GameObject particleLight;
    private List<GameObject> particleLights;
    private ParticleSystem.Particle[] particleBuffer;

    void Start() {
        particleBuffer = new ParticleSystem.Particle[100];
        particleLights = new List<GameObject>();
    }

    void Update() {
        int particleCount = GetComponent<ParticleSystem>().GetParticles(particleBuffer);

        //maintain list of light objects equal to the amount of particles
        while(particleLights.Count < particleCount) {
            particleLights.Add(Instantiate(particleLight, transform));
        }
        while(particleLights.Count > particleCount) {
            Destroy(particleLights[particleLights.Count]);
            particleLights.RemoveAt(particleLights.Count-1);
        }
        //adjust positions of light objects to match particles
        for(int i = 0; i < particleCount; i++)
            particleLights[i].transform.position = particleBuffer[i].position;
    }
}
