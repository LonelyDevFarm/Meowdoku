using System;
using System.IO;
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class ProfilePagePrefabInstaller
    {
        internal const string PagePath =
            "Assets/_Project/Prefabs/UI/ProfilePage.prefab";
        private const string AvatarPath =
            "Assets/_Project/Prefabs/UI/ProfileAvatarView.prefab";
        private const string CellPath =
            "Assets/_Project/Prefabs/UI/ProfileSelectionCell.prefab";
        private const string SpriteRoot = "Assets/_Project/Sprites/profile/";
        private const string CommonRoot = "Assets/_Project/Sprites/common/";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";

        static ProfilePagePrefabInstaller()
        {
            EditorApplication.delayCall += QueueInstall;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += QueueInstall;
            };
        }

        [MenuItem("Meowdoku/Port/Install Profile Page")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PagePath);
        }

        internal static GameObject InstallIfReady()
        {
            if (!CanEdit())
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.delayCall += QueueInstall;
                return null;
            }
            if (LoadSprite(SpriteRoot + "avatars/head_0000.png") == null ||
                AssetDatabase.LoadAssetAtPath<Font>(FontPath) == null)
                return null;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            GameObject avatar =
                AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath) ??
                BuildAvatarPrefab();
            GameObject cell =
                AssetDatabase.LoadAssetAtPath<GameObject>(CellPath) ??
                BuildCellPrefab(avatar);
            GameObject page =
                AssetDatabase.LoadAssetAtPath<GameObject>(PagePath) ??
                BuildPagePrefab(avatar, cell);
            if (page != null) UIRegistryAssetInstaller.InstallIfReady();
            return page;
        }

        private static void QueueInstall()
        {
            InstallIfReady();
        }

        private static GameObject BuildAvatarPrefab()
        {
            var root = new GameObject(
                "ProfileAvatarView",
                typeof(RectTransform),
                typeof(ProfileAvatarView));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(185f, 185f);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);

            Image baseImage = CreateImage(
                "Base",
                rootRect,
                LoadSprite(SpriteRoot + "avatars/head_0000.png"),
                Color.white);
            SetTopLeft(baseImage.rectTransform, 15f, 15f, 155f, 155f);
            baseImage.preserveAspect = true;
            baseImage.gameObject.AddComponent<RoundedImageView>()
                .Configure(baseImage, rounded, 20f);

            Image avatar = CreateImage(
                "Avatar",
                baseImage.rectTransform,
                LoadSprite(SpriteRoot + "avatars/head_0001.png"),
                Color.white);
            Stretch(avatar.rectTransform);
            avatar.preserveAspect = true;
            avatar.gameObject.AddComponent<RoundedImageView>()
                .Configure(avatar, rounded, 20f);

            Image frame = CreateImage(
                "Frame",
                rootRect,
                LoadSprite(SpriteRoot + "frames/frame_0001.png"),
                Color.white);
            Stretch(frame.rectTransform);
            frame.preserveAspect = true;

            RectTransform badge = CreateRect("CountBadge", frame.rectTransform);
            SetTopLeft(badge, 0f, 29f, 40f, 30f);
            Text count = CreateText("Count", badge, 20, "99", Color.white);
            Stretch(count.rectTransform);
            count.alignment = TextAnchor.MiddleCenter;

            Image redDot = CreateImage(
                "RedDot",
                rootRect,
                LoadSprite(CommonRoot + "red_dot.png"),
                Color.white);
            SetTopLeft(redDot.rectTransform, 149f, 4f, 42f, 42f);
            redDot.preserveAspect = true;
            redDot.gameObject.SetActive(false);

            var avatarSprites = new Sprite[8];
            var frameSprites = new Sprite[9];
            for (int index = 0; index < 8; index++)
            {
                avatarSprites[index] = LoadSprite(
                    SpriteRoot + $"avatars/head_{index + 1:0000}.png");
                frameSprites[index] = LoadSprite(
                    SpriteRoot + $"frames/frame_{index + 1:0000}.png");
            }
            frameSprites[8] = LoadSprite(
                SpriteRoot + "frames/frame_0100.png");

            SerializedObject data = new(root.GetComponent<ProfileAvatarView>());
            SetRef(data, "baseRoot", baseImage.gameObject);
            SetRef(data, "avatarImage", avatar);
            SetRef(data, "frameImage", frame);
            SetRef(data, "countBadge", badge.gameObject);
            SetRef(data, "countText", count);
            SetRef(data, "redDot", redDot.gameObject);
            SetSpriteArray(data, "avatarSprites", avatarSprites);
            SetSpriteArray(data, "frameSprites", frameSprites);
            data.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, AvatarPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        private static GameObject BuildCellPrefab(GameObject avatarPrefab)
        {
            if (avatarPrefab == null) return null;
            var root = new GameObject(
                "ProfileSelectionCell",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image),
                typeof(Button),
                typeof(ProfileSelectionCell));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(185f, 185f);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = 185f;
            layout.preferredHeight = 185f;
            Image hit = root.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.001f);

            GameObject avatar = (GameObject)PrefabUtility.InstantiatePrefab(
                avatarPrefab);
            avatar.name = "AvatarSlot";
            avatar.transform.SetParent(rootRect, false);
            Stretch((RectTransform)avatar.transform);

            Image lockImage = CreateImage(
                "Lock",
                rootRect,
                LoadSprite(SpriteRoot + "lock.png"),
                Color.white);
            SetTopLeft(lockImage.rectTransform, 69f, 63.5f, 47f, 58f);
            lockImage.preserveAspect = true;
            lockImage.gameObject.SetActive(false);

            RectTransform check = CreateRect("Check", rootRect);
            SetTopLeft(check, 120f, 120f, 58f, 58f);
            Image circle = check.gameObject.AddComponent<Image>();
            circle.color = new Color(0.007843f, 0.745098f, 0.321569f, 1f);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            check.gameObject.AddComponent<RoundedImageView>()
                .Configure(circle, rounded, 29f);
            Image tick = CreateImage(
                "CheckMark",
                check,
                LoadSprite(SpriteRoot + "select_check.png"),
                Color.white);
            SetTopLeft(tick.rectTransform, 9f, 16f, 40f, 29f);
            tick.preserveAspect = true;
            check.gameObject.SetActive(false);

            SerializedObject data = new(root.GetComponent<ProfileSelectionCell>());
            SetRef(data, "avatarView", avatar.GetComponent<ProfileAvatarView>());
            SetRef(data, "check", check.gameObject);
            SetRef(data, "lockVisual", lockImage.rectTransform);
            SetRef(data, "clickButton", root.GetComponent<Button>());
            data.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CellPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        private static GameObject BuildPagePrefab(
            GameObject avatarPrefab,
            GameObject cellPrefab)
        {
            if (avatarPrefab == null || cellPrefab == null) return null;
            var page = new GameObject(
                "ProfilePage",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(ProfilePagePresenter),
                typeof(GenericPopupAnimator));
            Stretch((RectTransform)page.transform);
            Canvas canvas = page.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            RectTransform content = CreateRect("Content", page.transform);
            SetCentered(content, Vector2.zero, new Vector2(900f, 1253f));
            CanvasGroup contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);

            Image shadow = CreateImage(
                "Shadow",
                content,
                LoadSprite(SpriteRoot + "popup_bg.png"),
                Color.white);
            SetOffsets(shadow.rectTransform, -14f, 10f, 14f, -18f);
            shadow.type = Image.Type.Sliced;

            Image background = CreateImage(
                "Background",
                content,
                null,
                new Color(1f, 0.984314f, 0.968627f, 1f));
            Stretch(background.rectTransform);
            background.gameObject.AddComponent<RoundedImageView>()
                .Configure(background, rounded, 60f);

            RectTransform title = CreateRect("Title", content);
            SetTopLeft(title, 0f, 0f, 900f, 130f);
            Image titleBg = title.gameObject.AddComponent<Image>();
            titleBg.color = new Color(0.723077f, 0.508657f, 0.36571f, 1f);
            title.gameObject.AddComponent<RoundedImageView>()
                .Configure(titleBg, rounded, new Vector4(60f, 60f, 0f, 0f));
            Text titleText = CreateText(
                "PopupTitle",
                title,
                85,
                "Profile",
                Color.white);
            SetTopLeft(titleText.rectTransform, 140f, 16f, 620f, 100f);
            titleText.alignment = TextAnchor.MiddleCenter;
            Button close = CreateIconButton(
                "CloseBtn",
                title,
                780f,
                20f,
                100f,
                100f,
                LoadSprite(SpriteRoot + "close_icon.png"),
                new Vector2(56.7f, 56.7f));

            GameObject header = (GameObject)PrefabUtility.InstantiatePrefab(
                avatarPrefab);
            header.name = "HeaderAvatarSlot";
            header.transform.SetParent(content, false);
            SetTopLeft((RectTransform)header.transform, 35f, 185f, 185f, 185f);

            NicknameRefs nickname = CreateNickname(content);
            TabRefs tabs = CreateTabs(content, rounded);
            GridRefs grids = CreateGrids(content, rounded);
            ButtonRefs confirm = CreateConfirm(content, rounded);
            TipRefs tip = CreateLockTip(content, rounded);

            ProfilePagePresenter presenter =
                page.GetComponent<ProfilePagePresenter>();
            SerializedObject data = new(presenter);
            SetRef(data, "rootCanvas", canvas);
            SetRef(data, "rootCanvasGroup", pageGroup);
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Popup;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = true;
            SetRef(data, "content", content);
            SetRef(data, "popupAnimator", page.GetComponent<GenericPopupAnimator>());
            SetRef(data, "actionCloseButton", close);
            SetRef(data, "confirmButton", confirm.Button);
            SetRef(data, "titleText", titleText);
            SetRef(data, "confirmText", confirm.Text);
            SetRef(data, "headerAvatar", header.GetComponent<ProfileAvatarView>());
            SetRef(data, "nicknameInput", nickname.Input);
            SetRef(data, "nicknameClickButton", nickname.Click);
            SetRef(data, "renameButton", nickname.Rename);
            SetRef(data, "avatarTabButton", tabs.AvatarButton);
            SetRef(data, "frameTabButton", tabs.FrameButton);
            SetRef(data, "avatarActive", tabs.AvatarActive);
            SetRef(data, "frameActive", tabs.FrameActive);
            SetRef(data, "frameRedDot", tabs.RedDot);
            SetRef(data, "gridBackground", grids.Background);
            SetRef(data, "avatarTabText", tabs.AvatarText);
            SetRef(data, "frameTabText", tabs.FrameText);
            SetRef(data, "scroll", grids.Scroll);
            SetRef(data, "avatarGrid", grids.AvatarGrid);
            SetRef(data, "leaderboardDivider", grids.LeaderboardDivider);
            SetRef(data, "leaderboardTitle", grids.LeaderboardTitle);
            SetRef(data, "leaderboardGrid", grids.LeaderboardGrid);
            SetRef(data, "classicDivider", grids.ClassicDivider);
            SetRef(data, "classicTitle", grids.ClassicTitle);
            SetRef(data, "classicGrid", grids.ClassicGrid);
            SetRef(data, "cellPrefab", cellPrefab.GetComponent<ProfileSelectionCell>());
            SetRef(data, "lockTipRoot", tip.Root);
            SetRef(data, "lockTipBubble", tip.Bubble);
            SetRef(data, "lockTipGroup", tip.Group);
            SetRef(data, "lockTipText", tip.Text);
            SetRef(data, "lockTipDismissButton", tip.Dismiss);
            SetRef(data, "lockTipGoButton", tip.GoButton);
            SetRef(data, "lockTipGoRoot", tip.GoRoot);
            SetRef(data, "lockTipGoText", tip.GoText);
            SetRef(data, "localization",
                LocalizationCatalogAssetInstaller.GetOrCreate());
            data.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject animatorData = new(
                page.GetComponent<GenericPopupAnimator>());
            SetRef(animatorData, "content", content);
            SetRef(animatorData, "contentGroup", contentGroup);
            animatorData.ApplyModifiedPropertiesWithoutUndo();

            page.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(page, PagePath);
            UnityEngine.Object.DestroyImmediate(page);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        private static NicknameRefs CreateNickname(RectTransform parent)
        {
            RectTransform root = CreateRect("Nickname", parent);
            SetTopLeft(root, 240f, 218f, 610f, 120f);
            Image background = CreateImage(
                "NicknameBg",
                root,
                LoadSprite(SpriteRoot + "nickname_bg.png"),
                Color.white);
            Stretch(background.rectTransform);
            background.type = Image.Type.Sliced;

            RectTransform inputRoot = CreateRect("NameEdit", root);
            SetTopLeft(inputRoot, 25f, 11f, 440f, 100f);
            Image inputImage = inputRoot.gameObject.AddComponent<Image>();
            inputImage.color = new Color(1f, 1f, 1f, 0.001f);
            InputField input = inputRoot.gameObject.AddComponent<InputField>();
            Text text = CreateText(
                "Text",
                inputRoot,
                60,
                "Player name!",
                new Color(0.576923f, 0.352256f, 0.352256f, 1f));
            Stretch(text.rectTransform);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            input.textComponent = text;
            // Godot counts Unicode code points. The presenter enforces 12
            // without splitting surrogate pairs; InputField's UTF-16 limit
            // must stay disabled.
            input.characterLimit = 0;
            input.readOnly = true;
            input.lineType = InputField.LineType.SingleLine;

            Button click = CreateButton("ReadOnlyClick", root, Color.clear);
            SetTopLeft((RectTransform)click.transform, 25f, 11f, 440f, 100f);

            Button rename = CreateButton("RenameBtn", root, Color.white);
            SetTopLeft((RectTransform)rename.transform, 490f, 0f, 120f, 120f);
            Image renameBg = rename.GetComponent<Image>();
            renameBg.sprite = LoadSprite(SpriteRoot + "rename_btn_bg.png");
            renameBg.type = Image.Type.Sliced;
            Image icon = CreateImage(
                "Icon",
                rename.transform,
                LoadSprite(SpriteRoot + "rename_icon.png"),
                Color.white);
            SetTopLeft(icon.rectTransform, 33f, 26f, 54f, 68f);
            icon.preserveAspect = true;
            return new NicknameRefs { Input = input, Click = click, Rename = rename };
        }

        private static TabRefs CreateTabs(RectTransform parent, Shader rounded)
        {
            RectTransform root = CreateRect("TabGroup", parent);
            SetTopLeft(root, 50f, 425f, 800f, 270f);
            TabPiece avatar = CreateTab(root, "AvatarTab", 0f, "Avatar", rounded);
            TabPiece frame = CreateTab(root, "FrameTab", 405f, "Frame", rounded);
            Image redDot = CreateImage(
                "RedDot",
                frame.Root,
                LoadSprite(CommonRoot + "red_dot.png"),
                Color.white);
            SetTopLeft(redDot.rectTransform, 357f, -17.5f, 56f, 56f);
            redDot.preserveAspect = true;
            return new TabRefs
            {
                AvatarButton = avatar.Button,
                FrameButton = frame.Button,
                AvatarActive = avatar.Active,
                FrameActive = frame.Active,
                AvatarText = avatar.Text,
                FrameText = frame.Text,
                RedDot = redDot.gameObject
            };
        }

        private static TabPiece CreateTab(
            RectTransform parent,
            string name,
            float x,
            string label,
            Shader rounded)
        {
            RectTransform root = CreateRect(name, parent);
            SetTopLeft(root, x, 0f, 395f, 270f);
            Image inactive = root.gameObject.AddComponent<Image>();
            inactive.sprite = LoadSprite(SpriteRoot + "tab_inactive_bg.png");
            inactive.type = Image.Type.Sliced;
            Text text = CreateText("Label", root, 60, label,
                new Color(0.576f, 0.353f, 0.353f, 1f));
            SetTopLeft(text.rectTransform, 80f, 7f, 235f, 86f);
            text.alignment = TextAnchor.MiddleCenter;

            RectTransform active = CreateRect("Active", root);
            SetTopLeft(active, 0f, 0f, 395f, 100f);
            Image activeImage = active.gameObject.AddComponent<Image>();
            activeImage.color = new Color(0.723077f, 0.508657f, 0.36571f, 1f);
            active.gameObject.AddComponent<RoundedImageView>()
                .Configure(activeImage, rounded, new Vector4(30f, 30f, 0f, 0f));
            Text activeText = CreateText(
                "ActiveLabel",
                active,
                60,
                label,
                Color.white);
            Stretch(activeText.rectTransform);
            activeText.alignment = TextAnchor.MiddleCenter;

            Button button = CreateButton("Button", root, Color.clear);
            SetTopLeft((RectTransform)button.transform, 0f, 0f, 395f, 100f);
            return new TabPiece
            {
                Root = root,
                Button = button,
                Active = active.gameObject,
                Text = text
            };
        }

        private static GridRefs CreateGrids(RectTransform parent, Shader rounded)
        {
            Image background = CreateImage(
                "GridBg",
                parent,
                null,
                new Color(0.976471f, 0.92549f, 0.882353f, 1f));
            SetTopLeft(background.rectTransform, 50f, 525f, 800f, 418f);
            RoundedImageView roundedView =
                background.gameObject.AddComponent<RoundedImageView>();
            roundedView.Configure(
                background,
                rounded,
                new Vector4(0f, 30f, 30f, 30f));

            RectTransform viewport = CreateRect("AvatarScroll", parent);
            SetTopLeft(viewport, 71f, 546f, 758f, 376f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup vertical =
                content.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 9f;
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;
            ContentSizeFitter fitter =
                content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            RectTransform avatarGrid = CreateGrid("AvatarGrid", content, 4);
            DividerRefs leaderboard = CreateDivider(
                "LeaderboardDivider",
                content,
                "Leaderboard");
            RectTransform leaderboardGrid = CreateGrid(
                "LeaderboardGrid",
                content,
                4);
            DividerRefs classic = CreateDivider(
                "ClassicDivider",
                content,
                "Classic");
            RectTransform classicGrid = CreateGrid("ClassicGrid", content, 4);
            RectTransform pad = CreateRect("BottomPad", content);
            LayoutElement padLayout = pad.gameObject.AddComponent<LayoutElement>();
            padLayout.preferredHeight = 10f;

            leaderboard.Root.SetActive(false);
            leaderboardGrid.gameObject.SetActive(false);
            classic.Root.SetActive(false);
            classicGrid.gameObject.SetActive(false);
            return new GridRefs
            {
                Background = roundedView,
                Scroll = scroll,
                AvatarGrid = avatarGrid,
                LeaderboardDivider = leaderboard.Root,
                LeaderboardTitle = leaderboard.Title,
                LeaderboardGrid = leaderboardGrid,
                ClassicDivider = classic.Root,
                ClassicTitle = classic.Title,
                ClassicGrid = classicGrid
            };
        }

        private static RectTransform CreateGrid(
            string name,
            Transform parent,
            int columns)
        {
            RectTransform root = CreateRect(name, parent);
            GridLayoutGroup grid = root.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(185f, 185f);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            ContentSizeFitter fitter =
                root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return root;
        }

        private static DividerRefs CreateDivider(
            string name,
            Transform parent,
            string label)
        {
            RectTransform root = CreateRect(name, parent);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 56f;
            Image left = CreateImage(
                "LeftLine",
                root,
                LoadSprite(SpriteRoot + "divider_line_left.png"),
                Color.white);
            SetTopLeft(left.rectTransform, 20f, 27f, 193f, 2f);
            Text title = CreateText("Title", root, 40, label,
                new Color(0.576f, 0.353f, 0.353f, 1f));
            SetTopLeft(title.rectTransform, 215f, 0f, 328f, 56f);
            title.alignment = TextAnchor.MiddleCenter;
            Image right = CreateImage(
                "RightLine",
                root,
                LoadSprite(SpriteRoot + "divider_line_right.png"),
                Color.white);
            SetTopLeft(right.rectTransform, 545f, 27f, 193f, 2f);
            return new DividerRefs { Root = root.gameObject, Title = title };
        }

        private static ButtonRefs CreateConfirm(RectTransform parent, Shader rounded)
        {
            Button button = CreateButton(
                "Confirm",
                parent,
                new Color(0.945098f, 0.576471f, 0.12549f, 1f));
            RectTransform rect = (RectTransform)button.transform;
            SetTopLeft(rect, 56f, 935f, 784f, 258f);
            button.gameObject.AddComponent<RoundedImageView>()
                .Configure(button.GetComponent<Image>(), rounded, 55f);
            Text text = CreateText(
                "Label",
                rect,
                68,
                "Confirm",
                Color.white);
            SetTopLeft(text.rectTransform, 53f, 69f, 686f, 158f);
            text.alignment = TextAnchor.MiddleCenter;
            return new ButtonRefs { Button = button, Text = text };
        }

        private static TipRefs CreateLockTip(RectTransform parent, Shader rounded)
        {
            RectTransform root = CreateRect("LockTip", parent);
            Stretch(root);
            Button dismiss = CreateButton("Catcher", root, Color.clear);
            Stretch((RectTransform)dismiss.transform);

            RectTransform bubble = CreateRect("Bubble", root);
            bubble.anchorMin = bubble.anchorMax = new Vector2(0f, 1f);
            bubble.pivot = new Vector2(0f, 1f);
            bubble.sizeDelta = new Vector2(780f, 185f);
            CanvasGroup group = bubble.gameObject.AddComponent<CanvasGroup>();
            Image background = CreateImage(
                "PopupBg",
                bubble,
                LoadSprite(SpriteRoot + "lock_tip_bg.png"),
                Color.white);
            SetTopLeft(background.rectTransform, 0f, 0f, 780f, 160f);
            background.type = Image.Type.Sliced;
            Text tipText = CreateText(
                "TipText",
                bubble,
                40,
                "Get first place in the challenge to get this frame!",
                Color.white);
            SetTopLeft(tipText.rectTransform, 40f, 4f, 700f, 152f);
            tipText.alignment = TextAnchor.MiddleCenter;
            Image arrow = CreateImage(
                "Arrow",
                bubble,
                LoadSprite(SpriteRoot + "lock_tip_arrow.png"),
                Color.white);
            SetTopLeft(arrow.rectTransform, 372f, 159f, 36f, 25f);

            Button go = CreateButton(
                "GoBtn",
                bubble,
                new Color(0.945098f, 0.576471f, 0.12549f, 1f));
            SetTopLeft((RectTransform)go.transform, 560f, 30f, 190f, 100f);
            go.gameObject.AddComponent<RoundedImageView>()
                .Configure(go.GetComponent<Image>(), rounded, 30f);
            Text goText = CreateText("GoText", go.transform, 50, "GO", Color.white);
            Stretch(goText.rectTransform);
            goText.alignment = TextAnchor.MiddleCenter;
            go.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return new TipRefs
            {
                Root = root.gameObject,
                Bubble = bubble,
                Group = group,
                Text = tipText,
                Dismiss = dismiss,
                GoButton = go,
                GoRoot = go.gameObject,
                GoText = goText
            };
        }

        private static Button CreateIconButton(
            string name,
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            Sprite icon,
            Vector2 iconSize)
        {
            Button button = CreateButton(name, parent, Color.clear);
            SetTopLeft((RectTransform)button.transform, x, y, width, height);
            Image visual = CreateImage("Visual", button.transform, icon, Color.white);
            SetCentered(visual.rectTransform, Vector2.zero, iconSize);
            visual.preserveAspect = true;
            return button;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return gameObject.GetComponent<Button>();
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            int fontSize,
            string value,
            Color color)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.text = value;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void SetTopLeft(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
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

        private static void SetOffsets(
            RectTransform rect,
            float left,
            float top,
            float right,
            float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        private static Sprite LoadSprite(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
                if (assets[index] is Sprite sprite) return sprite;
            return null;
        }

        private static void SetRef(
            SerializedObject data,
            string name,
            UnityEngine.Object value)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetSpriteArray(
            SerializedObject data,
            string name,
            Sprite[] values)
        {
            SerializedProperty property = data.FindProperty(name);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static bool CanEdit()
        {
            return !EditorApplication.isCompiling &&
                   !EditorApplication.isUpdating &&
                   !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private sealed class NicknameRefs
        {
            public InputField Input;
            public Button Click;
            public Button Rename;
        }

        private sealed class TabPiece
        {
            public RectTransform Root;
            public Button Button;
            public GameObject Active;
            public Text Text;
        }

        private sealed class TabRefs
        {
            public Button AvatarButton;
            public Button FrameButton;
            public GameObject AvatarActive;
            public GameObject FrameActive;
            public GameObject RedDot;
            public Text AvatarText;
            public Text FrameText;
        }

        private sealed class GridRefs
        {
            public RoundedImageView Background;
            public ScrollRect Scroll;
            public RectTransform AvatarGrid;
            public GameObject LeaderboardDivider;
            public Text LeaderboardTitle;
            public RectTransform LeaderboardGrid;
            public GameObject ClassicDivider;
            public Text ClassicTitle;
            public RectTransform ClassicGrid;
        }

        private sealed class DividerRefs
        {
            public GameObject Root;
            public Text Title;
        }

        private sealed class ButtonRefs
        {
            public Button Button;
            public Text Text;
        }

        private sealed class TipRefs
        {
            public GameObject Root;
            public RectTransform Bubble;
            public CanvasGroup Group;
            public Text Text;
            public Button Dismiss;
            public Button GoButton;
            public GameObject GoRoot;
            public Text GoText;
        }
    }
}
