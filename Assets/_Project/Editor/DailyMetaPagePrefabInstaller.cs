using System;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class DailyMetaPagePrefabInstaller
    {
        internal const string StreakPrefabPath =
            "Assets/_Project/Prefabs/UI/StreakPage.prefab";
        internal const string ResumePrefabPath =
            "Assets/_Project/Prefabs/UI/StreakResumePage.prefab";
        internal const string BackfillPrefabPath =
            "Assets/_Project/Prefabs/UI/StreakBackfillPage.prefab";
        internal const string AwardPrefabPath =
            "Assets/_Project/Prefabs/UI/AwardPage.prefab";
        internal const string AbSwitchPrefabPath =
            "Assets/_Project/Prefabs/UI/AbSwitchPopup.prefab";

        private const string HomePrefabPath =
            "Assets/_Project/Prefabs/UI/HomePage.prefab";
        private const string FontPath =
            "Assets/_Project/Fonts/Roboto.ttf";
        private const string BgPath =
            "Assets/_Project/Sprites/daily_streak/bg_9grid.png";
        private const string BestFramePath =
            "Assets/_Project/Sprites/daily_streak/sudoku_bg_round20.png";
        private const string SunPath =
            "Assets/_Project/Sprites/daily_streak/sun.png";
        private const string DotPath =
            "Assets/_Project/Sprites/daily_streak/dot.png";
        private const string CheckBarPath =
            "Assets/_Project/Sprites/Effects/mask/et_mask_008.png";
        private const string ChestPath =
            "Assets/_Project/Sprites/daily_streak/treasure_box.png";
        private const string EntryCheckedPath =
            "Assets/_Project/Sprites/daily_streak/state_checked1.png";
        private const string EntryUncheckedPath =
            "Assets/_Project/Sprites/daily_streak/state_unchecked.png";
        private const string MiniBgPath =
            "Assets/_Project/Sprites/daily_streak/mini_bg.png";
        private const string MiniGlowPath =
            "Assets/_Project/Sprites/daily_streak/mini_glow.png";
        private const string MiniSunPath =
            "Assets/_Project/Sprites/daily_streak/sun.png";
        private const string MiniCheckedPath =
            "Assets/_Project/Sprites/daily_streak/state_checked2.png";
        private const string MiniUncheckedPath =
            "Assets/_Project/Sprites/daily_streak/mini_unchecked_icon.png";
        private const string MiniShinePath =
            "Assets/_Project/Sprites/Effects/shine/et_shine_001.png";
        private const string ArrowPath =
            "Assets/_Project/Sprites/daily_streak/streak_arrow.png";
        private const string BackIconPath =
            "Assets/_Project/Sprites/daily_streak/vector_1.png";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string PrimaryButtonPath =
            "Assets/_Project/Sprites/common/btn_primary.png";
        private const string NormalButtonPath =
            "Assets/_Project/Sprites/common/normal_btn_bg.png";
        private const string ToolBgPath =
            "Assets/_Project/Sprites/game/btn_tool_bg.png";
        private const string LocatePath =
            "Assets/_Project/Sprites/game/tool_cat_item.png";
        private const string HintPath =
            "Assets/_Project/Sprites/game/icon_hint_lamp.png";
        private const string DialogFramePath =
            "Assets/_Project/Sprites/common/dialog_frame_9patch.png";
        private const string StreakCatPath =
            "Assets/_Project/Sprites/common/cat_daily_streak.png";
        private const string DialogPriorityPath =
            "Assets/_Project/Data/dialog_priority_strategy.json";
        private const string AbSwitchStrategyPath =
            "Assets/_Project/Data/ab_switch_popup_strategy.json";

        private static readonly Color Cream =
            new(1f, 0.965f, 0.925f, 1f);
        private static readonly Color Brown =
            new(0.455f, 0.31f, 0.22f, 1f);
        private static readonly Color Orange =
            new(0.945f, 0.576f, 0.125f, 1f);

        static DailyMetaPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += InstallIfReady;
            };
        }

        [MenuItem("Meowdoku/Port/Create Daily Meta Prefabs")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StreakPrefabPath);
        }

        internal static void InstallIfReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfReady;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            Sprite bg = LoadSprite(BgPath);
            Sprite bestFrame = LoadSprite(BestFramePath);
            Sprite sun = LoadSprite(SunPath);
            Sprite dot = LoadSprite(DotPath);
            Sprite checkBar = LoadSprite(CheckBarPath);
            Sprite chest = LoadSprite(ChestPath);
            Sprite checkedEntry = LoadSprite(EntryCheckedPath);
            Sprite uncheckedEntry = LoadSprite(EntryUncheckedPath);
            Sprite miniBg = LoadSprite(MiniBgPath);
            Sprite miniGlow = LoadSprite(MiniGlowPath);
            Sprite miniSun = LoadSprite(MiniSunPath);
            Sprite miniChecked = LoadSprite(MiniCheckedPath);
            Sprite miniUnchecked = LoadSprite(MiniUncheckedPath);
            Sprite miniShine = LoadSprite(MiniShinePath);
            Sprite arrow = LoadSprite(ArrowPath);
            Sprite backIcon = LoadSprite(BackIconPath);
            Sprite primary = LoadSprite(PrimaryButtonPath);
            Sprite normal = LoadSprite(NormalButtonPath);
            Shader roundedShader =
                AssetDatabase.LoadAssetAtPath<Shader>(RoundedShaderPath);
            Sprite toolBg = LoadSprite(ToolBgPath);
            Sprite locate = LoadSprite(LocatePath);
            Sprite hint = LoadSprite(HintPath);
            Sprite dialogFrame = LoadSprite(DialogFramePath);
            Sprite streakCat = LoadSprite(StreakCatPath);
            TextAsset dialogPriority =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    DialogPriorityPath);
            TextAsset abSwitchStrategy =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    AbSwitchStrategyPath);
            if (font == null || localization == null || bg == null ||
                bestFrame == null || backIcon == null ||
                sun == null || dot == null || chest == null ||
                checkBar == null ||
                checkedEntry == null || uncheckedEntry == null ||
                miniShine == null ||
                arrow == null || primary == null || normal == null ||
                toolBg == null || locate == null || hint == null ||
                dialogFrame == null || streakCat == null ||
                roundedShader == null ||
                dialogPriority == null || abSwitchStrategy == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            EnsureStreakPresentationPrefab(
                StreakPrefabPath,
                () => BuildStreak(
                    font, localization, bg, sun, dot, chest,
                    checkBar, bestFrame, primary, normal, backIcon,
                    roundedShader));
            EnsurePrefab(
                ResumePrefabPath,
                () => BuildRevive(
                    "StreakResumePage", font, localization,
                    bg, sun, arrow, primary, normal));
            EnsurePrefab(
                BackfillPrefabPath,
                () => BuildRevive(
                    "StreakBackfillPage", font, localization,
                    bg, sun, arrow, primary, normal));
            EnsurePrefab(
                AwardPrefabPath,
                () => BuildAward(
                    font, localization, bg, primary,
                    toolBg, locate, hint));
            EnsurePrefab(
                AbSwitchPrefabPath,
                () => BuildAbSwitchPopup(
                    font,
                    localization,
                    dialogFrame,
                    streakCat,
                    primary,
                    toolBg,
                    locate,
                    hint));
            UpgradeHomeEntry(
                font,
                localization,
                checkedEntry,
                uncheckedEntry,
                sun,
                miniChecked,
                miniBg,
                miniGlow,
                miniSun,
                miniChecked,
                miniUnchecked,
                miniShine,
                roundedShader,
                dialogPriority,
                abSwitchStrategy);
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static GameObject BuildStreak(
            Font font,
            LocalizationCatalog localization,
            Sprite background,
            Sprite sun,
            Sprite dot,
            Sprite chest,
            Sprite checkBar,
            Sprite bestFrameSprite,
            Sprite primary,
            Sprite normal,
            Sprite backIcon,
            Shader roundedShader)
        {
            GameObject page =
                CreatePage<StreakPagePresenter>("StreakPage");
            RectTransform root = CreateRect("StreakContent", page.transform);
            Stretch(root);

            Image bg = CreateImage("Background", root, background);
            Stretch(bg.rectTransform);
            bg.type = Image.Type.Simple;

            RectTransform top = CreateRect("Top", root);
            Stretch(top);
            Text title = CreateText(
                "Title", top, font, 56, "Daily Streak", Brown);
            SetCentered(title.rectTransform, new Vector2(0f, 920f),
                new Vector2(700f, 90f));

            Button back = CreateButton(
                "BackBtn", top, null, Color.clear);
            SetCentered((RectTransform)back.transform,
                new Vector2(-450f, 920f), new Vector2(100f, 100f));
            Image backBase = CreateImage("Base", back.transform, normal);
            SetCentered(backBase.rectTransform, Vector2.zero,
                new Vector2(140f, 140f));
            backBase.preserveAspect = true;
            Image backVisual = CreateImage("Icon", back.transform, backIcon);
            SetCentered(backVisual.rectTransform, Vector2.zero,
                new Vector2(54f, 46f));
            backVisual.preserveAspect = true;

            RectTransform hero = CreateRect("Hero", root);
            Stretch(hero);
            GameObject sunRoot = CreateRect(
                "SunRoot", hero).gameObject;
            SetCentered((RectTransform)sunRoot.transform,
                new Vector2(0f, 450f), new Vector2(512f, 512f));
            Image sunImage = CreateImage(
                "SunImg", sunRoot.transform, sun);
            Stretch(sunImage.rectTransform);
            sunImage.preserveAspect = true;
            Button sunButton = sunRoot.AddComponent<Button>();
            sunButton.targetGraphic = sunImage;

            RectTransform numberRoll = CreateRect("NumberRoll", hero);
            SetCentered(numberRoll, new Vector2(0f, 35f),
                new Vector2(1000f, 300f));
            numberRoll.gameObject.AddComponent<RectMask2D>();
            Text number = CreateText(
                "StreakNumber", numberRoll, font, 200, "0", Orange);
            Stretch(number.rectTransform);
            Text numberNew = CreateText(
                "StreakNumberNext", numberRoll, font, 200, "1", Orange);
            Stretch(numberNew.rectTransform);
            numberNew.rectTransform.anchoredPosition =
                new Vector2(0f, -220f);
            numberNew.gameObject.SetActive(false);
            Text current = CreateText(
                "CurrentStreak", hero, font, 70,
                "Current Streak", Brown);
            SetCentered(current.rectTransform, new Vector2(0f, -120f),
                new Vector2(720f, 90f));
            Image bestFrame = CreateImage(
                "BestFrame", hero, bestFrameSprite);
            SetCentered(bestFrame.rectTransform, new Vector2(0f, -225f),
                new Vector2(470f, 80f));
            bestFrame.preserveAspect = true;
            Text best = CreateText(
                "BestStreak", bestFrame.transform, font, 50,
                "Best Streak: 0", Brown);
            Stretch(best.rectTransform, new Vector2(20f, 8f));

            RectTransform slotRoot = CreateRect("WeekSlots", root);
            SetCentered(slotRoot, new Vector2(0f, -450f),
                new Vector2(980f, 200f));
            var slotViews = new StreakDaySlotView[7];
            for (int index = 0; index < slotViews.Length; index++)
            {
                RectTransform slot = CreateRect(
                    "Day" + (index + 1), slotRoot);
                SetCentered(slot,
                    new Vector2(-420f + index * 140f, 0f),
                    new Vector2(120f, 200f));
                slotViews[index] = BuildDaySlot(
                    slot, font, localization, dot, chest, checkBar,
                    roundedShader);
            }

            RectTransform instructions = CreateRect("Instructions", root);
            Stretch(instructions);
            Text tapText = CreateText(
                "TapSunText", instructions, font, 54,
                "Tap the sun, spark your streak!", Brown);
            SetCentered(tapText.rectTransform, new Vector2(0f, -650f),
                new Vector2(900f, 130f));

            RectTransform actions = CreateRect("Actions", root);
            Stretch(actions);
            Button claim = CreateButton(
                "ClaimBtn", actions, primary, Color.white);
            SetCentered((RectTransform)claim.transform,
                new Vector2(0f, -790f), new Vector2(750f, 160f));
            Text claimText = CreateText(
                "Text", claim.transform, font, 72, "Continue",
                Color.white);
            Stretch(claimText.rectTransform, new Vector2(55f, 20f));

            Button play = CreateButton(
                "GoToPlayBtn", actions, primary, Color.white);
            SetCentered((RectTransform)play.transform,
                new Vector2(0f, -790f), new Vector2(750f, 160f));
            Text playText = CreateText(
                "Text", play.transform, font, 64, "Go to Play",
                Color.white);
            Stretch(playText.rectTransform, new Vector2(55f, 20f));

            Button tapSurface = CreateButton(
                "LitTapSurface", root, null, Color.clear);
            Stretch((RectTransform)tapSurface.transform);
            tapSurface.transform.SetAsFirstSibling();

            StreakPagePresenter presenter =
                page.GetComponent<StreakPagePresenter>();
            ConfigureWindow(presenter, page, UiLayer.Default, true, false);
            SerializedObject data = new(presenter);
            SetRef(data, "titleText", title);
            SetRef(data, "streakText", number);
            SetRef(data, "streakTextNew", numberNew);
            SetRef(data, "currentText", current);
            SetRef(data, "bestText", best);
            SetRef(data, "tapSunText", tapText);
            SetRef(data, "continueText", claimText);
            SetRef(data, "goToPlayText", playText);
            SetRef(data, "sunRoot", sunRoot);
            SetRef(data, "sunButton", sunButton);
            SetRef(data, "tapSurface", tapSurface);
            SetRef(data, "backButton", back);
            SetRef(data, "continueButton", claim);
            SetRef(data, "goToPlayButton", play);
            SetRef(data, "localization", localization);
            SetArray(data, "slots", slotViews);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static StreakDaySlotView BuildDaySlot(
            RectTransform root,
            Font font,
            LocalizationCatalog localization,
            Sprite dot,
            Sprite chest,
            Sprite checkBar,
            Shader roundedShader)
        {
            StreakDaySlotView view =
                root.gameObject.AddComponent<StreakDaySlotView>();
            Text weekday = CreateText(
                "WeekLabel", root, font, 34, "WED", Brown);
            SetCentered(weekday.rectTransform, new Vector2(0f, 78f),
                new Vector2(120f, 44f));

            Image uncheckedDot = CreateImage(
                "UncheckedDot", root, null);
            SetCentered(uncheckedDot.rectTransform,
                new Vector2(0f, -40f), new Vector2(120f, 120f));
            uncheckedDot.color = new Color(0.886f, 0.835f, 0.769f, 1f);
            ConfigureRounded(uncheckedDot, roundedShader, 60f);
            CanvasGroup uncheckedCanvas =
                uncheckedDot.gameObject.AddComponent<CanvasGroup>();

            Image checkedDot = CreateImage(
                "CheckedDot", root, dot);
            SetCentered(checkedDot.rectTransform,
                new Vector2(0f, -44f), new Vector2(148f, 148f));
            checkedDot.preserveAspect = true;
            CanvasGroup checkedCanvas =
                checkedDot.gameObject.AddComponent<CanvasGroup>();

            Image checkShort = CreateImage(
                "CheckShort", checkedDot.transform, checkBar);
            SetCentered(checkShort.rectTransform,
                new Vector2(-20f, -7f), new Vector2(58f, 20f));
            checkShort.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, -45f);
            checkShort.color = Color.white;
            checkShort.preserveAspect = false;

            Image checkLong = CreateImage(
                "CheckLong", checkedDot.transform, checkBar);
            SetCentered(checkLong.rectTransform,
                new Vector2(17f, 5f), new Vector2(88f, 20f));
            checkLong.rectTransform.localEulerAngles =
                new Vector3(0f, 0f, 45f);
            checkLong.color = Color.white;
            checkLong.preserveAspect = false;

            Image chestImage = CreateImage("Chest", root, chest);
            SetCentered(chestImage.rectTransform,
                new Vector2(0f, -42f), new Vector2(110f, 120f));
            chestImage.preserveAspect = true;
            CanvasGroup chestCanvas =
                chestImage.gameObject.AddComponent<CanvasGroup>();

            SerializedObject data = new(view);
            SetRef(data, "weekdayText", weekday);
            SetRef(data, "uncheckedDot", uncheckedDot.gameObject);
            SetRef(data, "checkedDot", checkedDot.gameObject);
            SetRef(data, "chest", chestImage.gameObject);
            SetRef(data, "uncheckedCanvas", uncheckedCanvas);
            SetRef(data, "checkedCanvas", checkedCanvas);
            SetRef(data, "chestCanvas", chestCanvas);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static GameObject BuildRevive(
            string name,
            Font font,
            LocalizationCatalog localization,
            Sprite background,
            Sprite sun,
            Sprite arrow,
            Sprite primary,
            Sprite normal)
        {
            GameObject page =
                CreatePage<StreakRevivePagePresenter>(name);
            Image overlay = CreateImage(
                "Overlay", page.transform, null);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform content = CreateRect("Content", page.transform);
            SetCentered(content, Vector2.zero, new Vector2(900f, 1378f));
            Image frame = CreateImage("FrameBg", content, background);
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;

            Text title = CreateText(
                "PopupTitle", content, font, 68,
                "Daily Streak", Brown);
            SetTop(title.rectTransform, 16f, 100f, 760f);
            Button close = CreateButton(
                "CloseBtn", content, null, Color.clear);
            SetAnchored((RectTransform)close.transform,
                new Vector2(1f, 1f), new Vector2(-70f, -70f),
                new Vector2(100f, 100f));
            Text closeText = CreateText(
                "Text", close.transform, font, 54, "×", Brown);
            Stretch(closeText.rectTransform);

            RectTransform suns = CreateRect("SunGroup", content);
            SetTop(suns, 190f, 260f, 540f);
            Image fromSun = CreateImage("SunFrom", suns, sun);
            SetCentered(fromSun.rectTransform,
                new Vector2(-174f, 35f), new Vector2(191f, 191f));
            fromSun.preserveAspect = true;
            Text from = CreateText(
                "SunFromNum", suns, font, 48, "0", Brown);
            SetCentered(from.rectTransform,
                new Vector2(-174f, -105f), new Vector2(191f, 55f));
            Image arrowImage = CreateImage("Arrow", suns, arrow);
            SetCentered(arrowImage.rectTransform,
                new Vector2(0f, 40f), new Vector2(99f, 40f));
            arrowImage.preserveAspect = true;
            Image toSun = CreateImage("SunTo", suns, sun);
            SetCentered(toSun.rectTransform,
                new Vector2(174f, 35f), new Vector2(191f, 191f));
            toSun.preserveAspect = true;
            Text to = CreateText(
                "SunToNum", suns, font, 48, "0", Brown);
            SetCentered(to.rectTransform,
                new Vector2(174f, -105f), new Vector2(191f, 55f));

            Image infoBg = CreateImage("Info", content, null);
            SetTop(infoBg.rectTransform, 488f, 380f, 800f);
            infoBg.color = Cream;
            Text info = CreateText(
                "InfoText", infoBg.transform, font, 48,
                "Daily Streak interrupted!", Brown);
            Stretch(info.rectTransform, new Vector2(25f, 36f));

            Button restore = CreateButton(
                "RestoreBtn", content, primary, Color.white);
            SetTop((RectTransform)restore.transform,
                859f, 158f, 784f);
            Text restoreText = CreateText(
                "Text", restore.transform, font, 60,
                "Restore", Color.white);
            Stretch(restoreText.rectTransform, new Vector2(40f, 18f));

            Button giveUp = CreateButton(
                "GiveUpBtn", content, normal, Color.white);
            SetTop((RectTransform)giveUp.transform,
                1059f, 158f, 784f);
            Text giveUpText = CreateText(
                "Text", giveUp.transform, font, 54,
                "Give up", Brown);
            Stretch(giveUpText.rectTransform, new Vector2(40f, 18f));

            StreakRevivePagePresenter presenter =
                page.GetComponent<StreakRevivePagePresenter>();
            ConfigureWindow(presenter, page, UiLayer.Popup, false, true);
            SerializedObject data = new(presenter);
            SetRef(data, "infoText", info);
            SetRef(data, "fromStreakText", from);
            SetRef(data, "toStreakText", to);
            SetRef(data, "restoreText", restoreText);
            SetRef(data, "giveUpText", giveUpText);
            SetRef(data, "restoreButton", restore);
            SetRef(data, "giveUpButton", giveUp);
            SetRef(data, "actionCloseButton", close);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject BuildAward(
            Font font,
            LocalizationCatalog localization,
            Sprite background,
            Sprite primary,
            Sprite toolBackground,
            Sprite locate,
            Sprite hint)
        {
            GameObject page =
                CreatePage<AwardPagePresenter>("AwardPage");
            Image overlay = CreateImage(
                "Overlay", page.transform, null);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform panel = CreateRect("AwardPanel", page.transform);
            SetCentered(panel, Vector2.zero, new Vector2(1080f, 1600f));
            Image panelBg = CreateImage("Background", panel, background);
            Stretch(panelBg.rectTransform);
            panelBg.type = Image.Type.Sliced;

            Text title = CreateText(
                "Title", panel, font, 76, "Get it", Brown);
            SetCentered(title.rectTransform, new Vector2(0f, 510f),
                new Vector2(800f, 110f));

            RectTransform content = CreateRect("AwardContent", panel);
            SetCentered(content, new Vector2(0f, 180f),
                new Vector2(1080f, 430f));
            var views = new AwardItemView[3];
            float[] xs = { -198f, 198f, 0f };
            float[] ys = { 0f, 0f, -330f };
            for (int index = 0; index < views.Length; index++)
            {
                RectTransform slot = CreateRect(
                    "CellSlot" + (index + 1), content);
                SetCentered(slot, new Vector2(xs[index], ys[index]),
                    new Vector2(196f, 322f));
                views[index] = BuildAwardItem(
                    slot, font, toolBackground, locate, hint);
            }

            Button collect = CreateButton(
                "CollectBtn", panel, primary, Color.white);
            SetCentered((RectTransform)collect.transform,
                new Vector2(0f, -570f), new Vector2(750f, 160f));
            Text collectText = CreateText(
                "Text", collect.transform, font, 64,
                "Collect", Color.white);
            Stretch(collectText.rectTransform, new Vector2(45f, 18f));

            Button doubleCollect = CreateButton(
                "DoubleCollectBtn", panel, primary, Color.white);
            SetCentered((RectTransform)doubleCollect.transform,
                new Vector2(0f, -370f), new Vector2(750f, 160f));
            doubleCollect.gameObject.SetActive(false);

            AwardPagePresenter presenter =
                page.GetComponent<AwardPagePresenter>();
            ConfigureWindow(presenter, page, UiLayer.Popup, false, true);
            SerializedObject data = new(presenter);
            SetRef(data, "titleText", title);
            SetRef(data, "collectText", collectText);
            SetRef(data, "collectButton", collect);
            SetRef(data, "doubleCollectButton", doubleCollect);
            SetRef(data, "localization", localization);
            SetArray(data, "itemViews", views);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static AwardItemView BuildAwardItem(
            RectTransform root,
            Font font,
            Sprite background,
            Sprite locate,
            Sprite hint)
        {
            AwardItemView view =
                root.gameObject.AddComponent<AwardItemView>();
            Image bg = CreateImage("Frame", root, background);
            SetCentered(bg.rectTransform, new Vector2(0f, 43f),
                new Vector2(236f, 236f));
            Image icon = CreateImage("IconImg", root, locate);
            SetCentered(icon.rectTransform, new Vector2(0f, 43f),
                new Vector2(106f, 106f));
            icon.preserveAspect = true;
            Text count = CreateText(
                "CountTxt", root, font, 86, "1",
                new Color(1f, 0.916f, 0.44f, 1f));
            SetCentered(count.rectTransform, new Vector2(0f, -137f),
                new Vector2(120f, 90f));

            SerializedObject data = new(view);
            SetRef(data, "background", bg);
            SetRef(data, "icon", icon);
            SetRef(data, "countText", count);
            SetRef(data, "locateIcon", locate);
            SetRef(data, "hintIcon", hint);
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static GameObject BuildAbSwitchPopup(
            Font font,
            LocalizationCatalog localization,
            Sprite dialogFrame,
            Sprite cat,
            Sprite primary,
            Sprite toolBackground,
            Sprite locate,
            Sprite hint)
        {
            GameObject page =
                CreatePage<AbSwitchPopupPresenter>("AbSwitchPopup");
            Image overlay = CreateImage(
                "Overlay", page.transform, null);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform content = CreateRect("Content", page.transform);
            Stretch(content);
            Image catImage = CreateImage("CatDialog", content, cat);
            SetCentered(catImage.rectTransform,
                new Vector2(0f, 610f), new Vector2(585f, 407f));
            catImage.preserveAspect = true;

            RectTransform panel = CreateRect("CenterContainer", content);
            SetCentered(panel, new Vector2(0f, -120f),
                new Vector2(900f, 1250f));
            Image frame = CreateImage("Bg", panel, dialogFrame);
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;

            Text title = CreateText(
                "Title", panel, font, 70, "Major Update", Brown);
            SetTop(title.rectTransform, 24f, 110f, 650f);
            Button close = CreateButton(
                "CloseBtn", panel, null, Color.clear);
            SetAnchored((RectTransform)close.transform,
                new Vector2(1f, 1f), new Vector2(-70f, -70f),
                new Vector2(100f, 100f));
            Text closeText = CreateText(
                "Text", close.transform, font, 54, "×", Brown);
            Stretch(closeText.rectTransform);

            RectTransform toolGroup = CreateRect("ToolGroup", panel);
            SetCentered(toolGroup, new Vector2(0f, 340f),
                new Vector2(520f, 260f));
            BuildPopupReward(
                "RevealReward",
                toolGroup,
                font,
                toolBackground,
                locate,
                new Vector2(-130f, 0f),
                out GameObject locateReward,
                out Text locateCount);
            BuildPopupReward(
                "HintReward",
                toolGroup,
                font,
                toolBackground,
                hint,
                new Vector2(130f, 0f),
                out GameObject hintReward,
                out Text hintCount);

            Text body = CreateText(
                "PopUpText", panel, font, 70,
                "Good news! Daily Streak has been upgraded.",
                Brown);
            SetCentered(body.rectTransform, new Vector2(0f, 30f),
                new Vector2(800f, 560f));
            body.resizeTextMinSize = 40;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;

            Button action = CreateButton(
                "ActionBtn", panel, primary, Color.white);
            SetCentered((RectTransform)action.transform,
                new Vector2(0f, -420f), new Vector2(750f, 160f));
            Text actionText = CreateText(
                "Text", action.transform, font, 64,
                "Get it", Color.white);
            Stretch(actionText.rectTransform, new Vector2(45f, 18f));

            Button feedback = CreateButton(
                "FeedbackBtn", panel, null, Color.clear);
            SetCentered((RectTransform)feedback.transform,
                new Vector2(0f, -545f), new Vector2(500f, 80f));
            Text feedbackText = CreateText(
                "Text", feedback.transform, font, 42,
                "Feedback", Brown);
            Stretch(feedbackText.rectTransform);

            AbSwitchPopupPresenter presenter =
                page.GetComponent<AbSwitchPopupPresenter>();
            ConfigureWindow(presenter, page, UiLayer.Popup, false, true);
            SerializedObject data = new(presenter);
            SetRef(data, "titleText", title);
            SetRef(data, "bodyText", body);
            SetRef(data, "actionText", actionText);
            SetRef(data, "feedbackText", feedbackText);
            SetRef(data, "actionButton", action);
            SetRef(data, "actionCloseButton", close);
            SetRef(data, "feedbackButton", feedback);
            SetRef(data, "toolGroup", toolGroup.gameObject);
            SetRef(data, "locateReward", locateReward);
            SetRef(data, "locateCountText", locateCount);
            SetRef(data, "hintReward", hintReward);
            SetRef(data, "hintCountText", hintCount);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static void BuildPopupReward(
            string name,
            Transform parent,
            Font font,
            Sprite background,
            Sprite iconSprite,
            Vector2 position,
            out GameObject reward,
            out Text count)
        {
            RectTransform root = CreateRect(name, parent);
            SetCentered(root, position, new Vector2(220f, 260f));
            reward = root.gameObject;
            Image bg = CreateImage("Frame", root, background);
            SetCentered(bg.rectTransform, new Vector2(0f, 25f),
                new Vector2(220f, 220f));
            bg.preserveAspect = true;
            Image icon = CreateImage("Icon", root, iconSprite);
            SetCentered(icon.rectTransform, new Vector2(0f, 25f),
                new Vector2(106f, 106f));
            icon.preserveAspect = true;
            count = CreateText(
                "Count", root, font, 54, "x1", Orange);
            SetCentered(count.rectTransform, new Vector2(0f, -105f),
                new Vector2(180f, 65f));
        }

        private static void UpgradeHomeEntry(
            Font font,
            LocalizationCatalog localization,
            Sprite checkedSprite,
            Sprite uncheckedSprite,
            Sprite sunSprite,
            Sprite checkedBadgeSprite,
            Sprite miniBackground,
            Sprite miniGlow,
            Sprite miniSun,
            Sprite miniChecked,
            Sprite miniUnchecked,
            Sprite miniShine,
            Shader roundedShader,
            TextAsset dialogPriority,
            TextAsset abSwitchStrategy)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HomePrefabPath);
            if (prefab == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(
                HomePrefabPath);
            try
            {
                HomePagePresenter home =
                    root.GetComponent<HomePagePresenter>();
                Transform slot = root.transform.Find(
                    "Root/DailyStreakLayout/StreakEntrySlot");
                Transform miniSlot = root.transform.Find(
                    "Root/DailyStreakLayout/StreakSmallEntrySlot");
                if (home == null || slot is not RectTransform slotRect ||
                    miniSlot is not RectTransform miniSlotRect)
                    return;

                StreakEntryPresenter entry =
                    slot.GetComponentInChildren<StreakEntryPresenter>(true);
                bool changed = false;
                bool rebuildEntry = entry == null ||
                    entry.transform.Find("StateChecked/Sun") == null ||
                    entry.transform.Find("StateChecked/Checkmark") == null;
                if (rebuildEntry)
                {
                    if (entry != null)
                        UnityEngine.Object.DestroyImmediate(entry.gameObject);
                    entry = BuildHomeEntry(
                        slotRect,
                        font,
                        localization,
                        checkedSprite,
                        uncheckedSprite,
                        sunSprite,
                        checkedBadgeSprite,
                        roundedShader);
                    changed = entry != null;
                }

                StreakEntryPresenter miniEntry =
                    miniSlot.GetComponentInChildren<StreakEntryPresenter>(
                        true);
                bool rebuildMiniEntry = miniEntry == null ||
                    miniEntry.transform.Find("Panel") == null ||
                    miniEntry.transform.Find("Shadow") == null ||
                    miniEntry.transform.Find("AmbientVfx/Shine") == null;
                if (rebuildMiniEntry)
                {
                    if (miniEntry != null)
                        UnityEngine.Object.DestroyImmediate(
                            miniEntry.gameObject);
                    miniEntry = BuildHomeMiniEntry(
                        miniSlotRect,
                        font,
                        localization,
                        miniBackground,
                        miniGlow,
                        miniSun,
                        miniChecked,
                        miniUnchecked,
                        miniShine,
                        roundedShader);
                    changed |= miniEntry != null;
                }

                SerializedObject homeData = new(home);
                SetRef(homeData, "dialogPriorityConfig", dialogPriority);
                SetRef(homeData, "abSwitchPopupConfig", abSwitchStrategy);
                SerializedProperty entryProperty =
                    homeData.FindProperty("streakEntry");
                if (entryProperty != null &&
                    entryProperty.objectReferenceValue != entry)
                {
                    entryProperty.objectReferenceValue = entry;
                    changed = true;
                }
                SerializedProperty miniEntryProperty =
                    homeData.FindProperty("streakMiniEntry");
                if (miniEntryProperty != null &&
                    miniEntryProperty.objectReferenceValue != miniEntry)
                {
                    miniEntryProperty.objectReferenceValue = miniEntry;
                    changed = true;
                }
                changed |= homeData.ApplyModifiedPropertiesWithoutUndo();

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, HomePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static StreakEntryPresenter BuildHomeEntry(
            RectTransform parent,
            Font font,
            LocalizationCatalog localization,
            Sprite checkedSprite,
            Sprite uncheckedSprite,
            Sprite sunSprite,
            Sprite checkedBadgeSprite,
            Shader roundedShader)
        {
            RectTransform root = CreateRect("StreakEntryCell", parent);
            Stretch(root);
            StreakEntryPresenter presenter =
                root.gameObject.AddComponent<StreakEntryPresenter>();

            RectTransform checkedState = CreateRect("StateChecked", root);
            Stretch(checkedState);
            Image checkedBackground = CreateImage(
                "Background", checkedState, checkedSprite);
            Stretch(checkedBackground.rectTransform,
                new Vector2(20f, 20f));
            checkedBackground.preserveAspect = true;
            Image checkedSun = CreateImage(
                "Sun", checkedState, sunSprite);
            SetCentered(checkedSun.rectTransform,
                new Vector2(0f, 17f), new Vector2(297f, 297f));
            checkedSun.preserveAspect = true;
            Image checkedBadge = CreateImage(
                "Checkmark", checkedState, checkedBadgeSprite);
            SetCentered(checkedBadge.rectTransform,
                new Vector2(158f, 232f), new Vector2(104f, 86f));
            checkedBadge.preserveAspect = true;

            Image uncheckedImage = CreateImage(
                "StateUnchecked", root, uncheckedSprite);
            Stretch(uncheckedImage.rectTransform);
            uncheckedImage.preserveAspect = true;

            Text title = CreateText(
                "StreakTxt", root, font, 60, "Streak",
                new Color(0.77f, 0.46f, 0.076f, 1f));
            SetCentered(title.rectTransform,
                new Vector2(-99f, 226f), new Vector2(176f, 88f));

            Image badge = CreateImage("CountBadge", root, null);
            SetCentered(badge.rectTransform,
                new Vector2(0f, -205f), new Vector2(370f, 90f));
            badge.color = new Color(1f, 0.945f, 0.733f, 1f);
            ConfigureRounded(badge, roundedShader, 45f);
            Text count = CreateText(
                "CountTxt", badge.transform, font, 70, "0",
                new Color(0.676f, 0.387f, 0f, 1f));
            Stretch(count.rectTransform);

            Button click = CreateButton(
                "ClickBtn", root, null, Color.clear);
            Stretch((RectTransform)click.transform);

            SerializedObject data = new(presenter);
            SetRef(data, "checkedState", checkedState.gameObject);
            SetRef(data, "uncheckedState", uncheckedImage.gameObject);
            SetRef(data, "titleText", title);
            SetRef(data, "countText", count);
            SetRef(data, "clickButton", click);
            SetRef(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static StreakEntryPresenter BuildHomeMiniEntry(
            RectTransform parent,
            Font font,
            LocalizationCatalog localization,
            Sprite backgroundSprite,
            Sprite glowSprite,
            Sprite sunSprite,
            Sprite checkedSprite,
            Sprite uncheckedSprite,
            Sprite shineSprite,
            Shader roundedShader)
        {
            if (backgroundSprite == null || glowSprite == null ||
                sunSprite == null || checkedSprite == null ||
                uncheckedSprite == null || shineSprite == null)
                return null;

            RectTransform root = CreateRect("StreakMiniEntryCell", parent);
            Stretch(root);
            StreakEntryPresenter presenter =
                root.gameObject.AddComponent<StreakEntryPresenter>();

            Image shadow = CreateImage(
                "Shadow", root, backgroundSprite);
            Stretch(shadow.rectTransform, new Vector2(-18f, -18f));
            Image panel = CreateImage("Panel", root, null);
            Stretch(panel.rectTransform);
            panel.color = new Color(1f, 0.8318f, 0.4064f, 1f);
            ConfigureRounded(panel, roundedShader, 30f);

            RectTransform ambientVfx = CreateRect("AmbientVfx", root);
            Stretch(ambientVfx);
            Image glow = CreateImage("Glow", ambientVfx, glowSprite);
            SetCentered(glow.rectTransform,
                new Vector2(0f, 35f), new Vector2(213f, 212.5f));
            glow.preserveAspect = true;
            glow.raycastTarget = false;
            CanvasGroup glowGroup =
                glow.gameObject.AddComponent<CanvasGroup>();
            glowGroup.alpha = 0.35f;
            Image shine = CreateImage(
                "Shine", ambientVfx, shineSprite);
            SetCentered(shine.rectTransform,
                new Vector2(0f, 35f), new Vector2(220f, 220f));
            shine.preserveAspect = true;
            shine.raycastTarget = false;
            Color shineColor = shine.color;
            shineColor.a = 0.28f;
            shine.color = shineColor;

            RectTransform checkedState = CreateRect("CheckedState", root);
            Stretch(checkedState);
            Image sun = CreateImage("Sun", checkedState, sunSprite);
            SetCentered(sun.rectTransform,
                new Vector2(0f, 35f), new Vector2(163f, 163f));
            sun.preserveAspect = true;
            Image checkmark = CreateImage(
                "Checkmark", checkedState, checkedSprite);
            SetCentered(checkmark.rectTransform,
                new Vector2(158f, 84.5f), new Vector2(104f, 86f));
            checkmark.preserveAspect = true;

            RectTransform uncheckedState = CreateRect(
                "UncheckedState", root);
            Stretch(uncheckedState);
            Image uncheckedIcon = CreateImage(
                "Icon", uncheckedState, uncheckedSprite);
            SetCentered(uncheckedIcon.rectTransform,
                new Vector2(0f, 35f), new Vector2(163f, 164f));
            uncheckedIcon.preserveAspect = true;

            Image badge = CreateImage("CountBadge", root, null);
            SetCentered(badge.rectTransform,
                new Vector2(0f, -82.5f), new Vector2(300f, 56f));
            badge.color = new Color(1f, 0.945f, 0.733f, 1f);
            ConfigureRounded(badge, roundedShader, 28f);
            Text count = CreateText(
                "CountTxt", badge.transform, font, 46, "0",
                new Color(0.676f, 0.387f, 0f, 1f));
            Stretch(count.rectTransform);

            Button click = CreateButton(
                "ClickBtn", root, null, Color.clear);
            Stretch((RectTransform)click.transform);

            SerializedObject data = new(presenter);
            SetRef(data, "checkedState", checkedState.gameObject);
            SetRef(data, "uncheckedState", uncheckedState.gameObject);
            SetRef(data, "countText", count);
            SetRef(data, "clickButton", click);
            SetRef(data, "localization", localization);
            SetRef(data, "checkedSunVisual", sun.rectTransform);
            SetRef(data, "checkedShineVisual", shine.rectTransform);
            SetRef(data, "checkedGlowGroup", glowGroup);
            data.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static GameObject CreatePage<T>(string name)
            where T : UIFrameWindow
        {
            var page = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(T));
            RectTransform rect = (RectTransform)page.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return page;
        }

        private static void ConfigureWindow(
            UIFrameWindow presenter,
            GameObject page,
            UiLayer layer,
            bool fullscreen,
            bool mask)
        {
            SerializedObject data = new(presenter);
            SetRef(data, "rootCanvas", page.GetComponent<Canvas>());
            SetRef(data, "rootCanvasGroup", page.GetComponent<CanvasGroup>());
            SerializedProperty layerProperty = data.FindProperty("uiLayer");
            if (layerProperty != null)
                layerProperty.intValue = (int)layer;
            SerializedProperty fullscreenProperty =
                data.FindProperty("isFullscreen");
            if (fullscreenProperty != null)
                fullscreenProperty.boolValue = fullscreen;
            SerializedProperty maskProperty = data.FindProperty("showMask");
            if (maskProperty != null) maskProperty.boolValue = mask;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureStreakPresentationPrefab(
            string path,
            Func<GameObject> factory)
        {
            GameObject existing =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Transform content = existing != null
                ? existing.transform.Find("StreakContent")
                : null;
            Transform sun = content?.Find("Hero/SunRoot/SunImg");
            Transform best = content?.Find("Hero/BestFrame");
            Transform numberRoll = content?.Find("Hero/NumberRoll");
            Transform numberNew = content?.Find(
                "Hero/NumberRoll/StreakNumberNext");
            Transform backBase = content?.Find("Top/BackBtn/Base");
            Transform back = content?.Find("Top/BackBtn/Icon");
            Transform uncheckedDot = content?.Find(
                "WeekSlots/Day1/UncheckedDot");
            Transform checkShort = content?.Find(
                "WeekSlots/Day1/CheckedDot/CheckShort");
            Transform checkLong = content?.Find(
                "WeekSlots/Day1/CheckedDot/CheckLong");
            bool current = SpriteNameMatches(
                    sun?.GetComponent<Image>()?.sprite, "sun") &&
                SpriteNameMatches(
                    best?.GetComponent<Image>()?.sprite,
                    "sudoku_bg_round20") &&
                numberRoll?.GetComponent<RectMask2D>() != null &&
                numberNew?.GetComponent<Text>() != null &&
                SpriteNameMatches(
                    backBase?.GetComponent<Image>()?.sprite,
                    "normal_btn_bg") &&
                SpriteNameMatches(
                    back?.GetComponent<Image>()?.sprite, "vector_1") &&
                SpriteNameMatches(
                    checkShort?.GetComponent<Image>()?.sprite,
                    "et_mask_008") &&
                SpriteNameMatches(
                    checkLong?.GetComponent<Image>()?.sprite,
                    "et_mask_008") &&
                uncheckedDot?.GetComponent<RoundedImageView>() != null;
            if (current) return;

            GameObject root = factory();
            if (root == null) return;
            // Saving over the existing prefab preserves its .meta GUID and
            // every registry reference while replacing deterministic content.
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsurePrefab(
            string path,
            Func<GameObject> factory)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;
            GameObject root = factory();
            if (root == null) return;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureRounded(
            Image image,
            Shader shader,
            float radius)
        {
            RoundedImageView rounded =
                image.gameObject.AddComponent<RoundedImageView>();
            SerializedObject data = new(rounded);
            data.FindProperty("target").objectReferenceValue = image;
            data.FindProperty("roundedShader").objectReferenceValue = shader;
            data.FindProperty("cornerRadius").floatValue = radius;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool SpriteNameMatches(
            Sprite sprite,
            string expectedName)
        {
            if (sprite == null || string.IsNullOrEmpty(expectedName))
                return false;
            return string.Equals(
                       sprite.name,
                       expectedName,
                       StringComparison.Ordinal) ||
                   sprite.name.StartsWith(
                       expectedName + "_",
                       StringComparison.Ordinal);
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Math.Max(16, size / 2);
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Color color)
        {
            Image image = CreateImage(name, parent, sprite);
            image.color = color;
            image.raycastTarget = true;
            image.type = sprite != null ? Image.Type.Sliced :
                Image.Type.Simple;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
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

        private static void SetTop(
            RectTransform rect,
            float top,
            float height,
            float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                0f,
                -top - height * 0.5f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetAnchored(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 padding = default)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = padding;
            rect.offsetMax = -padding;
        }

        private static void SetRef(
            SerializedObject data,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property = data.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetArray<T>(
            SerializedObject data,
            string propertyName,
            T[] values)
            where T : UnityEngine.Object
        {
            SerializedProperty property = data.FindProperty(propertyName);
            if (property == null) return;
            property.arraySize = values?.Length ?? 0;
            for (int index = 0; index < property.arraySize; index++)
                property.GetArrayElementAtIndex(index)
                    .objectReferenceValue = values[index];
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
                if (assets[index] is Sprite candidate)
                    return candidate;
            return null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
