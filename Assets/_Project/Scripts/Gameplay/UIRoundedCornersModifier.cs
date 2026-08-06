using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Image))]
    public class UIRoundedCornersModifier : MonoBehaviour
    {
        [Header("Corner Settings")]
        [Range(0f, 500f)]
        public float cornerRadius = 20f;

        private Image _image;
        private RectTransform _rectTransform;
        private Material _instancedMaterial;

        // Các tên biến tương ứng trong file Shader
        private static readonly int WidthID = Shader.PropertyToID("_Width");
        private static readonly int HeightID = Shader.PropertyToID("_Height");
        private static readonly int RadiusID = Shader.PropertyToID("_Radius");

        private void OnEnable()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();

            // Tạo một bản sao (instance) của Material để tránh sửa nhầm ảnh hưởng tới thằng khác
            if (_image.material != null && _image.material.shader.name == "UI/RoundedCorners")
            {
                _instancedMaterial = new Material(_image.material);
                _image.material = _instancedMaterial;
                UpdateShaderProperties();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateShaderProperties();
        }

        private void Update()
        {
            // Liên tục cập nhật nếu bạn đang chỉnh Radius bằng tay trong Inspector (tiện cho việc test)
            if (Application.isEditor && !Application.isPlaying)
            {
                UpdateShaderProperties();
            }
        }

        private void UpdateShaderProperties()
        {
            if (_instancedMaterial == null || _rectTransform == null) return;

            // Đẩy chiều rộng, chiều cao thực tế và độ cong góc xuống Shader
            _instancedMaterial.SetFloat(WidthID, _rectTransform.rect.width);
            _instancedMaterial.SetFloat(HeightID, _rectTransform.rect.height);
            _instancedMaterial.SetFloat(RadiusID, cornerRadius);
        }

        private void OnDisable()
        {
            // Trả lại material gốc khi bị tắt (tránh leak bộ nhớ rác)
            if (_instancedMaterial != null)
            {
                DestroyImmediate(_instancedMaterial);
            }
        }
    }
}
