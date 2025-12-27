using UnityEngine;

public class PlayerMovementLimiter : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("引用")]
    [SerializeField] private LevelController levelController;

    private Camera mainCamera;
    private Vector3 lastValidPosition;

    private void Start()
    {
        mainCamera = Camera.main;

        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        lastValidPosition = transform.position;

        if (levelController == null)
        {
            Debug.LogError("找不到LevelController！");
        }
        else
        {
            Debug.Log($"找到LevelController，道路宽度: {levelController.RoadWidth}");
        }
    }

    private void Update()
    {
        HandleMovement();
        ConstrainToRoad();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical).normalized;

        if (movement.magnitude > 0.1f)
        {
            Vector3 moveDirection = movement;

            if (mainCamera != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                moveDirection = cameraForward * vertical + cameraRight * horizontal;
            }

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    rotationSpeed * Time.deltaTime);
            }

            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void ConstrainToRoad()
    {
        if (levelController == null) return;

        // 修改：使用RoadWidth属性而不是GetRoadWidth()方法
        float roadWidth = levelController.RoadWidth;  // ← 这里改了
        float halfWidth = roadWidth / 2f;

        // 简单X轴限制：将玩家X坐标限制在道路宽度内
        Vector3 currentPos = transform.position;
        float clampedX = Mathf.Clamp(currentPos.x, -halfWidth, halfWidth);

        if (Mathf.Abs(currentPos.x - clampedX) > 0.01f)
        {
            transform.position = new Vector3(clampedX, currentPos.y, currentPos.z);
            Debug.Log($"玩家超出边界，位置已修正: {currentPos.x:F2} -> {clampedX:F2}");
        }

        lastValidPosition = transform.position;
    }

    // 可选：检查玩家是否在道路上（使用射线检测）
    private bool IsPlayerOnRoad()
    {
        // 向下发射射线
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f))
        {
            // 检查是否击中"Road"标签的对象
            if (hit.collider.CompareTag("Road"))
            {
                return true;
            }
        }

        return false;
    }

    // 公共方法：获取当前是否在道路上
    public bool IsOnRoad()
    {
        if (levelController == null) return false;
        return levelController.IsPositionInsideRoad(transform.position);
    }

    // 公共方法：强制限制到道路内
    public void SnapToRoadCenter()
    {
        if (levelController == null) return;

        Vector3 currentPos = transform.position;
        Vector3 newPos = new Vector3(0, currentPos.y, currentPos.z);
        transform.position = newPos;
        lastValidPosition = newPos;

        Debug.Log("玩家已移动到道路中心");
    }

    // 可视化调试
    private void OnDrawGizmos()
    {
        if (levelController != null)
        {
            // 修改：使用RoadWidth属性而不是GetRoadWidth()方法
            float roadWidth = levelController.RoadWidth;  // ← 这里改了
            float halfWidth = roadWidth / 2f;

            Gizmos.color = Color.green;

            // 绘制道路边界线
            Vector3 pos = transform.position;
            Vector3 leftBound = new Vector3(-halfWidth, pos.y + 0.5f, pos.z);
            Vector3 rightBound = new Vector3(halfWidth, pos.y + 0.5f, pos.z);

            Gizmos.DrawLine(leftBound, rightBound);

            // 绘制边界标记点
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(leftBound, 0.1f);
            Gizmos.DrawSphere(rightBound, 0.1f);

            // 绘制道路区域
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawCube(
                new Vector3(0, pos.y, pos.z),
                new Vector3(roadWidth, 0.1f, 5f)
            );

            // 绘制玩家当前位置线
            Gizmos.color = Color.yellow;
            Vector3 playerLineStart = new Vector3(pos.x, pos.y + 0.2f, pos.z - 2.5f);
            Vector3 playerLineEnd = new Vector3(pos.x, pos.y + 0.2f, pos.z + 2.5f);
            Gizmos.DrawLine(playerLineStart, playerLineEnd);

            // 显示距离信息
#if UNITY_EDITOR
            float distanceToCenter = Mathf.Abs(pos.x);
            float distanceToBoundary = halfWidth - distanceToCenter;

            string info = $"X位置: {pos.x:F2}\n" +
                         $"道路宽度: {roadWidth:F1}\n" +
                         $"距边界: {distanceToBoundary:F2}";

            UnityEditor.Handles.Label(pos + Vector3.up * 2, info);
#endif
        }
    }
}