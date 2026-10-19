using LAC.VFX;
using UnityEngine;

namespace LAC.Player
{
    /// <summary>Giữ ảnh cartoon cố định, bàn chân diễn theo quãng đường thực, tay bám chuôi tre.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class PlayerCartoonVisual : MonoBehaviour
    {
        private PlayerCharacter _character;
        private PlayerMovement _movement;
        private PlayerHealth _health;
        private PlayerWeaponVisual _weapon;
        private SpriteRenderer _root;
        private SpriteRenderer _body, _leftFoot, _rightFoot, _freeHand, _gripHand;
        private CartoonCharacterData _data;
        private float _phase;
        private float _blend;
        private bool _originalForceOff;
        private bool _ownsRoot;

        public SpriteRenderer BodyRenderer => _body;
        public SpriteRenderer LeftFootRenderer => _leftFoot;
        public SpriteRenderer RightFootRenderer => _rightFoot;
        public SpriteRenderer GripRenderer => _gripHand;

        private void Awake()
        {
            _character = GetComponent<PlayerCharacter>();
            _movement = GetComponent<PlayerMovement>();
            _health = GetComponent<PlayerHealth>();
            _weapon = GetComponent<PlayerWeaponVisual>();
            _root = GetComponent<SpriteRenderer>();
            _body = Part("CartoonBody"); _leftFoot = Part("CartoonFootLeft"); _rightFoot = Part("CartoonFootRight");
            _freeHand = Part("CartoonHandFree"); _gripHand = Part("CartoonHandGrip");
        }

        private SpriteRenderer Part(string name)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>(); renderer.enabled = false;
            if (_root != null) renderer.sharedMaterial = _root.sharedMaterial;
            return renderer;
        }

        private void OnDisable()
        {
            RestoreRoot(); SetVisible(false);
        }

        private void RestoreRoot()
        {
            if (_ownsRoot && _root != null) _root.forceRenderingOff = _originalForceOff;
            _ownsRoot = false;
        }

        private void SetVisible(bool visible)
        {
            _body.enabled = _leftFoot.enabled = _rightFoot.enabled = _freeHand.enabled = _gripHand.enabled = visible;
        }

        private void LateUpdate()
        {
            CartoonCharacterData data = _character != null && _character.Data != null ? _character.Data.CartoonVisual : null;
            if (data != _data) { _data = data; _phase = _blend = 0f; }
            if (_data == null || _data.Body == null || _root == null || !_root.enabled)
            {
                SetVisible(false); RestoreRoot(); return;
            }
            if (!_ownsRoot) { _originalForceOff = _root.forceRenderingOff; _ownsRoot = true; }
            // Renderer gốc vẫn nhận flip/flash/animation và dùng cho dash-afterimage.
            // Chỉ tắt việc vẽ, không tắt component mà vũ khí đang đọc.
            _root.forceRenderingOff = true;
            bool alive = _health == null || _health.IsAlive;
            bool moving = alive && _movement != null && _movement.IsMoving && _movement.VisualSpeed > .1f;
            _blend = Mathf.MoveTowards(_blend, moving ? 1f : 0f, Time.deltaTime * 12f);
            if (moving) _phase = Mathf.Repeat(_phase + Mathf.Min(_movement.VisualSpeed, 8f) * Time.deltaTime /
                Mathf.Max(.1f, _data.StrideDistance) * Mathf.PI * 2f, Mathf.PI * 2f);
            float sign = _root.flipX ? -1f : 1f;
            float step = Mathf.Sin(_phase) * _blend;
            float bob = Mathf.Abs(step) * _data.BodyBob;
            _body.enabled = true; _body.sprite = _data.Body; _body.flipX = _root.flipX;
            _body.transform.localPosition = alive ? new Vector2(0f, bob) : _data.DownOffset;
            _body.transform.localRotation = Quaternion.Euler(0f, 0f, alive ? -sign * step * _data.LeanDegrees : sign * _data.DownAngle);
            Style(_body, 0);
            Foot(_leftFoot, _data.LeftFoot, _data.LeftFootOffset, step, sign, alive);
            Foot(_rightFoot, _data.RightFoot, _data.RightFootOffset, -step, sign, alive);
            _freeHand.enabled = alive; _freeHand.sprite = _root.flipX ? _data.RightHand : _data.LeftHand;
            _freeHand.flipX = _root.flipX;
            _freeHand.transform.localPosition = new Vector2(_data.FreeHandOffset.x * sign, _data.FreeHandOffset.y + bob - step * .02f);
            Style(_freeHand, 1);
            SpriteRenderer weapon = _weapon != null ? _weapon.WeaponRenderer : null;
            _gripHand.enabled = alive && weapon != null && weapon.enabled;
            _gripHand.sprite = _root.flipX ? _data.LeftHand : _data.RightHand;
            _gripHand.flipX = _root.flipX;
            if (weapon != null)
            {
                _gripHand.transform.localPosition = weapon.transform.localPosition;
                Style(_gripHand, weapon.sortingOrder - _root.sortingOrder + 1);
            }
        }

        private void Foot(SpriteRenderer renderer, Sprite sprite, Vector2 offset, float step, float sign, bool alive)
        {
            renderer.enabled = alive; renderer.sprite = sprite; renderer.flipX = _root.flipX;
            renderer.transform.localPosition = new Vector2((offset.x + step * _data.FootSwing) * sign,
                offset.y + Mathf.Max(0f, step) * _data.FootLift);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, -step * 10f * sign);
            Style(renderer, -1);
        }

        private void Style(SpriteRenderer renderer, int order)
        {
            renderer.color = _root.color;
            renderer.sortingLayerID = _root.sortingLayerID;
            renderer.sortingOrder = _root.sortingOrder + order;
        }
    }
}
