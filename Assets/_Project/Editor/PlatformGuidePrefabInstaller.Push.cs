using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    internal static partial class PlatformGuidePrefabInstaller
    {
        private const string PushClosePath =
            "Assets/_Project/Sprites/splash/push_guide_exp1/shape.png";
        private const string PushCatPath =
            "Assets/_Project/Sprites/splash/push_guide_exp1/group_957.png";
        private const string PushCatAccentPath =
            "Assets/_Project/Sprites/splash/push_guide_exp1/layer51.png";
        private const string PushButtonShadowPath =
            "Assets/_Project/Sprites/splash/push_guide_exp1/btn.png";

        private static GameObject BuildPrePush(
            Font font,
            Shader rounded,
            LocalizationCatalog localization)
        {
            GameObject page = CreatePage<PrePushGuidePresenter>(
                "PrePushGuidePage",
                out PrePushGuidePresenter presenter,
                out Canvas canvas,
                out CanvasGroup pageGroup);

            RectTransform popup = CreateRect("Popup", page.transform);
            popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0f);
            popup.pivot = new Vector2(0.5f, 0f);
            popup.anchoredPosition = Vector2.zero;
            popup.sizeDelta = new Vector2(1080f, 1336f);
            CanvasGroup popupGroup = popup.gameObject.AddComponent<CanvasGroup>();

            Image background = CreateRounded(
                "Bg",
                popup,
                rounded,
                60f,
                PanelColor);
            SetBottom(background.rectTransform, 0f, new Vector2(1080f, 1180f));

            Button close = CreateIconButton(
                "CloseButton",
                popup,
                LoadSprite(PushClosePath));
            SetTopRight((RectTransform)close.transform,
                new Vector2(-24f, -382f), new Vector2(100f, 108f));

            RectTransform cat = CreateRect("Cat", popup);
            SetCentered(cat, new Vector2(-5f, 830f), new Vector2(502f, 389f));
            Image catImage = CreateImage(
                "Group957Img", cat, LoadSprite(PushCatPath));
            catImage.rectTransform.anchorMin = new Vector2(0f, 0f);
            catImage.rectTransform.anchorMax = new Vector2(0f, 0f);
            catImage.rectTransform.pivot = new Vector2(0f, 0f);
            catImage.rectTransform.anchoredPosition = Vector2.zero;
            catImage.rectTransform.sizeDelta = new Vector2(444f, 389f);
            Image catAccent = CreateImage(
                "Layer51Img", cat, LoadSprite(PushCatAccentPath));
            catAccent.rectTransform.anchorMin =
                catAccent.rectTransform.anchorMax = new Vector2(0f, 0f);
            catAccent.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            catAccent.rectTransform.anchoredPosition = new Vector2(460.5f, 146.5f);
            catAccent.rectTransform.sizeDelta = new Vector2(83f, 75f);

            Text title = CreateText(
                "SubtitleText", popup, font, 70,
                "Your cat's been thinking about you",
                TextColor, FontStyle.Bold);
            SetCentered(title.rectTransform, new Vector2(0f, 535f),
                new Vector2(800f, 205f));
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 42;
            title.resizeTextMaxSize = 70;

            Image frame = CreateRounded(
                "PopupFrame", popup, rounded, 20f, HeaderColor);
            SetCentered(frame.rectTransform, new Vector2(0f, 275f),
                new Vector2(980f, 280f));
            Text description = CreateText(
                "TitleText", frame.transform, font, 54,
                "A little cat company can brighten your day.",
                TextColor, FontStyle.Normal);
            Stretch(description.rectTransform);
            description.rectTransform.offsetMin = new Vector2(40f, 45f);
            description.rectTransform.offsetMax = new Vector2(-40f, -45f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 34;
            description.resizeTextMaxSize = 54;

            Image shadow = CreateImage(
                "ButtonShadow", popup, LoadSprite(PushButtonShadowPath));
            SetCentered(shadow.rectTransform, new Vector2(0f, -55f),
                new Vector2(810f, 220f));
            Button allow = CreateTextButton(
                "AllowButton", popup, font, "Allow Notifications", 70,
                Color.white, AccentColor, out Text allowText,
                rounded, 80f);
            SetCentered((RectTransform)allow.transform,
                new Vector2(0f, -45f), new Vector2(750f, 160f));

            PushGuidePopupAnimator animator =
                page.AddComponent<PushGuidePopupAnimator>();
            SerializedObject animatorData = new(animator);
            SetReference(animatorData, "popup", popup);
            SetReference(animatorData, "popupGroup", popupGroup);
            SetReference(animatorData, "catAccent", catAccent.rectTransform);
            animatorData.ApplyModifiedPropertiesWithoutUndo();

            ConfigureWindow(presenter, canvas, pageGroup, true, 0.8f);
            SerializedObject data = new(presenter);
            SetReference(data, "popupAnimator", animator);
            SetReference(data, "titleText", title);
            SetReference(data, "descriptionText", description);
            SetReference(data, "allowText", allowText);
            SetReference(data, "allowButton", allow);
            SetReference(data, "guideCloseButton", close);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static void SetBottom(
            RectTransform rect,
            float y,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
        }
    }
}
