using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;
using Mirror;
using Cinemachine;
using TMPro;

public class Player : NetworkBehaviour
{

    //fields    
    [Header("Physics")]
    private PlanetManager pm;
    [SerializeField] private float movePower = 1250;
    [SerializeField] private float jumpPower = 750;
    [SerializeField] private float jetPackPower = 50;
    [SerializeField] private float jetCorrectionRate = 100;
    [SerializeField] private LineRenderer pathRenderer;

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
    [SerializeField] GameObject debugLinePrefab;

    [SerializeField] ManagerDictionary ProjectileDictionary;
    private ProjectileManager projectileProperties;
    public string projType = "HeavyEgg";

    //misc cache
    private Rigidbody2D rb;
    private Transform lookTransform;
    private GameObject model;
    private GameObject background;
    private ParticleSystem ps;
    [HideInInspector] public TMP_Text debugText;
    private PlanetController currentPlanet;
    private int debugLineIndex = 0;
    private List<LineRenderer> debugLines;

    //timers and ground detection
    [HideInInspector] public bool onGround = false;
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
    private bool jet = false;

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
        angle = angle + projectileProperties.projectileBaseAcc*UnityEngine.Random.Range(-1.0f,1.0f) * PI / 180;
        shoot(pos, vel, angle);
    }

    [ClientRpc]
    private void shoot(Vector3 pos, Vector2 vel, float angle) {
        GameObject projObj = Instantiate(projectilePrefab);
        projObj.GetComponent<ProjectileBehavior>().Init(projectileProperties, pos, vel, angle);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), projObj.GetComponent<Collider2D>());
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

    private void drawDebugLine(Vector3 a, Vector3 b, Color c) {
        if(debugLines.Count - 1 < debugLineIndex) {
            GameObject lrObj = Instantiate(debugLinePrefab);
            lrObj.transform.SetParent(transform);
            LineRenderer lr = lrObj.GetComponent<LineRenderer>();
            debugLines.Add(lr);
        }
        debugLines[debugLineIndex].gameObject.SetActive(true);
        debugLines[debugLineIndex].SetPositions(new Vector3[] {a, b});
        debugLines[debugLineIndex].startColor = c;
        debugLines[debugLineIndex].endColor = c;
        debugLineIndex++;
    }

    //updates
    void Update() {
        
        if(!isLocalPlayer) {
            return;
        }
        bool fire = Input.GetMouseButton(0);
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);
        jet = Input.GetKey(KeyCode.Space);

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

        if(!onGround) offGroundTimer += Time.deltaTime;
        else if(groundTiming) {
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
    public Vector3[] getPath() {
        List<Vector3> path = new List<Vector3>();
        path.Add(transform.position);
        Vector2 vel = rb.velocity * rb.mass;
        Vector2 pos = transform.position;
        for(int i = 1; i < 500; i++) {
            Vector2 gVecDt = new Vector2(0,0);
            if(pm.gravVectorSumAtDeltaTime(pos.x, pos.y, rb.mass, Time.fixedDeltaTime, ref gVecDt))
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
        if(!isLocalPlayer) return;
        debugText.text = "";
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
        rb.AddTorque(-15.0f * offGravRot * Time.fixedDeltaTime * healthMax / (health + 1/100000.0f));
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
                    debugText.text += "" + currentPlanet.id + ": " + Vector2.Distance(transform.position, hit.point);
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
        
        //handle unused debug lines if some are drawn conditionally
        while(debugLineIndex < debugLines.Count) {
            debugLines[debugLineIndex].gameObject.SetActive(false);
            debugLineIndex++;
        }
    }
    
    //initializations
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        health = healthMax;

        lookTransform = GameObject.Find("LookTransform").transform;
        CinemachineVirtualCamera vcam = GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>();
        vcam.Follow = lookTransform;

        hudController = GameObject.Find("HUD").GetComponent<HudController>();
        hudController.showNameChangeHud();
        hudController.playerScript = gameObject.GetComponent<Player>();
        debugText = hudController.gameObject.transform.GetChild(2).GetComponentInChildren<TMP_Text>();

        background = GameObject.Find("Background");

        

        ps = transform.GetComponentInChildren<ParticleSystem>();
    }

    void Awake() {
        debugLines = new List<LineRenderer>();
        projectileProperties = ProjectileDictionary.loadProperties(projType);
        projectilePrefab = projectileProperties.projectilePrefab;
    }

    void Start() {
        rb = transform.GetComponentInChildren<Rigidbody2D>();
        rb.centerOfMass = new Vector2(0, -0.4f);

        model = transform.GetChild(1).gameObject;
        pm = GameObject.Find("PlanetManager").GetComponent<PlanetManager>();

        //align player to gravity when spawned
        transform.position = pm.getSpawnLocation();
        rb.velocity = new Vector2(0,0);
        alignToGravity();
    }
}
