using System;
using System.Collections.Generic;
using UnityEngine;
using Mandarin;

[Serializable]
public struct DataEntity {
    public string name;
    public string dataLayer;
    public uint id;
    public int radius;
    public Color color;
    public StampMode add;
    public StampMode remove;
}

public enum StampMode {
    ADD = 1,
    SUB = -1,
}

public class Stamping : MonoBehaviour {
    
    private const int MAP_DIM = 32;
    private const int MAP_HALF_DIM = (int)(MAP_DIM * 0.5f);
    private const int MAP_AREA = MAP_DIM * MAP_DIM;

    [SerializeField]
    private Material         mat;
    [SerializeField]
    private AnimationCurve   falloff;
    [SerializeField]
    private KeyCode[]        keyCodes;
    [SerializeField]
    private DataEntity[]     dataEntities;

    private DataManager                       data;
    private EntityManager                     entities;
    private Dictionary<uint, int>             entityMap;
    private List<string>                      dirtyLayers;
    private Dictionary<string, Action<int[]>> layerHandlers;
    private LayerViz[]                        layerVizes;
    private string[]                          layerVizIndex;
    private LayerViz                          entityViz;
    
    private readonly Rectangle   mapRect = new Rectangle(
        MAP_HALF_DIM - 1, 
        MAP_HALF_DIM - 1, 
        -MAP_HALF_DIM,
        -MAP_HALF_DIM
    );
    
    private void Awake() {
        data = new DataManager(MAP_AREA);
        data.AddLayer("pollution");
        data.AddLayer("land_value");
        
        layerVizes = new [] {
            new LayerViz("pollution",  new Vector3(MAP_DIM + 2, 0, 0),                  MAP_AREA, mat),
            new LayerViz("land_value", new Vector3(MAP_DIM + 2, 0, (MAP_DIM + 2) * -1), MAP_AREA, mat),
        };
        layerVizIndex = new[] {"pollution", "land_value"};
        
        entities = new EntityManager(MAP_AREA);
        entityViz = new LayerViz("entities", Vector3.zero, MAP_AREA, mat);
        
        entityMap = new Dictionary<uint, int>();
        for (int i = 0; i < dataEntities.Length; ++i) {
            entityMap.Add(dataEntities[i].id, i);
        }
        
        dirtyLayers = new List<string>();
        layerHandlers = new Dictionary<string, Action<int[]>> {
            { "pollution",  OnPollutionUpdated },
            { "land_value", OnLandValueUpdated }
        };
        
        for (int i = 0; i < layerVizIndex.Length; ++i) {
            Action<int[]> handler = layerHandlers[layerVizIndex[i]];
            handler(data.GetLayer(layerVizIndex[i]));
        }

        DrawLayers();
    }

    private void Update() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit)) {
            return;
        }
        
        if (!Input.GetMouseButtonUp(0)) {
            return;
        }

        for (int i = 0; i < keyCodes.Length; ++i) {
            if (!Input.GetKey(keyCodes[i])) {
                continue;
            }
            
            int n = GetIndex(hit.point);
            DataEntity entity = dataEntities[i];
            
            uint eID = entities.GetEntity(n);
            StampMode mode = entity.add;
            int radius = entity.radius;
            uint newEID = entity.id;
            int newVal = radius;
            if (eID > 0) {
                mode = entity.remove;
                radius = entities.GetData(eID);
                newEID = 0;
                newVal = 0;
            }
            entities.SetEntity(n, newEID, newVal);
            StampBlobToMap(radius, SnapToMap(hit.point), entity.dataLayer, mode);
            dirtyLayers.Add(entity.dataLayer);
            break;
        }

        for (int i = 0; i < dirtyLayers.Count; ++i) {
            Action<int[]> handler = layerHandlers[dirtyLayers[i]];
            int[] layer = data.GetLayer(dirtyLayers[i]);
            handler(layer);
            int v = Array.IndexOf(layerVizIndex, dirtyLayers[i]);
            layerVizes[v].Colorize(layer);
        }
        
        dirtyLayers.Clear();
        DrawLayers();
        
        for (int i = 0; i < entities.numEntities; ++i) {
            uint id = entities.GetEntity(i);
            if (id == 0) {
                entityViz.Colorize(i, Color.black);
                continue;
            }
            DataEntity de = dataEntities[entityMap[id]];
            entityViz.Colorize(i, de.color);
        }
    }

    private void DrawLayers() {
        for (int i = 0; i < layerVizes.Length; ++i) {
            int[] layer = data.GetLayer(layerVizes[i].name);
            layerVizes[i].Colorize(layer);
        }
    }

//    private void OnDrawGizmos() {
//        for (int i = 0; i < MAP_DIM + 1; ++i) {
//            Gizmos.DrawLine(new Vector3(i, 0, 0), new Vector3(i, 0, MAP_DIM));
//        }
//        for (int i = 0; i < MAP_DIM + 1; ++i) {
//            Gizmos.DrawLine(new Vector3(0, 0, i), new Vector3(MAP_DIM, 0, i));
//        }
//    }

    private void OnGUI() {
        float height = keyCodes.Length * 20f;
        string help = "";
        for (int i = 0; i < keyCodes.Length; ++i) {
            help += keyCodes[i] + " : " + dataEntities[i].name + "\n";
        }
        GUI.Label(new Rect(10, Screen.height - height, 300, height), help);
    }

    private void OnPollutionUpdated(int[] pollution) {
        int[] landValue = data.GetLayer("land_value");
        
        for (int i = 0; i < pollution.Length; ++i) {
            landValue[i] = 255 - pollution[i];
        }
    }
    
    private void OnLandValueUpdated(int[] land_value) {}
    
    private void StampBlobToMap(int radius, Point2 coord, string dataLayer, StampMode mode) {
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
        int[] mapData = data.GetLayer(dataLayer);
        
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
    
    public static Point2 GetCoord(int i) {
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
}
