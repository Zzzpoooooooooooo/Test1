using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedMonsterSpawner : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public GameObject monsterPrefab;

    [Header("生成位置")]
    public float targetDistance = 15f;    // 目标距离
    public float minDistance = 8f;        // 最小距离
    public float maxDistance = 20f;       // 最大距离
    public LayerMask groundLayer;         // 地面层级
    public LayerMask obstacleLayer;       // 障碍物层级

    [Header("智能调整")]
    public bool adjustForObstacles = true;    // 自动避开障碍物
    public float raycastHeight = 5f;          // 射线起始高度
    public float maxRaycastDistance = 30f;    // 最大射线距离

    [Header("生成控制")]
    public float spawnRate = 0.5f;        // 每秒尝试生成概率
    public int maxMonstersInScene = 10;   // 场景最大怪物数
    public float monsterCleanupDistance = 50f; // 怪物清理距离

    private Vector3 currentSpawnPoint;
    private float nextSpawnTime;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        groundLayer = LayerMask.GetMask("Ground", "Default");
        obstacleLayer = LayerMask.GetMask("Obstacle", "Building");
    }

    void Update()
    {
        if (player == null) return;

        // 更新生成点位置
        UpdateSpawnPoint();

        // 概率生成怪物
        if (Time.time >= nextSpawnTime &&
            GetMonsterCount() < maxMonstersInScene &&
            Random.value < spawnRate * Time.deltaTime)
        {
            SpawnMonster();
            nextSpawnTime = Time.time + 1f / spawnRate;
        }

        // 清理过远的怪物
        CleanupDistantMonsters();
    }

    void UpdateSpawnPoint()
    {
        Vector3 idealPosition = CalculateIdealSpawnPosition();

        // 地面检测
        if (FindGroundPosition(idealPosition, out Vector3 groundPos))
        {
            currentSpawnPoint = groundPos;
        }
        else
        {
            // 如果找不到地面，使用备用位置
            currentSpawnPoint = idealPosition;
        }

        transform.position = currentSpawnPoint;
    }

    Vector3 CalculateIdealSpawnPosition()
    {
        Vector3 playerForward = player.forward;

        // 添加一些随机偏移，使生成位置更自然
        float angleVariation = 30f; // 角度变化范围
        float randomAngle = Random.Range(-angleVariation, angleVariation);
        Quaternion randomRotation = Quaternion.Euler(0, randomAngle, 0);
        Vector3 direction = randomRotation * playerForward;

        // 基于玩家速度调整距离（可选）
        float adjustedDistance = targetDistance;
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            // 玩家速度越快，生成距离越远
            float speedFactor = Mathf.Clamp01(playerRb.velocity.magnitude / 10f);
            adjustedDistance = Mathf.Lerp(minDistance, maxDistance, speedFactor);
        }

        return player.position + direction * adjustedDistance;
    }

    bool FindGroundPosition(Vector3 startPos, out Vector3 groundPos)
    {
        // 从高处向下发射射线检测地面
        Vector3 rayStart = startPos + Vector3.up * raycastHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                           maxRaycastDistance, groundLayer))
        {
            // 检查是否被障碍物阻挡
            if (adjustForObstacles)
            {
                Vector3 toPlayer = player.position - hit.point;
                if (!Physics.Raycast(hit.point, toPlayer.normalized,
                                    toPlayer.magnitude, obstacleLayer))
                {
                    groundPos = hit.point;
                    return true;
                }
                else
                {
                    // 被阻挡，尝试在两侧寻找新位置
                    return FindAlternativePosition(startPos, out groundPos);
                }
            }

            groundPos = hit.point;
            return true;
        }

        groundPos = startPos;
        return false;
    }

    bool FindAlternativePosition(Vector3 startPos, out Vector3 result)
    {
        // 尝试左右偏移寻找合适位置
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f; // 每45度尝试一次
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 offset = rotation * Vector3.right * 3f;
            Vector3 testPos = startPos + offset;

            Vector3 rayStart = testPos + Vector3.up * raycastHeight;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                               maxRaycastDistance, groundLayer))
            {
                Vector3 toPlayer = player.position - hit.point;
                if (!Physics.Raycast(hit.point, toPlayer.normalized,
                                    toPlayer.magnitude, obstacleLayer))
                {
                    result = hit.point;
                    return true;
                }
            }
        }

        result = startPos;
        return false;
    }

    void SpawnMonster()
    {
        if (monsterPrefab == null) return;

        // 在当前位置生成怪物
        GameObject monster = Instantiate(monsterPrefab, currentSpawnPoint, Quaternion.identity);

        // 让怪物面向玩家
        Vector3 lookDirection = player.position - currentSpawnPoint;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            monster.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

    }

    int GetMonsterCount()
    {
        return GameObject.FindGameObjectsWithTag("Monster").Length;
    }

    void CleanupDistantMonsters()
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject monster in monsters)
        {
            if (Vector3.Distance(player.position, monster.transform.position) > monsterCleanupDistance)
            {
                Destroy(monster);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // 绘制生成范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentSpawnPoint, 1f);

        // 绘制到玩家的连线
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
        Gizmos.DrawLine(currentSpawnPoint, player.position);

        // 绘制理想生成区域
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Quaternion leftAngle = Quaternion.Euler(0, -30, 0);
        Quaternion rightAngle = Quaternion.Euler(0, 30, 0);
        Vector3 leftDir = leftAngle * player.forward * targetDistance;
        Vector3 rightDir = rightAngle * player.forward * targetDistance;

        Vector3 center = player.position + player.forward * targetDistance;
        Gizmos.DrawLine(player.position, player.position + leftDir);
        Gizmos.DrawLine(player.position, player.position + rightDir);
        Gizmos.DrawLine(player.position + leftDir, player.position + rightDir);
    }
}