using UnityEngine;

namespace LAC.Drum
{
    public static class DrumShockwave
    {
        public static bool Intersects(Vector2 start, Vector2 end, Vector2 center, float radius)
        {
            Vector2 segment = end - start;
            float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(center - start, segment) / segment.sqrMagnitude) : 0f;
            return (start + segment * t - center).sqrMagnitude <= radius * radius;
        }
    }
}
