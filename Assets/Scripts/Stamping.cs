using System;
using UnityEngine;
using System.Collections.Generic;
using Mandarin;

namespace Mandarin {

    public struct Rectangle {
        public int l;
        public int r;
        public int t;
        public int b;

        public Rectangle(int top, int right, int bottom, int left) {
            l = left;
            r = right;
            t = top;
            b = bottom;
        }

        public static Rectangle GetOverlap(Rectangle a, Rectangle b, Point2 apos) {
            return new Rectangle(
                Mathf.Min(apos.y + a.t, b.t) - apos.y,
                Mathf.Min(apos.x + a.r, b.r) - apos.x,
                Mathf.Max(apos.y + a.b, b.b) - apos.y,
                Mathf.Max(apos.x + a.l, b.l) - apos.x
            );
        }
    }
}

public class Stamping : MonoBehaviour {

    private enum StampMode {
        ADD = 1,
        SUB = -1,
    }
    
    private const int MAP_DIM = 32;
    private const int MAP_HALF_DIM = (int)(MAP_DIM * 0.5f);
    private const int MAP_AREA = MAP_DIM * MAP_DIM;

    [SerializeField]
    private Material mat;
    [SerializeField]
    private AnimationCurve falloff;
    private int[] pollution = new int[MAP_AREA];
    private Mesh[] meshes = new Mesh[MAP_AREA];
    private List<int> factories = new List<int>();
    private List<int> factoryIndex = new List<int>();
    private List<int> forests = new List<int>();
    private List<int> forestIndex = new List<int>();

    private int tileValue;

    private int[] entityRadius = new int[2] {
        4, // factory
        2, // forest
    };
    
    private KeyCode[] keyCodes;
    private Action<Vector3>[] keyCallbacks;
    private string[] keyHelp;
    
    private Rectangle mapRect = new Rectangle(
        MAP_HALF_DIM - 1, MAP_HALF_DIM - 1, 
        -MAP_HALF_DIM,    -MAP_HALF_DIM);
    
    private void Awake() {
    
        GameObject prefab = new GameObject();
        Mesh plane = MeshUtils.CreatePlane(1, 1, 1, 1);
        prefab.AddComponent<MeshFilter>().sharedMesh = plane;
        prefab.AddComponent<MeshRenderer>().sharedMaterial = mat;
        prefab.AddComponent<BoxCollider>().size = Vector3.forward + Vector3.right;
        
        for (int i = 0; i < MAP_AREA; ++i) {
            GameObject t = Instantiate(prefab);
            t.transform.position = GetCoord(i).ToVector3XZ();
            t.transform.SetParent(transform);
            t.name = GetCoord(i).x + "_" + GetCoord(i).y;
            meshes[i] = transform.GetChild(i).GetComponent<MeshFilter>().mesh;
            
            SetVertexColors(meshes[i], Color.black);
        }
        
        Destroy(prefab);
        
        keyCodes = new [] {
            KeyCode.Alpha1, KeyCode.Alpha2, 
        };
        keyCallbacks = new Action<Vector3>[] {
            HandleFactory, HandleForest
        };
        keyHelp = new[] {
            "1 : Factory", "2 : Forest"
        };
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

        for (int i = 0; i < keyCodes.Length; ++i) {
            if (!Input.GetKey(keyCodes[i])) {
                continue;
            }
            keyCallbacks[i].Invoke(hit.point);
            break;
        }
        ColorizeEntities();
    }

    private void OnDrawGizmos() {
        for (int i = 0; i < MAP_DIM; ++i) {
            Gizmos.DrawLine(new Vector3(i, 0, 0), new Vector3(i, 0, MAP_DIM));
        }
        for (int i = 0; i < MAP_DIM; ++i) {
            Gizmos.DrawLine(new Vector3(0, 0, i), new Vector3(MAP_DIM, 0, i));
        }
    }

    private void OnGUI() {
        GUI.Label(new Rect(10, 10, 50, 20), tileValue.ToString());
        float height = keyHelp.Length * 20f;
        string help = "";
        for (int i = 0; i < keyHelp.Length; ++i) {
            help += keyHelp[i] + "\n";
        }
        GUI.Label(new Rect(10, Screen.height - height, 100, height), help);
    }

    private void HandleFactory(Vector3 coord) {
        int f = GetIndex(coord);
        int fi = factoryIndex.IndexOf(f);
        if (fi >= 0) {
            RemoveFactory(f, new Vector3(coord.x, factories[fi], coord.z));
        } else {
            AddFactory(f, new Vector3(coord.x, entityRadius[0], coord.z));
        }
    }

    private void HandleForest(Vector3 coord) {
        int f = GetIndex(coord);
        int fi = forestIndex.IndexOf(f);
        if (fi >= 0) {
            RemoveForest(f, new Vector3(coord.x, forests[fi], coord.z));
        } else {
            AddForest(f, new Vector3(coord.x, entityRadius[1], coord.z));
        }
    }

    private void RemoveForest(int f, Vector3 coord) {
        int n = forestIndex.IndexOf(f);
        int radius = (int)coord.y;
        forestIndex[n] = -1;
        forests[n] = 0;

        StampBlobToMap(radius, SnapToMap(coord), pollution, StampMode.ADD);
    }

    private void AddForest(int f, Vector3 coord) {
        int radius = (int)coord.y;
        StampBlobToMap(radius, SnapToMap(coord), pollution, StampMode.SUB);
        forests.Add(radius);
        forestIndex.Add(GetIndex(coord));
    }

    public void AddFactory(int f, Vector3 coord) {
        int radius = (int)coord.y;
        StampBlobToMap(radius, SnapToMap(coord), pollution, StampMode.ADD);
        factories.Add(radius);
        factoryIndex.Add(GetIndex(coord));
    }

    public void RemoveFactory(int f, Vector3 coord) {
        int n = factoryIndex.IndexOf(f);
        int radius = (int)coord.y;
        factoryIndex[n] = -1;
        factories[n] = 0;

        StampBlobToMap(radius, SnapToMap(coord), pollution, StampMode.SUB);
    }
    
    private void StampBlobToMap(int radius, Point2 coord, int[] mapData, StampMode mode) {
        SetVertexColors(meshes[GetIndex(coord)], Color.magenta);

        Rectangle blitRect = new Rectangle(radius, radius, -radius, -radius);
        Rectangle cropped = Rectangle.GetOverlap(blitRect, mapRect, coord - MAP_HALF_DIM);
        int[] buffer = GetStampBuffer(radius);
        DrawFalloff(buffer, radius, falloff);
        
        int bufferWidth = radius * 2 + 1;
        int start = GetIndex(new Point2(radius + cropped.l, radius + cropped.b), bufferWidth);
        int n = 0;
        int width = cropped.l * -1 + cropped.r + 1;
        int len = width * (cropped.b * -1 + cropped.t + 1);
        Vector3[] croppedBuffer = new Vector3[len];
        
        for (int i = 0; i < len; ++i) {
            Point2 bc = GetCoord(start + n, bufferWidth);
            croppedBuffer[i] = new Vector3(bc.x, buffer[start + n], bc.y);
            n = (n + 1) % width;
            if (n == 0) {
                start += bufferWidth;
            }
        }

        for (int i=0; i<croppedBuffer.Length; ++i) {
            Point2 c = new Point2(
                coord.x + croppedBuffer[i].x - radius, 
                coord.y + croppedBuffer[i].z - radius);
            int j = GetIndex(c);
            mapData[j] = mapData[j] + (int)croppedBuffer[i].y * (int)mode;
            float pf = (float)mapData[j] / 255;
            SetVertexColors(meshes[j], new Color(pf, pf, pf));
        }
    }

    private void ColorizeEntities() {
        for (int i = 0; i < factories.Count; ++i) {
            if (factoryIndex[i] < 0) {
                continue;
            }
            SetVertexColors(meshes[factoryIndex[i]], Color.red);
        }

        for (int i = 0; i < forests.Count; ++i) {
            if (forestIndex[i] < 0) {
                continue;
            }
            SetVertexColors(meshes[forestIndex[i]], Color.green);
        }
    }

    private static int[] GetStampBuffer(int radius) {
        int line = 2 * radius + 1;
        return new int[line * line];
    }

    private static void DrawFalloff(int[] buffer, int radius, AnimationCurve falloff) {
        int line = 2 * radius + 1;
        
        for (int i = 0; i < buffer.Length; ++i) {
            int x = (i % line) - radius;
            int y = Mathf.FloorToInt((float)i / line) - radius;
            float v = Mathf.Clamp01((float)(x * x + y * y) / (radius * radius));
            buffer[i] = Mathf.RoundToInt(falloff.Evaluate(v) * 255);
        }
    }
    
    private static Point2 GetCoord(int i) {
        return new Point2(i % MAP_DIM, i >> 5);
    }
    
    private static Point2 GetCoord(int i, int width) {
        return new Point2(i % width, Mathf.FloorToInt((float)i / width));
    }

    private static Point2 SnapToMap(Vector3 coord) {
        return new Point2(Mathf.FloorToInt(coord.x), Mathf.FloorToInt(coord.z));
    }

    private static int GetIndex(Vector3 coord) {
        return Mathf.FloorToInt(coord.z) * MAP_DIM + Mathf.FloorToInt(coord.x);
    }

    private static int GetIndex(Point2 coord) {
        return Mathf.FloorToInt(coord.y) * MAP_DIM + Mathf.FloorToInt(coord.x);
    }

    private static int GetIndex(Point2 coord, int dim) {
        return Mathf.FloorToInt(coord.y) * dim + Mathf.FloorToInt(coord.x);
    }

    private static void SetVertexColors(Mesh mesh, Color col) {
        Color[] colors = new Color[mesh.vertices.Length];
        for (int i = 0; i < mesh.colors.Length; ++i) {
            colors[i] = col;
        }
        mesh.colors = colors;
    }
}
