using Meowdoku.Core.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StreakDaySlotView : MonoBehaviour
    {
        private static readonly string[] WeekdayKeys =
        {
            "WEEKDAY_SUN", "WEEKDAY_MON", "WEEKDAY_TUE",
            "WEEKDAY_WED", "WEEKDAY_THU", "WEEKDAY_FRI",
            "WEEKDAY_SAT"
        };

        [SerializeField] private Text weekdayText;
        [SerializeField] private GameObject uncheckedDot;
        [SerializeField] private GameObject checkedDot;
        [SerializeField] private GameObject chest;
        [SerializeField] private LocalizationCatalog localization;

        public void BindLocalization(LocalizationCatalog catalog)
        {
            localization = catalog;
        }

        public void ApplyStatic(
            int weekday,
            bool isChecked,
            bool isChest)
        {
            if (weekdayText != null)
            {
                string key = WeekdayKeys[
                    Mathf.Clamp(weekday, 0, WeekdayKeys.Length - 1)];
                string value = localization != null
                    ? localization.Translate(key)
                    : key;
                weekdayText.text = string.IsNullOrEmpty(value) ||
                                   value == key
                    ? key.Replace("WEEKDAY_", string.Empty)
                    : value;
                weekdayText.color = isChecked
                    ? new Color32(241, 147, 32, 255)
                    : new Color32(147, 90, 90, 255);
            }

            SetActive(uncheckedDot, !isChecked && !isChest);
            SetActive(checkedDot, isChecked);
            SetActive(chest, !isChecked && isChest);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
