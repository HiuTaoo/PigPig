using System.Collections.Generic;
using UnityEngine;
using _Script;

public class GridManager : MonoBehaviour
{
    [field: SerializeField] 
    [Tooltip("Kích thước bàn cờ chơi chính (ví dụ: 5 -> vùng 5x5)")]
    public int Size { get; private set; } = 5;

    [Header("Tham chiếu đối tượng")]
    [SerializeField] private GameObject gridBase;

    [Header("Bảng màu hệ thống")]
    [SerializeField] private ColorPaletteSO globalColorPalette;

    [Header("Cấu hình hiển thị")]
    [Range(0.5f, 1f)]
    [SerializeField] private float tileFillRatio = 0.95f;
    [SerializeField] private float tileThickness = 5f;

    [Header("Cấu hình Lợn")]
    [Tooltip("Tỉ lệ kích thước con lợn so với ô cube (0.7 = 70% bề rộng ô)")]
    [Range(0.2f, 1f)]
    [SerializeField] private float pigScaleRatio = 0.7f;

    private Node[,] gridNodes;
    
    private List<GameObject> spawnedPigs = new List<GameObject>();

    public void InitializeGrid()
    {
        if (!ValidateReferences()) return;

        ClearGrid();

        gridNodes = new Node[Size, Size];

        Transform baseTransform = gridBase.transform;
        GetGridBounds(baseTransform, out float gridWidth, out float gridTopY);

        float cellSize = gridWidth / Size;
        float cubeWidth = cellSize * tileFillRatio;
        float startOffset = (gridWidth - cellSize) * 0.5f;
        float spawnY = gridTopY + (tileThickness * 0.5f);

        Vector3 localScale = CalculateLocalScale(baseTransform, cubeWidth, tileThickness);

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
                    GameObject cube = Instantiate(PrefabConfig.Instance.cube, slotPos, Quaternion.identity, baseTransform);
                    cube.name = $"Cube_{r}_{c}";
                    cube.transform.localScale = localScale;

                    Node node = cube.GetComponent<Node>();
                    if (node == null)
                    {
                        node = cube.AddComponent<Node>();
                    }

                    int regionId = (r + c) % 5; 
                    node.Init(r, c, regionId, globalColorPalette);

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
                // Cho lợn bắt đầu chạy vòng quanh viền
                pig.StartPatrolling(loopPath);
            }
        }
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
            Vector3 temp = borderPositions[i];
            borderPositions[i] = borderPositions[randomIndex];
            borderPositions[randomIndex] = temp;
        }

        float targetPigWidth = cubeWidth * pigScaleRatio;
        // Số lượng lợn sinh ra đúng bằng Size ban đầu (ví dụ: 5 con)
        int pigsToSpawn = Mathf.Min(Size, borderPositions.Count);

        for (int i = 0; i < pigsToSpawn; i++)
        {
            Vector3 spawnPos = borderPositions[i];
            spawnPos.y += 0.25f;

            GameObject pigPrefab = PrefabConfig.Instance.pig[i % PrefabConfig.Instance.pig.Length];
            if (pigPrefab == null) continue;

            GameObject newPig = Instantiate(pigPrefab, spawnPos, Quaternion.Euler(0, 180, 0), transform);
            newPig.name = $"Pig_{i}";

            FitPigUniformScale(newPig, targetPigWidth);

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

        // 1. Cạnh trên cùng: từ trái sang phải (r = Size, c từ -1 -> Size)
        for (int c = -1; c < Size; c++)
            path.Add(GetWorldPos(Size, c, cellSize, startOffset, spawnY));

        // 2. Cạnh bên phải: từ trên xuống dưới (c = Size, r từ Size -> -1)
        for (int r = Size; r > -1; r--)
            path.Add(GetWorldPos(r, Size, cellSize, startOffset, spawnY));

        // 3. Cạnh đáy dưới cùng: từ phải sang trái (r = -1, c từ Size -> -1)
        for (int c = Size; c > -1; c--)
            path.Add(GetWorldPos(-1, c, cellSize, startOffset, spawnY));

        // 4. Cạnh bên trái: từ dưới lên trên (c = -1, r từ -1 -> Size)
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

    private void FitPigUniformScale(GameObject pigObj, float targetWidth)
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
    
    public void SetSize(int newSize)
    {
        Size = Mathf.Max(1, newSize);
        InitializeGrid();
    }
}