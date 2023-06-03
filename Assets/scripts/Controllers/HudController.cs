using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Mathf;
using TMPro;

public class HudController : MonoBehaviour
{
    [SerializeField] Canvas nameChangeCanvas;
    [SerializeField] public TMP_Text nameText;
    [SerializeField] Button nameChangeButton;
    [SerializeField] Button mm_scaleUp;
    [SerializeField] Button mm_scaleDown;
    [SerializeField] GameObject mmPlanetPrefab;
    [SerializeField] float mm_scale = 1;
    private GameObject mm_player;
    private GameObject[] planets;
    private GameObject[] mm_planets;
    private bool initialized = false;

    [HideInInspector] public Player playerScript;
    
    void Start()
    {
        nameChangeButton.onClick.AddListener(nameChangeButtonOnClick);
        mm_scaleUp.onClick.AddListener(OnMMScaleUp);
        mm_scaleDown.onClick.AddListener(OnMMScaleDown);
        mm_player = transform.GetChild(1).transform.GetChild(3).gameObject;
    }

    void Update() {
        if(initialized) {
            mm_player.transform.localPosition = 
                new Vector3(
                    playerScript.gameObject.transform.position.x * mm_scale,
                    playerScript.gameObject.transform.position.y * mm_scale,
                    0
                );
            mm_player.transform.localScale = new Vector3(mm_scale, mm_scale, 1);
            for(int i = 0; i < planets.Length; i++) {
                mm_planets[i].transform.localPosition = planets[i].transform.position * mm_scale;
                mm_planets[i].transform.localScale = new Vector3(mm_scale, mm_scale, 1);
                mm_planets[i].transform.localPosition =
                    new Vector3(
                        planets[i].transform.position.x * mm_scale,
                        planets[i].transform.position.y * mm_scale,
                        0
                    );
            }
        }
    }

    private Mesh generateMmMesh(List<Vector2> vertices, int resolution) {
        Mesh mesh = new Mesh();
        List<Vector3> newVert = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> tri = new List<int>();

        //only use every n vertices from given surface
        for(int i = 0; i < vertices.Count; i++) {
            if(i % resolution == 0) {
                newVert.Add(vertices[i]);
                uv.Add(new Vector2(0, 0));
            }
        }
        newVert.Add(new Vector3(0, 0, 0));
        uv.Add(new Vector2(0, 0));

        //triangles
        int c = newVert.Count;
        for(int i = 0; i < c - 1; i++) {
            tri.Add(i);
            tri.Add((i + 1) % (c - 1));
            tri.Add(c - 1);
        }

        mesh.vertices = newVert.ToArray();
        mesh.uv = uv.ToArray();
        mesh.triangles = tri.ToArray();

        return mesh;
    }

    private List<Vector2> circleVert(int resolution, float r) {
        List<Vector2> vert = new List<Vector2>();
        for(int i = 0; i < resolution; i++) {
            vert.Add(new Vector2(
                r * Cos(PI * 2 * ((float) i / resolution)),
                r * Sin(PI * 2 * ((float) i / resolution))
            ));
        }
        return vert;
    }

    public void initPlanets(GameObject[] planets) {
        this.planets = planets;
        mm_planets = new GameObject[planets.Length];
        Transform mmBase = transform.GetChild(1);
        for(int i = 0; i < planets.Length; i++) {
            mm_planets[i] = Instantiate(mmPlanetPrefab);
            mm_planets[i].transform.parent = mmBase;
            mm_planets[i].transform.localScale = new Vector3(mm_scale, mm_scale, 1);
            mm_planets[i].transform.localPosition = planets[i].transform.position * mm_scale;
            mm_planets[i].GetComponent<MeshFilter>().mesh = generateMmMesh(planets[i].GetComponent<PlanetController>().surface, 3);
        }
        mm_player.GetComponent<MeshFilter>().mesh = generateMmMesh(circleVert(20, 5), 1);
        initialized = true;
    }

    public void showNameChangeHud() {
        nameChangeCanvas.gameObject.SetActive(true);
    }

    void nameChangeButtonOnClick() {
        playerScript.changePlayerName(nameText.text);
        nameChangeCanvas.gameObject.SetActive(false);
    }

    void OnMMScaleUp() {
        mm_scale += 0.05f;
    }

    void OnMMScaleDown() {
        mm_scale -= 0.05f;
    }

}
