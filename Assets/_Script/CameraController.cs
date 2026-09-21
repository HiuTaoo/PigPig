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
    [Tooltip("Tỉ lệ bề ngang cạnh dưới so với màn hình: 0.9 = cạnh dưới rộng đúng 90% màn hình, 1.0 = chạm sát 2 mép")]
    [Range(0.6f, 1f)]
    [SerializeField] private float horizontalFillRatio = 0.95f;

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

        // Khóa góc quay chỉ xoay trục X (không xoay Y để cạnh song song mép màn hình)
        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);

        // 2. Tính góc mở Camera (Vertical & Horizontal FOV)
        float radVFov = cam.fieldOfView * Mathf.Deg2Rad;
        float currentAspect = (float)Screen.width / Screen.height;
        float radHFov = 2f * Mathf.Atan(Mathf.Tan(radVFov * 0.5f) * currentAspect);

        // 3. Tọa độ của cạnh trước (gần camera nhất)
        float halfWidth = bounds.size.x * 0.5f;
        float halfDepth = bounds.size.z * 0.5f;
        float topY = bounds.max.y;

        // Khoảng cách theo trục quang học (depth) để cạnh trước vừa khít horizontalFillRatio
        float targetVisibleWidthAtFront = (bounds.size.x) / horizontalFillRatio;
        float requiredDepthToFront = (targetVisibleWidthAtFront * 0.5f) / Mathf.Tan(radHFov * 0.5f);

        // Vector từ tâm bàn cờ đến điểm giữa của cạnh trước
        Vector3 frontEdgeMidPoint = new Vector3(center.x, topY, center.z - halfDepth);

        // Đặt camera lùi lại đúng khoảng cách requiredDepthToFront so với cạnh trước
        Vector3 camPos = frontEdgeMidPoint - transform.forward * requiredDepthToFront;

        // 4. Cân bằng tâm theo chiều dọc để bàn cờ không bị tụt xuống đáy
        // Điểm giữa cạnh sau
        Vector3 backEdgeMidPoint = new Vector3(center.x, topY, center.z + halfDepth);
        
        // Chiếu lên trục UP của Camera để tìm tâm biểu kiến
        float frontYProj = Vector3.Dot(frontEdgeMidPoint - camPos, transform.up) / requiredDepthToFront;
        float backDepth = Vector3.Dot(backEdgeMidPoint - camPos, transform.forward);
        float backYProj = Vector3.Dot(backEdgeMidPoint - camPos, transform.up) / backDepth;

        // Độ lệch tâm theo trục dọc
        float midYProj = (frontYProj + backYProj) * 0.5f;
        float verticalShift = midYProj * requiredDepthToFront;

        // Dịch camera để tâm hình học của bàn cờ rơi đúng vào tâm màn hình
        camPos += transform.up * verticalShift;

        // Áp dụng thêm tùy chỉnh của người dùng (nếu muốn)
        if (!Mathf.Approximately(verticalOffset, 0f))
        {
            camPos -= transform.up * (verticalOffset * requiredDepthToFront * Mathf.Tan(radVFov * 0.5f));
        }

        transform.position = camPos;
    }

    private Bounds CalculateTotalBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one * 100f);
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }
}