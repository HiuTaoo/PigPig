using _Script;
using UnityEngine;

public class Node : MonoBehaviour
    {
        [Header("Tọa độ & Vùng")]
        public int row;
        public int col;
        public int colorRegionID;
        public NodeStatus nodeStatus = NodeStatus.Empty;

        [Header("Bảng màu")]
        [SerializeField] private ColorPaletteSO colorPalette;

        private Renderer cubeRenderer;
        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            cubeRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
        }

        public void Init(int r, int c, int regionId, ColorPaletteSO palette)
        {
            this.row = r;
            this.col = c;
            this.colorRegionID = regionId;
            this.colorPalette = palette;

            ApplyColorByRegionId();
        }

        /// <summary>
        /// Tự đọc colorRegionID từ palette và set màu cho Renderer
        /// </summary>
        public void ApplyColorByRegionId()
        {
            if (colorPalette == null)
            {
                Debug.LogWarning($"[Node] Ô ({row}, {col}) chưa được gán ColorPaletteSO!", this);
                return;
            }

            Color targetColor = colorPalette.GetColor(colorRegionID);
            SetColor(targetColor);
        }

        public void SetColor(Color color)
        {
            if (cubeRenderer == null) cubeRenderer = GetComponent<Renderer>();

            // Dùng MaterialPropertyBlock tối ưu hiệu năng mobile
            cubeRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(BaseColorId, color);
            cubeRenderer.SetPropertyBlock(propBlock);
        }

        public void CycleNextState()
        {
            switch (nodeStatus)
            {
                case NodeStatus.Empty:
                    SetStatus(NodeStatus.Correct);
                    break;
                case NodeStatus.Correct:
                    SetStatus(NodeStatus.Marked);
                    break;
                case NodeStatus.Marked:
                    SetStatus(NodeStatus.Empty);
                    break;
            }
        }

        public void SetStatus(NodeStatus newStatus)
        {
            if (newStatus == nodeStatus) return;

            nodeStatus = newStatus;
            ClearMarker();

            switch (nodeStatus)
            {
                case NodeStatus.Marked:
                    break;
                case NodeStatus.Correct:
                    break;
                case NodeStatus.Empty:
                    break;
            }
        }

        private void SpawnMarker(GameObject prefab)
        {
        }

        private void ClearMarker()
        {
        }
    }