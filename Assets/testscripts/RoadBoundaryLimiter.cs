using UnityEngine;

public class RoadBoundaryLimiter : MonoBehaviour
{
    [Header("道路限制设置")]
    [SerializeField] private float roadWidth = 5f;          // 道路宽度（应该和LevelController一致）
    [SerializeField] private float boundaryForce = 20f;     // 边界排斥力
    [SerializeField] private float boundaryMargin = 0.3f;   // 边界边距
    [SerializeField] private bool enableLimiter = true;     // 是否启用限制器

    [Header("引用")]
    [SerializeField] private LevelController levelController;
    [SerializeField] private CharacterController characterController;

    [Header("调试")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logBoundaryHits = false;

    private float lastBoundaryHitTime;
    private Vector3 lastValidPosition;

    private void Start()
    {
        // 自动获取引用
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        lastValidPosition = transform.position;

        if (levelController != null)
        {
            // 使用LevelController的道路宽度设置
            roadWidth = levelController.RoadWidth;
            Debug.Log($"道路限制器初始化，道路宽度: {roadWidth}");
        }
        else
        {
            Debug.LogWarning("未找到LevelController，使用默认道路宽度");
        }
    }

    private void Update()
    {
        if (!enableLimiter) return;

        ApplyRoadBoundary();
    }

    private void ApplyRoadBoundary()
    {
        if (characterController == null) return;

        float halfWidth = roadWidth / 2f;
        float marginHalfWidth = halfWidth - boundaryMargin;

        Vector3 currentPos = transform.position;

        // 检查是否超出边界（带边距）
        if (Mathf.Abs(currentPos.x) > marginHalfWidth)
        {
            // 计算排斥力：越靠近边界，力越大
            float distanceBeyond = Mathf.Abs(currentPos.x) - marginHalfWidth;
            float pushStrength = boundaryForce * (1f + distanceBeyond) * Time.deltaTime;

            // 计算推力方向（总是朝向道路中心）
            float pushDirection = currentPos.x > 0 ? -1 : 1;

            // 如果是CharacterController，需要特殊处理
            Vector3 pushVector = new Vector3(pushStrength * pushDirection, 0, 0);

            // 应用推力
            characterController.Move(pushVector);

            // 记录日志
            if (logBoundaryHits && Time.time - lastBoundaryHitTime > 0.5f)
            {
                Debug.Log($"道路边界排斥：推力 {pushStrength:F2}, 方向 {pushDirection}");
                lastBoundaryHitTime = Time.time;
            }

            // 确保不会超出硬边界
            Vector3 newPos = transform.position;
            if (Mathf.Abs(newPos.x) > halfWidth)
            {
                float clampedX = Mathf.Clamp(newPos.x, -halfWidth, halfWidth);

                // 对于CharacterController，需要暂时禁用来设置位置
                characterController.enabled = false;
                transform.position = new Vector3(clampedX, newPos.y, newPos.z);
                characterController.enabled = true;
            }
        }

        // 更新最后有效位置
        if (Mathf.Abs(currentPos.x) <= halfWidth)
        {
            lastValidPosition = currentPos;
        }
    }

    // 公共方法：强制将玩家限制到道路内
    public void SnapToRoad()
    {
        if (characterController == null) return;

        float halfWidth = roadWidth / 2f;
        Vector3 currentPos = transform.position;

        if (Mathf.Abs(currentPos.x) > halfWidth)
        {
            float clampedX = Mathf.Clamp(currentPos.x, -halfWidth, halfWidth);

            characterController.enabled = false;
            transform.position = new Vector3(clampedX, currentPos.y, currentPos.z);
            characterController.enabled = true;

            Debug.Log($"已将玩家限制到道路内: {currentPos.x:F2} -> {clampedX:F2}");
        }
    }

    // 检查当前位置是否在道路上
    public bool IsInsideRoad()
    {
        float halfWidth = roadWidth / 2f;
        return Mathf.Abs(transform.position.x) <= halfWidth;
    }

    // 获取到最近边界的距离（正值表示在道路内，负值表示超出）
    public float GetDistanceToNearestBoundary()
    {
        float halfWidth = roadWidth / 2f;
        float distanceToRight = halfWidth - transform.position.x;
        float distanceToLeft = transform.position.x - (-halfWidth);

        // 返回较小的距离
        return Mathf.Min(distanceToRight, distanceToLeft);
    }

    // 设置道路宽度
    public void SetRoadWidth(float width)
    {
        roadWidth = Mathf.Max(1f, width);
    }

    // 切换限制器
    public void SetLimiterEnabled(bool enabled)
    {
        enableLimiter = enabled;
        Debug.Log($"道路限制器: {(enabled ? "启用" : "禁用")}");
    }

    // 可视化调试
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        float halfWidth = roadWidth / 2f;
        float marginHalfWidth = halfWidth - boundaryMargin;
        Vector3 pos = transform.position;

        // 绘制硬边界（红色）
        Gizmos.color = Color.red;
        Vector3 hardLeft = new Vector3(-halfWidth, pos.y + 1f, pos.z);
        Vector3 hardRight = new Vector3(halfWidth, pos.y + 1f, pos.z);
        Gizmos.DrawLine(hardLeft, hardRight);
        Gizmos.DrawSphere(hardLeft, 0.15f);
        Gizmos.DrawSphere(hardRight, 0.15f);

        // 绘制软边界（黄色）- 排斥力开始生效的地方
        Gizmos.color = Color.yellow;
        Vector3 softLeft = new Vector3(-marginHalfWidth, pos.y + 0.8f, pos.z);
        Vector3 softRight = new Vector3(marginHalfWidth, pos.y + 0.8f, pos.z);
        Gizmos.DrawLine(softLeft, softRight);

        // 绘制道路区域（半透明绿色）
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Vector3 roadCenter = new Vector3(0, pos.y + 0.5f, pos.z);
        Vector3 roadSize = new Vector3(roadWidth, 1f, 5f);
        Gizmos.DrawCube(roadCenter, roadSize);

        // 绘制当前位置指示器
        Gizmos.color = IsInsideRoad() ? Color.green : Color.red;
        Gizmos.DrawLine(
            new Vector3(pos.x, pos.y, pos.z - 2f),
            new Vector3(pos.x, pos.y, pos.z + 2f)
        );

        // 显示距离信息
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            float distance = GetDistanceToNearestBoundary();
            string status = IsInsideRoad() ? "在道路内" : "超出道路";
            string info = $"道路宽度: {roadWidth:F1}\n" +
                         $"当前位置: X={pos.x:F2}\n" +
                         $"距边界: {distance:F2}\n" +
                         $"状态: {status}\n" +
                         $"限制器: {(enableLimiter ? "开" : "关")}";

            UnityEditor.Handles.Label(pos + Vector3.up * 3, info);
        }
#endif
    }
}