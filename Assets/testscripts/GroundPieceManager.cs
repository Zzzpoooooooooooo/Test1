using System.Collections.Generic;
using UnityEngine;

public class GroundPieceManager : MonoBehaviour
{
    [Header("地面设置")]
    [SerializeField] private GameObject groundTilePrefab;
    [SerializeField] private float tileWidth = 1f;
    [SerializeField] private float tileLength = 1f;
    [SerializeField] private int groundPieceWidth = 20;
    [SerializeField] private int groundPieceLength = 20;
    [SerializeField] private float pieceLength = 20f;

    [Header("对象池")]
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private int initialPoolSize = 5;

    private Queue<GroundPieceData> activeGroundPieces = new Queue<GroundPieceData>();
    private List<GroundPieceData> allActiveGroundPieces = new List<GroundPieceData>();
    private Queue<GameObject> groundTilePool = new Queue<GameObject>();
    private float lastZPosition = 0f;

    // 公共属性
    public int GroundPieceWidth => groundPieceWidth;
    public int GroundPieceLength => groundPieceLength;
    public float TileWidth => tileWidth;
    public float TileLength => tileLength;
    public float PieceLength => pieceLength;

    public void Initialize()
    {
        if (useObjectPooling)
        {
            InitializeGroundTilePool();
        }
    }

    private void InitializeGroundTilePool()
    {
        int totalTiles = groundPieceWidth * groundPieceLength;
        int poolSize = initialPoolSize * totalTiles;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject tile = Instantiate(groundTilePrefab);
            tile.SetActive(false);
            tile.name = $"GroundTile_Pooled_{i}";
            groundTilePool.Enqueue(tile);
        }
    }

    public GroundPieceData CreateGroundPiece()
    {
        GroundPieceData groundData = new GroundPieceData
        {
            zPosition = lastZPosition,
            container = new GameObject($"GroundPiece_{Time.frameCount}"),
            pieceLength = this.pieceLength // 设置地块长度
        };

        groundData.container.transform.position = new Vector3(0, 0, lastZPosition);
        GenerateGroundTiles(groundData);

        activeGroundPieces.Enqueue(groundData);
        allActiveGroundPieces.Add(groundData);

        lastZPosition += pieceLength;
        return groundData;
    }

    private void GenerateGroundTiles(GroundPieceData groundData)
    {
        float startX = -groundPieceWidth * tileWidth / 2f;
        float startZ = -groundPieceLength * tileLength / 2f;

        for (int x = 0; x < groundPieceWidth; x++)
        {
            for (int z = 0; z < groundPieceLength; z++)
            {
                GameObject tile = GetGroundTile();
                if (tile == null) continue;

                Vector3 position = new Vector3(
                    startX + x * tileWidth + tileWidth / 2f,
                    0,
                    startZ + z * tileLength + tileLength / 2f
                ) + groundData.container.transform.position;

                tile.transform.position = position;
                tile.transform.rotation = Quaternion.identity;
                tile.transform.SetParent(groundData.container.transform);
                tile.SetActive(true);

                groundData.groundTiles.Add(tile);
            }
        }
    }

    public void DespawnOldestPiece()
    {
        if (activeGroundPieces.Count == 0) return;

        GroundPieceData piece = activeGroundPieces.Dequeue();
        DespawnPiece(piece);
        allActiveGroundPieces.Remove(piece);
    }

    private void DespawnPiece(GroundPieceData piece)
    {
        foreach (var tile in piece.groundTiles)
        {
            if (useObjectPooling)
                ReturnGroundTile(tile);
            else
                Destroy(tile);
        }

        // 销毁道路和物体
        if (piece.roadVisual != null)
            Destroy(piece.roadVisual);

        foreach (var obj in piece.spawnedObjects)
        {
            Destroy(obj);
        }

        Destroy(piece.container);
    }

    private GameObject GetGroundTile()
    {
        if (!useObjectPooling || groundTilePool.Count == 0)
            return Instantiate(groundTilePrefab);

        return groundTilePool.Dequeue();
    }

    private void ReturnGroundTile(GameObject tile)
    {
        tile.SetActive(false);
        tile.transform.SetParent(null);
        groundTilePool.Enqueue(tile);
    }

    // 检查方法
    public bool ShouldSpawnNewPiece(Vector3 currentPos, int drawDistance, float pieceLength)
    {
        if (allActiveGroundPieces.Count < drawDistance) return true;

        var lastPiece = allActiveGroundPieces[allActiveGroundPieces.Count - 1];
        float distance = lastPiece.zPosition - currentPos.z;
        return distance < pieceLength * 0.5f;
    }

    public bool ShouldDespawnOldPiece(Vector3 currentPos, float despawnDistance)
    {
        if (allActiveGroundPieces.Count == 0) return false;

        var firstPiece = allActiveGroundPieces[0];
        float distance = currentPos.z - firstPiece.zPosition;
        return distance > despawnDistance;
    }
}