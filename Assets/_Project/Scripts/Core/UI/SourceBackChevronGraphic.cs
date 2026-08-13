using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Core.UI
{
    /// <summary>
    /// UGUI mesh adapter for the source btn_back_white.svg. The project does
    /// not include Unity's optional SVG importer, so keeping an Image with a
    /// null sprite would render a white rectangle instead of the chevron.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SourceBackChevronGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            float width = rect.width;
            float height = rect.height;
            if (width <= 0f || height <= 0f) return;

            Vector2[] normalized =
            {
                new(0.77f, 1f),
                new(0f, 0.5f),
                new(0.77f, 0f),
                new(1f, 0.143f),
                new(0.46f, 0.5f),
                new(1f, 0.857f)
            };
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            for (int index = 0; index < normalized.Length; index++)
            {
                vertex.position = new Vector3(
                    rect.xMin + normalized[index].x * width,
                    rect.yMin + normalized[index].y * height);
                helper.AddVert(vertex);
            }

            helper.AddTriangle(0, 1, 4);
            helper.AddTriangle(0, 4, 5);
            helper.AddTriangle(1, 2, 3);
            helper.AddTriangle(1, 3, 4);
        }
    }
}
