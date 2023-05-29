using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;
using Cinemachine;
using TMPro;

public class PlayerEditor : MonoBehaviour
{
    
    [Header("Physics")]
    [SerializeField] GameObject planetObj;
    [SerializeField] private float movePower = 1000;
    [SerializeField] private float jumpPower = 500;

    private Rigidbody2D rb;
    private GameObject model;
    private PlanetController planetScript;

    //timers and ground detection
    private bool onGround = false;
    private bool groundTiming = false;
    private float offGroundTimer = 0;
    private float jumpCooldown = 0.5f;
    private float jumpTimer = 0;

    //input
    private bool left = false;
    private bool right = false;
    private bool jump = false;
    private bool forward = true;

    void Start() {
        rb = transform.GetComponentInChildren<Rigidbody2D>();
        model = transform.GetChild(1).gameObject;
        rb.centerOfMass = new Vector2(0, -0.3f);
        planetObj = GameObject.Find("MainPlanet");
        planetScript = planetObj.GetComponent<PlanetController>();
        //align player to gravity when spawned
        rb.rotation -= Vector2.SignedAngle(
            planetScript.gravVector(transform.position.x, transform.position.y, rb.mass),
            new Vector2(Cos(rb.rotation * PI / 180), Sin(rb.rotation * PI / 180))
        ) - 90;
    }

    void setForward(bool _new) {
        forward = _new;
        if(forward) model.transform.localScale = new Vector3(1, 1, 1);
        else model.transform.localScale = new Vector3(-1, 1, 1);
    }
   
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

    void FixedUpdate() {
        if(left) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(-1, 0));
        if(right) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(1, 0));
        if(jump && jumpTimer >= jumpCooldown) {
                rb.AddRelativeForce(jumpPower * new Vector2(0, 1));
                jumpTimer = 0;
        }

        //gravity
        Vector2 gVector = planetScript.gravVector(
            transform.position.x, transform.position.y, rb.mass
        );

        //adjust rotation to match gravity
        Vector2 rotVec = new Vector2(Cos(rb.rotation * PI / 180), Sin(rb.rotation * PI / 180));
        float offGravRot = Vector2.SignedAngle(
            gVector,
            rotVec
        ) - 90;
        rb.AddTorque(-15.0f * offGravRot * Time.fixedDeltaTime);
        rb.AddForce(Time.fixedDeltaTime * gVector);
    }

    void Update() {
        if(!onGround) offGroundTimer += Time.deltaTime;

        bool w = Input.GetKey(KeyCode.W);
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);
        bool fire = Input.GetKeyDown(KeyCode.Space);
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mouseAngle = Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x);
        left = false;
        right = false;
        jump = false;
        if(onGround) {
            if(a) left = true;
            if(d) right = true;
            if(w) jump = true;
            if(groundTiming) {
                offGroundTimer -= Time.deltaTime;
                if(offGroundTimer <= 0) {
                    onGround = false;
                    groundTiming = false;
                }
            }
        }

        jumpTimer += Time.deltaTime;

        //mirror sprite depending on velocity
        Vector2 rotVec = new Vector2(Cos(rb.rotation * PI / 180), Sin(rb.rotation * PI / 180));
        float offVelRot = Vector2.Angle(
            rb.velocity,
            rotVec
        );
        if(offVelRot < 85 && rb.velocity.magnitude > 0.5f) {
            setForward(false);
        } else if(offVelRot > 95 && rb.velocity.magnitude > 0.5f) {
            setForward(true);
        }
    }
}
