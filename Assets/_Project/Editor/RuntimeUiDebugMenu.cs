using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.UI;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Editor
{
    internal static class RuntimeUiDebugMenu
    {
        [MenuItem("Meowdoku/Test UI/Open Home", false, 100)]
        private static void OpenHome() => Show(UiName.Home);

        [MenuItem("Meowdoku/Test UI/Open Tutorial", false, 101)]
        private static void OpenTutorial() => Show(UiName.Tutorial);

        [MenuItem("Meowdoku/Test UI/Open Settings", false, 102)]
        private static void OpenSettings() => Show(UiName.Setting);

        [MenuItem("Meowdoku/Test UI/Open Language", false, 103)]
        private static void OpenLanguage() => Show(UiName.Language);

        [MenuItem("Meowdoku/Test UI/Open How To Play", false, 104)]
        private static void OpenHowToPlay() => Show(UiName.HowToPlay);

        [MenuItem("Meowdoku/Test UI/Open Paged How To Play", false, 105)]
        private static void OpenPagedHowToPlay() =>
            Show(UiName.HowToPlayPaged);

        [MenuItem("Meowdoku/Test UI/Open Bank", false, 106)]
        private static void OpenBank() => Show(UiName.Bank);

        [MenuItem("Meowdoku/Test UI/Open Current Game", false, 107)]
        private static void OpenGame()
        {
            Show(
                UiName.Game,
                new Dictionary<string, object>
                {
                    ["level_index"] = GameStateRuntime.Current.CurrentLevel
                });
        }

        private static void Show(
            UiName name,
            IReadOnlyDictionary<string, object> parameters = null)
        {
            if (!EditorApplication.isPlaying) return;
            UIManager manager =
                Object.FindFirstObjectByType<UIManager>();
            manager?.Show(name, parameters);
        }
    }
}
