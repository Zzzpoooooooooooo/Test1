using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("主要设置")]
    [SerializeField] private Transform referenceTransform;
    [SerializeField] private int drawDistance = 3;
    [SerializeField] private float despawnDistance = 50f;
    [SerializeField] private float pieceLength = 10f;

    [Header("管理器引用")]
    [SerializeField] private GroundPieceManager groundManager;
    [SerializeField] private RoadManager roadManager;
    [SerializeField] private ObjectSpawner objectSpawner;

    [Header("道路限制")]
    [SerializeField] private float roadWidth = 5f; // 可在这里调整道路宽度
    [SerializeField] private bool showRoadBoundary = true; // 是否显示边界

    private Vector3 lastReferencePosition;
    private bool isInitialized = false;

    // 公共属性
    public float RoadWidth => roadWidth;

    private void Start()
    {
        if (!isInitialized)
            InitializeLevel();
    }

    private void InitializeLevel()
    {
        ValidateReferences();

        if (referenceTransform == null)
        {
            referenceTransform = transform;
            Debug.LogWarning("LevelController: Using self as reference transform.");
        }

        lastReferencePosition = referenceTransform.position;

        // 初始化地面
        groundManager?.Initialize();

        // 生成起始地块
        for (int i = 0; i < drawDistance; i++)
        {
            SpawnNewGroundPiece();
        }

        isInitialized = true;

        Debug.Log($"LevelController初始化完成，道路宽度: {roadWidth}");
    }

    private void Update()
    {
        if (!isInitialized || referenceTransform == null) return;

        Vector3 currentPos = referenceTransform.position;
        UpdateDistanceBased(currentPos);
        lastReferencePosition = currentPos;
    }

    private void UpdateDistanceBased(Vector3 currentPos)
    {
        if (groundManager == null) return;

        // 简单的距离检查
        if (groundManager.ShouldSpawnNewPiece(currentPos, drawDistance, pieceLength) ||
            groundManager.ShouldDespawnOldPiece(currentPos, despawnDistance))
        {
            groundManager.DespawnOldestPiece();
            SpawnNewGroundPiece();
        }
    }

    private void SpawnNewGroundPiece()
    {
        if (groundManager == null) return;

        GroundPieceData groundData = groundManager.CreateGroundPiece();

        if (groundData == null) return;

        // 生成道路
        roadManager?.GenerateRoad(groundData);

        // 生成物体
        objectSpawner?.GenerateObjects(groundData);
    }

    private void ValidateReferences()
    {
        if (groundManager == null)
        {
            groundManager = GetComponent<GroundPieceManager>();
            if (groundManager == null)
            {
                Debug.LogError("LevelController: GroundPieceManager is required!");
                enabled = false;
            }
        }

        // 检查其他管理器
        if (roadManager == null)
            roadManager = GetComponent<RoadManager>();

        if (objectSpawner == null)
            objectSpawner = GetComponent<ObjectSpawner>();
    }

    // ===== 道路限制相关方法 =====

    // 检查位置是否在道路边界内
    public bool IsPositionInsideRoad(Vector3 position)
    {
        float halfWidth = roadWidth / 2f;
        return Mathf.Abs(position.x) <= halfWidth;
    }

    // 将位置限制在道路边界内
    public Vector3 ClampToRoad(Vector3 position)
    {
        float halfWidth = roadWidth / 2f;
        float clampedX = Mathf.Clamp(position.x, -halfWidth, halfWidth);
        return new Vector3(clampedX, position.y, position.z);
    }

    // 计算当前位置离道路边界的距离
    public float GetDistanceToBoundary(Vector3 position)
    {
        float halfWidth = roadWidth / 2f;
        float distanceToRight = halfWidth - position.x;  // 到右边界的距离
        float distanceToLeft = position.x - (-halfWidth); // 到左边界的距离

        // 返回较小的那个距离（离哪个边界近）
        return Mathf.Min(distanceToRight, distanceToLeft);
    }

    // 获取最近的道路边界点
    public Vector3 GetNearestBoundaryPoint(Vector3 position)
    {
        float halfWidth = roadWidth / 2f;

        // 如果位置在道路内，不需要调整
        if (IsPositionInsideRoad(position))
            return position;

        // 超出右侧边界
        if (position.x > halfWidth)
            return new Vector3(halfWidth, position.y, position.z);

        // 超出左侧边界
        return new Vector3(-halfWidth, position.y, position.z);
    }

    public void SetDrawDistance(int distance)
    {
        drawDistance = Mathf.Max(2, distance);
    }

    // 设置道路宽度
    public void SetRoadWidth(float width)
    {
        roadWidth = Mathf.Max(1f, width);
        Debug.Log($"道路宽度设置为: {roadWidth}");
    }

    // 可视化道路边界
    private void OnDrawGizmos()
    {
        if (!showRoadBoundary) return;

        float halfWidth = roadWidth / 2f;

        // 绘制道路边界
        Gizmos.color = Color.green;

        // 绘制一条沿Z轴的线表示道路边界
        float drawLength = 50f; // 绘制的长度

        // 左侧边界
        Vector3 leftStart = new Vector3(-halfWidth, 0.5f, -drawLength / 2);
        Vector3 leftEnd = new Vector3(-halfWidth, 0.5f, drawLength / 2);
        Gizmos.DrawLine(leftStart, leftEnd);

        // 右侧边界
        Vector3 rightStart = new Vector3(halfWidth, 0.5f, -drawLength / 2);
        Vector3 rightEnd = new Vector3(halfWidth, 0.5f, drawLength / 2);
        Gizmos.DrawLine(rightStart, rightEnd);

        // 绘制道路区域（半透明）
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Vector3 roadCenter = new Vector3(0, 0.25f, 0);
        Vector3 roadSize = new Vector3(roadWidth, 0.5f, drawLength);
        Gizmos.DrawCube(roadCenter, roadSize);

        // 显示道路宽度信息
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
            $"道路宽度: {roadWidth}\n当前限制: 启用");
#endif
    }
}