using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Mục tiêu")]
    [Tooltip("Kéo GridBase hoặc GridManager vào đây")]
    [SerializeField] private Transform targetGrid;

    [Header("Góc nhìn 3D")]
    [Tooltip("Góc nghiêng nhìn từ trên xuống")]
    [Range(20f, 75f)]
    [SerializeField] private float pitchAngle = 45f;

    [Header("Căn chỉnh hiển thị")]
    [Tooltip("Tỉ lệ bao phủ tối đa của bàn cờ so với màn hình (áp dụng cho cả chiều ngang và chiều dọc): 0.9 = chiếm 90%")]
    [Range(0.5f, 1f)]
    [SerializeField] private float screenFillRatio = 0.9f;

    [Tooltip("Dịch cả bàn cờ lên/xuống (-0.1: hạ xuống chút nhường chỗ cho UI, 0: chính giữa)")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float verticalOffset = 0f;

    private Camera cam;

    private void Awake()
    {
        SetupCamera();
    }

    private void LateUpdate()
    {
        AdjustCamera();
    }

    private void SetupCamera()
    {
        if (cam == null) cam = GetComponent<Camera>();
        cam.orthographic = false;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 5000f;
    }

    [ContextMenu("Fit Camera Now")]
    public void AdjustCamera()
    {
        if (targetGrid == null) return;
        SetupCamera();

        // 1. Tính toán Bounds tổng
        Bounds bounds = CalculateTotalBounds(targetGrid);
        Vector3 center = bounds.center;

        // Khóa góc quay chỉ xoay trục X (nghiêng nhìn xuống, không xoay Y/Z)
        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);

        // 2. Góc mở Camera (Vertical & Horizontal FOV)
        float radVFov = cam.fieldOfView * Mathf.Deg2Rad;
        float currentAspect = (float)Screen.width / Screen.height;
        float radHFov = 2f * Mathf.Atan(Mathf.Tan(radVFov * 0.5f) * currentAspect);

        // 3. Tọa độ các điểm mốc trên bàn cờ
        float halfDepth = bounds.size.z * 0.5f;
        float topY = bounds.max.y;

        Vector3 frontEdgeMid = new Vector3(center.x, topY, center.z - halfDepth);
        Vector3 backEdgeMid = new Vector3(center.x, topY, center.z + halfDepth);

        // --- TÍNH KHOẢNG CÁCH FIT THEO CHIỀU NGANG ---
        // Cạnh trước gần nhất có độ rộng bounds.size.x
        float targetVisibleWidthAtFront = bounds.size.x / screenFillRatio;
        float depthForWidth = (targetVisibleWidthAtFront * 0.5f) / Mathf.Tan(radHFov * 0.5f);

        // --- TÍNH KHOẢNG CÁCH FIT THEO CHIỀU DỌC (KHI XOAY NGANG) ---
        // Độ chênh lệch giữa cạnh trước và sau trong không gian camera (chiếu theo hướng Up và Forward)
        Vector3 edgeDelta = backEdgeMid - frontEdgeMid;
        float deltaForward = Vector3.Dot(edgeDelta, transform.forward);
        float deltaUp = Vector3.Dot(edgeDelta, transform.up);

        // Tính khoảng cách cần lùi để góc mở dọc chứa trọn độ dài chiếu của bàn cờ
        float tanHalfVFov = Mathf.Tan(radVFov * 0.5f) * screenFillRatio;
        float depthForHeight = (deltaUp + deltaForward * tanHalfVFov) / (2f * tanHalfVFov);

        // Chọn khoảng cách an toàn lớn nhất để KHÔNG BAO GIỜ bị tràn cả chiều ngang lẫn chiều dọc
        float chosenDepthToFront = Mathf.Max(depthForWidth, depthForHeight);

        // 4. Đặt vị trí Camera lùi lại từ cạnh trước
        Vector3 camPos = frontEdgeMid - transform.forward * chosenDepthToFront;

        // 5. Cân bằng tâm theo chiều dọc để bàn cờ luôn nằm ngay chính giữa màn hình
        float frontYProj = Vector3.Dot(frontEdgeMid - camPos, transform.up) / chosenDepthToFront;
        float backDepth = Vector3.Dot(backEdgeMid - camPos, transform.forward);
        float backYProj = Vector3.Dot(backEdgeMid - camPos, transform.up) / backDepth;

        float midYProj = (frontYProj + backYProj) * 0.5f;
        float verticalShift = midYProj * chosenDepthToFront;
        camPos += transform.up * verticalShift;

        // Áp dụng offset dịch chuyển dọc (nếu có)
        if (!Mathf.Approximately(verticalOffset, 0f))
        {
            camPos -= transform.up * (verticalOffset * chosenDepthToFront * Mathf.Tan(radVFov * 0.5f));
        }

        transform.position = camPos;
    }

    private Bounds CalculateTotalBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, root.lossyScale);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }
}