using System.Collections.Generic;
using UnityEngine;
using _Script;

public class GridManager : MonoBehaviour
{
    [field: SerializeField] 
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
    [Tooltip("Tỉ lệ kích thước của con lợn so với độ rộng ô (0.7 = bằng 70% bề rộng ô cube)")]
    [Range(0.2f, 1f)]
    [SerializeField] private float pigScaleRatio = 0.7f;

    [Tooltip("Khoảng cách đẩy lùi lên phía trên cạnh trên cùng (tính theo số ô, ví dụ 1.0 = cách đúng 1 ô)")]
    [SerializeField] private float pigTopMargin = 1.0f;

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

        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                float posX = baseTransform.position.x + (c * cellSize) - startOffset;
                float posZ = baseTransform.position.z + (r * cellSize) - startOffset;
                Vector3 worldPos = new Vector3(posX, spawnY, posZ);

                GameObject cube = Instantiate(PrefabConfig.Instance.cube, worldPos, Quaternion.identity, baseTransform);
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

        // Truyền chính xác cubeWidth vào để lợn ăn đúng 70% bề rộng ô cờ
        SpawnPigsAboveGrid(cellSize, cubeWidth, spawnY);
    }

    /// <summary>
    /// Sinh các con lợn xếp thành 1 hàng ngang phía trên cạnh trên của bàn cờ
    /// </summary>
    private void SpawnPigsAboveGrid(float cellSize, float cubeWidth, float spawnY)
    {
        ClearPigs();

        if (PrefabConfig.Instance == null || PrefabConfig.Instance.pig == null || PrefabConfig.Instance.pig.Length == 0)
        {
            Debug.LogWarning("[GridManager] PrefabConfig chưa có prefab pig!", this);
            return;
        }

        int topRowIndex = Size - 1;
        float targetPigWidth = cubeWidth * pigScaleRatio; 

        for (int c = 0; c < Size; c++)
        {
            Node referenceNode = gridNodes[topRowIndex, c];
            if (referenceNode == null) continue;

            GameObject pigPrefab = PrefabConfig.Instance.pig[c % PrefabConfig.Instance.pig.Length];
            if (pigPrefab == null) continue;

            Vector3 refPos = referenceNode.transform.position;
            Vector3 pigPos = new Vector3(
                refPos.x,
                spawnY + 0.25f,
                refPos.z + (cellSize * pigTopMargin)
            );

            // Sinh con lợn làm con của gridBase để ăn theo không gian tọa độ thế giới chuẩn
            GameObject newPig = Instantiate(pigPrefab, pigPos, Quaternion.identity, transform);
            newPig.name = $"Pig_Col_{c}";
            newPig.transform.rotation = Quaternion.Euler(0, 180, 0);

            // Tự động đo kích thước mesh thực tế của prefab và scale đều 3 trục về đúng targetPigWidth
            FitPigUniformScale(newPig, targetPigWidth);

            spawnedPigs.Add(newPig);
        }
    }

    /// <summary>
    /// Đo đạc mesh của prefab lợn và scale đều 3 trục (X, Y, Z) để đạt đúng kích thước targetWidth mà không méo dáng
    /// </summary>
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

    public Node GetNode(int row, int col)
    {
        if (gridNodes == null || row < 0 || row >= Size || col < 0 || col >= Size)
        {
            return null;
        }

        return gridNodes[row, col];
    }
    
    public void SetSize(int newSize)
    {
        Size = Mathf.Max(1, newSize);
        InitializeGrid();
    }
}