namespace Meowdoku.Core.Config
{
    public sealed class SettingsLanguageConfig : AbConfigBase<int>
    {
        public const int ValueHide = 0;
        public const int ValuePopup = 1;
        public const int ValueDropdown = 2;

        public SettingsLanguageConfig()
            : base("settings_language", ValueHide, AbConfigTiming.OpenSetting) { }

        public bool IsLanguageSwitchEnabled()
        {
            return Value == ValuePopup || Value == ValueDropdown;
        }

        public bool IsLanguageSwitchEnabledPeek(IAbValueProvider provider = null)
        {
            int value = PeekValue(provider);
            return value == ValuePopup || value == ValueDropdown;
        }

        public bool IsPopupMode() => Value == ValuePopup;
        public bool IsDropdownMode() => Value == ValueDropdown;
    }

    public sealed class BlindModConfig : AbConfigBase<int>
    {
        public const int ValueControl = 0;
        public const int ValueHideOnFilled = 1;
        public const int ValueKeepOnFilled = 2;

        public BlindModConfig()
            : base("blind_mod", ValueControl, AbConfigTiming.GameStart) { }

        public bool IsEnabled() => Value != ValueControl;
        public bool IsKeepOnFilled() => Value == ValueKeepOnFilled;
    }
}
