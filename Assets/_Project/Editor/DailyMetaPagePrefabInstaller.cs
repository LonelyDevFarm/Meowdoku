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
        private const string SunPath =
            "Assets/_Project/Sprites/daily_streak/sun.png";
        private const string DotPath =
            "Assets/_Project/Sprites/daily_streak/dot.png";
        private const string ChestPath =
            "Assets/_Project/Sprites/daily_streak/treasure_box.png";
        private const string EntryCheckedPath =
            "Assets/_Project/Sprites/daily_streak/state_checked1.png";
        private const string EntryUncheckedPath =
            "Assets/_Project/Sprites/daily_streak/state_unchecked.png";
        private const string ArrowPath =
            "Assets/_Project/Sprites/daily_streak/streak_arrow.png";
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
            Sprite sun = LoadSprite(SunPath);
            Sprite dot = LoadSprite(DotPath);
            Sprite chest = LoadSprite(ChestPath);
            Sprite checkedEntry = LoadSprite(EntryCheckedPath);
            Sprite uncheckedEntry = LoadSprite(EntryUncheckedPath);
            Sprite arrow = LoadSprite(ArrowPath);
            Sprite primary = LoadSprite(PrimaryButtonPath);
            Sprite normal = LoadSprite(NormalButtonPath);
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
                sun == null || dot == null || chest == null ||
                checkedEntry == null || uncheckedEntry == null ||
                arrow == null || primary == null || normal == null ||
                toolBg == null || locate == null || hint == null ||
                dialogFrame == null || streakCat == null ||
                dialogPriority == null || abSwitchStrategy == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            EnsurePrefab(
                StreakPrefabPath,
                () => BuildStreak(
                    font, localization, bg, sun, dot, chest, primary));
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
            Sprite primary)
        {
            GameObject page =
                CreatePage<StreakPagePresenter>("StreakPage");
            RectTransform root = CreateRect("StreakContent", page.transform);
            Stretch(root);
            Image bg = CreateImage("Background", root, background);
            Stretch(bg.rectTransform);
            bg.type = Image.Type.Sliced;

            Text title = CreateText(
                "Title", root, font, 72, "Daily Streak", Brown);
            SetCentered(title.rectTransform, new Vector2(0f, 920f),
                new Vector2(800f, 100f));

            Button back = CreateButton(
                "BackBtn", root, null, new Color(1f, 1f, 1f, 0.9f));
            SetCentered((RectTransform)back.transform,
                new Vector2(-450f, 920f), new Vector2(100f, 100f));
            Text backText = CreateText(
                "Text", back.transform, font, 64, "‹", Brown);
            Stretch(backText.rectTransform);

            GameObject sunRoot = CreateRect(
                "SunRoot", root).gameObject;
            SetCentered((RectTransform)sunRoot.transform,
                new Vector2(0f, 450f), new Vector2(512f, 512f));
            Image sunImage = CreateImage(
                "SunImg", sunRoot.transform, sun);
            Stretch(sunImage.rectTransform);
            sunImage.preserveAspect = true;
            Button sunButton = sunRoot.AddComponent<Button>();
            sunButton.targetGraphic = sunImage;

            Text number = CreateText(
                "StreakNumber", root, font, 200, "0", Orange);
            SetCentered(number.rectTransform, new Vector2(0f, 35f),
                new Vector2(1000f, 300f));
            Text current = CreateText(
                "CurrentStreak", root, font, 70,
                "Current Streak", Brown);
            SetCentered(current.rectTransform, new Vector2(0f, -120f),
                new Vector2(720f, 90f));
            Image bestFrame = CreateImage(
                "BestFrame", root, background);
            SetCentered(bestFrame.rectTransform, new Vector2(0f, -225f),
                new Vector2(470f, 80f));
            bestFrame.type = Image.Type.Sliced;
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
                    slot, font, localization, dot, chest);
            }

            Text tapText = CreateText(
                "TapSunText", root, font, 54,
                "Tap the sun, spark your streak!", Brown);
            SetCentered(tapText.rectTransform, new Vector2(0f, -650f),
                new Vector2(900f, 130f));

            Button claim = CreateButton(
                "ClaimBtn", root, primary, Color.white);
            SetCentered((RectTransform)claim.transform,
                new Vector2(0f, -790f), new Vector2(750f, 160f));
            Text claimText = CreateText(
                "Text", claim.transform, font, 72, "Continue",
                Color.white);
            Stretch(claimText.rectTransform, new Vector2(55f, 20f));

            Button play = CreateButton(
                "GoToPlayBtn", root, primary, Color.white);
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
            Sprite chest)
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

            Image checkedDot = CreateImage(
                "CheckedDot", root, dot);
            SetCentered(checkedDot.rectTransform,
                new Vector2(0f, -44f), new Vector2(148f, 148f));
            checkedDot.preserveAspect = true;

            Image chestImage = CreateImage("Chest", root, chest);
            SetCentered(chestImage.rectTransform,
                new Vector2(0f, -42f), new Vector2(110f, 120f));
            chestImage.preserveAspect = true;

            SerializedObject data = new(view);
            SetRef(data, "weekdayText", weekday);
            SetRef(data, "uncheckedDot", uncheckedDot.gameObject);
            SetRef(data, "checkedDot", checkedDot.gameObject);
            SetRef(data, "chest", chestImage.gameObject);
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
                if (home == null || slot is not RectTransform slotRect)
                    return;

                StreakEntryPresenter entry =
                    slot.GetComponentInChildren<StreakEntryPresenter>(true);
                bool changed = false;
                if (entry == null)
                {
                    entry = BuildHomeEntry(
                        slotRect,
                        font,
                        localization,
                        checkedSprite,
                        uncheckedSprite);
                    changed = entry != null;
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
            Sprite uncheckedSprite)
        {
            RectTransform root = CreateRect("StreakEntryCell", parent);
            Stretch(root);
            StreakEntryPresenter presenter =
                root.gameObject.AddComponent<StreakEntryPresenter>();

            Image checkedImage = CreateImage(
                "StateChecked", root, checkedSprite);
            Stretch(checkedImage.rectTransform, new Vector2(20f, 20f));
            checkedImage.preserveAspect = true;
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
            Text count = CreateText(
                "CountTxt", badge.transform, font, 70, "0",
                new Color(0.676f, 0.387f, 0f, 1f));
            Stretch(count.rectTransform);

            Button click = CreateButton(
                "ClickBtn", root, null, Color.clear);
            Stretch((RectTransform)click.transform);

            SerializedObject data = new(presenter);
            SetRef(data, "checkedState", checkedImage.gameObject);
            SetRef(data, "uncheckedState", uncheckedImage.gameObject);
            SetRef(data, "titleText", title);
            SetRef(data, "countText", count);
            SetRef(data, "clickButton", click);
            SetRef(data, "localization", localization);
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
