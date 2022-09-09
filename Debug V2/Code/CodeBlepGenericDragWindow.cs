using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace blep
{
    public class CodeBlepGenericDragWindow : MonoBehaviour, IPointerDownHandler
    {
        //[BoxGroup("CONFIG")] public bool IsActive;
        [Required][BoxGroup("MISC REFS")] public RectTransform Root;
        [Required][BoxGroup("MISC REFS")] public Image DragImage;
        
#pragma warning disable 0414
        private bool MouseHeldDown;

        private Vector2 DragOffset;
        private bool IsDragging;
        private Vector3 MousePosition;
        
        //---------------------------------------------------------------------------------------

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                MouseHeldDown = true;
            }
            else
            {
                IsDragging = false;
                MouseHeldDown = false;
            }
            
            DragWindow();
        }
        
        //---------------------------------------------------------------------------------------
        
        private void DragWindow()
        {
            MousePosition = Input.mousePosition;
            
            if (IsDragging)
            {
                Root.gameObject.transform.position = (Vector2)MousePosition + DragOffset;
            }
        }
        
        //---------------------------------------------------------------------------------------
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (DragImage.gameObject == eventData.pointerCurrentRaycast.gameObject)
            {
                IsDragging = true;
                DragOffset = Root.gameObject.transform.position - Input.mousePosition;
            }
        }
        
        //---------------------------------------------------------------------------------------

        public void VirtualPointerDown(Vector3 mousePosition)
        {
            IsDragging = true;
            DragOffset = Root.gameObject.transform.position - mousePosition;
        }
    }
}