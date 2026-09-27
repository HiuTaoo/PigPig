using System.Collections;
using _Script;
using _Script.Events;
using UnityEngine;

public class Node : MonoBehaviour
    {
        [Header("Tọa độ & Vùng")]
        public int row;
        public int col;
        public int colorRegionID;

        public NodeStatus nodeStatus { get; private set; } = NodeStatus.Empty;

        private Renderer cubeRenderer;
        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        
        private Coroutine singleClickCoroutine;
        private const float DoubleClickThreshold = 0.2f;

        private void Awake()
        {
            cubeRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
        }

        public void Init(int r, int c, int regionId, Color directColor)
        {
            this.row = r; 
            this.col = c; 
            this.colorRegionID = regionId; 
            SetColor(directColor); 
        }
        
        private void OnMouseDown()
        {
            if (singleClickCoroutine != null)
            {
                // Nếu coroutine đang chờ mà có click thứ 2 -> Xác nhận là Double Click
                StopCoroutine(singleClickCoroutine);
                singleClickCoroutine = null;

                // Bắn duy nhất sự kiện Double Click
                EventManager.Raise(new CellDoubleClickedEvent(this));
            }
            else
            {
                // Lần click đầu tiên -> Bắt đầu đếm thời gian chờ
                singleClickCoroutine = StartCoroutine(WaitSingleClickRoutine());
            }
        }

        private IEnumerator WaitSingleClickRoutine()
        {
            yield return new WaitForSeconds(DoubleClickThreshold);

            // Hết thời gian chờ mà không có click 2 -> Xác nhận là Single Click
            singleClickCoroutine = null;
            EventManager.Raise(new CellClickedEvent(this));
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

        public void ShowNodeInfo()
        {
            Debug.Log($"Hàng số {row}, cột số {col}, màu {colorRegionID}, Trạng thái {nodeStatus}");
        }
    }