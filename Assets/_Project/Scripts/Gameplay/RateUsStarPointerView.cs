using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Meowdoku.Gameplay
{
    /// <summary>Pointer/drag adapter for the five source star hit areas.</summary>
    [DisallowMultipleComponent]
    public sealed class RateUsStarPointerView : MonoBehaviour,
        IPointerDownHandler, IDragHandler
    {
        [SerializeField] private int starIndex = 1;
        private Action<int> _select;

        public void Bind(Action<int> select) => _select = select;

        public void OnPointerDown(PointerEventData eventData) => _select?.Invoke(starIndex);

        public void OnDrag(PointerEventData eventData)
        {
            if (_select == null) return;
            RectTransform parent = transform.parent as RectTransform;
            if (parent == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, eventData.position, eventData.pressEventCamera, out local))
                return;
            float width = parent.rect.width / 5f;
            int selected = Mathf.FloorToInt((local.x - parent.rect.xMin) / width) + 1;
            _select(Mathf.Clamp(selected, 1, 5));
        }
    }
}
