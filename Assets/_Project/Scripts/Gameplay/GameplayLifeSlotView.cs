using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>Unity presentation equivalent of the source LifeSlot.</summary>
    public sealed class GameplayLifeSlotView : MonoBehaviour
    {
        [SerializeField] private Image dimImage;
        [SerializeField] private Image fullImage;
        [SerializeField] private Image reviveGlow;
        [SerializeField] private Image[] fishParticles = new Image[6];
        [SerializeField] private Image[] glowParticles = new Image[6];

        private Sequence _sequence;
        private bool _isLost;

        public void ShowAlive()
        {
            KillSequence();
            ResetEffects();
            _isLost = false;
            if (dimImage != null) dimImage.gameObject.SetActive(false);
            if (fullImage == null) return;
            fullImage.gameObject.SetActive(true);
            fullImage.color = Color.white;
            fullImage.rectTransform.anchoredPosition = Vector2.zero;
            fullImage.rectTransform.localScale = Vector3.one;
        }

        public void ShowLost(bool animate, bool silent = false)
        {
            if (!animate && _isLost) return;
            KillSequence();
            ResetEffects();
            _isLost = true;
            if (dimImage != null) dimImage.gameObject.SetActive(true);
            if (fullImage == null) return;
            fullImage.gameObject.SetActive(true);
            fullImage.color = Color.white;
            fullImage.rectTransform.anchoredPosition = Vector2.zero;
            fullImage.rectTransform.localScale = Vector3.one;

            if (!animate)
            {
                fullImage.gameObject.SetActive(false);
                return;
            }

            if (silent)
            {
                _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
                _sequence.Join(DOVirtual.Float(1f, 0f, 0.3f, SetFullAlpha)
                    .SetEase(Ease.Linear));
                _sequence.Join(fullImage.rectTransform.DOScale(0.8f, 0.3f)
                    .SetEase(Ease.Linear));
                _sequence.OnComplete(() => fullImage.gameObject.SetActive(false));
                return;
            }

            // Source Appear keys: y 0 -> -25 -> 0, then Full hides at 0.25 s.
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(DOVirtual.Float(0f, -25f, 0.15176266f, SetFullY)
                .SetEase(Ease.InOutQuad));
            _sequence.Append(DOVirtual.Float(-25f, 0f, 0.21321014f, SetFullY)
                .SetEase(Ease.InOutQuad));
            _sequence.InsertCallback(0.21666667f, PlayLostBurst);
            _sequence.InsertCallback(0.25f, () => fullImage.gameObject.SetActive(false));
            _sequence.AppendInterval(0.8f - 0.3649728f);
        }

        public void PlayRevive()
        {
            KillSequence();
            ResetEffects();
            _isLost = false;
            if (dimImage != null) dimImage.gameObject.SetActive(false);
            if (fullImage == null) return;
            fullImage.gameObject.SetActive(true);
            fullImage.color = new Color(1f, 1f, 1f, 0f);
            fullImage.rectTransform.anchoredPosition = Vector2.zero;
            fullImage.rectTransform.localScale = Vector3.one * 0.3f;

            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Append(fullImage.rectTransform.DOScale(1.3f, 0.08333331f)
                .SetEase(Ease.OutQuad));
            _sequence.Insert(0f, DOVirtual.Float(0f, 1f, 0.1f, SetFullAlpha)
                .SetEase(Ease.Linear));
            _sequence.InsertCallback(0.06666666f, PlayReviveGlow);
            _sequence.InsertCallback(0.13333333f, PlayReviveBurst);
            _sequence.Append(fullImage.rectTransform.DOScale(0.85f, 0.15f)
                .SetEase(Ease.InOutQuad));
            _sequence.Append(fullImage.rectTransform.DOScale(1f, 0.26666665f)
                .SetEase(Ease.OutQuad));
        }

        private void OnDisable()
        {
            KillSequence();
        }

        private void KillSequence()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill(false);
            _sequence = null;
            DOTween.Kill(this, false);
        }

        private void SetFullAlpha(float alpha)
        {
            if (fullImage == null) return;
            Color color = fullImage.color;
            color.a = alpha;
            fullImage.color = color;
        }

        private void SetFullY(float y)
        {
            if (fullImage == null) return;
            Vector2 position = fullImage.rectTransform.anchoredPosition;
            position.y = y;
            fullImage.rectTransform.anchoredPosition = position;
        }

        private void PlayLostBurst()
        {
            PlayParticleSet(fishParticles, true);
            PlayParticleSet(glowParticles, false);
        }

        private void PlayReviveBurst()
        {
            int count = Mathf.Min(2, fishParticles?.Length ?? 0);
            for (int index = 0; index < count; index++)
                PlayParticle(fishParticles[index], true, index, true);
        }

        private void PlayReviveGlow()
        {
            if (reviveGlow == null) return;
            reviveGlow.gameObject.SetActive(true);
            reviveGlow.color = new Color(1f, 1f, 1f, 0f);
            reviveGlow.rectTransform.localScale = Vector3.one * 0.63f;
            Sequence glow = DOTween.Sequence().SetUpdate(true).SetId(this);
            glow.Append(reviveGlow.DOFade(1f, 0.08333338f));
            glow.AppendInterval(0.08333337f);
            glow.Append(reviveGlow.DOFade(0f, 0.23333329f));
            glow.Join(reviveGlow.rectTransform.DOScale(0.7f, 0.4f));
            glow.OnComplete(() =>
            {
                if (reviveGlow != null) reviveGlow.gameObject.SetActive(false);
            });
        }

        private void PlayParticleSet(Image[] particles, bool fish)
        {
            if (particles == null) return;
            for (int index = 0; index < particles.Length; index++)
                PlayParticle(particles[index], fish, index, false);
        }

        private void PlayParticle(
            Image particle,
            bool fish,
            int index,
            bool revive)
        {
            if (particle == null) return;
            RectTransform rect = particle.rectTransform;
            particle.gameObject.SetActive(true);
            particle.color = Color.white;
            rect.anchoredPosition = new Vector2(
                Random.Range(-3f, 4f), Random.Range(-3f, 4f));
            rect.localScale = Vector3.one * (fish ? 0.7f : 0.15f);
            rect.localRotation = Quaternion.Euler(
                0f, 0f, Random.Range(-25f, 25f));

            float angle = revive
                ? (index == 0 ? 35f : 145f)
                : Random.Range(0f, 360f);
            Vector2 direction = new(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad));
            float duration = revive ? 0.5f : 0.8f;
            Vector2 apex = rect.anchoredPosition + direction *
                (fish ? Random.Range(28f, 62f) : Random.Range(20f, 45f));
            Vector2 end = apex + new Vector2(
                fish ? Random.Range(12f, 40f) : Random.Range(4f, 18f),
                fish ? -Random.Range(35f, 75f) : -Random.Range(10f, 35f));

            Sequence flight = DOTween.Sequence().SetUpdate(true).SetId(this);
            flight.Append(rect.DOAnchorPos(apex, duration * 0.38f)
                .SetEase(Ease.OutQuad));
            flight.Append(rect.DOAnchorPos(end, duration * 0.62f)
                .SetEase(Ease.InQuad));
            flight.Join(particle.DOFade(0f, duration * 0.48f)
                .SetDelay(duration * 0.14f));
            flight.Join(rect.DOScale(
                fish ? 0.18f : 0.3f,
                duration * 0.62f));
            flight.OnComplete(() =>
            {
                if (particle != null) particle.gameObject.SetActive(false);
            });
        }

        private void ResetEffects()
        {
            ResetParticleSet(fishParticles);
            ResetParticleSet(glowParticles);
            if (reviveGlow != null)
            {
                reviveGlow.gameObject.SetActive(false);
                reviveGlow.color = Color.white;
                reviveGlow.rectTransform.localScale = Vector3.one;
            }
        }

        private static void ResetParticleSet(Image[] particles)
        {
            if (particles == null) return;
            for (int index = 0; index < particles.Length; index++)
            {
                Image particle = particles[index];
                if (particle == null) continue;
                particle.gameObject.SetActive(false);
                particle.color = Color.white;
                particle.rectTransform.anchoredPosition = Vector2.zero;
                particle.rectTransform.localScale = Vector3.one;
                particle.rectTransform.localRotation = Quaternion.identity;
            }
        }
    }
}
