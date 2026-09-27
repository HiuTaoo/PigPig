using System;
using System.Collections.Generic;
using _Script.Events;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Script
{
    public class GameManager: MonoBehaviour
    {
        public static GameManager Instance;
        private GridManager gridManager;
        private LevelManager levelManager;

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
            
            gridManager = gameObject.GetComponentInChildren<GridManager>();
            levelManager = gameObject.GetComponentInChildren<LevelManager>();
            
        }

        private void Start()
        {
            levelManager.InitLevel();
        }

        private void OnEnable()
        {
            EventManager.Subscribe<CellClickedEvent>(OnCellClicked);
            EventManager.Subscribe<CellDoubleClickedEvent>(OnCellDoubleClicked);
        }
        
        private void OnDisable()
        {
            EventManager.Unsubscribe<CellClickedEvent>(OnCellClicked);
            EventManager.Unsubscribe<CellDoubleClickedEvent>(OnCellDoubleClicked);
        }
        
        private void OnCellClicked(CellClickedEvent e)
        {
            if (e.ClickedNode == null) return;
            gridManager.SpawnMarker(e.ClickedNode);
        }
        
        private void OnCellDoubleClicked(CellDoubleClickedEvent e)
        {
            if (e.ClickedNode == null) return;
            gridManager.ClearMarker(e.ClickedNode);
            TrySelectCell(e);
        }
        
        private void TrySelectCell(CellDoubleClickedEvent e)
        {
            Node targetNode = e.ClickedNode;
        
            if (!LevelManager.Instance.ValidatePlacement(targetNode))
            {
                gridManager.SpawnMarker(e.ClickedNode, true);
                targetNode.SetStatus(NodeStatus.Incorrect);
                Debug.LogWarning("Không thể đặt lợn vào ô này do vi phạm luật!");
                EventManager.Raise(new CellInCorrectClickEvent(targetNode));
                return;
            }
            
            if (!LevelManager.Instance.CanLeadToSolution(targetNode))
            {
                gridManager.SpawnMarker(e.ClickedNode, true);
                targetNode.SetStatus(NodeStatus.Incorrect);
                Debug.LogWarning("Nước đi này dẫn vào ngõ cụt (không thể giải thắng bàn cờ)! Hãy chọn ô khác.");
                EventManager.Raise(new CellInCorrectClickEvent(targetNode));
                return;
            }

            List<Pig> availablePigs = new List<Pig>();
            foreach (var pigObj in gridManager.GetSpawnedPigs()) 
            {
                if (pigObj != null && pigObj.TryGetComponent<Pig>(out var pig))
                {
                    if (pig.IsPatrolling && !pig.IsPlaced) 
                    {
                        availablePigs.Add(pig);
                    }
                }
            }

            if (availablePigs.Count == 0) return;

            int randomIndex = Random.Range(0, availablePigs.Count);
            Pig selectedPig = availablePigs[randomIndex];

            selectedPig.StopPatrolling(); 

            Vector3 targetPosition = targetNode.transform.position;
            targetPosition.y = selectedPig.transform.position.y;

            selectedPig.transform.position = targetPosition;
            selectedPig.transform.rotation = Quaternion.Euler(0,180,0);

            LevelManager.Instance.ConfirmPlacement(selectedPig, targetNode);
            e.ClickedNode.SetStatus(NodeStatus.Correct);
            EventManager.Raise(new CellCorrectClickEvent(e.ClickedNode));
            
            e.ClickedNode.ShowNodeInfo();
        }
        
        public GridManager GetGridManager()
        {
            return gridManager;
        }
    }
}