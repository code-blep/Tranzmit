using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace blep
{
    public class CodeBlepGenericResizeWindow : MonoBehaviour, IPointerDownHandler
    {
        public ResizeModes ResizeMode;
        
        [Required][BoxGroup("MISC REFS")] public RectTransform Root;
        [Required][BoxGroup("MISC REFS")] public Image ResizeImage;

        public Vector2Int StartSize = new Vector2Int(500, 500);
        public Vector2Int MinSize = new Vector2Int(300, 300);

#pragma warning disable 0414
        private bool MouseHeldDown;
        
        private bool IsResizing;
        private int ResizeMouseXDifference;
        private int ResizeMouseYDifference;
        private int ResizeDeltaXFinal;
        private int ResizeDeltaYFinal;
        private Vector3 ResizeMouseStart;
        private Vector2 ResizeRootDeltaSizeStart;
        private Vector3 MousePosition;
        
        public enum ResizeModes {None, X, Y, All}
        
        //---------------------------------------------------------------------------------------

        private void Start()
        {
            Root.sizeDelta = StartSize;
        }

        //---------------------------------------------------------------------------------------
        
        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                MouseHeldDown = true;
            }
            else
            {
                IsResizing = false;
                MouseHeldDown = false;
            }
            
            ResizeWindow();
        }
        
        //---------------------------------------------------------------------------------------
        
        private void ResizeWindow()
        {
            MousePosition = Input.mousePosition;
            
            // RESIZING
            if (IsResizing && ResizeMode != ResizeModes.None)
            {
                // X
                if (ResizeMode == ResizeModes.X || ResizeMode == ResizeModes.All)
                {
                    ResizeMouseXDifference = (int) ResizeMouseStart.x - (int) Input.mousePosition.x;
                    ResizeDeltaXFinal = (int) ResizeRootDeltaSizeStart.x - ResizeMouseXDifference;
                    ResizeDeltaXFinal = Mathf.Clamp(ResizeDeltaXFinal, MinSize.x, int.MaxValue);
                    Root.sizeDelta = new Vector2(ResizeDeltaXFinal, Root.sizeDelta.y);
                }
                
                // Y
                if (ResizeMode == ResizeModes.Y || ResizeMode == ResizeModes.All)
                {
                    ResizeMouseYDifference = (int) ResizeMouseStart.y - (int) Input.mousePosition.y;
                    ResizeDeltaYFinal = (int) ResizeRootDeltaSizeStart.y + ResizeMouseYDifference;
                    ResizeDeltaYFinal = Mathf.Clamp(ResizeDeltaYFinal, MinSize.y, int.MaxValue);
                    Root.sizeDelta = new Vector2(Root.sizeDelta.x, ResizeDeltaYFinal);
                }
            }
        }
        
        //---------------------------------------------------------------------------------------
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (ResizeImage.gameObject == eventData.pointerCurrentRaycast.gameObject)
            {
                IsResizing = true;
                ResizeMouseStart = MousePosition;
                ResizeRootDeltaSizeStart = Root.sizeDelta;
            }
        }
    }
}