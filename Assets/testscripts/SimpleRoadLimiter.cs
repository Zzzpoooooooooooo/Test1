using UnityEngine;

public class SimpleRoadLimiter : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 8f;           // 移动速度
    [SerializeField] private float boundaryForce = 15f;      // 边界排斥力
    [SerializeField] private float smoothFactor = 0.3f;      // 平滑系数（0-1，越小越平滑）

    [Header("玩家视觉")]
    [SerializeField] private GameObject playerVisualPrefab; // 玩家的视觉模型（可选）
    [SerializeField] private Transform visualParent;        // 视觉模型的父对象

    [Header("引用")]
    [SerializeField] private LevelController levelController;

    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool enableBoundary = true;

    private GameObject playerVisual;
    private Vector3 targetPosition;
    private bool isInitialized = false;

    private void Start()
    {
        InitializePlayer();
    }

    private void InitializePlayer()
    {
        // 获取LevelController引用
        if (levelController == null)
        {
            levelController = FindObjectOfType<LevelController>();
            if (levelController == null)
            {
                Debug.LogError("找不到LevelController！");
                return;
            }
        }

        // 创建玩家视觉（如果有预设）
        if (playerVisualPrefab != null && playerVisual == null)
        {
            Transform parent = visualParent != null ? visualParent : transform;
            playerVisual = Instantiate(playerVisualPrefab, parent);
            playerVisual.transform.localPosition = Vector3.zero;
            Debug.Log("创建玩家视觉");
        }

        // 设置初始位置
        targetPosition = transform.position;
        isInitialized = true;

        Debug.Log($"玩家初始化完成，移动速度: {moveSpeed}, 边界排斥力: {boundaryForce}");
    }

    private void Update()
    {
        if (!isInitialized) return;

        HandleInput();
        ApplyBoundaryForce();
        SmoothMovement();
        UpdateVisual();
    }

    private void HandleInput()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 计算移动方向
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (moveDirection.magnitude > 0.1f)
        {
            // 应用摄像机方向
            if (Camera.main != null)
            {
                Vector3 cameraForward = Camera.main.transform.forward;
                Vector3 cameraRight = Camera.main.transform.right;

                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                moveDirection = cameraForward * vertical + cameraRight * horizontal;
            }

            // 应用移动
            targetPosition += moveDirection * moveSpeed * Time.deltaTime;

            // 旋转朝向移动方向
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
            }
        }
    }

    private void ApplyBoundaryForce()
    {
        if (!enableBoundary || levelController == null) return;

        float roadWidth = levelController.RoadWidth;
        float halfWidth = roadWidth / 2f;

        Vector3 currentPos = targetPosition;

        // 检查是否超出边界
        if (Mathf.Abs(currentPos.x) > halfWidth)
        {
            // 计算排斥力：越靠近边界，力越大
            float distanceToBoundary = Mathf.Abs(Mathf.Abs(currentPos.x) - halfWidth);
            float pushStrength = boundaryForce * (1f + distanceToBoundary) * Time.deltaTime;

            // 计算推力方向（总是朝向道路中心）
            float pushDirection = currentPos.x > 0 ? -1 : 1;

            // 应用推力
            targetPosition += new Vector3(pushStrength * pushDirection, 0, 0);

            // 确保不会超出边界太多
            float clampedX = Mathf.Clamp(targetPosition.x, -halfWidth, halfWidth);
            targetPosition = new Vector3(clampedX, targetPosition.y, targetPosition.z);

            if (showDebugInfo)
            {
                Debug.Log($"边界推力: {pushStrength:F2}, 方向: {pushDirection}");
            }
        }
    }

    private void SmoothMovement()
    {
        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothFactor);

        // 直接设置位置（如果想要更直接的控制）
        // transform.position = targetPosition;
    }

    private void UpdateVisual()
    {
        // 更新视觉对象的位置和旋转
        if (playerVisual != null)
        {
            playerVisual.transform.position = transform.position;
            playerVisual.transform.rotation = transform.rotation;
        }
    }

    // 公共方法：设置移动速度
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(1f, speed);
    }

    // 公共方法：切换边界限制
    public void ToggleBoundary(bool enable)
    {
        enableBoundary = enable;
        Debug.Log($"边界限制: {(enable ? "启用" : "禁用")}");
    }

    // 公共方法：立即将玩家限制到道路内
    public void SnapToRoad()
    {
        if (levelController != null)
        {
            targetPosition = levelController.ClampToRoad(targetPosition);
            transform.position = targetPosition;
            Debug.Log("玩家已限制到道路内");
        }
    }

    // 可视化调试
    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // 绘制玩家位置
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.3f);

        // 绘制移动方向
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetPosition);

            // 绘制目标位置
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }

        // 绘制道路边界信息
        if (levelController != null)
        {
            float roadWidth = levelController.RoadWidth;
            float halfWidth = roadWidth / 2f;

            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(
                new Vector3(0, transform.position.y, transform.position.z),
                new Vector3(roadWidth, 0.1f, 2f)
            );

            // 显示距离信息
#if UNITY_EDITOR
            float distanceToCenter = Mathf.Abs(transform.position.x);
            float distanceToBoundary = halfWidth - distanceToCenter;

            string info = $"玩家位置: {transform.position.x:F1}\n" +
                         $"道路宽度: {roadWidth:F1}\n" +
                         $"距边界: {distanceToBoundary:F1}\n" +
                         $"边界限制: {(enableBoundary ? "开" : "关")}";

            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, info);
#endif
        }
    }
}