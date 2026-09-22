using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Script
{
    [RequireComponent(typeof(Animator))]
    public class Pig : MonoBehaviour
    {
        [Header("Thông số chạy")]
        [Tooltip("Tốc độ chạy (chỉnh khoảng 3 - 6 cho vừa khớp với nhịp đạp chân)")]
        [SerializeField] private float moveSpeed = 4f;

        [Tooltip("Tốc độ quay đầu khi rẽ góc")]
        [SerializeField] private float turnSpeed = 15f;

        public bool IsPatrolling { get; private set; } = false;

        private Animator anim;
        private Coroutine patrolCoroutine;
        private List<Vector3> cachedLoopPath;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            HelpMethod.FitBoxCollider(gameObject);
        }

        /// <summary>
        /// Nạp danh sách đường đi nhưng chưa chạy ngay, đợi va chạm với Grid
        /// </summary>
        public void PreparePatrolPath(List<Vector3> loopPath)
        {
            cachedLoopPath = loopPath;
        }

        /// <summary>
        /// Bắt đầu chạy tuần tra quanh viền
        /// </summary>
        public void StartPatrolling(List<Vector3> loopPath = null)
        {
            if (loopPath != null) cachedLoopPath = loopPath;
            if (cachedLoopPath == null || cachedLoopPath.Count == 0) return;

            StopPatrolling();
            patrolCoroutine = StartCoroutine(PatrolRoutine(cachedLoopPath));
        }

        public void StopPatrolling(Action onComplete = null)
        {
            IsPatrolling = false;

            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }

            if (anim != null)
            {
                anim.SetBool("IsRunning", false);
            }

            onComplete?.Invoke();
        }
        

        private IEnumerator PatrolRoutine(List<Vector3> path)
        {
            IsPatrolling = true;
            anim.SetBool("IsRunning", true);

            // Tìm điểm mốc gần nhất
            int currentIndex = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < path.Count; i++)
            {
                float d = Vector3.Distance(transform.position, path[i]);
                if (d < minDistance)
                {
                    minDistance = d;
                    currentIndex = i;
                }
            }

            while (IsPatrolling)
            {
                Vector3 targetPoint = path[currentIndex];
                targetPoint.y = transform.position.y; 

                // 1. Xoay đầu về hướng mục tiêu
                Vector3 moveDir = (targetPoint - transform.position);
                moveDir.y = 0;
                if (moveDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }

                // 2. Tịnh tiến về phía trước
                transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

                // 3. Đã tới mốc ô viền -> chuyển sang mốc tiếp theo
                if (Vector3.Distance(transform.position, targetPoint) < 0.05f)
                {
                    currentIndex = (currentIndex + 1) % path.Count;
                }

                yield return null;
            }

            patrolCoroutine = null;
        }
    }
}