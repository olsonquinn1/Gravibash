using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;
using Cinemachine;
using TMPro;

public class Player : NetworkBehaviour
{
    
    [Header("Physics")]
    private PlanetManager pm;
    [SerializeField] private float movePower = 1250;
    [SerializeField] private float jumpPower = 750;

    [Header("Projectile")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform launchOffset;

    [Header("Camera")]
    [SerializeField] float maxCameraDist;

    [Header("Floating Info")]
    [SerializeField] HudController hudController;
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] GameObject floatingInfo;
    [SerializeField] GameObject healthFill;
    [SerializeField] float healthMax = 100;
    
    private Rigidbody2D rb;
    private Transform lookTransform;
    private GameObject model;

    //timers and ground detection
    private bool onGround = false;
    private bool groundTiming = false;
    private float offGroundTimer = 0;
    private float jumpCooldown = 0.5f;
    private float jumpTimer = 0;
    private float shootCooldown = 0.25f;
    private float shootTimer = 0;

    //input
    private bool left = false;
    private bool right = false;
    private bool jump = false;

    //synced variables
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health;
    private void OnHealthChanged(float _old, float _new) {
        adjustHealthbar();
    }
    [Command]
    private void CmdChangeHealth(float _new) {
        health = _new;
    }
    public void changeHealth(float _new) {
        if(!isLocalPlayer) return;
        health = _new;
        adjustHealthbar();
        CmdChangeHealth(_new);
    }
    private void adjustHealthbar() {
        float scale = health / healthMax;
        if(scale < 0) scale = 0;
        else if(scale > 1) scale = 1;
        healthFill.transform.localScale = new Vector3(0.5f * scale, 0.08f, 1);
        healthFill.GetComponent<SpriteRenderer>().color = new Color((1.0f - scale) * 0.5f, scale * 0.5f, 0);
    }

    [SyncVar(hook= nameof(OnNameChanged))]
    private string playerName = "player";
    private void OnNameChanged(string _old, string _new) {
        playerNameText.text = playerName;
    }
    [Command]
    private void CmdChangePlayerName(string _new) {
        playerName = _new;
    }
    public void changePlayerName(string _new) {
        playerName = _new;
        playerNameText.text = _new;
        CmdChangePlayerName(_new);
    }

    [SyncVar(hook= nameof(OnPlayerForward))]
    public bool forward = true;
    void OnPlayerForward(bool _old, bool _new) {
        if(forward) model.transform.localScale = new Vector3(1, 1, 1);
        else model.transform.localScale = new Vector3(-1, 1, 1);
    }
    [Command]
    void CmdSetForward(bool _new) {
        forward = _new;
    }
    void setForward(bool _new) {
        forward = _new;
        if(forward) model.transform.localScale = new Vector3(1, 1, 1);
        else model.transform.localScale = new Vector3(-1, 1, 1);
        CmdSetForward(_new);
    }

    //synced actions
    [Command]
    private void cmdShoot(Vector3 pos, Vector2 vel, float angle) {
        shoot(pos, vel, angle);
    }

    [ClientRpc]
    private void shoot(Vector3 pos, Vector2 vel, float angle) {
        GameObject projObj = Instantiate(projectilePrefab);
        projObj.GetComponent<ProjectileBehavior>().Init(pos, vel, angle);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), projObj.GetComponent<Collider2D>());
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
    
    private void alignToGravity() {
        rb.rotation -= Vector2.SignedAngle(
            pm.gravVectorSum(transform.position.x, transform.position.y, rb.mass),
            new Vector2(Cos(rb.rotation * PI / 180), Sin(rb.rotation * PI / 180))
        ) - 90;
    }

    void Update() {
        
        if(!isLocalPlayer) {
            return;
        }
        if(!onGround) offGroundTimer += Time.deltaTime;

        bool w = Input.GetKey(KeyCode.W);
        bool a = Input.GetKey(KeyCode.A);
        bool d = Input.GetKey(KeyCode.D);
        bool fire = Input.GetKeyDown(KeyCode.Space);

        if(Input.GetKeyDown(KeyCode.R)) {
            changeHealth(healthMax);
            transform.position = pm.getSpawnLocation();
            rb.velocity = new Vector2(0, 0);
            alignToGravity();
        }

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
        
        if(fire && shootTimer >= shootCooldown) {
            cmdShoot(launchOffset.position, rb.velocity, mouseAngle);
            shootTimer = 0;
        }

        jumpTimer += Time.deltaTime;
        shootTimer += Time.deltaTime;

        //camera
        lookTransform.position = (transform.position + mousePos) / 2.0f;
        lookTransform.rotation = transform.rotation;
        float lookDistance = Vector3.Distance(transform.position, lookTransform.position);
        if(lookDistance > maxCameraDist) {
            lookTransform.position = transform.position + (maxCameraDist * (lookTransform.position - transform.position).normalized);
        }

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

    void FixedUpdate() {
        if(!isLocalPlayer) return;
        if(left) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(-1, 0));
        if(right) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(1, 0));
        if(jump && jumpTimer >= jumpCooldown) {
                rb.AddRelativeForce(jumpPower * new Vector2(0, 1));
                jumpTimer = 0;
        }

        //gravity
        Vector2 gVector = pm.gravVectorSum(
            transform.position.x, transform.position.y, rb.mass
        );

        //adjust rotation to match gravity
        Vector2 rotVec = new Vector2(Cos(rb.rotation * PI / 180), Sin(rb.rotation * PI / 180));
        float offGravRot = Vector2.SignedAngle(
            gVector,
            rotVec
        ) - 90;
        rb.AddTorque(-15.0f * offGravRot * Time.fixedDeltaTime * healthMax / (health + 1/100000.0f));
        rb.AddForce(Time.fixedDeltaTime * gVector);
    }
    
    public override void OnStartLocalPlayer()
    {
        health = healthMax;
        lookTransform = GameObject.Find("LookTransform").transform;
        if(lookTransform == null) Debug.Log("e");
        CinemachineVirtualCamera vcam = GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>();
        if(vcam == null) Debug.Log("a");
        vcam.Follow = lookTransform;
        hudController = GameObject.Find("HUD").GetComponent<HudController>();
        hudController.showNameChangeHud();
        hudController.playerScript = gameObject.GetComponent<Player>();
    }
    
    void Start() {
        rb = transform.GetComponentInChildren<Rigidbody2D>();
        model = transform.GetChild(1).gameObject;
        rb.centerOfMass = new Vector2(0, -0.3f);
        pm = GameObject.Find("PlanetManager").GetComponent<PlanetManager>();
        //align player to gravity when spawned
        alignToGravity();
    }
}
