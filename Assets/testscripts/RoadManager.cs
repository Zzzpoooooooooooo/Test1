using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [Header("道路设置")]
    [SerializeField] private bool hasRoad = true;
    [SerializeField] private float roadWidth = 5f; // 这个值应该和LevelController一致
    [SerializeField] private Material roadMaterial;
    [SerializeField] private float roadHeight = 0.01f;
    [SerializeField] private bool addVisual = true;

    public float RoadWidth => roadWidth;

    public void GenerateRoad(GroundPieceData groundData)
    {
        if (!hasRoad || !addVisual) return;

        GameObject road = new GameObject("Road");
        road.transform.SetParent(groundData.container.transform);
        road.transform.localPosition = Vector3.zero;

        // 设置道路标签（如果你在Unity中创建了Road标签）
        try
        {
            road.tag = "Road";
        }
        catch
        {
            Debug.LogWarning("Road标签不存在，请在Unity编辑器中创建Road标签");
        }

        // 添加网格渲染器
        MeshFilter meshFilter = road.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = road.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateRoadMesh(groundData);
        meshRenderer.material = roadMaterial ?? CreateDefaultMaterial();

        groundData.roadVisual = road;

        Debug.Log($"生成道路，宽度: {roadWidth}, 长度: {groundData.pieceLength}");
    }

    private Mesh CreateRoadMesh(GroundPieceData groundData)
    {
        Mesh mesh = new Mesh();
        float halfWidth = roadWidth / 2f;
        float halfLength = groundData.pieceLength / 2f;

        Vector3[] vertices = {
            new(-halfWidth, roadHeight, -halfLength),
            new(-halfWidth, roadHeight, halfLength),
            new(halfWidth, roadHeight, -halfLength),
            new(halfWidth, roadHeight, halfLength)
        };

        int[] triangles = { 0, 1, 2, 2, 1, 3 };
        Vector2[] uv = { new(0, 0), new(0, 1), new(1, 0), new(1, 1) };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.gray;
        return mat;
    }

    public bool IsPositionOnRoad(Vector3 worldPosition)
    {
        if (!hasRoad) return false;
        return Mathf.Abs(worldPosition.x) <= roadWidth / 2f;
    }

    public void SetRoadWidth(float width)
    {
        roadWidth = Mathf.Max(1f, width);
    }
}