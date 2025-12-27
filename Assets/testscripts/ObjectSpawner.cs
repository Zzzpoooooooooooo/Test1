using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private bool generateObjects = true;
    [SerializeField] private SpawnableObject[] spawnableObjects;
    [SerializeField] private int minObjects = 5;
    [SerializeField] private int maxObjects = 15;
    [SerializeField] private float density = 0.3f;

    [Header("引用")]
    [SerializeField] private RoadManager roadManager;
    [SerializeField] private GroundPieceManager groundManager; // 添加这个引用

    private Dictionary<int, Queue<GameObject>> objectPools = new Dictionary<int, Queue<GameObject>>();

    private void Start()
    {
        // 尝试自动获取引用
        if (groundManager == null)
            groundManager = GetComponent<GroundPieceManager>();

        if (roadManager == null)
            roadManager = GetComponent<RoadManager>();
    }

    public void GenerateObjects(GroundPieceData groundData)
    {
        if (!generateObjects || spawnableObjects.Length == 0) return;

        int count = Random.Range(minObjects, maxObjects + 1);
        count = Mathf.FloorToInt(count * density);

        for (int i = 0; i < count; i++)
        {
            var objData = GetRandomObjectData();
            GameObject obj = GetOrCreateObject(objData.index);

            Vector3 position = GetRandomPosition(groundData);

            // 检查是否在道路上
            if (!objData.data.canSpawnOnRoad && roadManager != null && roadManager.IsPositionOnRoad(position))
                continue;

            SetupObject(obj, objData.data, position);
            obj.transform.SetParent(groundData.container.transform);
            groundData.spawnedObjects.Add(obj);
        }
    }

    private Vector3 GetRandomPosition(GroundPieceData groundData)
    {
        float halfWidth = 10f; // 默认值
        float halfLength = 10f; // 默认值

        if (groundManager != null)
        {
            // 使用GroundPieceManager的属性计算
            halfWidth = groundManager.GroundPieceWidth * groundManager.TileWidth / 2f;
            halfLength = groundManager.GroundPieceLength * groundManager.TileLength / 2f;
        }
        else
        {
            // 使用groundData中的pieceLength作为后备
            halfWidth = groundData.pieceLength * 0.5f;
            halfLength = groundData.pieceLength * 0.5f;
        }

        return groundData.container.transform.position + new Vector3(
            Random.Range(-halfWidth, halfWidth),
            0,
            Random.Range(-halfLength, halfLength)
        );
    }

    private void SetupObject(GameObject obj, SpawnableObject data, Vector3 position)
    {
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        obj.transform.localScale = Vector3.one * Random.Range(data.minScale, data.maxScale);
        obj.SetActive(true);
    }

    private (int index, SpawnableObject data) GetRandomObjectData()
    {
        List<int> probabilityList = BuildProbabilityList();
        int index = probabilityList[Random.Range(0, probabilityList.Count)];
        return (index, spawnableObjects[index]);
    }

    private List<int> BuildProbabilityList()
    {
        List<int> list = new List<int>();
        for (int i = 0; i < spawnableObjects.Length; i++)
        {
            for (int j = 0; j < spawnableObjects[i].probability; j++)
                list.Add(i);
        }
        return list;
    }

    private GameObject GetOrCreateObject(int index)
    {
        // 简化：直接实例化
        return Instantiate(spawnableObjects[index].prefab);
    }

    public void SetObjectGeneration(bool enable)
    {
        generateObjects = enable;
    }
}