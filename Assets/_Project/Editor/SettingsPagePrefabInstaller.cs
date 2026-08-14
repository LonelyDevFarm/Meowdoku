using Meowdoku.Core.UI;
using Meowdoku.Core.Localization;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    /// <summary>
    /// Builds the source SettingPage hierarchy through Editor APIs so prefab
    /// GUIDs and serialized references are owned by Unity, not hand-written YAML.
    /// </summary>
    [InitializeOnLoad]
    internal static class SettingsPagePrefabInstaller
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/SettingsPage.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string EastAsianFontPath =
            "Assets/_Project/Fonts/NotoSourceHan-subset.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string CloseIconPath =
            "Assets/_Project/Sprites/common/btn_close.png";
        private const string CirclePath =
            "Assets/_Project/Sprites/setting/circle_white.png";
        private const string MusicOnPath =
            "Assets/_Project/Sprites/setting/icon_music.png";
        private const string MusicOffPath =
            "Assets/_Project/Sprites/setting/icon_music_off.png";
        private const string SoundOnPath =
            "Assets/_Project/Sprites/setting/icon_sound.png";
        private const string SoundOffPath =
            "Assets/_Project/Sprites/setting/icon_sound_off.png";
        private const string VibrationOnPath =
            "Assets/_Project/Sprites/setting/icon_vibrate.png";
        private const string VibrationOffPath =
            "Assets/_Project/Sprites/setting/icon_vibrate_off.png";
        private const string PeopleOnPath =
            "Assets/_Project/Sprites/setting/icon_people.png";
        private const string PeopleOffPath =
            "Assets/_Project/Sprites/setting/icon_people_off.png";
        private const string LanguageGlobePath =
            "Assets/_Project/Sprites/setting/language_switch/lang_icon.png";
        private const string LanguageArrowPath =
            "Assets/_Project/Sprites/setting/language_switch/arrow_close.png";
        private const string LanguageShadowPath =
            "Assets/_Project/Sprites/setting/language_switch/panel_shadow.png";

        private static readonly Color PanelColor =
            new(1f, 0.984f, 0.969f, 1f);
        private static readonly Color TitleColor =
            new(0.976f, 0.925f, 0.882f, 1f);
        private static readonly Color BorderColor =
            new(0.91f, 0.839f, 0.78f, 1f);
        private static readonly Color TextColor =
            new(0.576f, 0.353f, 0.353f, 1f);
        private static readonly Color TitleTextColor =
            new(0.427f, 0.325f, 0.345f, 1f);
        private static readonly Color ToggleOnColor =
            new(0.278f, 0.702f, 0.341f, 1f);
        private static readonly Color ToggleOffColor =
            new(0.808f, 0.729f, 0.635f, 1f);
        private static readonly Color AccentColor =
            new(0.945f, 0.576f, 0.125f, 1f);

        static SettingsPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfMissing;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        [MenuItem("Meowdoku/Port/Create Settings Page Prefab")]
        private static void InstallFromMenu()
        {
            InstallIfMissing();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        internal static bool InstallIfReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            InstallIfMissing();
            return true;
        }

        private static void InstallIfMissing()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfMissing;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null && !NeedsUpgrade(existing))
            {
                UIRegistryAssetInstaller.InstallIfReady();
                return;
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Font eastAsianFont =
                AssetDatabase.LoadAssetAtPath<Font>(EastAsianFontPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            Shader rounded =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            Texture2D close =
                AssetDatabase.LoadAssetAtPath<Texture2D>(CloseIconPath);
            Texture2D circle =
                AssetDatabase.LoadAssetAtPath<Texture2D>(CirclePath);
            if (font == null || eastAsianFont == null || localization == null ||
                rounded == null || close == null || circle == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            GameObject page = Build(
                font,
                eastAsianFont,
                localization,
                rounded,
                close,
                circle);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(page, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(page);
            }
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static bool NeedsUpgrade(GameObject prefab)
        {
            if (prefab == null ||
                prefab.GetComponent<SettingsPagePresenter>() == null ||
                prefab.GetComponent<GenericPopupAnimator>() == null ||
                prefab.GetComponentInChildren<LanguageSwitchWidget>(true) == null)
                return true;
            SettingsPagePresenter presenter =
                prefab.GetComponent<SettingsPagePresenter>();
            SerializedObject data = new(presenter);
            SerializedProperty version =
                data.FindProperty("versionLocalizedText");
            LanguageSwitchWidget widget =
                prefab.GetComponentInChildren<LanguageSwitchWidget>(true);
            SerializedObject widgetData = new(widget);
            SerializedProperty outsideBlocker =
                widgetData.FindProperty("outsideBlocker");
            Text termsLabel = prefab.transform.Find(
                "Root/Content/PanelContainer/VBoxContainer/TermContainer/TermsBtn/Label")
                ?.GetComponent<Text>();
            Text privacyLabel = prefab.transform.Find(
                "Root/Content/PanelContainer/VBoxContainer/TermContainer/PrivacyBtn/Label")
                ?.GetComponent<Text>();
            Text privacyPreferenceLabel = prefab.transform.Find(
                "Root/Content/PanelContainer/VBoxContainer/PrivacyContainer/PrivacyPreferenceBtn/Label")
                ?.GetComponent<Text>();
            HorizontalLayoutGroup legalLayout = prefab.transform.Find(
                "Root/Content/PanelContainer/VBoxContainer/TermContainer")
                ?.GetComponent<HorizontalLayoutGroup>();
            return version == null || version.objectReferenceValue == null ||
                   outsideBlocker == null ||
                   outsideBlocker.objectReferenceValue == null ||
                   legalLayout == null || !legalLayout.childControlWidth ||
                   termsLabel == null || !termsLabel.resizeTextForBestFit ||
                   privacyLabel == null || !privacyLabel.resizeTextForBestFit ||
                   privacyPreferenceLabel == null ||
                   !privacyPreferenceLabel.resizeTextForBestFit;
        }

        private static void HandlePlayModeChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += InstallIfMissing;
        }

        private static GameObject Build(
            Font font,
            Font eastAsianFont,
            LocalizationCatalog localization,
            Shader rounded,
            Texture2D closeTexture,
            Texture2D circleTexture)
        {
            var page = new GameObject(
                "SettingsPage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(SettingsPagePresenter));
            SetUiLayer(page);
            Stretch((RectTransform)page.transform);
            Canvas canvas = page.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            RectTransform root = CreateRect("Root", page.transform);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(
                SettingsPageContract.SourceReferenceWidth, 0f);

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            content.pivot = new Vector2(0.5f, 0.5f);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            GenericPopupAnimator popupAnimator =
                page.AddComponent<GenericPopupAnimator>();
            SerializedObject popupData = new(popupAnimator);
            SetReference(popupData, "content", content);
            SetReference(popupData, "contentGroup", contentGroup);
            popupData.ApplyModifiedPropertiesWithoutUndo();

            Image panelImage = CreateRoundedImage(
                "PanelContainer", content, rounded, 60f, PanelColor);
            RectTransform panel = panelImage.rectTransform;
            SetCentered(panel, Vector2.zero,
                new Vector2(SettingsPageContract.PanelWidth, 100f));

            RectTransform vbox = CreateRect("VBoxContainer", panel);
            Stretch(vbox);
            VerticalLayoutGroup vertical =
                vbox.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 0f;
            vertical.padding = new RectOffset(0, 0, 0, 0);
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childForceExpandWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandHeight = false;

            Image title = CreateRoundedImage(
                "TitleBar", vbox, rounded, 60f, TitleColor);
            SetPreferred(title.gameObject,
                SettingsPageContract.PanelWidth,
                SettingsPageContract.TitleBarHeight);
            Text titleText = CreateText(
                "TitleLabel", title.transform, font, 86, "Settings",
                TitleTextColor, FontStyle.Bold);
            Stretch(titleText.rectTransform);
            Localize(
                titleText,
                localization,
                font,
                eastAsianFont,
                "SETTING_TITLE",
                "Settings");
            Button closeButton = CreateTransparentButton(
                "CloseBtn", title.transform);
            SetAnchored(
                (RectTransform)closeButton.transform,
                new Vector2(1f, 0.5f),
                new Vector2(-69f, 5f),
                new Vector2(100f, 100f),
                new Vector2(0.5f, 0.5f));
            RawImage closeIcon = CreateRawImage("CloseIcon", closeButton.transform);
            Stretch(closeIcon.rectTransform);
            closeIcon.texture = closeTexture;

            CreateSpacer("Control", vbox, 100f);
            RectTransform grid = CreateRect("GridContainer", vbox);
            SetPreferred(grid.gameObject, 0f,
                SettingsPageContract.ToggleButtonSize);
            HorizontalLayoutGroup gridLayout =
                grid.gameObject.AddComponent<HorizontalLayoutGroup>();
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.spacing = 30f;
            gridLayout.childControlWidth = false;
            gridLayout.childControlHeight = false;
            gridLayout.childForceExpandWidth = false;
            gridLayout.childForceExpandHeight = false;

            SettingsToggleView music = CreateToggle(
                "MusicCtrl", grid, font, rounded, circleTexture,
                MusicOnPath, MusicOffPath);
            SettingsToggleView sound = CreateToggle(
                "SoundCtrl", grid, font, rounded, circleTexture,
                SoundOnPath, SoundOffPath);
            SettingsToggleView people = CreateToggle(
                "PeopleCtrl", grid, font, rounded, circleTexture,
                PeopleOnPath, PeopleOffPath);
            SettingsToggleView vibration = CreateToggle(
                "VibrationCtrl", grid, font, rounded, circleTexture,
                VibrationOnPath, VibrationOffPath);

            GameObject optionalSpacer = CreateSpacer("Control4", vbox, 30f);
            RectTransform optional = CreateRect("ToggleContainer", vbox);
            VerticalLayoutGroup optionalLayout =
                optional.gameObject.AddComponent<VerticalLayoutGroup>();
            optionalLayout.spacing = 40f;
            optionalLayout.childAlignment = TextAnchor.UpperCenter;
            optionalLayout.childControlWidth = false;
            optionalLayout.childControlHeight = false;
            optionalLayout.childForceExpandWidth = false;
            optionalLayout.childForceExpandHeight = false;

            PatternParts pattern = CreatePatternSwitch(
                optional,
                font,
                eastAsianFont,
                localization,
                rounded,
                circleTexture);
            LanguageSwitchWidget languageWidget = CreateLanguageSwitchWidget(
                optional,
                font,
                eastAsianFont,
                localization,
                rounded);
            languageWidget.gameObject.SetActive(false);

            GameObject actionSpacer = CreateSpacer("Control2", vbox, 100f);
            RectTransform actions = CreateRect("BtnContainer", vbox);
            VerticalLayoutGroup actionLayout =
                actions.gameObject.AddComponent<VerticalLayoutGroup>();
            actionLayout.spacing = 40f;
            actionLayout.childAlignment = TextAnchor.UpperCenter;
            actionLayout.childControlWidth = false;
            actionLayout.childControlHeight = false;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;

            Button languageButton = CreateOutlineButton(
                "LanguageBtn", actions, font, rounded,
                "Language", 80,
                new Vector2(SettingsPageContract.MainButtonWidth,
                    SettingsPageContract.MainButtonHeight));
            Button feedbackButton = CreateOutlineButton(
                "FeedbackBtn", actions, font, rounded,
                "Feedback", 80,
                new Vector2(SettingsPageContract.MainButtonWidth,
                    SettingsPageContract.MainButtonHeight));
            Button howToPlayButton = CreateOutlineButton(
                "HowToPlayBtn", actions, font, rounded,
                "How to Play", 80,
                new Vector2(SettingsPageContract.MainButtonWidth,
                    SettingsPageContract.MainButtonHeight));
            LocalizeButton(
                languageButton, localization, font, eastAsianFont,
                "SETTING_LANGUAGE", "Language");
            LocalizeButton(
                feedbackButton, localization, font, eastAsianFont,
                "FEEDBACK_TITLE", "Feedback");
            LocalizeButton(
                howToPlayButton, localization, font, eastAsianFont,
                "SETTING_HOW_TO_PLAY", "How to Play");

            RectTransform restartRow = CreateRect("OrangeRestartBtn", actions);
            SetPreferred(restartRow.gameObject,
                SettingsPageContract.MainButtonWidth,
                SettingsPageContract.MainButtonHeight);
            restartRow.sizeDelta = new Vector2(
                SettingsPageContract.MainButtonWidth,
                SettingsPageContract.MainButtonHeight);
            Shadow restartShadow = restartRow.gameObject.AddComponent<Shadow>();
            restartShadow.effectColor = new Color(
                AccentColor.r, AccentColor.g, AccentColor.b, 0.7f);
            restartShadow.effectDistance = new Vector2(0f, -14f);
            Image restartBg = CreateRoundedImage(
                "Bg", restartRow, rounded, 80f, AccentColor);
            Stretch(restartBg.rectTransform);
            restartBg.raycastTarget = true;
            Button restartButton = restartBg.gameObject.AddComponent<Button>();
            restartButton.targetGraphic = restartBg;
            Text restartText = CreateText(
                "Label", restartBg.transform, font, 80, "Restart",
                Color.white, FontStyle.Bold);
            Stretch(restartText.rectTransform);
            Localize(
                restartText,
                localization,
                font,
                eastAsianFont,
                "SETTING_RESTART",
                "Restart");

            languageButton.gameObject.SetActive(false);
            howToPlayButton.gameObject.SetActive(false);
            restartRow.gameObject.SetActive(false);

            GameObject afterActions =
                CreateSpacer("Control3", vbox, 50f);
            LayoutElement afterActionsElement =
                afterActions.GetComponent<LayoutElement>();

            RectTransform cmpRow = CreateRect("PrivacyContainer", vbox);
            SetPreferred(cmpRow.gameObject, 0f, 80f);
            Button cmpButton = CreateLinkButton(
                "PrivacyPreferenceBtn", cmpRow, font,
                "Privacy Preference", 48);
            Stretch((RectTransform)cmpButton.transform);
            LocalizeButton(
                cmpButton, localization, font, eastAsianFont,
                "SETTING_PRIVACY_PREFERENCE", "Privacy Preference");
            cmpRow.gameObject.SetActive(false);

            RectTransform terms = CreateRect("TermContainer", vbox);
            SetPreferred(terms.gameObject, 0f, 80f);
            HorizontalLayoutGroup termsLayout =
                terms.gameObject.AddComponent<HorizontalLayoutGroup>();
            termsLayout.childAlignment = TextAnchor.MiddleCenter;
            termsLayout.spacing = 20f;
            termsLayout.childControlWidth = true;
            termsLayout.childControlHeight = true;
            termsLayout.childForceExpandWidth = true;
            termsLayout.childForceExpandHeight = true;
            Button termsButton = CreateLinkButton(
                "TermsBtn", terms, font, "Terms of Service", 48);
            SetPreferred(termsButton.gameObject, 420f, 80f);
            termsButton.GetComponent<LayoutElement>().flexibleWidth = 1f;
            Button privacyButton = CreateLinkButton(
                "PrivacyBtn", terms, font, "Privacy Policy", 48);
            SetPreferred(privacyButton.gameObject, 420f, 80f);
            privacyButton.GetComponent<LayoutElement>().flexibleWidth = 1f;
            LocalizeButton(
                termsButton, localization, font, eastAsianFont,
                "SETTING_TOS", "Terms of Service");
            LocalizeButton(
                privacyButton, localization, font, eastAsianFont,
                "SETTING_PRIVACY", "Privacy Policy");

            RectTransform version = CreateRect("HBoxContainer", vbox);
            SetPreferred(version.gameObject, 0f, 80f);
            Text versionText = CreateText(
                "VersionLabel", version, font, 48, "Version",
                TitleTextColor, FontStyle.Bold);
            versionText.color = new Color(
                TitleTextColor.r, TitleTextColor.g, TitleTextColor.b, 0.5f);
            Stretch(versionText.rectTransform);
            LocalizedText versionLocalized = Localize(
                versionText,
                localization,
                font,
                eastAsianFont,
                "SETTING_VERSION",
                "Version %s");
            GameObject bottom = CreateSpacer("Control6", vbox, 30f);
            LayoutElement bottomElement = bottom.GetComponent<LayoutElement>();

            SourceToastView toast = CreateToast(content, font, rounded);

            SettingsPagePresenter presenter =
                page.GetComponent<SettingsPagePresenter>();
            ConfigureWindow(
                presenter, canvas, pageGroup, closeButton);
            SerializedObject data = new(presenter);
            SetReference(data, "panel", panel);
            SetReference(data, "popupAnimator", popupAnimator);
            SetReference(data, "titleText", titleText);
            SetReference(data, "toggleGrid", grid);
            SetReference(data, "toggleGridLayout", gridLayout);
            SetReference(data, "musicToggle", music);
            SetReference(data, "soundToggle", sound);
            SetReference(data, "vibrationToggle", vibration);
            SetReference(data, "peopleToggle", people);
            SetReference(data, "optionalSwitchSpacer", optionalSpacer);
            SetReference(data, "optionalSwitchContainer", optional.gameObject);
            SetReference(data, "languageSwitchWidget", languageWidget);
            SetReference(data, "patternSwitch", pattern.Root);
            SetReference(data, "patternButton", pattern.Button);
            SetReference(data, "patternOn", pattern.On);
            SetReference(data, "patternOff", pattern.Off);
            SetReference(data, "patternDot", pattern.Dot);
            SetReference(data, "actionSpacer", actionSpacer);
            SetReference(data, "languageButton", languageButton);
            SetReference(data, "feedbackButton", feedbackButton);
            SetReference(data, "howToPlayButton", howToPlayButton);
            SetReference(data, "restartRow", restartRow.gameObject);
            SetReference(data, "restartButton", restartButton);
            SetReference(data, "afterActionsSpacer", afterActionsElement);
            SetReference(data, "cmpRow", cmpRow.gameObject);
            SetReference(data, "cmpButton", cmpButton);
            SetReference(data, "termsRow", terms.gameObject);
            SetReference(data, "termsButton", termsButton);
            SetReference(data, "privacyButton", privacyButton);
            SetReference(data, "versionRow", version.gameObject);
            SetReference(data, "versionText", versionText);
            SetReference(data, "versionLocalizedText", versionLocalized);
            SetReference(data, "bottomSpacer", bottomElement);
            SetReference(data, "toast", toast);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();

            music.gameObject.SetActive(false);
            pattern.Root.SetActive(false);
            optional.gameObject.SetActive(false);
            optionalSpacer.SetActive(false);
            toast.gameObject.SetActive(false);
            return page;
        }

        private static SettingsToggleView CreateToggle(
            string name,
            Transform parent,
            Font font,
            Shader rounded,
            Texture2D circle,
            string onPath,
            string offPath)
        {
            RectTransform root = CreateRect(name, parent);
            root.sizeDelta = new Vector2(250f, 250f);
            SetPreferred(root.gameObject, 250f, 250f);
            Image border = CreateRoundedImage(
                name.Replace("Ctrl", "Btn"), root, rounded, 48f, BorderColor);
            Stretch(border.rectTransform);
            border.raycastTarget = true;
            Image inner = CreateRoundedImage(
                "Inner", border.transform, rounded, 45f, PanelColor);
            Stretch(inner.rectTransform, new Vector2(3f, 3f));
            Button button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;

            RawImage icon = CreateRawImage(
                "Icon" + name.Replace("Ctrl", string.Empty), border.transform);
            SetAnchored(icon.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0f, -73f),
                new Vector2(120f, 120f), new Vector2(0.5f, 0.5f));
            Texture2D on = AssetDatabase.LoadAssetAtPath<Texture2D>(onPath);
            Texture2D off = AssetDatabase.LoadAssetAtPath<Texture2D>(offPath);
            icon.texture = on;

            ToggleParts onParts = CreateToggleState(
                "ToggleOn", border.transform, font, rounded, circle,
                true, 194f, 75f, 48);
            ToggleParts offParts = CreateToggleState(
                "ToggleOff", border.transform, font, rounded, circle,
                false, 194f, 75f, 48);
            onParts.Root.SetActive(true);
            offParts.Root.SetActive(false);

            SettingsToggleView view =
                root.gameObject.AddComponent<SettingsToggleView>();
            SerializedObject data = new(view);
            SetReference(data, "button", button);
            SetReference(data, "icon", icon);
            SetReference(data, "onIcon", on);
            SetReference(data, "offIcon", off);
            SetReference(data, "toggleOn", onParts.Root);
            SetReference(data, "toggleOff", offParts.Root);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static PatternParts CreatePatternSwitch(
            Transform parent,
            Font font,
            Font eastAsianFont,
            LocalizationCatalog localization,
            Shader rounded,
            Texture2D circle)
        {
            Image border = CreateRoundedImage(
                "PatternModeSwitch", parent, rounded, 38f, BorderColor);
            SetPreferred(border.gameObject, 804f, 120f);
            border.rectTransform.sizeDelta = new Vector2(804f, 120f);
            border.raycastTarget = true;
            Image inner = CreateRoundedImage(
                "Content", border.transform, rounded, 35f, PanelColor);
            Stretch(inner.rectTransform, new Vector2(3f, 3f));
            Button button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            Text label = CreateText(
                "PatternModeTxt", inner.transform, font, 50,
                "Pattern Mode", TextColor, FontStyle.Bold);
            SetAnchored(label.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(88f, 0f),
                new Vector2(590f, 120f), new Vector2(0f, 0.5f));
            label.alignment = TextAnchor.MiddleLeft;
            Localize(
                label,
                localization,
                font,
                eastAsianFont,
                "GAME_SETTING_PATTERN_MODE",
                "Pattern Mode");

            RectTransform switchRoot = CreateRect("Switch", inner.transform);
            SetAnchored(switchRoot,
                new Vector2(1f, 0.5f), new Vector2(-71f, 0f),
                new Vector2(144f, 56f), new Vector2(0.5f, 0.5f));
            ToggleParts on = CreateSmallToggleState(
                "On", switchRoot, font, rounded, circle, true);
            ToggleParts off = CreateSmallToggleState(
                "Off", switchRoot, font, rounded, circle, false);
            on.Root.SetActive(true);
            off.Root.SetActive(false);

            Image dot = CreateRoundedImage(
                "RedDot", border.transform, rounded, 18f,
                new Color(0.93f, 0.25f, 0.18f, 1f));
            SetAnchored(dot.rectTransform,
                new Vector2(1f, 1f), new Vector2(-22f, -22f),
                new Vector2(36f, 36f), new Vector2(0.5f, 0.5f));
            return new PatternParts(
                border.gameObject, button, on.Root, off.Root,
                dot.gameObject);
        }

        private static LanguageSwitchWidget CreateLanguageSwitchWidget(
            Transform parent,
            Font font,
            Font eastAsianFont,
            LocalizationCatalog localization,
            Shader rounded)
        {
            RectTransform root = CreateRect("LanguageSwitchWidget", parent);
            root.sizeDelta = new Vector2(804f, 120f);
            SetPreferred(root.gameObject, 804f, 120f);

            RectTransform blockerRect = CreateRect("OutsideBlocker", root);
            Image blocker = blockerRect.gameObject.AddComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true;
            SetAnchored(
                blockerRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -500f),
                new Vector2(1600f, 3000f),
                new Vector2(0.5f, 0.5f));

            RectTransform dropdown = CreateRect("DropdownPanel", root);
            SetTopLeft(
                dropdown,
                new Vector2(0f, -37f),
                new Vector2(804f, 483f));
            RawImage shadow = CreateRawImage("PanelShadow", dropdown);
            SetTopLeft(
                shadow.rectTransform,
                new Vector2(-14f, 25f),
                new Vector2(832f, 0f));
            shadow.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                LanguageShadowPath);
            CanvasGroup shadowGroup =
                shadow.gameObject.AddComponent<CanvasGroup>();
            Image panel = CreateRoundedImage(
                "PanelBg", dropdown, rounded, 40f, Color.white);
            SetTopLeft(
                panel.rectTransform,
                new Vector2(0f, 25f),
                new Vector2(804f, 0f));

            Button system = CreateLanguageDropdownOption(
                "SystemLangOption",
                dropdown,
                font,
                rounded,
                string.Empty,
                new Vector2(24f, -107f),
                out CanvasGroup systemGroup,
                out Text systemLabel,
                out GameObject systemHighlight);
            Button english = CreateLanguageDropdownOption(
                "EnglishOption",
                dropdown,
                font,
                rounded,
                "English",
                new Vector2(24f, -295f),
                out CanvasGroup englishGroup,
                out _,
                out GameObject englishHighlight);
            ConfigurePressHighlight(system, systemHighlight);
            ConfigurePressHighlight(english, englishHighlight);

            Button row = CreateTransparentButton("Row", root);
            Stretch((RectTransform)row.transform);
            Image rowBorder = CreateRoundedImage(
                "RowBorder", row.transform, rounded, 38f, BorderColor);
            Stretch(rowBorder.rectTransform);
            Image rowInner = CreateRoundedImage(
                "Inner", rowBorder.transform, rounded, 35f, PanelColor);
            Stretch(rowInner.rectTransform, new Vector2(3f, 3f));

            RectTransform iconRoot = CreateRect("LangIcon", row.transform);
            SetTopLeft(
                iconRoot,
                new Vector2(23f, -24f),
                new Vector2(64f, 70f));
            Image iconBackground = CreateRoundedImage(
                "IconBg", iconRoot, rounded, 32f, TextColor);
            SetTopLeft(
                iconBackground.rectTransform,
                new Vector2(0f, -4f),
                new Vector2(64f, 64f));
            Text letter = CreateText(
                "IconLetter", iconRoot, font, 44, "A",
                PanelColor, FontStyle.Bold);
            Stretch(letter.rectTransform);
            letter.rectTransform.offsetMax = new Vector2(0f, -5f);
            Image underline = CreateRoundedImage(
                "IconUnderline", iconRoot, rounded, 2f, PanelColor);
            SetTopLeft(
                underline.rectTransform,
                new Vector2(16f, -50f),
                new Vector2(34f, 4f));
            RawImage globe = CreateRawImage("IconGlobeVisual", iconRoot);
            SetTopLeft(
                globe.rectTransform,
                new Vector2(1.8f, -51.4f),
                new Vector2(14.2f, 14.2f));
            globe.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                LanguageGlobePath);

            Text languageLabel = CreateText(
                "LangLabel", row.transform, font, 50, "Language",
                TextColor, FontStyle.Bold);
            SetTopLeft(
                languageLabel.rectTransform,
                new Vector2(104f, -21f),
                new Vector2(560f, 80f));
            languageLabel.alignment = TextAnchor.MiddleLeft;
            Localize(
                languageLabel,
                localization,
                font,
                eastAsianFont,
                "SETTING_LANGUAGE",
                "Language");
            RawImage arrow = CreateRawImage("ArrowIcon", row.transform);
            SetTopLeft(
                arrow.rectTransform,
                new Vector2(720f, -42f),
                new Vector2(44f, 36f));
            arrow.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                LanguageArrowPath);

            LanguageSwitchWidget widget =
                root.gameObject.AddComponent<LanguageSwitchWidget>();
            SerializedObject data = new(widget);
            SetReference(data, "outsideBlocker", blocker);
            SetReference(data, "rowButton", row);
            SetReference(data, "arrow", arrow.rectTransform);
            SetReference(data, "dropdown", dropdown.gameObject);
            SetReference(data, "panelBackground", panel.rectTransform);
            SetReference(data, "panelShadow", shadow.rectTransform);
            SetReference(data, "shadowGroup", shadowGroup);
            SetReference(data, "systemOption", system);
            SetReference(data, "systemGroup", systemGroup);
            SetReference(data, "systemLabel", systemLabel);
            SetReference(data, "primaryFont", font);
            SetReference(data, "eastAsianFallbackFont", eastAsianFont);
            SetReference(data, "englishOption", english);
            SetReference(data, "englishGroup", englishGroup);
            data.ApplyModifiedPropertiesWithoutUndo();
            blocker.gameObject.SetActive(false);
            dropdown.gameObject.SetActive(false);
            return widget;
        }

        private static Button CreateLanguageDropdownOption(
            string name,
            Transform parent,
            Font font,
            Shader rounded,
            string value,
            Vector2 position,
            out CanvasGroup group,
            out Text label,
            out GameObject highlightObject)
        {
            Button button = CreateTransparentButton(name, parent);
            SetTopLeft(
                (RectTransform)button.transform,
                position,
                new Vector2(756f, 164f));
            group = button.gameObject.AddComponent<CanvasGroup>();
            Image normal = CreateRoundedImage(
                "Default", button.transform, rounded, 32f,
                new Color(0.9843137f, 0.95686275f, 0.93333334f, 1f));
            Stretch(normal.rectTransform);
            Image highlight = CreateRoundedImage(
                "Highlight", button.transform, rounded, 32f,
                new Color(1f, 0.88235295f, 0.73333335f, 1f));
            Stretch(highlight.rectTransform);
            highlightObject = highlight.gameObject;
            highlightObject.SetActive(false);
            label = CreateText(
                "Label", button.transform, font, 60, value,
                TextColor, FontStyle.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        private static void ConfigurePressHighlight(
            Button button,
            GameObject highlight)
        {
            LanguageSwitchOptionPressView press =
                button.gameObject.AddComponent<LanguageSwitchOptionPressView>();
            SerializedObject data = new(press);
            SetReference(data, "highlight", highlight);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ToggleParts CreateToggleState(
            string name,
            Transform parent,
            Font font,
            Shader rounded,
            Texture2D circle,
            bool isOn,
            float width,
            float height,
            int fontSize)
        {
            Image pill = CreateRoundedImage(
                name, parent, rounded, height * 0.5f,
                isOn ? ToggleOnColor : ToggleOffColor);
            SetAnchored(pill.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0f, -185.5f),
                new Vector2(width, height), new Vector2(0.5f, 0.5f));
            AddToggleContents(
                pill.transform, font, circle, isOn, width, height, fontSize);
            return new ToggleParts(pill.gameObject);
        }

        private static ToggleParts CreateSmallToggleState(
            string name,
            Transform parent,
            Font font,
            Shader rounded,
            Texture2D circle,
            bool isOn)
        {
            Image pill = CreateRoundedImage(
                name, parent, rounded, 28f,
                isOn ? ToggleOnColor : ToggleOffColor);
            Stretch(pill.rectTransform);
            AddToggleContents(
                pill.transform, font, circle, isOn, 144f, 56f, 36);
            return new ToggleParts(pill.gameObject);
        }

        private static void AddToggleContents(
            Transform parent,
            Font font,
            Texture2D circle,
            bool isOn,
            float width,
            float height,
            int fontSize)
        {
            float circleSize = height >= 70f ? 61f : 45f;
            RawImage knob = CreateRawImage(
                isOn ? "CircleOn" : "CircleOff", parent);
            SetAnchored(knob.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(
                    isOn ? width * 0.31f : -width * 0.31f,
                    0f),
                new Vector2(circleSize, circleSize),
                new Vector2(0.5f, 0.5f));
            knob.texture = circle;
            Text label = CreateText(
                isOn ? "LabelOn" : "LabelOff",
                parent, font, fontSize, isOn ? "ON" : "OFF",
                PanelColor, FontStyle.Normal);
            SetAnchored(label.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(
                    isOn ? -width * 0.22f : width * 0.22f,
                    0f),
                new Vector2(width * 0.48f, height),
                new Vector2(0.5f, 0.5f));
        }

        private static SourceToastView CreateToast(
            Transform parent,
            Font font,
            Shader rounded)
        {
            RectTransform root = CreateRect("Toast", parent);
            Stretch(root);
            root.SetAsLastSibling();
            SourceToastView toast =
                root.gameObject.AddComponent<SourceToastView>();
            Image panelImage = CreateRoundedImage(
                "Panel", root, rounded, 30f, Color.white);
            RectTransform panel = panelImage.rectTransform;
            SetAnchored(panel,
                new Vector2(0.5f, 1f), new Vector2(0f, -750f),
                new Vector2(870f, 108f), new Vector2(0.5f, 0.5f));
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            Text label = CreateText(
                "Label", panel, font, 46, string.Empty,
                TextColor, FontStyle.Normal);
            Stretch(label.rectTransform, new Vector2(60f, 20f));
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            SerializedObject data = new(toast);
            SetReference(data, "panel", panel);
            SetReference(data, "panelGroup", group);
            SetReference(data, "label", label);
            data.ApplyModifiedPropertiesWithoutUndo();
            return toast;
        }

        private static Button CreateOutlineButton(
            string name,
            Transform parent,
            Font font,
            Shader rounded,
            string text,
            int fontSize,
            Vector2 size)
        {
            Image border = CreateRoundedImage(
                name, parent, rounded, size.y * 0.5f, BorderColor);
            SetPreferred(border.gameObject, size.x, size.y);
            border.rectTransform.sizeDelta = size;
            border.raycastTarget = true;
            Image inner = CreateRoundedImage(
                "Inner", border.transform, rounded,
                Mathf.Max(0f, size.y * 0.5f - 4f), PanelColor);
            Stretch(inner.rectTransform, new Vector2(4f, 4f));
            Button button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = border;
            Text label = CreateText(
                "Label", border.transform, font, fontSize, text,
                new Color(0.753f, 0.455f, 0.255f, 1f), FontStyle.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        private static Button CreateLinkButton(
            string name,
            Transform parent,
            Font font,
            string text,
            int size)
        {
            Button button = CreateTransparentButton(name, parent);
            Text label = CreateText(
                "Label", button.transform, font, size, text,
                TextColor, FontStyle.Normal);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Min(30, size);
            label.resizeTextMaxSize = size;
            Stretch(label.rectTransform);
            return button;
        }

        private static Button CreateTransparentButton(
            string name,
            Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static GameObject CreateSpacer(
            string name,
            Transform parent,
            float height)
        {
            RectTransform rect = CreateRect(name, parent);
            SetPreferred(rect.gameObject, 0f, height);
            return rect.gameObject;
        }

        private static Image CreateRoundedImage(
            string name,
            Transform parent,
            Shader shader,
            float radius,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RoundedImageView rounded =
                image.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(rounded);
            SetReference(data, "target", image);
            SetReference(data, "roundedShader", shader);
            data.FindProperty("cornerRadius").floatValue = radius;
            data.ApplyModifiedPropertiesWithoutUndo();
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value,
            Color color,
            FontStyle style)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static void LocalizeButton(
            Button button,
            LocalizationCatalog catalog,
            Font primary,
            Font eastAsian,
            string key,
            string fallback)
        {
            if (button == null) return;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                Localize(label, catalog, primary, eastAsian, key, fallback);
        }

        private static LocalizedText Localize(
            Text target,
            LocalizationCatalog catalog,
            Font primary,
            Font eastAsian,
            string key,
            string fallback)
        {
            if (target == null) return null;
            LocalizedText localized =
                target.gameObject.AddComponent<LocalizedText>();
            SerializedObject data = new(localized);
            SetReference(data, "catalog", catalog);
            SetReference(data, "target", target);
            SetReference(data, "primaryFont", primary);
            SetReference(data, "eastAsianFallbackFont", eastAsian);
            data.FindProperty("key").stringValue = key;
            data.FindProperty("fallbackText").stringValue = fallback;
            data.ApplyModifiedPropertiesWithoutUndo();
            localized.Refresh();
            return localized;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            SetUiLayer(target);
            RectTransform rect = (RectTransform)target.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void ConfigureWindow(
            SettingsPagePresenter presenter,
            Canvas canvas,
            CanvasGroup group,
            Button closeButton)
        {
            SerializedObject data = new(presenter);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Popup;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = true;
            data.FindProperty("maskOpacity").floatValue = 0.8f;
            data.FindProperty("playOpenSound").boolValue = true;
            SetReference(data, "rootCanvas", canvas);
            SetReference(data, "rootCanvasGroup", group);
            SetReference(data, "closeButton", closeButton);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(
            SerializedObject data,
            string name,
            Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetPreferred(
            GameObject target,
            float width,
            float height)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null) layout = target.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
        }

        private static void SetCentered(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetAnchored(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopLeft(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void SetUiLayer(GameObject target)
        {
            int layer = LayerMask.NameToLayer("UI");
            target.layer = layer >= 0 ? layer : 0;
        }

        private readonly struct ToggleParts
        {
            public ToggleParts(GameObject root)
            {
                Root = root;
            }

            public GameObject Root { get; }
        }

        private readonly struct PatternParts
        {
            public PatternParts(
                GameObject root,
                Button button,
                GameObject on,
                GameObject off,
                GameObject dot)
            {
                Root = root;
                Button = button;
                On = on;
                Off = off;
                Dot = dot;
            }

            public GameObject Root { get; }
            public Button Button { get; }
            public GameObject On { get; }
            public GameObject Off { get; }
            public GameObject Dot { get; }
        }
    }
}
