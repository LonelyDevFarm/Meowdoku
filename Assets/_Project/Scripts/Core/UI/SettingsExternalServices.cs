using UnityEngine;

namespace Meowdoku.Core.UI
{
    /// <summary>
    /// Provider-neutral boundary for Settings actions owned by platform SDKs.
    /// A production adapter may implement this together with
    /// IAppStartupExternalServices; the offline fallback never blocks UI.
    /// </summary>
    public interface ISettingsExternalServices
    {
        bool IsOnline { get; }
        bool IsConsentManagementRequired { get; }
        void OpenFeedbackFaq();
        void ShowConsentManagement();
        void OpenLocalizedPrivacyUrl(string defaultUrl);
    }

    public interface ISettingsExternalServicesConsumer
    {
        void BindSettingsExternalServices(ISettingsExternalServices services);
    }

    public sealed class OfflineSettingsExternalServices :
        ISettingsExternalServices
    {
        public static readonly OfflineSettingsExternalServices Instance = new();

        private OfflineSettingsExternalServices() { }

        public bool IsOnline => false;
        public bool IsConsentManagementRequired => false;
        public void OpenFeedbackFaq() { }
        public void ShowConsentManagement() { }

        public void OpenLocalizedPrivacyUrl(string defaultUrl)
        {
            if (!string.IsNullOrWhiteSpace(defaultUrl))
                Application.OpenURL(defaultUrl);
        }
    }
}
