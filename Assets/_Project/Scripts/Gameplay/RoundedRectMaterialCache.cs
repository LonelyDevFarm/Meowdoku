using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    internal static class RoundedRectMaterialCache
    {
        private readonly struct Key : IEquatable<Key>
        {
            public Key(int shaderId, Vector2 size, Vector4 radii, bool hard)
            {
                ShaderId = shaderId;
                Width = Mathf.RoundToInt(size.x);
                Height = Mathf.RoundToInt(size.y);
                TopLeft = Mathf.RoundToInt(radii.x);
                TopRight = Mathf.RoundToInt(radii.y);
                BottomRight = Mathf.RoundToInt(radii.z);
                BottomLeft = Mathf.RoundToInt(radii.w);
                Hard = hard;
            }

            private int ShaderId { get; }
            private int Width { get; }
            private int Height { get; }
            private int TopLeft { get; }
            private int TopRight { get; }
            private int BottomRight { get; }
            private int BottomLeft { get; }
            private bool Hard { get; }

            public bool Equals(Key other) => ShaderId == other.ShaderId &&
                Width == other.Width && Height == other.Height &&
                TopLeft == other.TopLeft && TopRight == other.TopRight &&
                BottomRight == other.BottomRight && BottomLeft == other.BottomLeft &&
                Hard == other.Hard;

            public override bool Equals(object obj) => obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = ShaderId;
                    hash = hash * 397 ^ Width;
                    hash = hash * 397 ^ Height;
                    hash = hash * 397 ^ TopLeft;
                    hash = hash * 397 ^ TopRight;
                    hash = hash * 397 ^ BottomRight;
                    hash = hash * 397 ^ BottomLeft;
                    return hash * 397 ^ (Hard ? 1 : 0);
                }
            }
        }

        private static readonly Dictionary<Key, Material> Materials =
            new Dictionary<Key, Material>();
        private static readonly int ShapeSizeId = Shader.PropertyToID("_ShapeSize");
        private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
        private static readonly int HardEdgeId = Shader.PropertyToID("_HardEdge");

        public static Material Get(Shader shader, Vector2 size, Vector4 radii, bool hard)
        {
            if (shader == null) return null;
            var key = new Key(shader.GetInstanceID(), size, radii, hard);
            if (Materials.TryGetValue(key, out Material material) && material != null)
                return material;

            material = new Material(shader)
            {
                name = $"RoundedRect_{Mathf.RoundToInt(radii.x)}_{Mathf.RoundToInt(radii.y)}_" +
                       $"{Mathf.RoundToInt(radii.z)}_{Mathf.RoundToInt(radii.w)}_{(hard ? 1 : 0)}",
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetVector(ShapeSizeId, size);
            material.SetVector(CornerRadiusId, radii);
            material.SetFloat(HardEdgeId, hard ? 1f : 0f);
            Materials.Add(key, material);
            return material;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            foreach (Material material in Materials.Values)
            {
                if (material == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            Materials.Clear();
        }
    }
}
