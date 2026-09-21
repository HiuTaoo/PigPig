using UnityEngine;
using _Script;

public class GridManager : MonoBehaviour
{
    [field: SerializeField] 
    public int Size { get; private set; } = 5;

    [Header("Tham chiếu đối tượng")]
    [SerializeField] private GameObject gridCubePrefab;
    [SerializeField] private GameObject gridBase;

    [Header("Bảng màu hệ thống")]
    [SerializeField] private ColorPaletteSO globalColorPalette;

    [Header("Cấu hình hiển thị")]
    [Range(0.5f, 1f)]
    [SerializeField] private float tileFillRatio = 0.95f;
    [SerializeField] private float tileThickness = 5f;

    // Lưu trữ ma trận các Node
    private Node[,] gridNodes;

    private void Start()
    {
        InitializeGrid();
    }

    public void SetSize(int newSize)
    {
        Size = Mathf.Max(1, newSize);
        InitializeGrid();
    }

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

                // 1. Sinh Cube
                GameObject cube = Instantiate(gridCubePrefab, worldPos, Quaternion.identity, baseTransform);
                cube.name = $"Cube_{r}_{c}";
                cube.transform.localScale = localScale;

                // 2. Lấy component Node và gán thông tin
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

    private bool ValidateReferences()
    {
        if (gridCubePrefab == null)
        {
            Debug.LogError("[GridManager] gridCubePrefab chưa được gán!", this);
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
}