using LAC.VFX;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>Cơ thể tiếp tục chạy; chỉ tre ngà diễn timeline do driver nhận từ host.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponVisual : MonoBehaviour
    {
        private PlayerCharacter _character;
        private PlayerMovement _movement;
        private PlayerHealth _health;
        private SpriteRenderer _body;
        private SpriteRenderer _renderer;
        private DetachedWeaponData _data;
        private Vector2 _direction = Vector2.right;
        private Vector2 _restDirection = Vector2.right;
        private double _startedAt;
        private float _duration;
        private float _contact;
        private float _range;
        private bool _attacking;

        public bool UsesSeparateWeapon => _character != null && _character.Data != null &&
            _character.Data.DetachedWeapon != null && _character.Data.DetachedWeapon.Sprite != null;
        public bool IsAttacking => _attacking;
        public SpriteRenderer WeaponRenderer => _renderer;
        public float Progress => _attacking ? Mathf.Clamp01((float)(Time.timeAsDouble - _startedAt) / _duration) : 0f;

        private void Awake()
        {
            _character = GetComponent<PlayerCharacter>();
            _movement = GetComponent<PlayerMovement>();
            _health = GetComponent<PlayerHealth>();
            _body = GetComponent<SpriteRenderer>();
            // Một con mỗi player, tạo trong khởi tạo; không sinh/hủy hiệu ứng trong Update.
            var child = new GameObject("DetachedWeapon");
            child.transform.SetParent(transform, false);
            _renderer = child.AddComponent<SpriteRenderer>();
            _renderer.enabled = false;
            if (_body != null) _renderer.sharedMaterial = _body.sharedMaterial;
            if (GetComponent<PlayerCartoonVisual>() == null) gameObject.AddComponent<PlayerCartoonVisual>();
        }

        public void PlayAttack(Vector2 direction, float duration, float contactNormalized, float range, float elapsed = 0f)
        {
            if (!UsesSeparateWeapon || (_health != null && !_health.IsAlive) ||
                !Finite(duration) || duration <= 0f || !Finite(elapsed) || elapsed >= duration ||
                !Finite(direction.x) || !Finite(direction.y) || direction.sqrMagnitude < .0001f ||
                !Finite(range) || range <= 0f || !Finite(contactNormalized)) return;
            _direction = direction.normalized;
            _data = _character.Data.DetachedWeapon;
            _duration = duration;
            _contact = Mathf.Clamp(contactNormalized, .001f, .999f);
            _range = range;
            _startedAt = Time.timeAsDouble - Mathf.Max(0f, elapsed);
            _attacking = true;
        }

        public void CancelAttack() => _attacking = false;

        private void OnDisable()
        {
            CancelAttack();
            if (_renderer != null) _renderer.enabled = false;
        }

        private void LateUpdate()
        {
            DetachedWeaponData data = _character != null && _character.Data != null ? _character.Data.DetachedWeapon : null;
            if (data != _data) { _data = data; CancelAttack(); }
            bool visible = _data != null && _data.Sprite != null && _body != null && _body.enabled &&
                (_health == null || _health.IsAlive);
            _renderer.enabled = visible;
            if (!visible) { CancelAttack(); return; }

            float progress = Progress;
            if (_attacking && progress >= 1f) CancelAttack();
            Vector2 facing = _movement != null ? _movement.Facing : Vector2.right;
            if (facing.sqrMagnitude > .0001f)
                _restDirection = Vector2.Lerp(_restDirection, facing.normalized, 1f - Mathf.Exp(-18f * Time.deltaTime));
            Vector2 direction = _attacking ? _direction : _restDirection.normalized;
            if (direction.sqrMagnitude < .0001f) direction = Vector2.right;
            float sign = direction.x < 0f ? -1f : 1f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float offset = _attacking ? _data.AngleAt(progress, _contact) : _data.RestAngle;
            Vector2 hand = _data.HandPosition(_body.sprite, _body.flipX, direction);
            // Tre giữ nguyên chiều dài qua cả đòn; chỉ nâng cấp tầm mới đổi tỷ lệ.
            float reach = _attacking ? _range : _character.Data.AttackRange *
                (_character.Upgrades != null ? _character.Upgrades.AttackRangeMultiplier : 1f);
            // Tại contact, đầu tre nằm trên bán kính host; khoảng trống alpha không tính vào chiều dài.
            float perpendicular = hand.x * direction.y - hand.y * direction.x;
            float length = Mathf.Sqrt(Mathf.Max(.01f, reach * reach - perpendicular * perpendicular)) - Vector2.Dot(hand, direction);
            float scale = Mathf.Max(.01f, length) / Mathf.Max(.01f, _data.TipDistance);
            Transform weapon = _renderer.transform;
            weapon.localPosition = hand;
            weapon.localRotation = Quaternion.Euler(0f, 0f, angle + offset * sign);
            weapon.localScale = new Vector3(scale, 1f, 1f);
            _renderer.sprite = _data.Sprite;
            _renderer.color = _body.color;
            _renderer.sortingLayerID = _body.sortingLayerID;
            _renderer.sortingOrder = _body.sortingOrder + (direction.y > .2f ? -1 : 1);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
