using UnityEngine;
using Mandarin;

public class LayerViz {
    
    public string name { get; private set; }
    
    private readonly Transform parent;
    private readonly Mesh[]    meshes;
    
    public LayerViz(string name, Vector3 pos, int size, Material mat) {
        this.name = name;
        meshes = new Mesh[size];
        parent = new GameObject(name).transform;

        GameObject prefab = new GameObject();
        Mesh plane = MeshUtils.CreatePlane(1, 1, 1, 1);
        prefab.AddComponent<MeshFilter>().sharedMesh = plane;
        prefab.AddComponent<MeshRenderer>().sharedMaterial = mat;
        prefab.AddComponent<BoxCollider>().size = Vector3.forward + Vector3.right;

        for (int i = 0; i < size; ++i) {
            GameObject t = Object.Instantiate(prefab);
            t.transform.localPosition = Stamping.GetCoord(i).ToVector3XZ();
            t.transform.SetParent(parent);
            t.name = Stamping.GetCoord(i).x + "_" + Stamping.GetCoord(i).y;
            meshes[i] = parent.GetChild(i).GetComponent<MeshFilter>().mesh;
        
            SetVertexColors(meshes[i], Color.black);
        }
        
        parent.position = pos;
        Object.Destroy(prefab);
    }

    public void Colorize(int[] data) {
        for (int i = 0; i < data.Length; ++i) {
            float pf = (float)data[i] / 255;
            SetVertexColors(meshes[i], new Color(pf, pf, pf));
        }
    }

    public void Colorize(int index, Color color) {
        SetVertexColors(meshes[index], color);
    }
    
    private static void SetVertexColors(Mesh mesh, Color col) {
        Color[] colors = new Color[mesh.vertices.Length];
        for (int i = 0; i < mesh.colors.Length; ++i) {
            colors[i] = col;
        }
        mesh.colors = colors;
    }
}
