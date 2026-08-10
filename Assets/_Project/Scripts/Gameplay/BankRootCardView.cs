using System;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BankRootCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text title;
        [SerializeField] private Text subtitle;
        [SerializeField] private Text count;
        [SerializeField] private Text metadata;
        [SerializeField] private Text arrow;

        private Action _pressed;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandlePressed);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandlePressed);
            _pressed = null;
        }

        public void Configure(
            string titleText,
            string subtitleText,
            string countText,
            string metadataText,
            Color accent,
            Color backgroundColor,
            Action pressed)
        {
            if (title != null)
            {
                title.text = titleText ?? string.Empty;
                title.color = accent;
            }
            if (subtitle != null) subtitle.text = subtitleText ?? string.Empty;
            if (count != null) count.text = countText ?? string.Empty;
            if (metadata != null)
            {
                metadata.text = metadataText ?? string.Empty;
                metadata.gameObject.SetActive(!string.IsNullOrEmpty(metadataText));
            }
            if (arrow != null) arrow.color = accent;
            if (background != null) background.color = backgroundColor;
            if (button != null) button.interactable = pressed != null;
            _pressed = pressed;
        }

        private void HandlePressed()
        {
            _pressed?.Invoke();
        }
    }
}
