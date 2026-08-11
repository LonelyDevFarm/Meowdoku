using Meowdoku.Core.Daily;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AwardItemView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text countText;
        [SerializeField] private GameObject frameRoot;
        [SerializeField] private Sprite locateIcon;
        [SerializeField] private Sprite hintIcon;

        public void Apply(AwardItem item)
        {
            bool valid = item != null && item.IsValid();
            gameObject.SetActive(valid);
            if (!valid) return;

            bool tool = item.Category == AwardCategory.Tool;
            bool frame = item.Category == AwardCategory.Frame;
            if (background != null) background.gameObject.SetActive(tool);
            if (frameRoot != null) frameRoot.SetActive(frame);
            if (icon != null)
            {
                icon.gameObject.SetActive(tool);
                icon.sprite = item.Kind == "locate"
                    ? locateIcon
                    : item.Kind == "hint" ? hintIcon : null;
                icon.preserveAspect = true;
            }
            if (countText != null)
            {
                countText.gameObject.SetActive(tool);
                countText.text = item.Count.ToString();
            }
        }
    }
}
