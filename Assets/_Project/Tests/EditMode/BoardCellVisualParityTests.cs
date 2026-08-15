using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Meowdoku.Tests.EditMode
{
    public sealed class BoardCellVisualParityTests
    {
        [Test]
        public void SingleLineBoard_UsesSourceOuterCornersAndRegionBoundaries()
        {
            GameObject boardRoot = CreateBoardRoot("SingleLineBoard");
            var cellPrefab = new GameObject(
                "SingleLineCell",
                typeof(RectTransform),
                typeof(CellView));
            try
            {
                BoardView board = boardRoot.AddComponent<BoardView>();
                board.cellPrefab = cellPrefab;
                board.cellsContainer = boardRoot.transform;
                int[][] regions =
                {
                    new[] { 0, 0, 1, 1 },
                    new[] { 0, 2, 2, 1 },
                    new[] { 3, 3, 2, 1 },
                    new[] { 3, 3, 2, 2 }
                };
                int[] colorMap = { 0, 1, 2, 3 };
                var gridConfig = new GameGridUiConfig();
                gridConfig.SetDebugOverride(GameGridUiConfig.ValueSingleLine);

                board.SetupBoard(
                    4,
                    regions,
                    colorMap,
                    gridUiConfig: gridConfig);

                BoardGridOverlayGraphic overlay = board.GridOverlayForTests;
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                Assert.That(overlay.RegionBoundaryCountForTests, Is.EqualTo(11));
                Assert.That(
                    overlay.HasVerticalRegionBoundaryForTests(0, 2),
                    Is.True);
                Assert.That(
                    overlay.HasVerticalRegionBoundaryForTests(0, 1),
                    Is.False);
                Assert.That(
                    overlay.HasHorizontalRegionBoundaryForTests(1, 1),
                    Is.True);
                Assert.That(
                    overlay.HasHorizontalRegionBoundaryForTests(1, 0),
                    Is.False);

                float scale = SourceBoardLayout.Resolve(4, gridConfig)
                    .ScaleFor(4, SourceBoardLayout.FixedBoardWidth);
                float outer = Mathf.RoundToInt(30f / scale);
                AssertCorner(board, 0, 0, new Vector4(outer, 0f, 0f, 0f), true);
                AssertCorner(board, 0, 3, new Vector4(0f, outer, 0f, 0f), true);
                AssertCorner(board, 3, 3, new Vector4(0f, 0f, outer, 0f), true);
                AssertCorner(board, 3, 0, new Vector4(0f, 0f, 0f, outer), true);
                AssertCorner(board, 1, 1, Vector4.zero, true);

                board.SetCellState(0, 0, CellStateType.CAT, false);
                board.SetCellState(0, 0, CellStateType.MARK, false);
                Assert.That(
                    board.GetCellForTests(0, 0).GetState(),
                    Is.EqualTo(CellStateType.CAT));
                board.RestoreCellState(0, 0, CellStateType.EMPTY, false);
                Assert.That(
                    board.GetCellForTests(0, 0).GetState(),
                    Is.EqualTo(CellStateType.EMPTY),
                    "An authoritative model rollback must still repaint CAT.");

                board.SetupBoard(4, regions, colorMap);
                Assert.That(overlay.gameObject.activeSelf, Is.False);
                float defaultScale = SourceBoardLayout.ScaleFor(4);
                float baseRadius = Mathf.RoundToInt(10f / defaultScale);
                AssertCorner(
                    board,
                    0,
                    0,
                    new Vector4(baseRadius, baseRadius, baseRadius, baseRadius),
                    false);
            }
            finally
            {
                Object.DestroyImmediate(boardRoot);
                Object.DestroyImmediate(cellPrefab);
            }
        }

        [Test]
        public void CellStates_MatchSourceVisibilityAndLockedStateIsImmutable()
        {
            CellFixture fixture = CreateVisualCell("StateCell");
            try
            {
                CellView cell = fixture.Cell;
                AssertVisual(cell, false, false, false);

                cell.ChangeState(CellStateType.CAT, false);
                AssertVisual(cell, true, false, false);

                cell.ResetToEmpty();
                cell.ChangeState(CellStateType.MARK, false);
                AssertVisual(cell, false, true, false);
                Assert.That(fixture.Cross.color, Is.EqualTo(Color.white));

                cell.ResetToEmpty();
                cell.ChangeState(CellStateType.ERROR, false);
                AssertVisual(cell, false, false, true);
                Assert.That(fixture.Error.color.r,
                    Is.EqualTo(0.99215686f).Within(0.00001f));
                Assert.That(fixture.Error.color.g,
                    Is.EqualTo(0.41568628f).Within(0.00001f));
                Assert.That(fixture.Error.color.b,
                    Is.EqualTo(0.18039216f).Within(0.00001f));

                cell.ResetToEmpty();
                cell.ChangeState(CellStateType.DRAFT_CROSS, false);
                AssertVisual(cell, false, false, false);
                Assert.That(cell.GetState(), Is.EqualTo(CellStateType.DRAFT_CROSS));

                cell.ResetToEmpty();
                cell.ChangeState(CellStateType.DRAFT_CAT, false);
                AssertVisual(cell, false, false, false);
                Assert.That(cell.GetState(), Is.EqualTo(CellStateType.DRAFT_CAT));

                cell.ResetToEmpty();
                cell.ChangeState(CellStateType.LOCKED_MARK, false);
                AssertVisual(cell, false, true, false);
                cell.ChangeState(CellStateType.EMPTY, false);
                Assert.That(cell.GetState(), Is.EqualTo(CellStateType.LOCKED_MARK));
                AssertVisual(cell, false, true, false);

                cell.ResetToEmpty();
                Assert.That(cell.GetState(), Is.EqualTo(CellStateType.EMPTY));
                AssertVisual(cell, false, false, false);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void CellPoolReset_ClearsVisualsParticleHintsAndTransforms()
        {
            CellFixture fixture = CreateVisualCell("PoolCell");
            try
            {
                CellView cell = fixture.Cell;
                cell.PrepareForUse(1, 2);
                cell.ChangeState(CellStateType.CAT, false);
                fixture.Hint.gameObject.SetActive(true);
                fixture.PromptFrame.gameObject.SetActive(true);
                fixture.PromptCross.gameObject.SetActive(true);
                fixture.Pattern.gameObject.SetActive(true);
                fixture.Background.rectTransform.localScale = Vector3.one * 0.5f;
                fixture.Cat.rectTransform.localScale = Vector3.one * 1.7f;
                fixture.Particles.Emit(3);
                Assert.That(fixture.Particles.particleCount, Is.EqualTo(3));

                cell.ReleaseToPool();

                Assert.That(cell.gameObject.activeSelf, Is.False);
                Assert.That(cell.GetState(), Is.EqualTo(CellStateType.EMPTY));
                AssertVisual(cell, false, false, false);
                Assert.That(cell.IsHintVisibleForTests, Is.False);
                Assert.That(cell.IsPromptVisibleForTests, Is.False);
                Assert.That(cell.IsPatternVisibleForTests, Is.False);
                Assert.That(fixture.Particles.particleCount, Is.Zero);
                Assert.That(fixture.Background.rectTransform.localScale,
                    Is.EqualTo(Vector3.one));
                Assert.That(fixture.Cat.rectTransform.localScale,
                    Is.EqualTo(Vector3.one));

                cell.PrepareForUse(3, 4);
                Assert.That(cell.gameObject.activeSelf, Is.True);
                Assert.That(cell.Row, Is.EqualTo(3));
                Assert.That(cell.Col, Is.EqualTo(4));
                Assert.That(cell.GetState(), Is.EqualTo(CellStateType.EMPTY));
                AssertVisual(cell, false, false, false);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [Test]
        public void HintLayer_StaysBehindCatCrossAndErrorIcons()
        {
            CellFixture fixture = CreateVisualCell("HintLayerCell");
            try
            {
                CellView cell = fixture.Cell;
                cell.PlayHint();

                cell.ChangeState(CellStateType.CAT, false);
                Assert.That(fixture.Cat.transform.GetSiblingIndex(),
                    Is.GreaterThan(fixture.Hint.transform.GetSiblingIndex()));

                cell.ResetToEmpty();
                cell.PlayHint();
                cell.ChangeState(CellStateType.MARK, false);
                Assert.That(fixture.Cross.transform.GetSiblingIndex(),
                    Is.GreaterThan(fixture.Hint.transform.GetSiblingIndex()));

                cell.ResetToEmpty();
                cell.PlayHint();
                cell.ChangeState(CellStateType.ERROR, false);
                Assert.That(fixture.Error.transform.GetSiblingIndex(),
                    Is.GreaterThan(fixture.Hint.transform.GetSiblingIndex()));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private static GameObject CreateBoardRoot(string name)
        {
            return new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(GridLayoutGroup));
        }

        private static void AssertCorner(
            BoardView board,
            int row,
            int column,
            Vector4 expected,
            bool hardEdge)
        {
            CellView cell = board.GetCellForTests(row, column);
            Assert.That(cell, Is.Not.Null);
            Assert.That(cell.CornerRadiiForTests, Is.EqualTo(expected));
            Assert.That(cell.HardEdgeForTests, Is.EqualTo(hardEdge));
        }

        private static void AssertVisual(
            CellView cell,
            bool cat,
            bool cross,
            bool error)
        {
            Assert.That(cell.IsCatVisibleForTests, Is.EqualTo(cat));
            Assert.That(cell.IsCrossVisibleForTests, Is.EqualTo(cross));
            Assert.That(cell.IsErrorVisibleForTests, Is.EqualTo(error));
        }

        private static CellFixture CreateVisualCell(string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            CellView cell = root.AddComponent<CellView>();
            Image background = CreateImage(root.transform, "Background");
            Image cat = CreateImage(root.transform, "Cat");
            Image cross = CreateImage(root.transform, "Cross");
            Image error = CreateImage(root.transform, "Error");
            Image pattern = CreateImage(root.transform, "Pattern");
            Image hint = CreateImage(root.transform, "Hint");
            Image promptFrame = CreateImage(root.transform, "PromptFrame");
            Image promptCross = CreateImage(root.transform, "PromptCross");
            var particleRoot = new GameObject("Particles", typeof(ParticleSystem));
            particleRoot.transform.SetParent(root.transform, false);
            ParticleSystem particles = particleRoot.GetComponent<ParticleSystem>();

            cell.bgImage = background;
            cell.catIcon = cat;
            cell.crossIcon = cross;
            cell.errorIcon = error;
            cell.patternImage = pattern;
            cell.appearVFX = particles;
            var serialized = new SerializedObject(cell);
            serialized.FindProperty("hintLight").objectReferenceValue = hint;
            serialized.FindProperty("promptFrame").objectReferenceValue = promptFrame;
            serialized.FindProperty("promptCross").objectReferenceValue = promptCross;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            cell.PrepareForUse(0, 0);

            return new CellFixture(
                root,
                cell,
                background,
                cat,
                cross,
                error,
                pattern,
                hint,
                promptFrame,
                promptCross,
                particles);
        }

        private static Image CreateImage(Transform parent, string name)
        {
            var root = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(parent, false);
            return root.GetComponent<Image>();
        }

        private sealed class CellFixture
        {
            public CellFixture(
                GameObject root,
                CellView cell,
                Image background,
                Image cat,
                Image cross,
                Image error,
                Image pattern,
                Image hint,
                Image promptFrame,
                Image promptCross,
                ParticleSystem particles)
            {
                Root = root;
                Cell = cell;
                Background = background;
                Cat = cat;
                Cross = cross;
                Error = error;
                Pattern = pattern;
                Hint = hint;
                PromptFrame = promptFrame;
                PromptCross = promptCross;
                Particles = particles;
            }

            public GameObject Root { get; }
            public CellView Cell { get; }
            public Image Background { get; }
            public Image Cat { get; }
            public Image Cross { get; }
            public Image Error { get; }
            public Image Pattern { get; }
            public Image Hint { get; }
            public Image PromptFrame { get; }
            public Image PromptCross { get; }
            public ParticleSystem Particles { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(Root);
            }
        }
    }
}
