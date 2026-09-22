using UnityEngine;

namespace _Script
{
    public class HelpMethod
    {
        /// <summary>
        /// Tự động thêm hoặc chỉnh BoxCollider ôm khít mô hình con lợn
        /// </summary>
        public static BoxCollider FitBoxCollider(GameObject target)
        {
            BoxCollider boxCollider = target.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = target.AddComponent<BoxCollider>();
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return boxCollider;

            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            boxCollider.center = target.transform.InverseTransformPoint(totalBounds.center);

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