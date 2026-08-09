using System;
using System.Collections.Generic;
using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    public enum GameplayFeedbackKind
    {
        CorrectCat,
        WrongGuess,
        LifeBonus
    }

    public enum GameplayFeedbackSource
    {
        UserAction,
        Locate,
        Hint,
        AutoComplete
    }

    /// <summary>
    /// View-independent payload matching the score/combo/life side effects in
    /// BaseGamePage. A later presenter owns positioning, tweening and audio.
    /// </summary>
    public sealed class GameplayFeedbackData
    {
        public GameplayFeedbackKind Kind { get; internal set; }
        public GameplayFeedbackSource Source { get; internal set; }
        public Vector2Int Position { get; internal set; } = new Vector2Int(-1, -1);
        public int ComboCount { get; internal set; }
        public int SuccessfulCatCount { get; internal set; }
        public int ScoreBefore { get; internal set; }
        public int ScoreAfter { get; internal set; }
        public int BaseGain { get; internal set; }
        public int DisplayGain { get; internal set; }
        public float Multiplier { get; internal set; } = 1f;
        public float PreviousMultiplier { get; internal set; } = 1f;
        public int SkillBonus { get; internal set; }
        public int TotalGain { get; internal set; }
        public int Deduction { get; internal set; }
        public int LivesBefore { get; internal set; }
        public int LivesAfter { get; internal set; }
        public int LifeSlotIndex { get; internal set; } = -1;
        public bool UsesScoreEncourage { get; internal set; }
        public bool ShowsMultiplier { get; internal set; }
        public bool UsesScrollMultiplierAnimation { get; internal set; }
        public bool HasFlyEffect { get; internal set; }
        public float FlyDelaySeconds { get; internal set; }
        public QueendokuCore.Rule RuleViolation { get; internal set; }
        public IReadOnlyList<Vector2Int> ConflictingCats { get; internal set; } =
            Array.Empty<Vector2Int>();

        public bool ShowsComboText => Kind == GameplayFeedbackKind.CorrectCat && ComboCount >= 3;
    }
}
