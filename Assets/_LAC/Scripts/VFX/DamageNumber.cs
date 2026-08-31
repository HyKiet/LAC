using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Con số sát thương bay lên rồi tan.</summary>
    /// <remarks>
    /// Đây là phần phản hồi mà người chơi đọc được bằng con mắt ngoại vi: họ không đọc từng
    /// con số, nhưng mật độ và độ lớn của chúng cho biết build của mình đang mạnh lên hay
    /// không. Vì vậy số phải bay lên chứ không đứng yên — chuyển động là thứ mắt ngoại vi
    /// bắt được, còn chữ đứng yên thì không.
    /// </remarks>
    public sealed class DamageNumber : MonoBehaviour, IPoolable
    {
        [SerializeField] private PixelNumber _number;

        [SerializeField, Min(0.1f)] private float _lifetime = 0.6f;
        [SerializeField] private float _riseSpeed = 1.6f;

        [Tooltip("Độ lệch ngang ngẫu nhiên để hai con số cùng lúc không chồng khít.")]
        [SerializeField] private float _spread = 0.35f;

        private ObjectPool<DamageNumber> _owner;
        private float _age;
        private Vector3 _velocity;
        private Color _color = Color.white;

        public void Play(ObjectPool<DamageNumber> owner, int value, Vector3 position, Color color)
        {
            _owner = owner;
            _age = 0f;
            _color = color;

            // Lệch ngang dùng ngẫu nhiên của Unity chứ không phải RunRandom: đây là hiệu ứng
            // thuần trang trí, đúng ngoại lệ duy nhất được nêu ở CLAUDE.md mục 3.3.
            float offset = Random.Range(-_spread, _spread);
            transform.position = position + new Vector3(offset, 0f, 0f);
            _velocity = new Vector3(offset * 0.6f, _riseSpeed, 0f);

            _number.SetValue(value);
            _number.SetColor(color);
        }

        public void OnSpawned() => _age = 0f;

        public void OnDespawned() => _owner = null;

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= _lifetime)
            {
                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
                return;
            }

            float t = _age / _lifetime;
            transform.position += _velocity * Time.deltaTime;
            _velocity.y -= 2.2f * Time.deltaTime;

            _color.a = 1f - t * t;
            _number.SetColor(_color);
        }
    }
}
