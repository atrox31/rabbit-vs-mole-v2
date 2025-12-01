using UnityEngine;

namespace HierarchyIcons
{
    [ExecuteAlways]
    public class Folder : MonoBehaviour
    {

#if UNITY_EDITOR

        [SerializeField] private bool haveZeroPos;
        [SerializeField] private string gizmoIconName = "Folder Icon";

        private void LateUpdate()
        {
            if (!Application.isPlaying && haveZeroPos)
            {
                transform.localPosition = Vector3.zero;
            }
        }

        private void OnValidate()
        {
            // Ensure the Gizmo is redrawn immediately in the Scene View
            // whenever the gizmoIconName field is changed in the Inspector.
            UnityEditor.SceneView.RepaintAll();
        }
        private void OnDrawGizmos()
        {
            if (string.IsNullOrEmpty(gizmoIconName))
            {
                return;
            }
            var filters = GetComponentsInChildren<Renderer>();
            if (filters.Length != 0)
            {
                float count = 0;
                Vector3 center = new Vector3();
                for (int i = 0; i < filters.Length; i++)
                {
                    center += (filters[i].bounds.center);
                    count++;
                }

                Gizmos.DrawIcon(center / count, gizmoIconName);
            }
            else
            {
                Gizmos.DrawIcon(transform.position, gizmoIconName);
            }
        }
        
#endif
    }
}