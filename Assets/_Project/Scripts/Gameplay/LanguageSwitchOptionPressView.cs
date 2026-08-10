using UnityEngine;
using UnityEngine.EventSystems;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LanguageSwitchOptionPressView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [SerializeField] private GameObject highlight;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (highlight != null) highlight.SetActive(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (highlight != null) highlight.SetActive(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlight != null) highlight.SetActive(false);
        }
    }
}
