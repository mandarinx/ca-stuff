using UnityEngine;
using System.Collections.Generic;

public class Stamping : MonoBehaviour {
    
    private const int MAP_SIZE = 32;
    private const int MAP_LEN = MAP_SIZE * MAP_SIZE;

    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private AnimationCurve falloff;
    private int[] pollution = new int[MAP_LEN];
    private Mesh[] meshes = new Mesh[MAP_LEN];
    private List<int> factories = new List<int>();
    private List<int> factoryIndex = new List<int>();

    private int tileValue;
    
    private void Awake() {
        for (int i = 0; i < MAP_LEN; ++i) {
            GameObject t = Instantiate(prefab);
            t.transform.position = GetCoord(i).ToVector3XZ();
            t.transform.SetParent(transform);
            meshes[i] = transform.GetChild(i).GetComponent<MeshFilter>().mesh;
            
            SetVertexColors(meshes[i], Color.black);
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
        int fi = GetIndex(hit.point);
        if (factoryIndex.IndexOf(fi) >= 0) {
            RemoveFactory(fi);
        } else {
            AddFactory(fi, new Vector3(hit.point.x, 6, hit.point.z));
        }

        for (int i = 0; i < factories.Count; ++i) {
            if (factoryIndex[i] < 0) {
                continue;
            }
            SetVertexColors(meshes[factoryIndex[i]], Color.red);
        }
    }

    private void OnGUI() {
        GUI.Label(new Rect(10, 10, 50, 20), tileValue.ToString());
    }

    public void AddFactory(int f, Vector3 coord) {
        factories.Add((int)coord.y);
        factoryIndex.Add(GetIndex(coord));

        int radius = (int) coord.y;
        int[] buffer = GetStampBuffer(radius);
        BlitBuffer(buffer, radius, falloff);

        coord.x -= radius - 1;
        coord.z -= radius - 1;
        int start = GetIndex(coord);

        int[] indexes = GetBufferIndexes(buffer, start, radius);

        for (int i = 0; i < indexes.Length; ++i) {
            int n = indexes[i];
            pollution[n] += buffer[i];
            float pf = (float)pollution[n] / 255;
            SetVertexColors(meshes[n], new Color(pf, pf, pf));
        }
    }

    public void RemoveFactory(int f) {
        int n = factoryIndex.IndexOf(f);
        int radius = factories[n];
        factoryIndex[n] = -1;
        factories[n] = 0;
        
        int[] buffer = GetStampBuffer(radius);
        BlitBuffer(buffer, radius, falloff);

        Point2 fCoord = GetCoord(f);
        fCoord = new Point2(fCoord.x - radius + 1, fCoord.y - radius + 1);
        int start = GetIndex(fCoord);

        int[] indexes = GetBufferIndexes(buffer, start, radius);

        for (int i = 0; i < indexes.Length; ++i) {
            int j = indexes[i];
            pollution[j] -= buffer[i];
            float pf = (float)pollution[j] / 255;
            SetVertexColors(meshes[j], new Color(pf, pf, pf));
        }
    }

    private static int[] GetStampBuffer(int radius) {
        int line = 2 * radius - 1;
        return new int[line * line];
    }

    private static void BlitBuffer(int[] buffer, int radius, AnimationCurve falloff) {
        int center = radius - 1;
        int line = 2 * radius - 1;
        
        for (int i = 0; i < buffer.Length; ++i) {
            int x = (i % line) - center;
            int y = Mathf.FloorToInt((float)i / line) - center;
            float v = Mathf.Clamp01((float)(x * x + y * y) / (radius * radius));
            buffer[i] = Mathf.RoundToInt(falloff.Evaluate(v) * 255);
        }
    }

    private static int[] GetBufferIndexes(int[] buffer, int start, int radius) {
        int line = 2 * radius - 1;
        int[] indexes = new int[line * line];

        for (int i = 0; i < buffer.Length; ++i) {
            if (i > 0 && i % line == 0) {
                start += MAP_SIZE;
            }
            int n = start + (i % line);
            indexes[i] = n;
        }

        return indexes;
    }

    private static float Sum(float[] values) {
        float s = 0f;
        for (int i = 0; i < values.Length; ++i) {
            s += values[i];
        }
        return s;
    }

    private static int Sum(int[] values) {
        int s = 0;
        for (int i = 0; i < values.Length; ++i) {
            s += values[i];
        }
        return s;
    }
    
    private static Point2 GetCoord(int i) {
        return new Point2(i % MAP_SIZE, i >> 5);
    }

    private static int GetIndex(Vector3 coord) {
        return Mathf.RoundToInt(coord.z) * MAP_SIZE + Mathf.RoundToInt(coord.x);
    }

    private static int GetIndex(Point2 coord) {
        return Mathf.RoundToInt(coord.y) * MAP_SIZE + Mathf.RoundToInt(coord.x);
    }

    private static void SetVertexColors(Mesh mesh, Color col) {
        Color[] colors = new Color[mesh.vertices.Length];
        for (int i = 0; i < mesh.colors.Length; ++i) {
            colors[i] = col;
        }
        mesh.colors = colors;
    }
}
