using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Online;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Rank;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using Meowdoku.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Meowdoku.Tests.EditMode
{
    public sealed class AppRuntimeCompositionTests
    {
        private const string RegistryPath =
            "Assets/_Project/Settings/UIRegistry.asset";
        private const string SplashPath =
            "Assets/_Project/Prefabs/UI/SplashPage.prefab";
        private const string HomePath =
            "Assets/_Project/Prefabs/UI/HomePage.prefab";
        private const string TutorialPath =
            "Assets/_Project/Prefabs/UI/TutorialPage.prefab";
        private const string SettingPath =
            "Assets/_Project/Prefabs/UI/SettingsPage.prefab";
        private const string LanguagePath =
            "Assets/_Project/Prefabs/UI/LanguagePage.prefab";
        private const string HowToPlayPath =
            "Assets/_Project/Prefabs/UI/HowToPlayPage.prefab";
        private const string HowToPlayPagedPath =
            "Assets/_Project/Prefabs/UI/HowToPlayPagedPage.prefab";
        private const string BankPath =
            "Assets/_Project/Prefabs/UI/BankPage.prefab";
        private const string GamePath =
            "Assets/_Project/Prefabs/UI/GamePage.prefab";
        private const string CellPath =
            "Assets/_Project/Prefabs/Cell.prefab";
        private const string WinPath =
            "Assets/_Project/Prefabs/UI/WinPage.prefab";
        private const string FailPath =
            "Assets/_Project/Prefabs/UI/FailPage.prefab";
        private const string PrivacyPath =
            "Assets/_Project/Prefabs/UI/PrivacyDialog.prefab";
        private const string PreAttPath =
            "Assets/_Project/Prefabs/UI/PreAttGuidePage.prefab";
        private const string PreAttV2Path =
            "Assets/_Project/Prefabs/UI/PreAttGuidePageV2.prefab";
        private const string PrePushPath =
            "Assets/_Project/Prefabs/UI/PrePushGuidePage.prefab";
        private const string FeedbackPath =
            "Assets/_Project/Prefabs/UI/FeedbackPage.prefab";
        private const string RateUsPath =
            "Assets/_Project/Prefabs/UI/RateUsPage.prefab";
        private const string RateUsV2Path =
            "Assets/_Project/Prefabs/UI/RateUsPageV2.prefab";
        private const string ConfirmPath =
            "Assets/_Project/Prefabs/UI/ConfirmDialog.prefab";
        private const string ProfilePath =
            "Assets/_Project/Prefabs/UI/ProfilePage.prefab";
        private const string AppScenePath =
            "Assets/_Project/Scenes/AppScene.unity";

        private static readonly PrimaryPageSpec[] PrimaryPages =
        {
            new(UiName.Home, HomePath, typeof(HomePagePresenter)),
            new(UiName.Tutorial, TutorialPath, typeof(TutorialPagePresenter)),
            new(UiName.Setting, SettingPath, typeof(SettingsPagePresenter)),
            new(UiName.Language, LanguagePath, typeof(LanguagePagePresenter)),
            new(UiName.HowToPlay, HowToPlayPath,
                typeof(HowToPlayPagePresenter)),
            new(UiName.HowToPlayPaged, HowToPlayPagedPath,
                typeof(HowToPlayPagedPagePresenter)),
            new(UiName.Bank, BankPath, typeof(BankBrowserPagePresenter)),
            new(UiName.Game, GamePath, typeof(GameplayPagePresenter))
        };

        [Test]
        public void Registry_ContainsStartupAndGamePages()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.TryGetPrefab(UiName.Splash, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Game, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Win, out _), Is.True);
            Assert.That(registry.TryGetPrefab(UiName.Fail, out _), Is.True);
        }

        [Test]
        public void Registry_ContainsEveryPrimaryNavigationPage()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.ValidateEntries(), Is.Empty);

            foreach (PrimaryPageSpec spec in PrimaryPages)
            {
                Assert.That(
                    registry.TryGetPrefab(spec.Name, out UIFrameWindow window),
                    Is.True,
                    spec.Name + " is missing from UIRegistry.");
                Assert.That(
                    AssetDatabase.GetAssetPath(window),
                    Is.EqualTo(spec.Path),
                    spec.Name + " points to the wrong prefab.");
                Assert.That(
                    window.GetComponent(spec.PresenterType),
                    Is.Not.Null,
                    spec.Path + " is missing " + spec.PresenterType.Name + ".");
                Assert.That(
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        window.gameObject),
                    Is.Zero,
                    spec.Path + " contains a missing script.");
            }
        }

        [Test]
        public void Registry_ContainsEveryPlatformGuidePage()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);

            AssertRegistryPage<PrivacyDialogPresenter>(
                registry, UiName.Privacy, PrivacyPath);
            AssertRegistryPage<PreAttGuidePresenter>(
                registry, UiName.PreAttGuide, PreAttPath);
            AssertRegistryPage<PreAttGuidePresenter>(
                registry, UiName.PreAttGuideV2, PreAttV2Path);
            AssertRegistryPage<PrePushGuidePresenter>(
                registry, UiName.PrePushGuide, PrePushPath);
        }

        [Test]
        public void Registry_ContainsProductServicePages()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            AssertRegistryPage<FeedbackPagePresenter>(
                registry, UiName.Feedback, FeedbackPath);
            AssertRegistryPage<RateUsPagePresenter>(
                registry, UiName.RateUs, RateUsPath);
            AssertRegistryPage<RateUsPagePresenter>(
                registry, UiName.RateUsV2, RateUsV2Path);
        }

        [Test]
        public void Registry_ContainsSourceConfirmDialog()
        {
            UIRegistry registry =
                AssetDatabase.LoadAssetAtPath<UIRegistry>(RegistryPath);
            AssertRegistryPage<ConfirmDialogPresenter>(
                registry, UiName.Confirm, ConfirmPath);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ConfirmPath);
            Assert.That(prefab.transform.Find(
                    "Root/Content/DialogRoot/CloseButton"),
                Is.Not.Null);
            Assert.That(prefab.transform.Find(
                    "Root/Content/DialogRoot/ActionButton"),
                Is.Not.Null);
        }

        [Test]
        public void ConfirmDialog_HasRequiredSourceBindings()
        {
            AssertBindings<ConfirmDialogPresenter>(
                ConfirmPath,
                "popupAnimator",
                "titleText",
                "contentText",
                "actionText",
                "actionButton",
                "confirmCloseButton",
                "localization");
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ConfirmPath);
            GenericPopupAnimator animator =
                prefab.GetComponent<GenericPopupAnimator>();
            Assert.That(animator, Is.Not.Null);
            SerializedProperty overlay = new SerializedObject(animator)
                .FindProperty("overlayGroup");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void ProductServicePrefabs_HaveSourceBindings()
        {
            AssertBindings<FeedbackPagePresenter>(
                FeedbackPath,
                "popupAnimator",
                "titleText",
                "descriptionText",
                "submitText",
                "thanksText",
                "inputField",
                "submitButton",
                "feedbackCloseButton",
                "localization");
            AssertBindings<RateUsPagePresenter>(
                RateUsPath,
                "popupAnimator",
                "titleText",
                "questionText",
                "litStar",
                "dimStar",
                "rateButton",
                "rateCloseButton",
                "localization");
            AssertBindings<RateUsPagePresenter>(
                RateUsV2Path,
                "popupAnimator",
                "titleText",
                "questionText",
                "litStar",
                "dimStar",
                "rateButton",
                "rateCloseButton",
                "localization");
            AssertArraySize<RateUsPagePresenter>(RateUsPath, "stars", 5);
            AssertArraySize<RateUsPagePresenter>(RateUsV2Path, "stars", 5);
        }

        [Test]
        public void PlatformGuidePrefabs_HaveRequiredBindingsAndHierarchy()
        {
            AssertBindings<PrivacyDialogPresenter>(
                PrivacyPath,
                "popupAnimator",
                "titleText",
                "contentText",
                "acceptText",
                "acceptButton",
                "termsButton",
                "privacyButton",
                "localization");
            AssertBindings<PreAttGuidePresenter>(
                PreAttPath,
                "popupAnimator",
                "titleText",
                "descriptionText",
                "continueText",
                "continueButton",
                "localization");
            AssertBindings<PreAttGuidePresenter>(
                PreAttV2Path,
                "popupAnimator",
                "titleText",
                "descriptionText",
                "continueText",
                "continueButton",
                "guideCloseButton",
                "localization");
            AssertBindings<PrePushGuidePresenter>(
                PrePushPath,
                "popupAnimator",
                "titleText",
                "descriptionText",
                "allowText",
                "allowButton",
                "guideCloseButton",
                "localization");

            GameObject privacy =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrivacyPath);
            GameObject preAtt =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreAttPath);
            GameObject preAttV2 =
                AssetDatabase.LoadAssetAtPath<GameObject>(PreAttV2Path);
            GameObject prePush =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrePushPath);
            Assert.That(privacy.transform.Find("Root/Content/Panel/AcceptButton"),
                Is.Not.Null);
            Assert.That(preAtt.transform.Find("Root/Content/ContinueButton"),
                Is.Not.Null);
            Assert.That(
                preAttV2.transform.Find("Root/Content/Panel/CloseButton"),
                Is.Not.Null);
            Assert.That(prePush.transform.Find("Popup/Cat/Group957Img"),
                Is.Not.Null);
            Assert.That(prePush.transform.Find("Popup/AllowButton"),
                Is.Not.Null);
        }

        [Test]
        public void PrimaryNavigationPresenters_HaveRequiredBindings()
        {
            AssertBindings<HomePagePresenter>(
                HomePath,
                "layoutSpace",
                "startButton",
                "settingsButton",
                "profileAvatar",
                "dailyEntry",
                "streakEntry",
                "rankEntry");
            GameObject homePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HomePath);
            ProfileAvatarView homeAvatar = homePrefab.transform.Find(
                "Root/VBoxContainer/Header/ProfileEntry/AvatarSlot")
                ?.GetComponentInChildren<ProfileAvatarView>(true);
            Assert.That(homeAvatar, Is.Not.Null);
            var homeAvatarData = new SerializedObject(homeAvatar);
            SerializedProperty avatarSprites =
                homeAvatarData.FindProperty("avatarSprites");
            Assert.That(avatarSprites, Is.Not.Null);
            Assert.That(avatarSprites.arraySize, Is.GreaterThan(0));
            Assert.That(avatarSprites.GetArrayElementAtIndex(0)
                .objectReferenceValue, Is.Not.Null);
            AssertBindings<TutorialPagePresenter>(
                TutorialPath,
                "boardView",
                "layoutSpace",
                "boardInputGroup",
                "cellPrefab",
                "hintButton",
                "confirmButton");
            GameObject tutorialPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TutorialPath);
            Assert.That(
                tutorialPrefab.GetComponent<TutorialPagePresenter>(),
                Is.InstanceOf<IAbConfigRuntimeConsumer>(),
                "Tutorial must receive the shared region_color config used by BoardView.");
            TutorialFinishEffects tutorialEffects =
                tutorialPrefab.GetComponentInChildren<TutorialFinishEffects>(true);
            Assert.That(tutorialEffects, Is.Not.Null);
            var tutorialEffectData = new SerializedObject(tutorialEffects);
            Assert.That(tutorialEffectData.FindProperty("effectRoot")
                .objectReferenceValue, Is.Not.Null);
            Assert.That(tutorialEffectData.FindProperty("lineSprite")
                .objectReferenceValue, Is.Not.Null);
            Assert.That(tutorialEffectData.FindProperty("starSprite")
                .objectReferenceValue, Is.Not.Null);
            Assert.That(tutorialEffectData.FindProperty("glowSprite")
                .objectReferenceValue, Is.Not.Null);
            Assert.That(tutorialEffectData.FindProperty("ribbonSprites")
                .arraySize, Is.EqualTo(4));
            AssertBindings<SettingsPagePresenter>(
                SettingPath,
                "popupAnimator",
                "musicToggle",
                "soundToggle",
                "vibrationToggle",
                "peopleToggle",
                "languageButton",
                "howToPlayButton");
            GameObject settingsPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SettingPath);
            LanguageSwitchWidget languageSwitch =
                settingsPrefab.GetComponentInChildren<LanguageSwitchWidget>(true);
            Assert.That(languageSwitch, Is.Not.Null);
            var languageSwitchData = new SerializedObject(languageSwitch);
            SerializedProperty outsideBlocker =
                languageSwitchData.FindProperty("outsideBlocker");
            Assert.That(outsideBlocker, Is.Not.Null);
            Graphic outsideGraphic =
                outsideBlocker.objectReferenceValue as Graphic;
            Assert.That(outsideGraphic, Is.Not.Null,
                "Language outside blocker must be a pointer-down Graphic.");
            Assert.That(outsideGraphic.raycastTarget, Is.True);
            Assert.That(outsideGraphic.GetComponent<Button>(), Is.Null,
                "A Button would close on release instead of source pointer-down.");
            AssertBindings<LanguagePagePresenter>(
                LanguagePath,
                "popupAnimator",
                "scrollRect",
                "confirmButton");
            AssertArraySize<LanguagePagePresenter>(
                LanguagePath,
                "optionViews",
                10);
            AssertBindings<HowToPlayPagePresenter>(
                HowToPlayPath,
                "popupAnimator",
                "tapCatcher");
            AssertArraySize<HowToPlayPagePresenter>(
                HowToPlayPath,
                "boards",
                3);
            AssertBindings<HowToPlayPagedPagePresenter>(
                HowToPlayPagedPath,
                "popupAnimator",
                "caption",
                "backButton",
                "mainButton");
            GameObject pagedHowToPlay =
                AssetDatabase.LoadAssetAtPath<GameObject>(HowToPlayPagedPath);
            Transform pagedBackIcon = pagedHowToPlay.transform.Find(
                "Root/Content/ButtonRow/BackBtn/BackIcon");
            Assert.That(pagedBackIcon, Is.Not.Null);
            Assert.That(
                pagedBackIcon.GetComponent<SourceBackChevronGraphic>(),
                Is.Not.Null,
                "Paged HTP must adapt the source SVG instead of showing a null-sprite Image.");
            Assert.That(pagedBackIcon.GetComponent<Image>(), Is.Null);
            AssertArraySize<HowToPlayPagedPagePresenter>(
                HowToPlayPagedPath,
                "boards",
                3);
            AssertArraySize<HowToPlayPagedPagePresenter>(
                HowToPlayPagedPath,
                "boardRects",
                3);
            AssertBindings<BankBrowserPagePresenter>(
                BankPath,
                "homeBackButton",
                "rootPanel",
                "regularCard",
                "lkCard",
                "lkModifiedCard",
                "lkStyleCard",
                "gcCard",
                "specialCard");
            AssertBindings<GameplayPagePresenter>(
                GamePath,
                "gameplayManager",
                "backButton",
                "settingsButton",
                "infoButton",
                "returnBankButton",
                "winToast");
        }

        [Test]
        public void GameplayBackground_MatchesSourceColorAndFillsPage()
        {
            GameObject game =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);
            Assert.That(game, Is.Not.Null, GamePath);
            Transform backgroundTransform = game.transform.Find("Background");
            Assert.That(backgroundTransform, Is.Not.Null,
                "GamePage is missing its direct Background child.");
            Image background = backgroundTransform.GetComponent<Image>();
            Assert.That(background, Is.Not.Null,
                "GamePage Background is missing Image.");
            Assert.That(backgroundTransform.GetSiblingIndex(), Is.Zero);
            Assert.That(background.rectTransform.anchorMin,
                Is.EqualTo(Vector2.zero));
            Assert.That(background.rectTransform.anchorMax,
                Is.EqualTo(Vector2.one));
            Assert.That(background.rectTransform.anchoredPosition,
                Is.EqualTo(Vector2.zero));
            Assert.That(background.rectTransform.sizeDelta,
                Is.EqualTo(Vector2.zero));
            Assert.That(background.raycastTarget, Is.False);
            Assert.That(background.sprite, Is.Null);
            Assert.That(background.color.r, Is.EqualTo(0.969f).Within(0.0001f));
            Assert.That(background.color.g, Is.EqualTo(0.949f).Within(0.0001f));
            Assert.That(background.color.b, Is.EqualTo(0.937f).Within(0.0001f));
            Assert.That(background.color.a, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GameplayPatternAssets_AreSerializedFromTheSourcePalette()
        {
            AssertBindings<CellView>(CellPath, "patternImage");

            GameObject game =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);
            Assert.That(game, Is.Not.Null, GamePath);
            BoardView board = game.GetComponentInChildren<BoardView>(true);
            Assert.That(board, Is.Not.Null, "GamePage is missing BoardView.");
            var serialized = new SerializedObject(board);
            SerializedProperty icons = serialized.FindProperty("patternIcons");
            Assert.That(icons, Is.Not.Null);
            Assert.That(icons.arraySize, Is.EqualTo(12));
            for (int index = 0; index < icons.arraySize; index++)
            {
                Assert.That(
                    icons.GetArrayElementAtIndex(index).objectReferenceValue,
                    Is.Not.Null,
                    "patternIcons[" + index + "] is not assigned.");
            }
        }

        [Test]
        public void GameplayBottomTools_HaveSerializedSourceSprites()
        {
            GameObject game =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);
            Assert.That(game, Is.Not.Null, GamePath);
            Transform locate = game.transform.Find("HUD/BottomTools/Locate");
            Transform hint = game.transform.Find("HUD/BottomTools/Hint");
            Assert.That(locate, Is.Not.Null, "Locate tool root is missing.");
            Assert.That(hint, Is.Not.Null, "Hint tool root is missing.");
            AssertToolSourceSprites(locate);
            AssertToolSourceSprites(hint);
        }

        [Test]
        public void GameplayLifeSlots_HaveSourceParticlePools()
        {
            GameObject game =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);
            GameplayLifeSlotView[] slots =
                game.GetComponentsInChildren<GameplayLifeSlotView>(true);
            Assert.That(slots, Has.Length.EqualTo(3));
            foreach (GameplayLifeSlotView slot in slots)
            {
                var data = new SerializedObject(slot);
                Assert.That(data.FindProperty("reviveGlow").objectReferenceValue,
                    Is.Not.Null);
                SerializedProperty fish = data.FindProperty("fishParticles");
                SerializedProperty glow = data.FindProperty("glowParticles");
                Assert.That(fish.arraySize, Is.EqualTo(6));
                Assert.That(glow.arraySize, Is.EqualTo(6));
                for (int index = 0; index < 6; index++)
                {
                    Assert.That(fish.GetArrayElementAtIndex(index)
                        .objectReferenceValue, Is.Not.Null);
                    Assert.That(glow.GetArrayElementAtIndex(index)
                        .objectReferenceValue, Is.Not.Null);
                }
            }
        }

        [Test]
        public void ResultPagePrefabs_HaveSourceBranchesAndPresenters()
        {
            GameObject win =
                AssetDatabase.LoadAssetAtPath<GameObject>(WinPath);
            GameObject fail =
                AssetDatabase.LoadAssetAtPath<GameObject>(FailPath);

            Assert.That(win, Is.Not.Null);
            Assert.That(fail, Is.Not.Null);
            Assert.That(win.GetComponent<GameWinPagePresenter>(), Is.Not.Null);
            Assert.That(fail.GetComponent<GameFailPagePresenter>(), Is.Not.Null);
            Assert.That(win.transform.Find("Root/Visuals"), Is.Not.Null);
            Assert.That(win.transform.Find("Root/Content/Actions"), Is.Not.Null);
            Assert.That(win.transform.Find("Root/PassPanel/Popup/Statistics"),
                Is.Not.Null);
            Assert.That(
                win.transform.Find("Root/PassPanel/Popup/ExtraStatistics"),
                Is.Not.Null);
            Assert.That(win.transform.Find("Root/PassPanel/Actions/Next"),
                Is.Not.Null);
            Assert.That(fail.transform.Find("Root/Visuals"), Is.Not.Null);
            Assert.That(fail.transform.Find("Root/Content/Actions"), Is.Not.Null);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(win),
                Is.Zero);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(fail),
                Is.Zero);
        }

        [Test]
        public void GeneratedPagePrefabs_HaveRequiredPresenters()
        {
            GameObject splash =
                AssetDatabase.LoadAssetAtPath<GameObject>(SplashPath);
            GameObject game = AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);

            Assert.That(splash, Is.Not.Null);
            Assert.That(game, Is.Not.Null);
            Assert.That(splash.GetComponent<SplashPagePresenter>(), Is.Not.Null);
            Assert.That(game.GetComponent<GameplayPagePresenter>(), Is.Not.Null);
            Assert.That(
                game.GetComponentInChildren<GameplayManager>(true),
                Is.Not.Null);
            Assert.That(game.transform.Find("Overlays/WinToast"), Is.Not.Null);
            Assert.That(
                game.GetComponentInChildren<GameplayWinToastPresenter>(true),
                Is.Not.Null);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(splash),
                Is.Zero);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(game),
                Is.Zero);
        }

        [Test]
        public void ProfilePrefab_UsesSourcePopupAndTabGeometry()
        {
            GameObject profile =
                AssetDatabase.LoadAssetAtPath<GameObject>(ProfilePath);
            Assert.That(profile, Is.Not.Null);
            ProfilePagePresenter presenter =
                profile.GetComponent<ProfilePagePresenter>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter, Is.InstanceOf<IRankActivityConsumer>());
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    profile),
                Is.Zero);

            RectTransform content = profile.transform.Find("Content") as
                RectTransform;
            Assert.That(content, Is.Not.Null);
            Assert.That(content.anchorMin,
                Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchorMax,
                Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.pivot,
                Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(content.sizeDelta,
                Is.EqualTo(new Vector2(900f, 1253f)));

            Image title = profile.transform.Find("Content/Title")
                .GetComponent<Image>();
            Text titleText = profile.transform.Find(
                    "Content/Title/PopupTitle")
                .GetComponent<Text>();
            AssertColor(title.color,
                new Color(0.9764706f, 0.9254902f, 0.88235295f, 1f));
            AssertColor(titleText.color,
                new Color(0.426923f, 0.3251181f, 0.34547916f, 1f));

            AssertTopLeft(
                profile.transform.Find(
                    "Content/TabGroup/AvatarTab/Label") as RectTransform,
                110f,
                7f,
                174f,
                86f);
            AssertTopLeft(
                profile.transform.Find(
                    "Content/TabGroup/FrameTab/Label") as RectTransform,
                112f,
                6f,
                170f,
                88f);
            LayoutElement bottomPad = profile.transform.Find(
                    "Content/AvatarScroll/Content/BottomPad")
                .GetComponent<LayoutElement>();
            Assert.That(bottomPad.preferredHeight, Is.Zero);
        }

        [Test]
        public void AppScene_HasSerializedBootstrapAndUiManager()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(AppScenePath);
            try
            {
                AppBootstrap bootstrap = Find<AppBootstrap>(scene);
                UIManager manager = Find<UIManager>(scene);
                AdRuntime adRuntime = Find<AdRuntime>(scene);
                AbConfigRuntime abRuntime = Find<AbConfigRuntime>(scene);
                AuthRuntime authRuntime = Find<AuthRuntime>(scene);
                DataSyncHttpApi dataSyncApi = Find<DataSyncHttpApi>(scene);
                DataSyncRuntime dataSyncRuntime = Find<DataSyncRuntime>(scene);
                PrivacyPermissionRuntime platformRuntime =
                    Find<PrivacyPermissionRuntime>(scene);
                ProductServiceRuntime productRuntime =
                    Find<ProductServiceRuntime>(scene);
                Assert.That(bootstrap, Is.Not.Null);
                Assert.That(manager, Is.Not.Null);
                Assert.That(adRuntime, Is.Not.Null);
                Assert.That(abRuntime, Is.Not.Null);
                Assert.That(authRuntime, Is.Not.Null);
                Assert.That(dataSyncApi, Is.Not.Null);
                Assert.That(dataSyncRuntime, Is.Not.Null);
                Assert.That(platformRuntime, Is.Not.Null);
                Assert.That(productRuntime, Is.Not.Null);
                Assert.That(platformRuntime.transform.parent.name,
                    Is.EqualTo("App"));
                Assert.That(platformRuntime.transform.name,
                    Is.EqualTo("Systems"));
                var bootstrapData = new SerializedObject(bootstrap);
                Assert.That(
                    bootstrapData.FindProperty("dataSyncRuntime")
                        .objectReferenceValue,
                    Is.SameAs(dataSyncRuntime));
                Assert.That(
                    bootstrapData.FindProperty("platformRuntime")
                        .objectReferenceValue,
                    Is.SameAs(platformRuntime));
                Assert.That(
                    bootstrapData.FindProperty("productServiceRuntime")
                        .objectReferenceValue,
                    Is.SameAs(productRuntime));
                var syncData = new SerializedObject(dataSyncRuntime);
                Assert.That(
                    syncData.FindProperty("authRuntime").objectReferenceValue,
                    Is.SameAs(authRuntime));
                Assert.That(
                    syncData.FindProperty("apiAdapter").objectReferenceValue,
                    Is.SameAs(dataSyncApi));
                var managerData = new SerializedObject(manager);
                Assert.That(
                    managerData.FindProperty("dataSyncRuntime")
                        .objectReferenceValue,
                    Is.SameAs(dataSyncRuntime));
                Assert.That(
                    managerData.FindProperty("platformRuntime")
                        .objectReferenceValue,
                    Is.SameAs(platformRuntime));
                Assert.That(
                    managerData.FindProperty("productServiceRuntime")
                        .objectReferenceValue,
                    Is.SameAs(productRuntime));
                var platformData = new SerializedObject(platformRuntime);
                Assert.That(
                    platformData.FindProperty("uiManager")
                        .objectReferenceValue,
                    Is.SameAs(manager));
                Assert.That(
                    platformData.FindProperty("localization")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    platformData.FindProperty("abConfigRuntime")
                        .objectReferenceValue,
                    Is.SameAs(abRuntime));
                Assert.That(
                    platformData.FindProperty("trackingRuntime")
                        .objectReferenceValue,
                    Is.Not.Null);
                var productData = new SerializedObject(productRuntime);
                Assert.That(
                    productData.FindProperty("uiManager")
                        .objectReferenceValue,
                    Is.SameAs(manager));
                Assert.That(
                    productData.FindProperty("abConfigRuntime")
                        .objectReferenceValue,
                    Is.SameAs(abRuntime));
                Assert.That(FindRoot(scene, "App"), Is.Not.Null);
                Assert.That(FindRoot(scene, "EventSystem"), Is.Not.Null);
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void StartupPrefabs_HaveRenderableRootScale()
        {
            UIRegistry registry = AssetDatabase.LoadAssetAtPath<UIRegistry>(
                RegistryPath);
            Assert.That(registry, Is.Not.Null);
            foreach (UiName name in new[]
            {
                UiName.Splash,
                UiName.Home,
                UiName.Tutorial
            })
            {
                Assert.That(registry.TryGetPrefab(
                    name,
                    out UIFrameWindow prefab), Is.True);
                Assert.That(prefab.transform.localScale,
                    Is.EqualTo(Vector3.one),
                    $"Startup prefab {name} root is not renderable.");
            }
        }

        [Test]
        public void AppScene_OwnsOneSharedSoundRuntimeAndGamePageOwnsNone()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(AppScenePath);
            try
            {
                UIManager manager = Find<UIManager>(scene);
                SoundRuntime runtime = Find<SoundRuntime>(scene);
                SoundService service = Find<SoundService>(scene);
                Assert.That(manager, Is.Not.Null);
                Assert.That(runtime, Is.Not.Null);
                Assert.That(service, Is.Not.Null);
                Assert.That(Count<SoundRuntime>(scene), Is.EqualTo(1));
                Assert.That(Count<SoundService>(scene), Is.EqualTo(1));
                Assert.That(runtime.Service, Is.SameAs(service));
                Assert.That(runtime.transform.name, Is.EqualTo("Audio"));
                Assert.That(runtime.transform.parent.name, Is.EqualTo("Systems"));

                var runtimeData = new SerializedObject(runtime);
                Assert.That(runtimeData.FindProperty("uiManager")
                        .objectReferenceValue,
                    Is.SameAs(manager));
                Assert.That(runtimeData.FindProperty("soundService")
                        .objectReferenceValue,
                    Is.SameAs(service));
                var serviceData = new SerializedObject(service);
                Assert.That(serviceData.FindProperty("catalog")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(serviceData.FindProperty("bgmSource")
                        .objectReferenceValue,
                    Is.Not.Null);
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            }

            GameObject game =
                AssetDatabase.LoadAssetAtPath<GameObject>(GamePath);
            Assert.That(game, Is.Not.Null);
            Assert.That(game.GetComponentInChildren<SoundRuntime>(true), Is.Null);
            Assert.That(game.GetComponentInChildren<SoundService>(true), Is.Null,
                "GamePage must consume the App-scoped SoundManager equivalent.");
            Assert.That(game.GetComponent<GameplayPagePresenter>(),
                Is.InstanceOf<ISoundServiceConsumer>());
        }

        [Test]
        public void AppScene_ExcludesLegacyPrototypeServices()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(AppScenePath);
            try
            {
                Assert.That(
                    Find<SceneLoader>(scene),
                    Is.Null,
                    "Production AppScene must not depend on prototype SceneLoader.");
                Assert.That(
                    Find<PoolManager>(scene),
                    Is.Null,
                    "Production AppScene must use BoardView-owned pools instead of the prototype PoolManager.");
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void BuildSettings_StartWithAppScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.EqualTo(1),
                "Prototype scenes must stay out of the portfolio player.");
            Assert.That(scenes[0].path, Is.EqualTo(AppScenePath));
            Assert.That(scenes[0].enabled, Is.True);

            var enabledPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (!scene.enabled) continue;
                Assert.That(
                    File.Exists(scene.path),
                    Is.True,
                    "Enabled build scene is missing: " + scene.path);
                Assert.That(
                    enabledPaths.Add(scene.path),
                    Is.True,
                    "Enabled build scene is duplicated: " + scene.path);
            }
        }

        [Test]
        public void PortfolioPlayerSettings_MatchSourcePortraitContract()
        {
            Assert.That(PlayerSettings.companyName,
                Is.EqualTo("Meowdoku Portfolio"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("Meowdoku"));
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("0.0.1"));
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(540));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(960));
            Assert.That(PlayerSettings.fullScreenMode,
                Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(PlayerSettings.resizableWindow, Is.True);
            Assert.That(PlayerSettings.defaultInterfaceOrientation,
                Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.True);
            Assert.That(
                PlayerSettings.allowedAutorotateToPortraitUpsideDown,
                Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft,
                Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight,
                Is.False);
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(
                    NamedBuildTarget.Standalone),
                Is.EqualTo("com.meowdoku.portfolio"));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(
                    NamedBuildTarget.Android),
                Is.EqualTo("com.meowdoku.portfolio"));
            Assert.That(PlayerSettings.Android.targetArchitectures,
                Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(PlayerSettings.Android.minSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevel25));
            Assert.That(
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
                Is.EqualTo(ScriptingImplementation.IL2CPP));
        }

        [Test]
        public void BuildSettingsScenes_HaveNoMissingScripts()
        {
            foreach (EditorBuildSettingsScene buildScene in
                EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled) continue;

                Scene scene = EditorSceneManager.OpenPreviewScene(buildScene.path);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        Assert.That(
                            GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                                root),
                            Is.Zero,
                            buildScene.path + " contains a missing script under " +
                            root.name + ".");
                    }
                }
                finally
                {
                    if (scene.IsValid())
                        EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        [Test]
        public void BoardPrewarm_BuildsFourCellsPerFrameAndReusesThem()
        {
            var root = new GameObject("BoardRoot", typeof(RectTransform));
            var cellPrefab = new GameObject(
                "CellPrefab",
                typeof(RectTransform),
                typeof(CellView));
            try
            {
                BoardView board = root.AddComponent<BoardView>();
                board.cellPrefab = cellPrefab;
                board.cellsContainer = root.transform;

                IEnumerator prewarm = board.PrewarmCells(4);
                Assert.That(prewarm.MoveNext(), Is.True);
                Assert.That(
                    root.GetComponentsInChildren<CellView>(true).Length,
                    Is.EqualTo(4));
                while (prewarm.MoveNext()) { }
                Assert.That(
                    root.GetComponentsInChildren<CellView>(true).Length,
                    Is.EqualTo(16));

                IEnumerator secondPass = board.PrewarmCells(4);
                Assert.That(secondPass.MoveNext(), Is.False);
                Assert.That(
                    root.GetComponentsInChildren<CellView>(true).Length,
                    Is.EqualTo(16));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cellPrefab);
            }
        }

        [Test]
        public void BoardPool_SetupClearReusesCellsAndResetsState()
        {
            var boardRoot = new GameObject(
                "BoardPoolLifecycleRoot",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            var cellPrefab = new GameObject(
                "BoardPoolLifecycleCell",
                typeof(RectTransform),
                typeof(CellView));
            try
            {
                BoardView board = boardRoot.AddComponent<BoardView>();
                board.cellPrefab = cellPrefab;
                board.cellsContainer = boardRoot.transform;

                int[][] regions =
                {
                    new[] { 0, 1 },
                    new[] { 1, 0 }
                };
                int[] colorMap = { 0, 1 };
                board.SetupBoard(2, regions, colorMap);

                CellView[] firstCells =
                    boardRoot.GetComponentsInChildren<CellView>(true);
                Assert.That(firstCells, Has.Length.EqualTo(4));
                board.SetCellState(0, 0, CellStateType.CAT, false);
                Assert.That(firstCells[0].GetState(), Is.EqualTo(CellStateType.CAT));

                board.ClearBoard();
                Assert.That(
                    boardRoot.GetComponentsInChildren<CellView>(true),
                    Has.Length.EqualTo(4));
                foreach (CellView cell in firstCells)
                {
                    Assert.That(cell.gameObject.activeSelf, Is.False);
                    Assert.That(cell.GetState(), Is.EqualTo(CellStateType.EMPTY));
                }

                board.SetupBoard(2, regions, colorMap);
                CellView[] secondCells =
                    boardRoot.GetComponentsInChildren<CellView>(true);
                Assert.That(secondCells, Has.Length.EqualTo(4));
                foreach (CellView cell in secondCells)
                {
                    Assert.That(firstCells, Does.Contain(cell));
                    Assert.That(cell.gameObject.activeSelf, Is.True);
                    Assert.That(cell.GetState(), Is.EqualTo(CellStateType.EMPTY));
                }
            }
            finally
            {
                Object.DestroyImmediate(boardRoot);
                Object.DestroyImmediate(cellPrefab);
            }
        }

        [Test]
        public void BoardGrid_AllSourceSizesRemainSquareAndRowMajorAfterResize()
        {
            var boardRoot = new GameObject(
                "BoardSizeOrderRoot",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            var cellPrefab = new GameObject(
                "BoardSizeOrderCell",
                typeof(RectTransform),
                typeof(CellView));
            try
            {
                BoardView board = boardRoot.AddComponent<BoardView>();
                board.cellPrefab = cellPrefab;
                board.cellsContainer = boardRoot.transform;
                int[] resizeSequence = { 4, 7, 5, 10, 6, 9, 8 };

                foreach (int size in resizeSequence)
                {
                    var regions = new int[size][];
                    var colorMap = new int[size];
                    for (int row = 0; row < size; row++)
                    {
                        regions[row] = new int[size];
                        colorMap[row] = row;
                        for (int column = 0; column < size; column++)
                            regions[row][column] = row;
                    }

                    board.SetupBoard(size, regions, colorMap);

                    GridLayoutGroup grid = boardRoot.GetComponent<GridLayoutGroup>();
                    Assert.That(board.PuzzleSize, Is.EqualTo(size));
                    Assert.That(grid.constraint,
                        Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                    Assert.That(grid.constraintCount, Is.EqualTo(size));
                    Assert.That(grid.startCorner,
                        Is.EqualTo(GridLayoutGroup.Corner.UpperLeft));
                    Assert.That(grid.startAxis,
                        Is.EqualTo(GridLayoutGroup.Axis.Horizontal));

                    CellView[] activeCells =
                        boardRoot.GetComponentsInChildren<CellView>(false);
                    Assert.That(activeCells, Has.Length.EqualTo(size * size));
                    for (int row = 0; row < size; row++)
                    {
                        for (int column = 0; column < size; column++)
                        {
                            int rowMajorIndex = row * size + column;
                            CellView cell = board.GetCellForTests(row, column);
                            Assert.That(cell, Is.Not.Null);
                            Assert.That(activeCells[rowMajorIndex], Is.SameAs(cell));
                            Assert.That(cell.Row, Is.EqualTo(row));
                            Assert.That(cell.Col, Is.EqualTo(column));
                            Assert.That(cell.name,
                                Is.EqualTo($"Cell_{row}_{column}"));
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(boardRoot);
                Object.DestroyImmediate(cellPrefab);
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static int Count<T>(Scene scene) where T : Component
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;
            return count;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        private static void AssertBindings<T>(
            string prefabPath,
            params string[] propertyNames)
            where T : Component
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            T presenter = prefab.GetComponent<T>();
            Assert.That(presenter, Is.Not.Null, typeof(T).Name);
            var serialized = new SerializedObject(presenter);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property =
                    serialized.FindProperty(propertyName);
                Assert.That(
                    property,
                    Is.Not.Null,
                    typeof(T).Name + "." + propertyName +
                    " is not serialized.");
                Assert.That(
                    property.objectReferenceValue,
                    Is.Not.Null,
                    typeof(T).Name + "." + propertyName +
                    " is not assigned in " + prefabPath + ".");
            }
        }

        private static void AssertToolSourceSprites(Transform toolRoot)
        {
            foreach (string path in new[] { "Visual/Background", "Visual/Icon" })
            {
                Transform visual = toolRoot.Find(path);
                Assert.That(visual, Is.Not.Null,
                    toolRoot.name + "/" + path + " is missing.");
                Image image = visual.GetComponent<Image>();
                Assert.That(image, Is.Not.Null,
                    toolRoot.name + "/" + path + " is missing Image.");
                Assert.That(image.sprite, Is.Not.Null,
                    toolRoot.name + "/" + path + " has no source sprite.");
            }

            Transform background = toolRoot.Find("Visual/Background");
            Transform icon = toolRoot.Find("Visual/Icon");
            Assert.That(
                icon.GetSiblingIndex(),
                Is.GreaterThan(background.GetSiblingIndex()),
                toolRoot.name +
                " Visual/Background must render behind the icon.");
        }

        private static void AssertRegistryPage<T>(
            UIRegistry registry,
            UiName name,
            string expectedPath)
            where T : Component
        {
            Assert.That(
                registry.TryGetPrefab(name, out UIFrameWindow window),
                Is.True,
                name + " is missing from UIRegistry.");
            Assert.That(AssetDatabase.GetAssetPath(window),
                Is.EqualTo(expectedPath));
            Assert.That(window.GetComponent<T>(), Is.Not.Null);
            Assert.That(
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    window.gameObject),
                Is.Zero);
        }

        private static void AssertArraySize<T>(
            string prefabPath,
            string propertyName,
            int expected)
            where T : Component
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            T presenter = prefab.GetComponent<T>();
            Assert.That(presenter, Is.Not.Null, typeof(T).Name);
            var serialized = new SerializedObject(presenter);
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            Assert.That(property.isArray, Is.True);
            Assert.That(
                property.arraySize,
                Is.EqualTo(expected),
                typeof(T).Name + "." + propertyName);
            for (int index = 0; index < property.arraySize; index++)
            {
                Assert.That(
                    property.GetArrayElementAtIndex(index).objectReferenceValue,
                    Is.Not.Null,
                    typeof(T).Name + "." + propertyName +
                    "[" + index + "] is not assigned.");
            }
        }

        private static void AssertTopLeft(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.pivot, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(rect.anchoredPosition,
                Is.EqualTo(new Vector2(x, -y)));
            Assert.That(rect.sizeDelta,
                Is.EqualTo(new Vector2(width, height)));
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.00001f));
        }

        private readonly struct PrimaryPageSpec
        {
            public PrimaryPageSpec(
                UiName name,
                string path,
                Type presenterType)
            {
                Name = name;
                Path = path;
                PresenterType = presenterType;
            }

            public UiName Name { get; }
            public string Path { get; }
            public Type PresenterType { get; }
        }
    }
}
