using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;

public class ProjectileBehavior : NetworkBehaviour
{
    [SerializeField] float ProjectileOffset = 0;

    [HideInInspector] public PlanetManager planetMan;

    private float timeAlive = 0;
    public float speed = 1;
    public float lifeTime = 5;
    private bool isDestroy = false;
    private float destructionTimer = 0;
    private float destructionTime = 1;
    private Rigidbody2D rb;

    //scriptable objects
    private ProjectileManager ProjectileSettings;
    private Vector2 lastContactPoint = new Vector2(0,0);

    public void Init(ProjectileManager ProjectileSettingsIn, Vector3 initialPosition, Vector2 initialVelocity, float angle) {
        planetMan = FindFirstObjectByType<PlanetManager>();
        ProjectileSettings = ProjectileSettingsIn;
        rb.velocity = initialVelocity + (ProjectileSettings.projectileBaseVel * speed * new Vector2(Cos(angle), Sin(angle)));
        transform.position = initialPosition;
        PhysicsMaterial2D newMaterial = Instantiate(rb.sharedMaterial);
        newMaterial.friction = 0.5f;//ProjectileSettings.projectileFriction;
        newMaterial.bounciness = ProjectileSettings.projectileElacticity;
        rb.sharedMaterial = newMaterial;
    }

    void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update() {
        if(timeAlive >= lifeTime && !isDestroy)
            beginDestroy();
        if(isDestroy) {
            destructionTimer += Time.deltaTime;
            if(destructionTimer > destructionTime)
                Destroy(gameObject);
        }
        rb.rotation = 180.0f / PI * Atan2(rb.velocity.y, rb.velocity.x) - 180;
        timeAlive += Time.deltaTime;
    }

    void beginDestroy() {
        GetComponent<SpriteRenderer>().enabled = false;
        rb.simulated = false;
        ParticleSystem.EmissionModule psem = GetComponent<ParticleSystem>().emission;
        psem.enabled = false;
        isDestroy = true;
    }

    void FixedUpdate()
    {
        Vector2 force = Time.fixedDeltaTime * ProjectileSettings.projectileGravFactor * planetMan.gravVectorSum(
            transform.position.x, transform.position.y, rb.mass);
             //- Time.fixedDeltaTime * ProjectileSettings.projectileDragCoef * rb.velocity * planetMan.atmoDensity(transform.position.x, transform.position.y);
        rb.AddForce(force);
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        GameObject other = collision.gameObject;
        if(other.CompareTag("Player")) {
            Rigidbody2D otherBody = other.GetComponent<Rigidbody2D>();
            Vector2 otherVel = otherBody.velocity;
            Vector2 relVel = (rb.velocity-otherVel);
            Player po = other.GetComponent<Player>();
            po.changeHealth(po.health - ProjectileSettings.projectileBaseDamage * Pow(relVel.magnitude,2)/Pow(ProjectileSettings.projectileBaseVel,2));
            Destroy(gameObject);
        }
        else {
            Rigidbody2D otherBody = other.GetComponent<Rigidbody2D>();
            Vector2 otherVel;
            if(otherBody != null)
                otherVel = otherBody.velocity;
            else
                otherVel = new Vector2(0,0);
            if((rb.velocity-otherVel).magnitude * 100 /(ProjectileSettings.projectileElacticity + 1) < ProjectileSettings.projectileBaseVel)
                beginDestroy();
        } 
    }
}
