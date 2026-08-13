using Meowdoku.Core.UI;
using UnityEngine;

namespace Meowdoku.Gameplay
{
    /// <summary>
    /// Separate script asset so Unity can serialize the source V2 presenter as
    /// a concrete MonoBehaviour rather than a secondary class in another file.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RateUsPagePresenterV2 : RateUsPagePresenter
    {
        protected override bool UsesDefaultCloseButton => false;
    }
}
