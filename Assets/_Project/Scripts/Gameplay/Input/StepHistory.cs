using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Gameplay.Input
{
    public sealed class StepHistory
    {
        public sealed class CellChange
        {
            public Vector2Int Position;
            public CellStateType Before;
            public CellStateType After;
        }

        public sealed class StepRecord
        {
            public readonly List<CellChange> Cells = new List<CellChange>();
            public bool IsCatPlacement;
            public bool IsWrongGuess;
        }

        private readonly List<StepRecord> _history = new List<StepRecord>();
        public int Count => _history.Count;

        public void Push(StepRecord step)
        {
            if (step != null && step.Cells.Count > 0) _history.Add(step);
        }

        public StepRecord PopLast()
        {
            if (_history.Count == 0) return null;
            int index = _history.Count - 1;
            StepRecord result = _history[index];
            _history.RemoveAt(index);
            return result;
        }

        public StepRecord PeekLast() { return _history.Count > 0 ? _history[_history.Count - 1] : null; }
        public StepRecord PeekAt(int index)
        {
            return index >= 0 && index < _history.Count ? _history[index] : null;
        }

        public bool HasStep() { return _history.Count > 0; }
        public void Clear() { _history.Clear(); }

        public List<object> Serialize()
        {
            var result = new List<object>(_history.Count);
            for (int i = 0; i < _history.Count; i++)
            {
                StepRecord step = _history[i];
                var cells = new List<object>(step.Cells.Count);
                for (int j = 0; j < step.Cells.Count; j++)
                {
                    CellChange cell = step.Cells[j];
                    cells.Add(new List<object>
                    {
                        cell.Position.x,
                        cell.Position.y,
                        (int)cell.Before,
                        (int)cell.After
                    });
                }

                result.Add(new Dictionary<string, object>
                {
                    { "cells", cells },
                    { "cat", step.IsCatPlacement },
                    { "wrong", step.IsWrongGuess }
                });
            }
            return result;
        }

        public void Deserialize(IList data)
        {
            _history.Clear();
            if (data == null) return;

            for (int i = 0; i < data.Count; i++)
            {
                IDictionary item = data[i] as IDictionary;
                if (item == null) continue;
                var step = new StepRecord
                {
                    IsCatPlacement = ReadBool(item, "cat"),
                    IsWrongGuess = ReadBool(item, "wrong")
                };

                IList cells = item["cells"] as IList;
                if (cells != null)
                {
                    for (int j = 0; j < cells.Count; j++)
                    {
                        IList values = cells[j] as IList;
                        if (values == null || values.Count < 4) continue;
                        step.Cells.Add(new CellChange
                        {
                            Position = new Vector2Int(
                                Convert.ToInt32(values[0]),
                                Convert.ToInt32(values[1])),
                            Before = (CellStateType)Convert.ToInt32(values[2]),
                            After = (CellStateType)Convert.ToInt32(values[3])
                        });
                    }
                }

                if (step.Cells.Count > 0) _history.Add(step);
            }
        }

        private static bool ReadBool(IDictionary item, string key)
        {
            return item.Contains(key) && Convert.ToBoolean(item[key]);
        }
    }
}
