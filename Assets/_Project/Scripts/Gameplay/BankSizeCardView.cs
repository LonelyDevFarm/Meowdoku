using System;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BankSizeCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text sizeLabel;
        [SerializeField] private Text tierLabel;
        [SerializeField] private Text countLabel;
        [SerializeField] private Text ranksLabel;

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
            int size,
            string tier,
            int count,
            string ranks,
            Color tierColor,
            Action pressed)
        {
            if (sizeLabel != null) sizeLabel.text = $"{size} x {size}";
            if (tierLabel != null)
            {
                tierLabel.text = tier ?? string.Empty;
                tierLabel.color = tierColor;
            }
            if (countLabel != null) countLabel.text = $"{count} levels";
            if (ranksLabel != null) ranksLabel.text = ranks ?? string.Empty;
            if (button != null) button.interactable = pressed != null;
            _pressed = pressed;
        }

        private void HandlePressed()
        {
            _pressed?.Invoke();
        }
    }
}
