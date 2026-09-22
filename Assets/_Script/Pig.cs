using System;
using UnityEngine;

namespace _Script
{
    public class Pig: MonoBehaviour
    {
        private BoxCollider boxCollider;

        private void Awake()
        {
            FitBoxCollider(gameObject);
        }

        /// <summary>
        /// Tự động thêm hoặc chỉnh BoxCollider ôm khít mô hình con lợn
        /// </summary>
        public static BoxCollider FitBoxCollider(GameObject target)
        {
            // Lấy hoặc thêm BoxCollider vào đối tượng gốc
            BoxCollider boxCollider = target.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = target.AddComponent<BoxCollider>();
            }

            // Lấy tất cả Renderer (MeshRenderer hoặc SkinnedMeshRenderer) của đối tượng và các con
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return boxCollider;

            // Tính Bounds tổng trong không gian thế giới (World Space)
            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            // Chuyển đổi Bounds từ World Space sang Local Space của target
            // 1. Tâm (Center) trong Local Space
            boxCollider.center = target.transform.InverseTransformPoint(totalBounds.center);

            // 2. Kích thước (Size) trong Local Space (loại bỏ ảnh hưởng bởi lossyScale của cha)
            Vector3 worldSize = totalBounds.size;
            Vector3 lossyScale = target.transform.lossyScale;

            boxCollider.size = new Vector3(
                lossyScale.x != 0 ? worldSize.x / lossyScale.x : 0,
                lossyScale.y != 0 ? worldSize.y / lossyScale.y : 0,
                lossyScale.z != 0 ? worldSize.z / lossyScale.z : 0
            );

            return boxCollider;
        }
    }
}