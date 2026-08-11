using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Tests.EditMode
{
    public sealed class FrameAwardPresentationTests
    {
        [Test]
        public void FrameEffectTiming_MatchesSourceAnimationAndHoldWindows()
        {
            Assert.That(FrameAwardEffectView.AppearDurationSeconds,
                Is.EqualTo(0.56666666f).Within(0.00001f));
            Assert.That(FrameAwardEffectView.HoldNewFrameSeconds,
                Is.EqualTo(0.6334f).Within(0.00001f));
            Assert.That(FrameAwardEffectView.HoldExistingFrameSeconds,
                Is.EqualTo(0.8f).Within(0.00001f));
            Assert.That(FrameAwardEffectView.DisappearDurationSeconds,
                Is.EqualTo(0.33333334f).Within(0.00001f));
            Assert.That(FrameAwardEffectView.TotalDuration(false),
                Is.EqualTo(1.5334f).Within(0.0001f));
            Assert.That(FrameAwardEffectView.TotalDuration(true),
                Is.EqualTo(1.7f).Within(0.0001f));
            Assert.That(RankGiftView.AppearWithBoxDuration,
                Is.EqualTo(3.45f).Within(0.00001f));
            Assert.That(RankGiftView.AppearWithoutBoxDuration,
                Is.EqualTo(3.3666666f).Within(0.00001f));
            Assert.That(RankGiftView.OpenNotifyDelay,
                Is.EqualTo(0.8834f).Within(0.00001f));
            Assert.That(RankGiftView.OpenDuration,
                Is.EqualTo(2f).Within(0.00001f));
            Assert.That(FrameAwardFlightView.TrailShowDelaySeconds,
                Is.EqualTo(0.2667f).Within(0.00001f));
            Assert.That(FrameAwardFlightView.FlightStartDelaySeconds,
                Is.EqualTo(0.3f).Within(0.00001f));
            Assert.That(FrameAwardFlightView.FlightDurationSeconds,
                Is.EqualTo(0.45f).Within(0.00001f));
            Assert.That(FrameAwardFlightView.ArrivalHoldSeconds,
                Is.EqualTo(0.4f).Within(0.00001f));
        }

        [Test]
        public void FrameFlightCurve_MatchesSourceCubicBezierAxes()
        {
            Vector2 from = new(10f, 20f);
            Vector2 to = new(110f, 220f);
            Assert.That(FrameAwardFlightView.Evaluate(0f, from, to),
                Is.EqualTo(from));
            Assert.That(FrameAwardFlightView.Evaluate(1f, from, to),
                Is.EqualTo(to));
            Vector2 middle = FrameAwardFlightView.Evaluate(0.5f, from, to);
            Assert.That(middle.x,
                Is.EqualTo(44.85262f).Within(0.001f));
            Assert.That(middle.y,
                Is.EqualTo(30.80021f).Within(0.001f));
        }

        [Test]
        public void AwardPrefab_HasDedicatedSerializedFrameAddEffect()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/AwardPage.prefab");
            Assert.That(prefab, Is.Not.Null);
            AwardPagePresenter presenter =
                prefab.GetComponent<AwardPagePresenter>();
            Assert.That(presenter, Is.Not.Null);
            FrameAwardEffectView effect =
                prefab.GetComponentInChildren<FrameAwardEffectView>(true);
            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.name, Is.EqualTo("FrameAddEffect"));
            Assert.That(effect.gameObject.activeSelf, Is.False);
            Assert.That(effect.transform.Find("EffectRayLight"), Is.Not.Null);
            Assert.That(effect.transform.Find("AvatarCell"), Is.Not.Null);
            Assert.That(effect.transform.Find("Flight"), Is.Not.Null);
            Assert.That(effect.transform.Find("Flight/Trail_16"), Is.Not.Null);
            Assert.That(effect.transform.Find("Flight/BurstStar_12"), Is.Not.Null);

            RankGiftView rankGift =
                prefab.GetComponentInChildren<RankGiftView>(true);
            Assert.That(rankGift, Is.Not.Null);
            Assert.That(rankGift.transform.Find("Effects/Firework_4/Star_8"),
                Is.Not.Null);

            SerializedObject presenterData = new(presenter);
            Assert.That(presenterData.FindProperty("frameAddEffect")
                .objectReferenceValue, Is.SameAs(effect));
            SerializedObject effectData = new(effect);
            Assert.That(effectData.FindProperty("rayGroup")
                .objectReferenceValue, Is.Not.Null);
            Assert.That(effectData.FindProperty("avatar")
                .objectReferenceValue, Is.Not.Null);
            FrameAwardFlightView flight = effectData.FindProperty("flight")
                .objectReferenceValue as FrameAwardFlightView;
            Assert.That(flight, Is.Not.Null);
            SerializedObject flightData = new(flight);
            Assert.That(flightData.FindProperty("trailSegments").arraySize,
                Is.EqualTo(16));
            Assert.That(flightData.FindProperty("burstStars").arraySize,
                Is.EqualTo(12));

            SerializedObject giftData = new(rankGift);
            Assert.That(giftData.FindProperty("backdrop")
                .objectReferenceValue, Is.Not.Null);
            Assert.That(giftData.FindProperty("chestVisual")
                .objectReferenceValue, Is.Not.Null);
            Assert.That(giftData.FindProperty("burstRoots").arraySize,
                Is.EqualTo(4));
            Assert.That(giftData.FindProperty("burstStars").arraySize,
                Is.EqualTo(32));
        }
    }
}
