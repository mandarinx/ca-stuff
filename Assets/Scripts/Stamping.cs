using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
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
        3, // factory
        1, // forest
    };
    
    private KeyCode[] keyCodes;
    private Action<Vector3>[] keyCallbacks;
    private string[] keyHelp;
    
    private Rectangle mapRect = new Rectangle(MAP_HALF_DIM, MAP_HALF_DIM, -MAP_HALF_DIM, -MAP_HALF_DIM);
    
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

        for (int i = 0; i < MAP_AREA; ++i) {
            SetVertexColors(meshes[i], Color.black);
        }

        ColorizeEntities();

        int radius = 0;
        for (int i = 0; i < keyCodes.Length; ++i) {
            if (!Input.GetKey(keyCodes[i])) {
                continue;
            }
            radius = entityRadius[i];
            break;
        }

        if (radius == 0) {
            return;
        }
        
        Point2 mouse = GetMousePoint(hit.point) - MAP_HALF_DIM;
        Rectangle blitRect = new Rectangle(radius, radius, -(radius - 1), -(radius - 1));
        Rectangle intr = Rectangle.GetOverlap(blitRect, mapRect, mouse);
        
        int x = Mathf.FloorToInt(hit.point.x);
        int z = Mathf.FloorToInt(hit.point.z);
        DrawRect(intr, new Vector3(x, 0f, z), Color.red);
        
        Point2 tl = new Point2(x + intr.l,     z + intr.t - 1);
        Point2 tr = new Point2(x + intr.r - 1, z + intr.t - 1);
        Point2 br = new Point2(x + intr.r - 1, z + intr.b);
        Point2 bl = new Point2(x + intr.l,     z + intr.b);
        SetVertexColors(meshes[GetIndex(tl)], Color.yellow);
        SetVertexColors(meshes[GetIndex(tr)], Color.yellow);
        SetVertexColors(meshes[GetIndex(bl)], Color.yellow);
        SetVertexColors(meshes[GetIndex(br)], Color.yellow);

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
    }

    private Vector3[] PrintValues(int[] values, Rectangle crop, Point2 cropCenter) {
        int rectWidth = cropCenter.x * 2;
        int start = GetIndex(new Point2(cropCenter.x + crop.l, cropCenter.y + crop.b), rectWidth);
        int n = start;
        int width = Mathf.Abs(crop.l) + crop.r;
        int len = width * (Mathf.Abs(crop.b) + crop.t);
        Vector3[] indexedValues = new Vector3[len];
        
        for (int i = 0; i < len; ++i) {
            Point2 coord = GetCoord(n, rectWidth);
            indexedValues[i] = new Vector3(coord.x, values[n], coord.y);
            ++n;
            if (i > 0 && i % width == 0) {
                n += width;
            }
        }

        return indexedValues;
    }

    private void OnDrawGizmos() {
        for (int i = 0; i < MAP_DIM; ++i) {
            Gizmos.DrawLine(new Vector3(i, 0, 0), new Vector3(i, 0, MAP_DIM));
        }
        for (int i = 0; i < MAP_DIM; ++i) {
            Gizmos.DrawLine(new Vector3(0, 0, i), new Vector3(MAP_DIM, 0, i));
        }
    }

    private void DrawRect(Rectangle r, Vector3 pos, Color col) {
        Vector3 tl = new Vector3(r.l, 0f, r.t) + pos;
        Vector3 tr = new Vector3(r.r, 0f, r.t) + pos;
        Vector3 br = new Vector3(r.r, 0f, r.b) + pos;
        Vector3 bl = new Vector3(r.l, 0f, r.b) + pos;
        Debug.DrawLine(tl, tr, col, Time.deltaTime);
        Debug.DrawLine(tr, br, col, Time.deltaTime);
        Debug.DrawLine(br, bl, col, Time.deltaTime);
        Debug.DrawLine(bl, tl, col, Time.deltaTime);
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

    private Point2 GetMousePoint(Vector3 pos) {
        return new Point2(
            Mathf.RoundToInt(pos.x) - 16, 
            Mathf.RoundToInt(pos.z) - 16);
    }

    private void HandleFactory(Vector3 coord) {
        int fi = GetIndex(coord);
        if (factoryIndex.IndexOf(fi) >= 0) {
            RemoveFactory(fi);
        } else {
            AddFactory(fi, new Vector3(coord.x, entityRadius[0], coord.z));
        }
    }

    private void HandleForest(Vector3 coord) {
        int fi = GetIndex(coord);
        if (forestIndex.IndexOf(fi) >= 0) {
            RemoveForest(fi);
        } else {
            AddForest(fi, new Vector3(coord.x, entityRadius[1], coord.z));
        }
    }
    
    private void RemoveForest(int f) {}

    private void AddForest(int f, Vector3 coord) {

        int radius = (int)coord.y;
        Rectangle intr = GetBlitRectangle(coord, radius);
        int[] buffer = GetStampBuffer(radius);
        BlitBuffer(buffer, radius, falloff);
        Point2 mouse = GetMousePoint(coord);

        forests.Add(radius);
        forestIndex.Add(GetIndex(coord));
        
//        Point2 tl = new Point2(x + intr.l,     z + intr.t - 1);
//        Point2 tr = new Point2(x + intr.r - 1, z + intr.t - 1);
//        Point2 br = new Point2(x + intr.r - 1, z + intr.b);
//        Point2 bl = new Point2(x + intr.l,     z + intr.b);
//        SetVertexColors(meshes[GetIndex(tl)], Color.yellow);
//        SetVertexColors(meshes[GetIndex(tr)], Color.yellow);
//        SetVertexColors(meshes[GetIndex(bl)], Color.yellow);
//        SetVertexColors(meshes[GetIndex(br)], Color.yellow);

        Vector3[] croppedBuffer = PrintValues(buffer, intr, new Point2(4, 4));
        for (int i=0; i<croppedBuffer.Length; ++i) {
            Point2 c = new Point2(croppedBuffer[i].x + mouse.x, croppedBuffer[i].z + mouse.y);
            int n = GetIndex(c);
            pollution[n] += (int)croppedBuffer[i].y;
        } 


//        int radius = (int) coord.y;
//        Rectangle bufferRect = new Rectangle(radius, radius, -radius, -radius);
//        Point2 mouse = GetMousePoint(coord);
//        Rectangle croppedRect = Rectangle.GetOverlap(bufferRect, mapRect, mouse);
//        int[] buffer = GetStampBuffer(radius);
        
//        forests.Add(radius);
//        forestIndex.Add(GetIndex(coord));
        
//        BlitBuffer(buffer, radius, falloff);


//        Point2 tl = new Point2(coord.x + croppedRect.l, coord.z + croppedRect.t);
//        Point2 tr = new Point2(coord.x + croppedRect.r, coord.z + croppedRect.t);
//        Point2 br = new Point2(coord.x + croppedRect.r, coord.z + croppedRect.b);
//        Point2 bl = new Point2(coord.x + croppedRect.l, coord.z + croppedRect.b);
//        SetVertexColors(meshes[GetIndex(tl)], Color.yellow);
//        SetVertexColors(meshes[GetIndex(tr)], Color.yellow);
//        SetVertexColors(meshes[GetIndex(bl)], Color.yellow);
//        SetVertexColors(meshes[GetIndex(br)], Color.yellow);
        
//        coord.x -= radius - 1;
//        coord.z -= radius - 1;
//        int start = GetIndex(coord);
//
//        int[] indexes = GetBufferIndexes(buffer, start, radius);
//
//        for (int i = 0; i < indexes.Length; ++i) {
//            int n = indexes[i];
//            pollution[n] -= buffer[i];
//            float pf = (float)pollution[n] / 255;
//            SetVertexColors(meshes[n], new Color(pf, pf, pf));
//        }
    }

    public void AddFactory(int f, Vector3 coord) {
        int radius = (int) coord.y;
        int[] buffer = GetStampBuffer(radius);
        
        factories.Add(radius);
        factoryIndex.Add(GetIndex(coord));

        BlitBuffer(buffer, radius, falloff);

        int startx = Mathf.RoundToInt(coord.x) - radius;
        int starty = Mathf.RoundToInt(coord.x) - radius;
        int endx = Mathf.RoundToInt(coord.z) + radius - 1;
        int endy = Mathf.RoundToInt(coord.z) + radius - 1;

        SetVertexColors(meshes[GetIndex(new Point2(startx, starty))], Color.magenta);
        SetVertexColors(meshes[GetIndex(new Point2(endx, endy))], Color.cyan);
        
//        Rectangle mapr = new Rectangle(
//            new Point2(0, 0), 
//            new Point2(MAP_DIM, MAP_DIM));
//        Rectangle stampr = new Rectangle(
//            new Point2(startx, starty), 
//            new Point2(endx, endy));
//        
//        Rectangle intersect = Rectangle.Intersect(stampr, mapr);
//        int start = GetIndex(intersect.bl);
//        int end = GetIndex(intersect.tr);
        
//        coord.x -= radius - 1;
//        coord.z -= radius - 1;
//
//        int endx = (int)coord.x + radius + radius - 1;
//        int endy = (int)coord.z + radius + radius - 1;
//        
//        if (coord.x < 0) {
//            coord.x = 0;
//        }
//        if (coord.z < 0) {
//            coord.z = 0;
//        }
//
//        if (endx >= MAP_DIM) {
//            endx = MAP_DIM - 1;
//        }
//        if (endy >= MAP_DIM) {
//            endy = MAP_DIM - 1;
//        }
//        
//        Point2 startp = new Point2(coord.x, coord.z);
//        Point2 endp = new Point2(endx, endy);
//        int start = GetIndex(startp);
//        int end = GetIndex(endp);
//
//        int[] indices = GetStampRect(startp, endp);
//        SetMapIndices(indices, new Point2(coord.x, coord.z), new Point2(endx, endy));
//
//        for (int i = 0; i < indices.Length; ++i) {
//            int n = indices[i];
//            pollution[n] += buffer[i];
//            float pf = (float)pollution[n] / 255;
//            SetVertexColors(meshes[n], new Color(pf, pf, pf));
//        }
        
//        SetVertexColors(meshes[end], Color.magenta);
//        SetVertexColors(meshes[start], Color.cyan);
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

    private Rectangle GetBlitRectangle(Vector3 hitPoint, int dim) {
        const float mapHalfDim = MAP_DIM * 0.5f;
        Rectangle blitRect = new Rectangle(dim, dim, -dim, -dim);
        Point2 mouse = new Point2(
            Mathf.FloorToInt(hitPoint.x) - mapHalfDim, 
            Mathf.FloorToInt(hitPoint.z) - mapHalfDim);
        return Rectangle.GetOverlap(blitRect, mapRect, mouse);
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

    private static int[] GetStampRect(Point2 start, Point2 end) {
        return new int[(end.x - start.x) * (end.y - start.y)];
    }

    private static void SetMapIndices(int[] indices, Point2 start, Point2 end) {
        int line = end.x - start.x;
        int mapstart = GetIndex(start);

        for (int i = 0; i < indices.Length; ++i) {
            if (i > 0 && i % line == 0) {
                mapstart += MAP_DIM;
            }
            int n = mapstart + (i % line);
            indices[i] = n;
        }
    }

    private static int[] GetBufferIndexes(int[] buffer, int start, int radius) {
        int line = 2 * radius - 1;
        int[] indexes = new int[line * line];

        for (int i = 0; i < buffer.Length; ++i) {
            if (i > 0 && i % line == 0) {
                start += MAP_DIM;
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
        return new Point2(i % MAP_DIM, i >> 5);
    }
    
    private static Point2 GetCoord(int i, int width) {
        return new Point2(i % MAP_DIM, Mathf.FloorToInt((float)i / width));
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
