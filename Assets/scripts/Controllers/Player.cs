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
    [SerializeField] private float jetPackPower = 50;

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
    private GameObject background;
    private ParticleSystem ps;

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
    private bool down = false;
    private bool up = false;

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

        bool fire = Input.GetKeyDown(KeyCode.Space);

        if(Input.GetKeyDown(KeyCode.R)) {
            changeHealth(healthMax);
            transform.position = pm.getSpawnLocation();
            rb.velocity = new Vector2(0, 0);
            alignToGravity();
        }

        //move background to follow player
        background.transform.position = transform.position * 0.8f;

        //input
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mouseAngle = Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x);
        left = false;
        right = false;
        up = false;
        down = false;
        if(Input.GetKey(KeyCode.A)) left = true;
        if(Input.GetKey(KeyCode.D)) right = true;
        if(Input.GetKey(KeyCode.W)) up = true;
        if(Input.GetKey(KeyCode.S)) down = true;
        if(onGround && groundTiming) {
            offGroundTimer -= Time.deltaTime;
            if(offGroundTimer <= 0) {
                onGround = false;
                groundTiming = false;
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

    }

    void FixedUpdate() {
        if(!isLocalPlayer) return;
        ParticleSystem.EmissionModule em = ps.emission;
        if(onGround) {
            em.enabled = false;
            if(left) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(-1, 0));
            if(right) rb.AddRelativeForce(Time.fixedDeltaTime * movePower * new Vector2(1, 0));
            if(up && jumpTimer >= jumpCooldown) {
                    rb.AddRelativeForce(jumpPower * new Vector2(0, 1));
                    jumpTimer = 0;
            }
        } else { //jetpack
            
            ParticleSystem.ShapeModule sm = ps.shape;
            Vector2 jetDir = new Vector2(0,0);
            if(left) jetDir.x -= 1;
            if(right) jetDir.x += 1;
            if(up) jetDir.y += 1;
            if(down) jetDir.y -= 1;
            if(jetDir.x != 0 || jetDir.y != 0) {
                em.enabled = true;
            }
            else em.enabled = false;
            sm.rotation = new Vector3(0, 0, 180.0f / PI * Atan2(jetDir.y, jetDir.x) - 180);
            rb.AddRelativeForce(Time.fixedDeltaTime * jetPackPower * jetDir);
        }

        //gravity
        Vector2 gVector = pm.gravVectorSum(
            transform.position.x, transform.position.y, rb.mass
        );

        //adjust rotation to match gravity
        Vector2 rotVec = new Vector2(Cos(rb.rotation * PI / 180), Sin(rb.rotation * PI / 180));
        float offGravRot = Vector2.SignedAngle( //difference between player rotation and gravity
            gVector,
            rotVec
        ) - 90;
        rb.AddTorque(-15.0f * offGravRot * Time.fixedDeltaTime * healthMax / (health + 1/100000.0f));
        rb.AddForce(Time.fixedDeltaTime * gVector);

        //mirror sprite depending on velocity vector offset to gravity vector
        float gravVelRot = Vector2.Angle( //difference between player velocity and gravity
            rb.velocity,
            gVector
        );

        if(gravVelRot < 85 && rb.velocity.magnitude > 0.5f) {
            setForward(false);
        } else if(gravVelRot > 95 && rb.velocity.magnitude > 0.5f) {
            setForward(true);
        }
        
    }
    
    public override void OnStartLocalPlayer()
    {
        health = healthMax;

        lookTransform = GameObject.Find("LookTransform").transform;
        CinemachineVirtualCamera vcam = GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>();
        vcam.Follow = lookTransform;

        hudController = GameObject.Find("HUD").GetComponent<HudController>();
        hudController.showNameChangeHud();
        hudController.playerScript = gameObject.GetComponent<Player>();

        background = GameObject.Find("Background");

        ps = transform.GetComponentInChildren<ParticleSystem>();
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
