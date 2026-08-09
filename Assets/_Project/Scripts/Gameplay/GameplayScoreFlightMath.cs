using UnityEngine;

namespace Meowdoku.Gameplay
{
    public static class GameplayScoreFlightMath
    {
        public static Vector2 Evaluate(float t, Vector2 from, Vector2 to, bool life)
        {
            t = Mathf.Clamp01(t);
            float x = CubicBezierEase(t, 0.2f, 0f, 0.8f, 1f);
            float y = life
                ? CubicBezierEase(t, 0.2f, -1.176f, 1f, 1f)
                : CubicBezierEase(t, 0.5f, -0.343f, 1f, 1f);
            return new Vector2(
                Mathf.LerpUnclamped(from.x, to.x, x),
                Mathf.LerpUnclamped(from.y, to.y, y));
        }

        public static float CubicBezierEase(
            float t,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            float cx = 3f * x1;
            float bx = 3f * (x2 - x1) - cx;
            float ax = 1f - cx - bx;
            float cy = 3f * y1;
            float by = 3f * (y2 - y1) - cy;
            float ay = 1f - cy - by;
            float guess = t;
            for (int index = 0; index < 8; index++)
            {
                float x = ((ax * guess + bx) * guess + cx) * guess;
                float derivative = (3f * ax * guess + 2f * bx) * guess + cx;
                if (Mathf.Abs(derivative) < 0.0000001f) break;
                guess -= (x - t) / derivative;
            }
            return ((ay * guess + by) * guess + cy) * guess;
        }
    }
}
