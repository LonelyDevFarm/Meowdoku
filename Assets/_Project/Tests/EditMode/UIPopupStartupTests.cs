using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core.UI;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class UIPopupStartupTests
    {
        [Test]
        public void PopupQueue_IsDescendingAndStableForEqualPriority()
        {
            var order = new List<string>();
            var queue = new UIPopupQueue();
            queue.Enqueue(Entry("low", 10, order));
            queue.Enqueue(Entry("high-a", 30, order));
            queue.Enqueue(Entry("middle", 20, order));
            queue.Enqueue(Entry("high-b", 30, order));

            Drain(queue.Flush());

            Assert.That(order, Is.EqualTo(new[]
                { "high-a", "high-b", "middle", "low" }));
            Assert.That(queue.IsRunning, Is.False);
            Assert.That(queue.Count, Is.Zero);
        }

        [Test]
        public void PopupQueue_InsertNextAndCancelMatchSource()
        {
            var order = new List<string>();
            var queue = new UIPopupQueue();
            queue.Enqueue(Entry("a", 10, order));
            queue.Enqueue(Entry("cancel", 50, order));
            queue.Enqueue(Entry("cancel", 5, order));
            queue.InsertNext(Entry("next", 0, order));
            queue.Cancel("cancel");

            Drain(queue.Flush());

            Assert.That(order, Is.EqualTo(new[] { "next", "a" }));
        }

        [Test]
        public void PopupQueue_AbortReleasesUnityStoppedCoroutineState()
        {
            var queue = new UIPopupQueue();
            queue.Enqueue(new UIPopupEntry(
                "wait",
                1,
                () => Endless()));
            IEnumerator flush = queue.Flush();
            Assert.That(flush.MoveNext(), Is.True);
            Assert.That(queue.IsRunning, Is.True);

            queue.Abort();

            Assert.That(queue.IsRunning, Is.False);
            Assert.That(queue.Count, Is.Zero);
        }

        [Test]
        public void PriorityConfig_ParsesAllFourSourceEntries()
        {
            const string json = "[" +
                "{\"OpenScene\":\"home\",\"Priority\":10012," +
                    "\"Key\":\"rank_reward_and_tryopen_popup\",\"CanExceedLimit\":1}," +
                "{\"OpenScene\":\"home\",\"Priority\":10011," +
                    "\"Key\":\"ab_switch_popup\",\"CanExceedLimit\":0}," +
                "{\"OpenScene\":\"home\",\"Priority\":10010," +
                    "\"Key\":\"ad_reward_restored\",\"CanExceedLimit\":1}," +
                "{\"OpenScene\":\"home\",\"Priority\":10009," +
                    "\"Key\":\"rank_open_popup\",\"CanExceedLimit\":1}]";

            IReadOnlyList<DialogPriorityRule> rules =
                UIPopupConfig.ParsePriorities(json);

            Assert.That(rules, Has.Count.EqualTo(4));
            Assert.That(rules[0].Priority, Is.EqualTo(10012));
            Assert.That(rules[0].CanExceedLimit, Is.True);
            Assert.That(rules[1].Key, Is.EqualTo("ab_switch_popup"));
            Assert.That(rules[1].CanExceedLimit, Is.False);
        }

        [Test]
        public void AbSwitchDsl_PreservesBraceListsAndNestedReward()
        {
            IReadOnlyDictionary<string, object> trigger =
                UIPopupConfig.ParseTriggerDsl(
                    "trigger=abtest_switch,key=daily_streak,bf={3},af={1,2,4}");
            IReadOnlyDictionary<string, object> parameters =
                UIPopupConfig.ParseParameterDsl(
                    "title=TITLE,reward={locate=2,hint=3},feedback=1");

            Assert.That(trigger["trigger"], Is.EqualTo("abtest_switch"));
            Assert.That(trigger["bf"], Is.EqualTo(new[] { 3 }));
            Assert.That(trigger["af"], Is.EqualTo(new[] { 1, 2, 4 }));
            var reward = (IReadOnlyDictionary<string, object>)parameters["reward"];
            Assert.That(reward["locate"], Is.EqualTo(2));
            Assert.That(reward["hint"], Is.EqualTo(3));
        }

        [Test]
        public void SwitchRule_UsesSourceOccurrenceForRequestedKey()
        {
            const char quote = (char)34;
            string json =
                "[{" + quote + "Trigger" + quote + ":" + quote +
                "trigger=abtest_switch,key=daily_streak" + quote + "," +
                quote + "Param" + quote + ":" + quote +
                "title=ONE" + quote + "},{" +
                quote + "Trigger" + quote + ":" + quote +
                "trigger=abtest_switch,key=other" + quote + "," +
                quote + "Param" + quote + ":" + quote +
                "title=OTHER" + quote + "},{" +
                quote + "Trigger" + quote + ":" + quote +
                "trigger=abtest_switch,key=daily_streak" + quote + "," +
                quote + "Param" + quote + ":" + quote +
                "title=TWO" + quote + "}]";
            IReadOnlyList<AbSwitchPopupRule> rules =
                UIPopupConfig.ParseAbSwitchRules(json);

            Assert.That(
                UIPopupConfig.FindSwitchRule(
                    rules,
                    "daily_streak",
                    2).Parameters["title"],
                Is.EqualTo("TWO"));
            Assert.That(
                UIPopupConfig.FindSwitchRule(
                    rules,
                    "daily_streak",
                    3),
                Is.Null);
        }

        [TestCase(0f, 2.5f)]
        [TestCase(0.5f, 2f)]
        [TestCase(2f, 0.5f)]
        [TestCase(5f, 0.5f)]
        public void SplashTiming_MatchesLauncher(float elapsed, float expected)
        {
            Assert.That(AppStartupContract.SplashWaitRemaining(elapsed),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void InitialRoute_UsesPersistedTutorialDone()
        {
            Assert.That(AppStartupContract.InitialRoute(false),
                Is.EqualTo(UiName.Tutorial));
            Assert.That(AppStartupContract.InitialRoute(true),
                Is.EqualTo(UiName.Home));
        }

        private static UIPopupEntry Entry(
            string key,
            int priority,
            ICollection<string> order)
        {
            return new UIPopupEntry(key, priority, () => Record(key, order));
        }

        private static IEnumerator Record(string key, ICollection<string> order)
        {
            order.Add(key);
            yield break;
        }

        private static IEnumerator Endless()
        {
            while (true) yield return null;
        }

        private static void Drain(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                if (current.Current is IEnumerator nested) stack.Push(nested);
            }
        }
    }
}
