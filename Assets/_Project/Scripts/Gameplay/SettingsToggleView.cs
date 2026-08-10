using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Serialized UGUI equivalent of one SettingPage toggle cell. The source
    /// keeps separate ToggleOn and ToggleOff branches, so the prefab does too.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsToggleView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RawImage icon;
        [SerializeField] private Texture onIcon;
        [SerializeField] private Texture offIcon;
        [SerializeField] private GameObject toggleOn;
        [SerializeField] private GameObject toggleOff;

        public Button Button => button;
        public bool Value { get; private set; }

        public void SetValue(bool value)
        {
            Value = value;
            if (toggleOn != null) toggleOn.SetActive(value);
            if (toggleOff != null) toggleOff.SetActive(!value);
            if (icon != null)
                icon.texture = value || offIcon == null ? onIcon : offIcon;
        }
    }
}
