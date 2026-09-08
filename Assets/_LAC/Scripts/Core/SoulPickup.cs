using LAC.Audio;
using LAC.Player;
using Mirror;
using UnityEngine;

namespace LAC.Core
{
    /// <summary>
    /// Một Hồn rơi ra từ quái vừa chết: bắn ra, nằm chờ, rồi hút về người chơi gần nhất.
    /// </summary>
    /// <remarks>
    /// Hồn là vòng phản hồi ngắn nhất của ván — xem CLAUDE.md mục 2.4. Vì vậy nó <b>tự bay
    /// về</b> chứ không bắt người chơi đi nhặt: buộc phải rời vị trí an toàn để nhặt từng
    /// món sẽ đối kháng với chính lối chơi né tránh, còn tự hút thì biến mỗi lần giết quái
    /// thành một phần thưởng đến ngay mà không đòi hỏi thao tác nào thêm.
    ///
    /// Toàn bộ chuyển động ở đây là <b>biểu diễn cục bộ</b>, không đồng bộ. Hai máy sinh ra
    /// cùng bấy nhiêu Hồn ở cùng chỗ vì cái chết của quái đã đồng bộ và số lượng suy ra từ
    /// seed chung, nhưng đường bay và thời điểm chạm thì mỗi máy tự tính. Chỉ con số đếm
    /// được là do host giữ — xem <see cref="RunManager.ReportSoulCollected"/>.
    /// </remarks>
    public sealed class SoulPickup : MonoBehaviour, IPoolable
    {
        [Header("Bắn ra")]
        [Tooltip("Tốc độ văng ra khỏi xác quái.")]
        [SerializeField, Min(0f)] private float _scatterSpeed = 3.4f;

        [Tooltip("Hệ số hãm khi chưa bị hút. Càng lớn càng dừng nhanh.")]
        [SerializeField, Min(0.01f)] private float _scatterDrag = 7f;

        [Header("Hút về")]
        [SerializeField, Min(0f)] private float _magnetRadius = 3.5f;
        [SerializeField, Min(0f)] private float _magnetAcceleration = 30f;
        [SerializeField, Min(0.1f)] private float _maxSpeed = 15f;
        [SerializeField, Min(0.05f)] private float _pickupRadius = 0.45f;

        [Tooltip("Sau chừng này giây thì bay về dù người chơi đứng ngoài tầm hút. 0 là không.")]
        [SerializeField, Min(0f)] private float _forceCollectAfter = 7f;

        private ObjectPool<SoulPickup> _owner;
        private PitchLadder _sound;
        private Vector2 _velocity;
        private float _age;
        private bool _collected;

        /// <summary>Đưa Hồn vào trạng thái vừa văng ra. Gọi ngay sau khi lấy từ pool.</summary>
        public void Launch(ObjectPool<SoulPickup> owner, PitchLadder sound, Vector2 direction)
        {
            _owner = owner;
            _sound = sound;
            _velocity = direction * _scatterSpeed;
        }

        public void OnSpawned()
        {
            _age = 0f;
            _collected = false;
            _velocity = Vector2.zero;
        }

        public void OnDespawned()
        {
            _owner = null;
            _sound = null;
        }

        private void Update()
        {
            if (_collected) return;

            _age += Time.deltaTime;

            // Không còn ai sống thì Hồn nằm lại trên sân cho tới khi ván dọn pool. Trả về
            // pool ngay tại đây sẽ làm biến mất phần thưởng của cú đánh cuối cùng đúng vào
            // lúc người chơi vừa gục — nhìn như game nuốt mất đồ rơi.
            PlayerCharacter target = PlayerRegistry.Nearest(transform.position);
            if (target == null)
            {
                Drift();
                return;
            }

            Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
            float distance = toTarget.magnitude;

            if (distance <= _pickupRadius)
            {
                Collect();
                return;
            }

            bool attracted = distance <= _magnetRadius ||
                             (_forceCollectAfter > 0f && _age >= _forceCollectAfter);

            if (attracted)
            {
                // Gia tốc chứ không đặt thẳng tốc độ: Hồn cong đường bay lại theo người chơi
                // đang chạy, nên đuôi Hồn kéo dài phía sau thành một vệt. Đặt thẳng tốc độ
                // thì mọi Hồn bay theo đường nối thẳng và chồng khít lên nhau.
                _velocity += toTarget / distance * (_magnetAcceleration * Time.deltaTime);

                float maxSqr = _maxSpeed * _maxSpeed;
                if (_velocity.sqrMagnitude > maxSqr) _velocity = _velocity.normalized * _maxSpeed;
            }
            else
            {
                ApplyDrag();
            }

            transform.position += (Vector3)(_velocity * Time.deltaTime);
        }

        private void Drift()
        {
            ApplyDrag();
            transform.position += (Vector3)(_velocity * Time.deltaTime);
        }

        private void ApplyDrag()
        {
            _velocity *= Mathf.Max(1f - _scatterDrag * Time.deltaTime, 0f);
        }

        private void Collect()
        {
            _collected = true;

            if (_sound != null) _sound.Advance();

            // Cả hai máy đều chạy tới đây, nhưng chỉ host được cộng vào bộ đếm của ván.
            if (NetworkServer.active && RunManager.Instance != null)
                RunManager.Instance.ReportSoulCollected();

            if (_owner != null) _owner.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
