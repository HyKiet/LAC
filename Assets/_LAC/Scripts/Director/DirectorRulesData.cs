using UnityEngine;

namespace LAC.Director
{
    [CreateAssetMenu(fileName = "DirectorRules", menuName = "LAC/Director Rules")]
    public sealed class DirectorRulesData : ScriptableObject
    {
        [Header("Đợt được điều tiết")]
        [SerializeField, Min(2)] private int _firstAdaptiveWave = 2;
        [SerializeField, Min(2)] private int _lastAdaptiveWave = 9;

        [Header("Hỗ trợ khi đội gặp khó")]
        [SerializeField, Range(0f, 1f)] private float _reliefHealthThreshold = 0.30f;
        [SerializeField, Range(0f, 1f)] private float _reliefDamageThreshold = 0.35f;
        [SerializeField, Min(1f)] private float _reliefIntervalMultiplier = 1.25f;
        [SerializeField, Range(0.01f, 1f)] private float _reliefBurstMultiplier = 0.75f;

        [Header("Khoảng trễ thoát hỗ trợ")]
        [SerializeField, Range(0f, 1f)] private float _reliefExitHealth = 0.50f;
        [SerializeField, Range(0f, 1f)] private float _reliefExitDamage = 0.20f;

        [Header("Đổi hướng và thành phần khi đội khoẻ")]
        [SerializeField, Range(0f, 1f)] private float _varietyHealthThreshold = 0.70f;
        [SerializeField, Range(0f, 1f)] private float _varietyDamageThreshold = 0.10f;
        [Tooltip("Xoay hướng sinh tối thiểu trên chu vi sân; 0.1 là một phần mười chu vi.")]
        [SerializeField, Range(0.01f, 0.49f)] private float _varietyMinRotation = 0.10f;
        [SerializeField, Range(0.01f, 0.49f)] private float _varietyMaxRotation = 0.30f;

        public int FirstAdaptiveWave => _firstAdaptiveWave;
        public int LastAdaptiveWave => _lastAdaptiveWave;
        public float ReliefHealthThreshold => _reliefHealthThreshold;
        public float ReliefDamageThreshold => _reliefDamageThreshold;
        public float ReliefIntervalMultiplier => _reliefIntervalMultiplier;
        public float ReliefBurstMultiplier => _reliefBurstMultiplier;
        public float ReliefExitHealth => _reliefExitHealth;
        public float ReliefExitDamage => _reliefExitDamage;
        public float VarietyHealthThreshold => _varietyHealthThreshold;
        public float VarietyDamageThreshold => _varietyDamageThreshold;
        public float VarietyMinRotation => _varietyMinRotation;
        public float VarietyMaxRotation => _varietyMaxRotation;

        public bool TryValidate(out string reason)
        {
            if (_firstAdaptiveWave < 2 || _lastAdaptiveWave < _firstAdaptiveWave || _lastAdaptiveWave > 9)
                return Invalid("invalid_wave_window", out reason);
            if (!Ratio(_reliefHealthThreshold) || !Ratio(_reliefDamageThreshold) ||
                !Ratio(_reliefExitHealth) || !Ratio(_reliefExitDamage) ||
                !Ratio(_varietyHealthThreshold) || !Ratio(_varietyDamageThreshold))
                return Invalid("invalid_threshold", out reason);
            if (_reliefExitHealth <= _reliefHealthThreshold || _reliefExitDamage >= _reliefDamageThreshold ||
                _varietyHealthThreshold < _reliefExitHealth || _varietyDamageThreshold > _reliefExitDamage)
                return Invalid("invalid_hysteresis", out reason);
            if (!SafetyConstraints.IsFinite(_reliefIntervalMultiplier) || _reliefIntervalMultiplier < 1f ||
                !Ratio(_reliefBurstMultiplier) || _reliefBurstMultiplier <= 0f)
                return Invalid("invalid_relief_multiplier", out reason);
            if (!SafetyConstraints.IsFinite(_varietyMinRotation) || !SafetyConstraints.IsFinite(_varietyMaxRotation) ||
                _varietyMinRotation <= 0f || _varietyMaxRotation < _varietyMinRotation || _varietyMaxRotation >= 0.5f)
                return Invalid("invalid_variety_rotation", out reason);
            reason = string.Empty;
            return true;
        }

        private static bool Ratio(float value) => SafetyConstraints.IsFinite(value) && value >= 0f && value <= 1f;
        private static bool Invalid(string value, out string reason) { reason = value; return false; }
    }
}
