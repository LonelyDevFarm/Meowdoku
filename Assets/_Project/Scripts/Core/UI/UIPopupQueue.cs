using System;
using System.Collections;
using System.Collections.Generic;

namespace Meowdoku.Core.UI
{
    public sealed class UIPopupEntry
    {
        public UIPopupEntry(string key, int priority, Func<IEnumerator> execute)
        {
            Key = key ?? string.Empty;
            Priority = priority;
            Execute = execute;
        }

        public string Key { get; }
        public int Priority { get; }
        public Func<IEnumerator> Execute { get; }
    }

    /// <summary>
    /// Independent popup sequence ported from ui_popup_queue.gd. Higher
    /// priority runs first; equal priorities retain enqueue order.
    /// </summary>
    public sealed class UIPopupQueue
    {
        private readonly List<UIPopupEntry> _queue = new();

        public int Count => _queue.Count;
        public bool IsRunning { get; private set; }

        public void Enqueue(UIPopupEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            int index = _queue.Count;
            for (int current = 0; current < _queue.Count; current++)
            {
                if (entry.Priority <= _queue[current].Priority) continue;
                index = current;
                break;
            }
            _queue.Insert(index, entry);
        }

        public void EnqueueAll(IEnumerable<UIPopupEntry> entries)
        {
            if (entries == null) return;
            foreach (UIPopupEntry entry in entries) Enqueue(entry);
        }

        public void InsertNext(UIPopupEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _queue.Insert(0, entry);
        }

        public void Cancel(string key)
        {
            _queue.RemoveAll(entry =>
                string.Equals(entry.Key, key, StringComparison.Ordinal));
        }

        public void Clear()
        {
            _queue.Clear();
        }

        /// <summary>
        /// Unity lifecycle adapter: StopCoroutine does not guarantee that an
        /// iterator's finally block runs. A page that owns a queue must abort
        /// it before its managed coroutine is stopped.
        /// </summary>
        public void Abort()
        {
            _queue.Clear();
            IsRunning = false;
        }

        public IEnumerator Flush()
        {
            if (IsRunning) yield break;
            IsRunning = true;
            try
            {
                while (_queue.Count > 0)
                {
                    UIPopupEntry entry = _queue[0];
                    _queue.RemoveAt(0);
                    IEnumerator routine = entry.Execute?.Invoke();
                    if (routine != null) yield return routine;
                }
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
