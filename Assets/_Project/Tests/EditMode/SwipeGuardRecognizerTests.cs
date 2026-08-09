using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay.Input;
using NUnit.Framework;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SwipeGuardRecognizerTests
    {
        private const int Size = 6;
        private const int Slot = 108;
        private const int Padding = 20;
        private const int Cell = 100;

        [Test]
        public void ControlConfig_UsesRawBoardCoordinates()
        {
            SwipeGuardRecognizer recognizer = Create(SwipeProtectConfig.ValueControl);
            recognizer.OnDragStart(Position(0, 0), 0);

            var actions = recognizer.OnDragOver(Position(1, 1), 20);

            Assert.That(actions[actions.Count - 1].Row, Is.EqualTo(1));
            Assert.That(actions[actions.Count - 1].Column, Is.EqualTo(1));
        }

        [Test]
        public void PointerDown_UsesAuthoritativeCellInsteadOfReResolvedCursorPosition()
        {
            SwipeGuardRecognizer recognizer = Create(SwipeProtectConfig.ValueControl);

            var actions = recognizer.OnDragStart(
                Position(3, 3),
                new Vector2Int(0, 0),
                0);

            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Row, Is.Zero);
            Assert.That(actions[0].Column, Is.Zero);
        }

        [Test]
        public void PressPositionLatch_KeepsPositionBeforeLaterPointInSameFrame()
        {
            var latch = new PointerPressPositionLatch();
            Vector2 pressedAt = Position(0, 0);
            latch.RecordPosition(pressedAt, 7);
            latch.RecordButton(true, 7);
            latch.RecordPosition(Position(3, 3), 7);

            Assert.That(latch.TryConsume(7, out Vector2 result), Is.True);
            Assert.That(result, Is.EqualTo(pressedAt));
            Assert.That(latch.TryConsume(7, out _), Is.False);
        }

        [Test]
        public void PressPositionLatch_RawPressCannotBeOverwrittenByActionCallback()
        {
            var latch = new PointerPressPositionLatch();
            Vector2 pressedAt = Position(0, 0);
            latch.RecordPosition(pressedAt, 7);
            latch.RecordPressPosition(pressedAt, 7);
            latch.RecordPosition(Position(3, 3), 7);
            latch.RecordButton(true, 7);

            Assert.That(latch.TryConsume(7, out Vector2 result), Is.True);
            Assert.That(result, Is.EqualTo(pressedAt));
        }

        [Test]
        public void EnabledGuard_LocksEstablishedRowWithinTolerance()
        {
            SwipeGuardRecognizer recognizer = Create(SwipeProtectConfig.ValueHotzone40);
            recognizer.OnDragStart(Position(0, 0), 0);
            recognizer.OnDragOver(Position(1, 0), 20);
            recognizer.OnDragOver(Position(2, 0), 40);
            recognizer.OnDragOver(Position(3, 0), 60);

            Vector2 withinToleranceInRawRowOne = new Vector2(Center(4), 150f);
            var actions = recognizer.OnDragOver(withinToleranceInRawRowOne, 80);

            Assert.That(actions[actions.Count - 1].Row, Is.Zero);
            Assert.That(actions[actions.Count - 1].Column, Is.EqualTo(4));
        }

        [Test]
        public void RaisedGuard_IsDisabledBelowMinimumBoardSize()
        {
            SwipeGuardRecognizer recognizer = Create(SwipeProtectConfig.ValueHotzoneRaised);
            recognizer.OnDragStart(Position(0, 0), 0);
            recognizer.OnDragOver(Position(1, 0), 20);
            recognizer.OnDragOver(Position(2, 0), 40);
            recognizer.OnDragOver(Position(3, 0), 60);

            var actions = recognizer.OnDragOver(Position(4, 1), 80);

            Assert.That(actions[actions.Count - 1].Row, Is.EqualTo(1));
            Assert.That(actions[actions.Count - 1].Column, Is.EqualTo(4));
        }

        private static SwipeGuardRecognizer Create(int configValue)
        {
            var config = new SwipeProtectConfig();
            config.SetDebugOverride(configValue);
            var inner = new BoardGestureRecognizer(
                new BoardInputScheme(new EmptyBoard()));
            var recognizer = new SwipeGuardRecognizer(inner, config, ResolveRawCell);
            recognizer.ConfigureBoard(Size, Slot, Padding, Cell);
            return recognizer;
        }

        private static Vector2Int ResolveRawCell(Vector2 position)
        {
            if (position.x < Padding || position.y < Padding)
                return new Vector2Int(-1, -1);
            int column = Mathf.FloorToInt((position.x - Padding) / Slot);
            int row = Mathf.FloorToInt((position.y - Padding) / Slot);
            return row >= 0 && row < Size && column >= 0 && column < Size
                ? new Vector2Int(column, row)
                : new Vector2Int(-1, -1);
        }

        private static Vector2 Position(int column, int row)
        {
            return new Vector2(Center(column), Center(row));
        }

        private static float Center(int index)
        {
            return Padding + index * Slot + Cell / 2f;
        }

        private sealed class EmptyBoard : IBoardStateReader
        {
            public CellStateType GetCellState(int row, int column)
            {
                return CellStateType.EMPTY;
            }
        }
    }
}
