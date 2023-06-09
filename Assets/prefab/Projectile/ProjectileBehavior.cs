using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;

public class ProjectileBehavior : NetworkBehaviour
{
    [SerializeField] ManagerDictionary ProjectileList;
    [SerializeField] float ProjectileOffset = 5.0f;

    [HideInInspector] public PlanetManager planetMan;

    private float timeAlive = 0;
    public float speed = 1;
    public float lifeTime = 5;
    private Rigidbody2D rb;

    //scriptable objects
    private ProjectileManager ProjectileSettings;

    public void Init(ProjectileManager ProjectileSettings, Vector3 initialPosition, Vector2 initialVelocity, float angle) {
        planetMan = FindFirstObjectByType<PlanetManager>();
        rb.velocity = initialVelocity + (ProjectileSettings.projectileBaseVel * speed * new Vector2(Cos(angle), Sin(angle)));
        transform.position = initialPosition + (ProjectileOffset * new Vector3(Cos(angle), Sin(angle),0));
    }

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        
        
    }

    void Update() {
        if(timeAlive >= lifeTime)
            Destroy(gameObject);
        rb.rotation = 180.0f / PI * Atan2(rb.velocity.y, rb.velocity.x) - 180;
        timeAlive += Time.deltaTime;

        

    }

    void FixedUpdate()
    {
        rb.AddForce(Time.fixedDeltaTime * ProjectileSettings.projectileGravFactor * planetMan.gravVectorSum(
            transform.position.x, transform.position.y, rb.mass
        ) - ProjectileSettings.projectileDragCoef * rb.velocity * planetMan.atmoDensity(transform.position.x, transform.position.y));
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        GameObject other = collision.gameObject;
        if(other.CompareTag("Player")) {
            Player po = other.GetComponent<Player>();
            po.changeHealth(po.health - 5);
        }
        Destroy(gameObject);
    }
}
