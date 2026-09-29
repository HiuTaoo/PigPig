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
            if (Time.timeScale == 0)
                return;
            
            if (singleClickCoroutine != null)
            {
                StopCoroutine(singleClickCoroutine);
                singleClickCoroutine = null;

                EventManager.Raise(new CellDoubleClickedEvent(this));
            }
            else
            {
                singleClickCoroutine = StartCoroutine(WaitSingleClickRoutine());
            }
        }

        private IEnumerator WaitSingleClickRoutine()
        {
            yield return new WaitForSeconds(DoubleClickThreshold);

            singleClickCoroutine = null;
            EventManager.Raise(new CellClickedEvent(this));
        }
        

        public void SetColor(Color color)
        {
            if (cubeRenderer == null) cubeRenderer = GetComponent<Renderer>();

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