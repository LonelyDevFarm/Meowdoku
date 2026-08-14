using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Tests.EditMode
{
    public sealed class StreakVisualCompositionTests
    {
        private const string StreakPrefabPath =
            "Assets/_Project/Prefabs/UI/StreakPage.prefab";
        private const string HomePrefabPath =
            "Assets/_Project/Prefabs/UI/HomePage.prefab";

        [Test]
        public void StreakPage_UsesSourceHeroAndCircularWeekSlots()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(StreakPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform content = Require(prefab.transform, "StreakContent");
            AssertSprite(content, "Hero/SunRoot/SunImg", "sun");
            AssertSprite(content, "Hero/BestFrame", "sudoku_bg_round20");
            AssertSprite(content, "Top/BackBtn/Base", "normal_btn_bg");
            AssertSprite(content, "Top/BackBtn/Icon", "vector_1");

            RectTransform back = (RectTransform)Require(
                content, "Top/BackBtn");
            RectTransform backBase = (RectTransform)Require(
                back, "Base");
            RectTransform backIcon = (RectTransform)Require(
                back, "Icon");
            Assert.That(back.sizeDelta, Is.EqualTo(new Vector2(100f, 100f)));
            Assert.That(
                backBase.sizeDelta, Is.EqualTo(new Vector2(140f, 140f)));
            Assert.That(
                backIcon.sizeDelta, Is.EqualTo(new Vector2(54f, 46f)));

            Transform week = Require(content, "WeekSlots");
            StreakDaySlotView[] slots =
                week.GetComponentsInChildren<StreakDaySlotView>(true);
            Assert.That(slots, Has.Length.EqualTo(7));
            foreach (StreakDaySlotView slot in slots)
            {
                Transform uncheckedDot = Require(
                    slot.transform, "UncheckedDot");
                Assert.That(
                    uncheckedDot.GetComponent<RoundedImageView>(),
                    Is.Not.Null);
                Assert.That(
                    ((RectTransform)uncheckedDot).sizeDelta,
                    Is.EqualTo(new Vector2(120f, 120f)));
                AssertSprite(slot.transform, "CheckedDot", "dot");
                AssertSprite(
                    slot.transform,
                    "CheckedDot/CheckShort",
                    "et_mask_008");
                AssertSprite(
                    slot.transform,
                    "CheckedDot/CheckLong",
                    "et_mask_008");
            }
        }

        [Test]
        public void HomeStreakEntries_KeepSourceBackgroundLayers()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HomePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Transform full = Require(prefab.transform,
                "Root/DailyStreakLayout/StreakEntrySlot/StreakEntryCell");
            AssertSprite(full, "StateChecked/Background", "state_checked1");
            AssertSprite(full, "StateChecked/Sun", "sun");
            AssertSprite(full, "StateChecked/Checkmark", "state_checked2");
            Assert.That(
                Require(full, "CountBadge")
                    .GetComponent<RoundedImageView>(),
                Is.Not.Null);

            Transform mini = Require(prefab.transform,
                "Root/DailyStreakLayout/StreakSmallEntrySlot/" +
                "StreakMiniEntryCell");
            AssertSprite(mini, "Shadow", "mini_bg");
            Assert.That(
                Require(mini, "Panel").GetComponent<RoundedImageView>(),
                Is.Not.Null);
            AssertSprite(mini, "CheckedState/Sun", "sun");
            AssertSprite(
                mini, "CheckedState/Checkmark", "state_checked2");
        }

        private static Transform Require(Transform root, string path)
        {
            Transform result = root.Find(path);
            Assert.That(result, Is.Not.Null, path);
            return result;
        }

        private static void AssertSprite(
            Transform root,
            string path,
            string expectedName)
        {
            Image image = Require(root, path).GetComponent<Image>();
            Assert.That(image, Is.Not.Null, path);
            Assert.That(image.sprite, Is.Not.Null, path);
            bool matches = image.sprite.name == expectedName ||
                image.sprite.name.StartsWith(expectedName + "_");
            Assert.That(matches, Is.True,
                path + ": " + image.sprite.name);
        }
    }
}
