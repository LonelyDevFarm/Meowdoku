using System;
using Meowdoku.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BankTierCardView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image badgeBackground;
        [SerializeField] private Text badgeLabel;
        [SerializeField] private Text descriptionLabel;
        [SerializeField] private Text countLabel;
        [SerializeField] private Text numberLabel;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button goButton;
        [SerializeField] private Image goBackground;

        private Action<int> _launch;
        private int _count;
        private int _number = 1;

        public int CountForTests => _count;
        public int NumberForTests => _number;

        private void Awake()
        {
            if (minusButton != null)
                minusButton.onClick.AddListener(Decrease);
            if (plusButton != null)
                plusButton.onClick.AddListener(Increase);
            if (goButton != null) goButton.onClick.AddListener(Launch);
        }

        private void OnDestroy()
        {
            if (minusButton != null)
                minusButton.onClick.RemoveListener(Decrease);
            if (plusButton != null)
                plusButton.onClick.RemoveListener(Increase);
            if (goButton != null) goButton.onClick.RemoveListener(Launch);
            _launch = null;
        }

        public void Configure(
            BankTierBucket bucket,
            Color backgroundColor,
            Color badgeColor,
            Color goColor,
            Action<int> launch)
        {
            _count = Mathf.Max(0, bucket.Count);
            _number = _count > 0 ? 1 : 0;
            _launch = launch;
            if (background != null) background.color = backgroundColor;
            if (badgeBackground != null) badgeBackground.color = badgeColor;
            if (goBackground != null) goBackground.color = goColor;
            if (badgeLabel != null) badgeLabel.text = bucket.Definition.Label;
            if (descriptionLabel != null)
                descriptionLabel.text = bucket.Definition.Description;
            if (countLabel != null) countLabel.text = $"Total: {_count} levels";
            RefreshNumber();
        }

        private void Decrease()
        {
            if (_count <= 0) return;
            _number = Mathf.Max(1, _number - 1);
            RefreshNumber();
        }

        private void Increase()
        {
            if (_count <= 0) return;
            _number = Mathf.Min(_count, _number + 1);
            RefreshNumber();
        }

        private void Launch()
        {
            if (_count > 0) _launch?.Invoke(_number - 1);
        }

        private void RefreshNumber()
        {
            if (numberLabel != null) numberLabel.text = _number.ToString();
            if (minusButton != null)
                minusButton.interactable = _number > 1;
            if (plusButton != null)
                plusButton.interactable = _number > 0 && _number < _count;
            if (goButton != null)
                goButton.interactable = _count > 0 && _launch != null;
        }
    }
}
