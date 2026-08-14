using System;
using Meowdoku.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BankLevelRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text indexLabel;
        [SerializeField] private Text primaryLabel;
        [SerializeField] private Text secondaryLabel;
        [SerializeField] private Text badgeLabel;
        [SerializeField] private Image badgeBackground;
        [SerializeField] private Text arrowLabel;

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

        public void ConfigureLk(
            LevelEntry entry,
            int zeroBasedIndex,
            Color accent,
            Color rankColor,
            Action pressed)
        {
            if (indexLabel != null)
            {
                indexLabel.text = $"#{zeroBasedIndex + 1}";
                indexLabel.color = accent;
            }
            if (primaryLabel != null)
            {
                primaryLabel.fontSize = 32;
                primaryLabel.text = $"{entry.Size} x {entry.Size}";
            }
            if (secondaryLabel != null) secondaryLabel.text = entry.Date;
            if (badgeLabel != null)
            {
                badgeLabel.text = string.IsNullOrEmpty(entry.Label)
                    ? $"R{entry.MaxRank}"
                    : entry.Label;
                badgeLabel.color = rankColor;
            }
            if (badgeBackground != null)
                badgeBackground.gameObject.SetActive(false);
            if (arrowLabel != null)
            {
                arrowLabel.gameObject.SetActive(true);
                arrowLabel.color = accent;
            }
            SetAlternatingBackground(zeroBasedIndex);
            SetPressed(pressed);
        }

        public void ConfigureSpecial(
            LevelEntry entry,
            int zeroBasedIndex,
            Color rankColor,
            Action pressed)
        {
            if (indexLabel != null)
            {
                indexLabel.text = $"#{zeroBasedIndex + 1}";
                indexLabel.color = new Color32(51, 51, 51, 255);
            }
            if (primaryLabel != null)
            {
                primaryLabel.fontSize = 46;
                primaryLabel.text = entry.Pattern;
            }
            if (secondaryLabel != null)
                secondaryLabel.text =
                    $"{entry.Size} x {entry.Size}  R{entry.Rank}";
            if (badgeLabel != null)
            {
                badgeLabel.text = $"R{entry.Rank}";
                badgeLabel.color = Color.white;
            }
            if (badgeBackground != null)
            {
                badgeBackground.gameObject.SetActive(true);
                badgeBackground.color = rankColor;
            }
            if (arrowLabel != null) arrowLabel.gameObject.SetActive(false);
            if (background != null) background.color = Color.white;
            SetPressed(pressed);
        }

        private void SetAlternatingBackground(int index)
        {
            if (background == null) return;
            background.color = index % 2 == 0
                ? Color.white
                : new Color(0.98f, 0.98f, 1f, 1f);
        }

        private void SetPressed(Action pressed)
        {
            _pressed = pressed;
            if (button != null) button.interactable = pressed != null;
        }

        private void HandlePressed()
        {
            _pressed?.Invoke();
        }
    }
}
