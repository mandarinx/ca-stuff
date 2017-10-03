using System;
using UnityEngine;

public class EntityManager {

    private readonly uint[] entities;
    private readonly int[] data;

    public EntityManager(int mapSize) {
        entities = new uint[mapSize];
        data = new int[mapSize];
    }

    public int numEntities {
        get { return entities.Length; }
    }

    public uint GetEntity(int index) {
        if (index >= entities.Length) {
            Debug.LogError("index is outside the bounds of the entity map");
            return 0;
        }
        return entities[index];
    }

    public int GetData(int index) {
        if (index >= data.Length) {
            Debug.LogError("index is outside the bounds of the data map");
            return -1;
        }
        return data[index];
    }

    public int GetData(uint entity) {
        int i = Array.IndexOf(entities, entity);
        if (i < 0) {
            Debug.LogError("Could not find entity "+entity);
            return -1;
        }
        return data[i];
    }

    public void SetEntity(int index, uint entity, int value) {
        entities[index] = entity;
        data[index] = value;
    }
}
