using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BoardGridOverlayGraphic : MaskableGraphic
    {
        private const float ThinWidth = 3f;
        private const float ThickWidth = 5f;
        private const float BorderWidth = 7f;
        private const float FrameCornerRadius = 30f;
        private const float ThinStart = 0.1f;
        private const float ThinDuration = 0.75f;
        private const float FrameDuration = 0.75f;
        private const float FrameLead = 0.2f;
        private const float StopMargin = 0.05f;
        private static readonly Color32 GridColor = new Color32(107, 57, 57, 255);

        private int[][] _regions;
        private int _size;
        private float _padding;
        private float _slot;
        private float _boardScale = 1f;
        private float _elapsed = 99999f;
        private float _synchronizedDuration;
        private bool _introActive;
        private readonly List<Vector2> _framePath = new List<Vector2>(53);

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            color = Color.white;
        }

        public void Configure(
            int[][] regions,
            int puzzleSize,
            float padding,
            float slot,
            float boardScale)
        {
            _regions = regions;
            _size = puzzleSize;
            _padding = padding;
            _slot = slot;
            _boardScale = boardScale > 0f ? boardScale : 1f;
            _elapsed = 99999f;
            _introActive = false;
            SetVerticesDirty();
        }

        public void BeginGridIntro(float synchronizedDuration = 0f)
        {
            _synchronizedDuration = Mathf.Max(0f, synchronizedDuration);
            _elapsed = 0f;
            _introActive = true;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (!_introActive) return;
            _elapsed += Time.deltaTime;
            SetVerticesDirty();
            if (_elapsed < IntroEnd()) return;
            _elapsed = 99999f;
            _introActive = false;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (_size <= 0 || !RegionsAreValid()) return;

            Rect rect = rectTransform.rect;
            float left = rect.xMin + _padding;
            float top = rect.yMax - _padding;
            float span = _size * _slot;
            float thinProgress = Mathf.Clamp01((_elapsed - ThinStart) / LineDuration());
            float front = thinProgress * (2f * (_size - 1) + 1f);
            float thinWidth = ThinWidth / _boardScale;
            float thickWidth = ThickWidth / _boardScale;

            for (int k = 1; k < _size; k++)
            {
                DrawGrowingGridLine(vertexHelper, k, 0, k, _size,
                    left, top, thinWidth, front);
                DrawGrowingGridLine(vertexHelper, 0, k, _size, k,
                    left, top, thinWidth, front);
            }

            for (int row = 0; row < _size; row++)
            {
                for (int column = 0; column < _size; column++)
                {
                    int region = _regions[row][column];
                    if (column + 1 < _size && _regions[row][column + 1] != region)
                        DrawGrowingGridLine(vertexHelper, column + 1, row,
                            column + 1, row + 1, left, top, thickWidth, front);
                    if (row + 1 < _size && _regions[row + 1][column] != region)
                        DrawGrowingGridLine(vertexHelper, column, row + 1,
                            column + 1, row + 1, left, top, thickWidth, front);
                }
            }

            float frameProgress = Mathf.Clamp01(_elapsed / FrameDurationValue());
            DrawRoundedFrame(vertexHelper,
                new Rect(left, top - span, span, span),
                FrameCornerRadius / _boardScale,
                BorderWidth / _boardScale,
                frameProgress,
                _framePath);
        }

        private void DrawGrowingGridLine(
            VertexHelper vertices,
            float ax,
            float ay,
            float bx,
            float by,
            float left,
            float top,
            float width,
            float front)
        {
            float ringA = ax - ay + _size - 1f;
            float ringB = bx - by + _size - 1f;
            if (ringA > ringB)
            {
                Swap(ref ax, ref bx);
                Swap(ref ay, ref by);
                Swap(ref ringA, ref ringB);
            }
            if (front <= ringA) return;
            float progress = front >= ringB
                ? 1f
                : (front - ringA) / Mathf.Max(ringB - ringA, 0.0001f);
            Vector2 a = GridToLocal(ax, ay, left, top);
            Vector2 b = GridToLocal(bx, by, left, top);
            AddLine(vertices, a, Vector2.LerpUnclamped(a, b, progress), width);
        }

        private Vector2 GridToLocal(float x, float y, float left, float top)
        {
            return new Vector2(left + x * _slot, top - y * _slot);
        }

        private static void AddLine(VertexHelper vertices, Vector2 a, Vector2 b, float width)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude <= 0.000001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            int start = vertices.currentVertCount;
            vertices.AddVert(a - normal, GridColor, Vector2.zero);
            vertices.AddVert(a + normal, GridColor, Vector2.zero);
            vertices.AddVert(b + normal, GridColor, Vector2.zero);
            vertices.AddVert(b - normal, GridColor, Vector2.zero);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }

        private static void DrawRoundedFrame(
            VertexHelper vertices,
            Rect frame,
            float radius,
            float width,
            float progress,
            List<Vector2> path)
        {
            if (progress <= 0f) return;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(frame.width, frame.height) * 0.5f);
            path.Clear();
            AppendArc(path, frame.xMin + radius, frame.yMax - radius, 180f, 90f, radius);
            AppendArc(path, frame.xMax - radius, frame.yMax - radius, 90f, 0f, radius);
            AppendArc(path, frame.xMax - radius, frame.yMin + radius, 0f, -90f, radius);
            AppendArc(path, frame.xMin + radius, frame.yMin + radius, -90f, -180f, radius);
            path.Add(path[0]);
            DrawPolylineProgress(vertices, path, width, progress);
        }

        private static void AppendArc(
            List<Vector2> path,
            float centerX,
            float centerY,
            float fromDegrees,
            float toDegrees,
            float radius)
        {
            const int Steps = 12;
            for (int step = 0; step <= Steps; step++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, step / (float)Steps) *
                              Mathf.Deg2Rad;
                path.Add(new Vector2(centerX + Mathf.Cos(angle) * radius,
                    centerY + Mathf.Sin(angle) * radius));
            }
        }

        private static void DrawPolylineProgress(
            VertexHelper vertices,
            IReadOnlyList<Vector2> path,
            float width,
            float progress)
        {
            float total = 0f;
            for (int i = 1; i < path.Count; i++) total += Vector2.Distance(path[i - 1], path[i]);
            float remaining = total * Mathf.Clamp01(progress);
            for (int i = 1; i < path.Count && remaining > 0f; i++)
            {
                Vector2 a = path[i - 1];
                Vector2 b = path[i];
                float length = Vector2.Distance(a, b);
                float amount = Mathf.Min(length, remaining);
                AddLine(vertices, a, Vector2.LerpUnclamped(a, b, amount / length), width);
                remaining -= amount;
            }
        }

        private bool RegionsAreValid()
        {
            if (_regions == null || _regions.Length != _size) return false;
            for (int row = 0; row < _size; row++)
                if (_regions[row] == null || _regions[row].Length != _size) return false;
            return true;
        }

#if UNITY_INCLUDE_TESTS
        internal int RegionBoundaryCountForTests
        {
            get
            {
                if (!RegionsAreValid()) return 0;
                int count = 0;
                for (int row = 0; row < _size; row++)
                {
                    for (int column = 0; column < _size; column++)
                    {
                        if (HasVerticalRegionBoundaryForTests(row, column + 1))
                            count++;
                        if (HasHorizontalRegionBoundaryForTests(row + 1, column))
                            count++;
                    }
                }
                return count;
            }
        }

        internal bool HasVerticalRegionBoundaryForTests(
            int row,
            int boundaryColumn)
        {
            return RegionsAreValid() && row >= 0 && row < _size &&
                   boundaryColumn > 0 && boundaryColumn < _size &&
                   _regions[row][boundaryColumn - 1] !=
                   _regions[row][boundaryColumn];
        }

        internal bool HasHorizontalRegionBoundaryForTests(
            int boundaryRow,
            int column)
        {
            return RegionsAreValid() && column >= 0 && column < _size &&
                   boundaryRow > 0 && boundaryRow < _size &&
                   _regions[boundaryRow - 1][column] !=
                   _regions[boundaryRow][column];
        }
#endif

        private float LineDuration() => _synchronizedDuration > 0f
            ? Mathf.Max(_synchronizedDuration - ThinStart, 0.01f)
            : ThinDuration;

        private float FrameDurationValue() => _synchronizedDuration > 0f
            ? Mathf.Max(_synchronizedDuration - FrameLead, 0.01f)
            : FrameDuration;

        private float IntroEnd() =>
            Mathf.Max(ThinStart + LineDuration(), FrameDurationValue()) + StopMargin;

        private static void Swap(ref float left, ref float right)
        {
            float value = left;
            left = right;
            right = value;
        }
    }
}
