using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;

public class ProjectileBehavior : NetworkBehaviour
{
    [HideInInspector] public PlanetController planetScript;

    

    private float timeAlive = 0;
    public float speed = 5;
    public float lifeTime = 5;
    private Rigidbody2D rb;

    public void Init(Vector3 initialPosition, Vector2 initialVelocity, float angle) {
        planetScript = FindFirstObjectByType<PlanetController>();
        rb.velocity = initialVelocity + ( speed * new Vector2(Cos(angle), Sin(angle)) );
        transform.position = initialPosition;
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
        rb.AddForce(Time.fixedDeltaTime * planetScript.gravVector(
            transform.position.x, transform.position.y, rb.mass
        ));
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
