using UnityEngine;
using System.Collections.Generic;

public class Stamping : MonoBehaviour {
    
    private const int MAP_SIZE = 32;
    private const int MAP_LEN = MAP_SIZE * MAP_SIZE;

    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private AnimationCurve falloff;
    private float[] pollution = new float[MAP_LEN];
    private Mesh[] meshes = new Mesh[MAP_LEN];
    private List<float> factories = new List<float>();
    private List<int> factoryIndex = new List<int>();

    private float tileValue;
    
    private void Awake() {
        for (int i = 0; i < MAP_LEN; ++i) {
            GameObject t = Instantiate(prefab);
            t.transform.position = GetCoord(i).ToVector3XZ();
            t.transform.SetParent(transform);
            meshes[i] = transform.GetChild(i).GetComponent<MeshFilter>().mesh;
        }
    }

    void Update() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit)) {
            return;
        }
        tileValue = pollution[GetIndex(hit.point)];
        if (!Input.GetMouseButtonUp(0)) {
            return;
        }
        AddFactory(GetIndex(hit.point), new Vector3(hit.point.x, 6, hit.point.z));
    }

    private void OnGUI() {
        GUI.Label(new Rect(10, 10, 50, 20), tileValue.ToString("0.000"));
    }

    public void AddFactory(int f, Vector3 coord) {
        factories.Add(coord.y);
        factoryIndex.Add(GetIndex(coord));
        
        float[] vals = new float[factories.Count];
        
        for (int i = 0; i < MAP_LEN; ++i) {
            Point2 p = GetCoord(i);
            
            for (int j = 0; j < factories.Count; ++j) {
                Point2 fc = GetCoord(factoryIndex[j]);
                float x = p.x - fc.x;
                float y = p.y - fc.y;
                float l = factories[j];
                float v = Mathf.Clamp01((x * x + y * y) / (l * l));
                vals[j] = falloff.Evaluate(v);
            }

            pollution[i] = Sum(vals);
            SetVertexColors(meshes[i], new Color(pollution[i], pollution[i], pollution[i]));
        }

        for (int i = 0; i < factories.Count; ++i) {
            SetVertexColors(meshes[factoryIndex[i]], Color.red);
        }
    }

    private static float Sum(float[] values) {
        float s = 0f;
        for (int i = 0; i < values.Length; ++i) {
            s += values[i];
        }
        return s;
    }
    
    private static Point2 GetCoord(int i) {
        return new Point2(i % MAP_SIZE, i >> 5);
    }

    private static int GetIndex(Vector3 coord) {
        return Mathf.FloorToInt(coord.z) * MAP_SIZE + Mathf.FloorToInt(coord.x);
    }

    private static void SetVertexColors(Mesh mesh, Color col) {
        Color[] colors = new Color[mesh.vertices.Length];
        for (int i = 0; i < mesh.colors.Length; ++i) {
            colors[i] = col;
        }
        mesh.colors = colors;
    }
}
