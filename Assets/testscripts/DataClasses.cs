using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnableObject
{
    public string name;
    public GameObject prefab;
    [Range(1, 100)] public int probability = 1;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public bool canSpawnOnRoad = false;
}

public class GroundPieceData
{
    public GameObject container;
    public List<GameObject> groundTiles = new List<GameObject>();
    public List<GameObject> spawnedObjects = new List<GameObject>();
    public float zPosition;
    public GameObject roadVisual;
    public float pieceLength = 20f; // 添加地块长度字段
}