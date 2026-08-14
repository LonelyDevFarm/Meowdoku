using UnityEngine;

namespace Meowdoku.Gameplay
{
    [CreateAssetMenu(
        fileName = "CatSpriteAnimationCatalog",
        menuName = "Meowdoku/Gameplay/Cat Sprite Animation Catalog")]
    public sealed class CatSpriteAnimationCatalog : ScriptableObject
    {
        [SerializeField] private Sprite[] appear = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] cry = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] frustrated = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] idle = System.Array.Empty<Sprite>();
        [SerializeField, Min(1f)] private float fps = 30f;
        [SerializeField, Min(0f)] private float idleInterval = 5f;

        public Sprite[] Appear => appear;
        public Sprite[] Cry => cry;
        public Sprite[] Frustrated => frustrated;
        public Sprite[] Idle => idle;
        public float Fps => fps;
        public float IdleInterval => idleInterval;
    }
}
