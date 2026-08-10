using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class RoundedImageView : MonoBehaviour
    {
        [SerializeField] private Image target;
        [SerializeField] private Shader roundedShader;
        [SerializeField, Min(0f)] private float cornerRadius = 32f;
        [SerializeField] private bool usePerCornerRadii;
        [SerializeField] private Vector4 cornerRadii =
            new Vector4(32f, 32f, 32f, 32f);

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) Apply();
        }

        public void Configure(Image image, Shader shader, float radius)
        {
            target = image;
            roundedShader = shader;
            cornerRadius = Mathf.Max(0f, radius);
            usePerCornerRadii = false;
            if (isActiveAndEnabled) Apply();
        }

        public void Configure(
            Image image,
            Shader shader,
            Vector4 radii)
        {
            target = image;
            roundedShader = shader;
            cornerRadii = new Vector4(
                Mathf.Max(0f, radii.x),
                Mathf.Max(0f, radii.y),
                Mathf.Max(0f, radii.z),
                Mathf.Max(0f, radii.w));
            usePerCornerRadii = true;
            if (isActiveAndEnabled) Apply();
        }

        private void Apply()
        {
            if (target == null) target = GetComponent<Image>();
            RectTransform rect = target != null ? target.rectTransform : null;
            if (target == null || rect == null || roundedShader == null ||
                rect.rect.width <= 0f || rect.rect.height <= 0f)
                return;

            float maximum = Mathf.Min(
                rect.rect.width,
                rect.rect.height) * 0.5f;
            Vector4 radii;
            if (usePerCornerRadii)
            {
                radii = new Vector4(
                    Mathf.Min(cornerRadii.x, maximum),
                    Mathf.Min(cornerRadii.y, maximum),
                    Mathf.Min(cornerRadii.z, maximum),
                    Mathf.Min(cornerRadii.w, maximum));
            }
            else
            {
                float radius = Mathf.Min(cornerRadius, maximum);
                radii = new Vector4(radius, radius, radius, radius);
            }
            target.material = RoundedRectMaterialCache.Get(
                roundedShader,
                rect.rect.size,
                radii,
                false);
        }
    }
}
