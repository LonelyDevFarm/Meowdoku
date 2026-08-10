using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TutorialFinishEffects : MonoBehaviour
    {
        private static readonly Color[] SourceColors =
        {
            new Color32(0xFF, 0x52, 0x52, 0xFF),
            new Color32(0x44, 0x8A, 0xFF, 0xFF),
            new Color32(0x69, 0xF0, 0xAE, 0xFF),
            new Color32(0xFF, 0xD7, 0x40, 0xFF),
            new Color32(0xFF, 0x40, 0x81, 0xFF),
            new Color32(0x40, 0xC4, 0xFF, 0xFF)
        };

        [SerializeField] private RectTransform effectRoot;

        private readonly List<GameObject> _active = new(30);

        public void PlayDefaultConfetti()
        {
            Clear();
            if (effectRoot == null) return;
            for (int index = 0; index < 30; index++)
            {
                var piece = new GameObject(
                    $"Confetti_{index:00}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                piece.layer = effectRoot.gameObject.layer;
                RectTransform rect = piece.GetComponent<RectTransform>();
                rect.SetParent(effectRoot, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(
                    Random.Range(6f, 14f),
                    Random.Range(10f, 22f));
                rect.anchoredPosition = new Vector2(
                    Random.Range(40f, 1040f),
                    Random.Range(40f, 150f));
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, Random.Range(0f, 360f));
                Image image = piece.GetComponent<Image>();
                image.color = SourceColors[Random.Range(0, SourceColors.Length)];
                image.raycastTarget = false;
                _active.Add(piece);

                rect.DOAnchorPosY(-1980f, Random.Range(2f, 3.5f))
                    .SetDelay(Random.Range(0f, 0.6f))
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .SetId(this)
                    .OnComplete(() => Release(piece));
            }
        }

        public void Clear()
        {
            DOTween.Kill(this, false);
            for (int index = _active.Count - 1; index >= 0; index--)
            {
                if (_active[index] != null) Destroy(_active[index]);
            }
            _active.Clear();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Release(GameObject piece)
        {
            _active.Remove(piece);
            if (piece != null) Destroy(piece);
        }
    }
}
