using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace blep
{
    public class CodeBlepGridLayoutGroupWidthModifier : MonoBehaviour
    {
        public int RowOrColumnCount = 5;

        public GridLayoutGroup GridLayout;
        public RectTransform GridContainer;

        [ReadOnly] public Vector2 GridContainerRectDimensions;

        private Vector2 PreviousGridContainerSize;

        // -----------------------------------------------------------------------------------------

        private void Update()
        {
            ResizeGridCell();
        }

        // -----------------------------------------------------------------------------------------

        [Button]
        public void ResizeGridCell(bool force = false)
        {
            GridContainerRectDimensions = new Vector2(GridContainer.rect.width, GridContainer.rect.height);

            if (force || GridContainerRectDimensions != PreviousGridContainerSize)
            {
                PreviousGridContainerSize = GridContainerRectDimensions;
                GridLayout.cellSize = new Vector2((GridContainerRectDimensions.x / RowOrColumnCount) - GridLayout.spacing.x, GridLayout.cellSize.y);
            }
        }
    }
}