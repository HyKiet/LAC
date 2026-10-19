using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Dữ liệu biểu diễn vũ khí rời; không quyết định sát thương hoặc tầm thật.</summary>
    [CreateAssetMenu(fileName = "DetachedWeapon", menuName = "LAC/Detached Weapon")]
    public sealed class DetachedWeaponData : ScriptableObject
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Vector2 _handOffset = new Vector2(.18f, .02f);
        [SerializeField, Min(.01f)] private float _tipDistance = 1.28125f;
        [SerializeField] private float _restAngle = -30f;
        [SerializeField] private float _windupAngle = -75f;
        [SerializeField] private float _followThroughAngle = 55f;
        [SerializeField, Range(.1f, .9f)] private float _strikeLead = .65f;
        [SerializeField, Range(.1f, .9f)] private float _followThrough = .4f;

        public Sprite Sprite => _sprite;
        public Vector2 HandOffset => _handOffset;
        public float TipDistance => _tipDistance;
        public float RestAngle => _restAngle;
        public float WindupAngle => _windupAngle;
        public float FollowThroughAngle => _followThroughAngle;
        public float StrikeLead => _strikeLead;
        public float FollowThrough => _followThrough;

        /// <summary>Góc tương đối đạt đúng hướng đòn tại contact, kể cả khi contact đổi.</summary>
        public float AngleAt(float progress, float contact)
        {
            progress = Mathf.Clamp01(progress);
            contact = Mathf.Clamp(contact, .001f, .999f);
            float strike = contact * _strikeLead;
            float follow = Mathf.Lerp(contact, 1f, _followThrough);
            if (progress < strike) return Mathf.Lerp(_restAngle, _windupAngle, Smooth(progress / strike));
            if (progress < contact) return Mathf.Lerp(_windupAngle, 0f, Smooth((progress - strike) / (contact - strike)));
            if (progress < follow) return Mathf.Lerp(0f, _followThroughAngle, Smooth((progress - contact) / (follow - contact)));
            return Mathf.Lerp(_followThroughAngle, _restAngle, Smooth((progress - follow) / (1f - follow)));
        }

        private static float Smooth(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
    }
}
