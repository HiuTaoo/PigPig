using System.Collections.Generic;
using UnityEngine;
using _Script;
using _Script.Data;
using _Script.Events;

public class GridManager : MonoBehaviour
{
    [field: SerializeField] 
    [Tooltip("Kích thước bàn cờ chơi chính (ví dụ: 5 -> vùng 5x5)")]
    public int Size { get; private set; } = 5;

    [Header("Tham chiếu đối tượng")]
    [SerializeField] private GameObject gridBase;
    [SerializeField] private GameObject gridGround;

    [Header("Bảng màu hệ thống")]
    [SerializeField] private ColorPaletteSO globalColorPalette;

    [Header("Cấu hình hiển thị")]
    [Range(0.5f, 1f)]
    [SerializeField] private float tileFillRatio = 0.95f;
    [SerializeField] private float tileThickness = 5f;


    [Range(0.2f, 1f)]
    [SerializeField] private float pigScaleRatio = 0.7f;
    [Range(0.2f, 1f)]
    [SerializeField] private float markerScaleRatio = 0.8f;

    private Node[,] gridNodes;

    private List<GameObject> spawnedPigs = new List<GameObject>();
    
    private Dictionary<Node, GameObject> activeMarkers = new Dictionary<Node, GameObject>();

    public void InitializeGrid(LevelJsonData levelData = null)
    {
        if (!ValidateReferences()) return;

        ClearGrid();
        ClearAllMarkers();
        
        if (levelData != null && levelData.n > 0)
        {
            Size = levelData.n;
        }

        gridNodes = new Node[Size, Size];

        Transform baseTransform = gridBase.transform;
        GetGridBounds(baseTransform, out float gridWidth, out float gridTopY);

        float cellSize = gridWidth / Size;
        float cubeWidth = cellSize * tileFillRatio;
        float startOffset = (gridWidth - cellSize) * 0.5f;
        float spawnY = gridTopY + (tileThickness * 0.5f);
        
        UpdateGroundSize(cellSize);

        Vector3 localScale = CalculateLocalScale(baseTransform, cubeWidth, tileThickness);

        Dictionary<Vector2Int, NodeJsonData> jsonNodeMap = new Dictionary<Vector2Int, NodeJsonData>();
        if (levelData != null && levelData.nodes != null)
        {
            foreach (var nData in levelData.nodes)
            {
                jsonNodeMap[new Vector2Int(nData.row, nData.col)] = nData;
            }
        }
        
        List<Vector3> borderSlotPositions = new List<Vector3>();

        for (int r = -1; r <= Size; r++)
        {
            for (int c = -1; c <= Size; c++)
            {
                float posX = baseTransform.position.x + (c * cellSize) - startOffset;
                float posZ = baseTransform.position.z + (r * cellSize) - startOffset;
                Vector3 slotPos = new Vector3(posX, spawnY, posZ);

                bool isVirtualBorder = (r == -1 || r == Size || c == -1 || c == Size);

                if (isVirtualBorder)
                {
                    borderSlotPositions.Add(slotPos);
                }
                else
                {
                    GameObject cube = ObjectPooler.Instance.GetCube(slotPos, Quaternion.identity, baseTransform);
                    cube.name = $"Cube_{r}_{c}";
                    cube.transform.localScale = localScale;

                    Node node = cube.GetComponent<Node>();
                    if (node == null)
                    {
                        node = cube.AddComponent<Node>();
                    }

                    Vector2Int coord = new Vector2Int(r, c);
                    if (jsonNodeMap.TryGetValue(coord, out NodeJsonData nData))
                    {
                        int regionId = nData.GetColorID();
                        Color nodeColor = nData.ToUnityColor();
                        node.Init(r, c, regionId, nodeColor);
                    }

                    cube.isStatic = true;
                    gridNodes[r, c] = node;
                }
            }
        }

        // Sinh ngẫu nhiên Size con lợn vào các ô viền ảo vừa tính
        SpawnRandomPigsOnBorder(borderSlotPositions, cubeWidth);
        
        List<Vector3> loopPath = GetPerimeterPath(cellSize, startOffset, spawnY);

        foreach (GameObject pigObj in spawnedPigs)
        {
            Pig pig = pigObj.GetComponent<Pig>();
            if (pig != null)
            {
                pig.StartPatrolling(loopPath);
            }
        }
    }

    #region SpawnPig On Border
    /// <summary>
    /// Điều chỉnh kích thước và vị trí của gridGround thành ma trận (Size + 2) x (Size + 2)
    /// </summary>
    private void UpdateGroundSize(float cellSize)
    {
        if (gridGround == null) return;

        // Chiều rộng bao quanh bao gồm cả 2 ô viền ngoài (trên/dưới và trái/phải)
        float targetGroundWidth = (Size + 2) * cellSize;

        Transform groundTransform = gridGround.transform;

        // Lấy kích thước mesh gốc (Renderer bounds không phụ thuộc scale)
        if (gridGround.TryGetComponent<Renderer>(out var rend))
        {
            Vector3 originalMeshSize = rend.bounds.size;
            Vector3 currentScale = groundTransform.localScale;

            // Kích thước chuẩn chưa tính scale
            float meshX = (currentScale.x != 0) ? originalMeshSize.x / currentScale.x : 1f;
            float meshZ = (currentScale.z != 0) ? originalMeshSize.z / currentScale.z : 1f;

            groundTransform.localScale = new Vector3(
                targetGroundWidth / meshX,
                currentScale.y, 
                targetGroundWidth / meshZ
            );
        }
        else
        {
            // Fallback nếu dùng Cube Unity mặc định (kích thước 1x1x1)
            groundTransform.localScale = new Vector3(
                targetGroundWidth,
                groundTransform.localScale.y,
                targetGroundWidth
            );
        }

        // Đồng bộ tâm của gridGround theo trục X và Z với gridBase
        Vector3 newGroundPos = gridGround.transform.position;
        newGroundPos.x = gridBase.transform.position.x;
        newGroundPos.z = gridBase.transform.position.z;
        gridGround.transform.position = newGroundPos;
    }

    /// <summary>
    /// Sinh ngẫu nhiên Size con lợn tại các tọa độ viền ảo (-1 hoặc Size)
    /// </summary>
    private void SpawnRandomPigsOnBorder(List<Vector3> borderPositions, float cubeWidth)
    {
        ClearPigs();

        if (PrefabConfig.Instance == null || PrefabConfig.Instance.pig == null || PrefabConfig.Instance.pig.Length == 0)
        {
            Debug.LogWarning("[GridManager] PrefabConfig chưa có prefab pig!", this);
            return;
        }

        if (borderPositions.Count == 0) return;

        // Trộn ngẫu nhiên (Fisher-Yates Shuffle) các vị trí viền
        for (int i = borderPositions.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (borderPositions[i], borderPositions[randomIndex]) = (borderPositions[randomIndex], borderPositions[i]);
        }

        float targetPigWidth = cubeWidth * pigScaleRatio;
        int pigsToSpawn = Mathf.Min(Size, borderPositions.Count);

        for (int i = 0; i < pigsToSpawn; i++)
        {
            Vector3 spawnPos = borderPositions[i];
            spawnPos.y += 0.25f;

            GameObject pigPrefab = PrefabConfig.Instance.pig[i % PrefabConfig.Instance.pig.Length];
            if (pigPrefab == null) continue;

            GameObject newPig = ObjectPooler.Instance.GetPig(i % PrefabConfig.Instance.pig.Length, spawnPos, Quaternion.Euler(0, 180, 0), transform);
            newPig.name = $"Pig_{i}";

            FitObjectUniformScale(newPig, targetPigWidth);

            spawnedPigs.Add(newPig);
        }
    }
    
    /// <summary>
    /// Tạo danh sách các tọa độ viền ảo theo vòng khép kín (theo chiều kim đồng hồ)
    /// </summary>
    public List<Vector3> GetPerimeterPath(float cellSize, float startOffset, float spawnY)
    {
        List<Vector3> path = new List<Vector3>();
        Transform baseTransform = gridBase.transform;

        for (int c = -1; c < Size; c++)
            path.Add(GetWorldPos(Size, c, cellSize, startOffset, spawnY));

        for (int r = Size; r > -1; r--)
            path.Add(GetWorldPos(r, Size, cellSize, startOffset, spawnY));

        for (int c = Size; c > -1; c--)
            path.Add(GetWorldPos(-1, c, cellSize, startOffset, spawnY));

        for (int r = -1; r < Size; r++)
            path.Add(GetWorldPos(r, -1, cellSize, startOffset, spawnY));

        return path;
    }
    
    private Vector3 GetWorldPos(int r, int c, float cellSize, float startOffset, float spawnY)
    {
        float posX = gridBase.transform.position.x + (c * cellSize) - startOffset;
        float posZ = gridBase.transform.position.z + (r * cellSize) - startOffset;
        return new Vector3(posX, spawnY + 0.25f, posZ);
    }

    private void FitObjectUniformScale(GameObject pigObj, float targetWidth)
    {
        Renderer[] rends = pigObj.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                b.Encapsulate(rends[i].bounds);
            }

            float currentWidth = Mathf.Max(b.size.x, b.size.z);
            if (currentWidth > 0.0001f)
            {
                float factor = targetWidth / currentWidth;
                pigObj.transform.localScale *= factor;
            }
        }
        else
        {
            pigObj.transform.localScale = Vector3.one * targetWidth;
        }
    }

    private void ClearPigs()
    {
        for (int i = spawnedPigs.Count - 1; i >= 0; i--)
        {
            if (spawnedPigs[i] != null)
            {
                Destroy(spawnedPigs[i]);
            }
        }
        spawnedPigs.Clear();
    }
    #endregion

    #region Spawn Marker
/// <summary>
    /// Sinh marker (ví dụ: hoa trắng/hoa đỏ) lên trên bề mặt node
    /// </summary>
    /// <param name="targetNode">Ô Node cần đặt marker</param>
    /// <param name="isRedFlower">true: redFlower, false: whiteFlower</param>
    public void SpawnMarker(Node targetNode, bool isRedFlower = false)
    {
        if (targetNode == null || targetNode.nodeStatus == NodeStatus.Correct 
                               || targetNode.nodeStatus == NodeStatus.Incorrect) return;

        if (activeMarkers.ContainsKey(targetNode))
        {
            ClearMarker(targetNode);
            return;
        }

        if (PrefabConfig.Instance == null)
        {
            Debug.LogWarning("[GridManager] PrefabConfig.Instance chưa khởi tạo!", this);
            return;
        }

        GameObject markerPrefab = isRedFlower ? PrefabConfig.Instance.redFlower : PrefabConfig.Instance.whiteFlower;
        if (markerPrefab == null)
        {
            Debug.LogWarning($"[GridManager] Prefab marker {(isRedFlower ? "redFlower" : "whiteFlower")} chưa được gán trong PrefabConfig!", this);
            return;
        }

        Vector3 spawnPos = targetNode.transform.position;
        if (targetNode.TryGetComponent<Renderer>(out var nodeRend))
        {
            spawnPos.y = nodeRend.bounds.max.y;
        }
        else
        {
            spawnPos.y += tileThickness * 0.5f;
        }

        GameObject markerObj = ObjectPooler.Instance.Get(markerPrefab, spawnPos, Quaternion.identity, transform);
        markerObj.name = $"Marker_{targetNode.row}_{targetNode.col}";

        Transform baseTransform = gridBase.transform;
        GetGridBounds(baseTransform, out float gridWidth, out _);
        float cellSize = gridWidth / Size;
        float targetMarkerWidth = cellSize * tileFillRatio * markerScaleRatio;

        FitObjectUniformScale(markerObj, targetMarkerWidth); 

        activeMarkers[targetNode] = markerObj;
        targetNode.SetStatus(NodeStatus.Marked);
    }

    /// <summary>
    /// Xóa marker tại một ô Node cụ thể
    /// </summary>
    public void ClearMarker(Node targetNode)
    {
        if (targetNode == null) return;

        if (activeMarkers.TryGetValue(targetNode, out GameObject markerObj))
        {
            if (markerObj != null)
            {
                ObjectPooler.Instance.Return(markerObj);
            }
            activeMarkers.Remove(targetNode);
            targetNode.SetStatus(NodeStatus.Empty);
        }
    }

    /// <summary>
    /// Xóa toàn bộ marker hiện có trên toàn bộ bàn cờ (ví dụ khi reset hoặc level up)
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (var kvp in activeMarkers)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        activeMarkers.Clear();
    }
    

    #endregion

    private bool ValidateReferences()
    {
        if (PrefabConfig.Instance == null || PrefabConfig.Instance.cube == null)
        {
            Debug.LogError("[GridManager] PrefabConfig.Instance.cube chưa được gán!", this);
            return false;
        }

        if (gridBase == null)
        {
            Debug.LogError("[GridManager] gridBase chưa được gán!", this);
            return false;
        }

        return true;
    }

    private void GetGridBounds(Transform baseTransform, out float width, out float topY)
    {
        if (baseTransform.TryGetComponent<Renderer>(out var rend))
        {
            width = rend.bounds.size.x;
            topY = rend.bounds.max.y;
        }
        else
        {
            width = baseTransform.lossyScale.x;
            topY = baseTransform.position.y;
        }
    }

    private Vector3 CalculateLocalScale(Transform parent, float width, float height)
    {
        Vector3 lossy = parent.lossyScale;
        return new Vector3(
            width / lossy.x,
            height / lossy.y,
            width / lossy.z
        );
    }

    private void ClearGrid()
    {
        ClearPigs();

        if (gridBase == null) return;

        Transform baseTransform = gridBase.transform;
        for (int i = baseTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(baseTransform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Lấy Node chơi chính (hàng, cột từ 0 đến Size - 1)
    /// </summary>
    public Node GetNode(int row, int col)
    {
        if (gridNodes == null || row < 0 || row >= Size || col < 0 || col >= Size)
        {
            return null;
        }

        return gridNodes[row, col];
    }

    public List<GameObject> GetSpawnedPigs()
    {
        return spawnedPigs;
    }
}