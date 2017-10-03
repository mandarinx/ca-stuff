using System;
using UnityEngine;
using System.Collections.Generic;

public class DataManager {

    private readonly int                       layerSize;
    private readonly Dictionary<string, int[]> layers;
    private readonly List<string>              layerNames;

    public DataManager(int layerSize) {
        layers = new Dictionary<string, int[]>();
        layerNames = new List<string>();
        this.layerSize = layerSize;
    }

    public void AddLayer(string name) {
        if (layers.ContainsKey(name)) {
            Debug.LogWarning("A layer with name "+name+" already exists");
            return;
        }
        layers.Add(name, new int[layerSize]);
        layerNames.Add(name);
    }

    public void RemoveLayer(string name) {
        layers.Remove(name);
        layerNames.Remove(name);
    }

    public int[] GetLayer(string name) {
        if (!layers.ContainsKey(name)) {
            Debug.LogError("A layer with name "+name+" could not be found");
            return null;
        }
        return layers[name];
    }

    public void SetValue(string name, int index, int value) {
        if (!layers.ContainsKey(name)) {
            Debug.LogError("A layer with name "+name+" could not be found");
            return;
        }
        int[] layer = layers[name];
        if (index >= layer.Length) {
            Debug.LogError("index is outside the boundaries of the layer");
            return;
        }
        layer[index] = value;
    }

    public int GetValue(string name, int index) {
        if (!layers.ContainsKey(name)) {
            Debug.LogError("A layer with name "+name+" could not be found");
            return -1;
        }
        int[] layer = layers[name];
        if (index >= layer.Length) {
            Debug.LogError("index is outside the boundaries of the layer");
            return -1;
        }
        return layer[index];
    }
}
