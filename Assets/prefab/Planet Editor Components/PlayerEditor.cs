using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;
using Cinemachine;
using TMPro;

public class PlayerEditor : MonoBehaviour
{

    //fields    
    [Header("Physics")]
    [SerializeField] private PlanetManagerEditor pm;
    [SerializeField] private float movePower = 1250;
    [SerializeField] private float jumpPower = 750;
    [SerializeField] private float jetPackPower = 50;
    [SerializeField] private float jetCorrectionRate = 100;

    //misc cache
    private Rigidbody2D rb;
    private Transform lookTransform;
    private GameObject model;
    private ParticleSystem ps;
    [HideInInspector] public TMP_Text debugText;
    private PlanetController currentPlanet;
    private int debugLineIndex = 0;

    //timers and ground detection
    [HideInInspector] public bool onGround = false;
    private bool groundTiming = false;
    private float offGroundTimer = 0;
    private float jumpCooldown = 0.5f;
    private float jumpTimer = 0;

    //input
    private bool left = false;
    private bool right = false;
    private bool down = false;
    private bool up = false;
    private bool jet = false;

    public bool forward = true;
    void OnPlayerForward(bool _old, bool _new) {
        if(forward) model.transform.localScale = new Vector3(1, 1, 1);
        else model.transform.localScale = new Vector3(-1, 1, 1);
    }

    void CmdSetForward(bool _new) {
        forward = _new;
    }
    void setForward(bool _new) {
        forward = _new;
        if(forward) model.transform.localScale = new Vector3(1, 1, 1);
        else model.transform.localScale = new Vector3(-1, 1, 1);
        CmdSetForward(_new);
    }

    //collisions
    private void OnCollisionEnter2D(Collision2D collision) {
        if(collision.gameObject.CompareTag("planet")) {
            onGround = true;
            groundTiming = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision) {
        if(collision.gameObject.CompareTag("planet")) {
            offGroundTimer = 0.2f;
            groundTiming = true;
        }
    }
    
    //util
    private void alignToGravity() {
        rb.rotation -= Vector2.SignedAngle(
            pm.gravVectorSum(transform.position.x, transform.position.y, rb.mass),
            new Vector2(Cos(rb.rotation * PI / 180), Sin(rb.rotation * PI / 180))
        ) - 90;
    }

    private Vector3 v2to3(Vector2 v2) {
        return v2;
    }

    //updates
    void Update() {
        
        bool fire = Input.GetMouseButton(0);
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);
        jet = Input.GetKey(KeyCode.Space);

        if(Input.GetKeyDown(KeyCode.R)) {
            transform.position = pm.getSpawnLocation();
            rb.velocity = new Vector2(0, 0);
            alignToGravity();
        }

        //input
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mouseAngle = Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x);

        if(!onGround) offGroundTimer += Time.deltaTime;
        else if(groundTiming) {
            offGroundTimer -= Time.deltaTime;
            if(offGroundTimer <= 0) {
                onGround = false;
                groundTiming = false;
            }
        }

        jumpTimer += Time.deltaTime;

    }
    public Vector3[] getPath() {
        List<Vector3> path = new List<Vector3>();
        path.Add(transform.position);
        Vector2 vel = rb.velocity * rb.mass;
        Vector2 pos = transform.position;
        for(int i = 1; i < 500; i++) {
            Vector2 gVecDt = new Vector2(0,0);
            if(pm.gravVectorSumAtDeltaTime(pos.x, pos.y, rb.mass, 0, ref gVecDt))
                break;
            vel += Time.fixedDeltaTime * gVecDt / rb.mass;
            pos += Time.fixedDeltaTime * vel;
            path.Add(new Vector3(
                pos.x,
                pos.y
            ));
        }
        return path.ToArray();
    }
    void FixedUpdate() {
        debugLineIndex = 0;
        ParticleSystem.EmissionModule em = ps.emission;
        if(onGround) {
            em.enabled = false;
            if(left) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(-1, 0));
            if(right) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(1, 0));
            if(up && jumpTimer >= jumpCooldown) {
                    rb.AddRelativeForce(jumpPower * new Vector2(0, 1));
                    jumpTimer = 0;
            }
        } 
        if(jet) { //jetpack
            
            em.enabled = true;

            ParticleSystem.ShapeModule sm = ps.shape;

            Vector3 jetDesiredVec = new Vector2(0, 0);
            jetDesiredVec.x = (left ? 1 : 0) + (right ? -1 : 0);
            jetDesiredVec.y = (down ? 1 : 0) + (up ? -1 : 0);

            Vector3 jetCurrentRotVec =  new Vector2(
                Cos(PI / 180 * (ps.shape.rotation.z)),
                Sin(PI / 180 * (ps.shape.rotation.z))
            );

            if(jetDesiredVec.x != 0 || jetDesiredVec.y != 0) {
                //rotate jet current vec towards desired vec and update particle shape rotation to match
                float angleDiff = Vector2.SignedAngle(jetCurrentRotVec, jetDesiredVec);

                float angleAdjust = Time.fixedDeltaTime * (angleDiff / 180.0f) * jetCorrectionRate;

                jetCurrentRotVec = Quaternion.AngleAxis(
                    angleAdjust,
                    Vector3.forward
                ) * jetCurrentRotVec;

                sm.rotation = new Vector3(0, 0,
                    (sm.rotation.z + angleAdjust)
                );
            }

            rb.AddRelativeForce(Time.fixedDeltaTime * -1 * jetPackPower * jetCurrentRotVec);
        } else {
            em.enabled = false;
        }

        //vectors
        Vector2 gVector = pm.gravVectorSum( //gravity sum vector
            transform.position.x, transform.position.y, rb.mass
        );
        Vector2 rotVec = new Vector2( //points in down direction relative to player
            Cos((rb.rotation - 90) * PI / 180),
            Sin((rb.rotation - 90) * PI / 180)
        );

        //drawDebugLine(transform.position, transform.position + 3 * v2to3(rb.velocity), Color.red);    //vel
        //drawDebugLine(transform.position, transform.position + v2to3(gVector), Color.blue);           //grav
        //drawDebugLine(transform.position, transform.position + 3 * v2to3(rotVec), Color.green);       //rot
        
        //adjust rotation to match gravity
        float offGravRot = Vector2.SignedAngle( //difference between player rotation and gravity
            gVector,
            rotVec
        );
        rb.AddTorque(-15.0f * offGravRot * Time.fixedDeltaTime);
        rb.AddForce(Time.fixedDeltaTime * gVector);

        //determines planet player is above
        RaycastHit2D[] hits = new RaycastHit2D[10];
        ContactFilter2D filter = new ContactFilter2D();
        int hitCount = Physics2D.Raycast(transform.position, rotVec, filter.NoFilter(), hits);
        if(hitCount > 0) {
            foreach(RaycastHit2D hit in hits) {
                if(!hit) continue;        
                if(hit.transform.gameObject.CompareTag("planet")) {
                    currentPlanet = hit.collider.gameObject.GetComponent<PlanetController>();
                    break;
                }
            }  
        }

        //mirror sprite depending on velocity vector offset to gravity vector
        Vector2 relVel = rb.velocity - pm.getVel(currentPlanet.id);
        float gravVelRot = Vector2.SignedAngle( //difference between player velocity and gravity
            relVel,
            gVector
        );

        if(gravVelRot < 85 && relVel.magnitude > 0.5f) {
            setForward(false);
        } else if(gravVelRot > 95 && relVel.magnitude > 0.5f) {
            setForward(true);
        }
    }
    
    //initializations
    void Start() {
        rb = transform.GetComponentInChildren<Rigidbody2D>();
        rb.centerOfMass = new Vector2(0, -0.4f);

        model = transform.GetChild(1).gameObject;

        //align player to gravity when spawned
        rb.velocity = new Vector2(0,0);
        alignToGravity();
        
        ps = transform.GetComponentInChildren<ParticleSystem>();
    }
}
