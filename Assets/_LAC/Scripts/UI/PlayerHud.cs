using System.Collections.Generic;
using LAC.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LAC.UI
{
    /// <summary>
    /// Thanh máu của người chơi ngồi trước máy này.
    /// </summary>
    /// <remarks>
    /// Hiển thị bằng các ô rời chứ không bằng một thanh liền. Ba nhân vật có 4, 6 và 10 máu —
    /// những con số nhỏ, và mỗi điểm máu là một quyết định. Thanh liền biến chúng thành một
    /// tỉ lệ phần trăm mờ nhạt; ô rời cho người chơi <b>đếm</b> được mình còn chịu được mấy
    /// đòn nữa, thứ họ thực sự cần biết khi đang bị vây.
    ///
    /// Số ô được dựng lúc chạy theo <c>MaxHealth</c> của nhân vật, nên không phải sửa giao
    /// diện mỗi lần đổi nhân vật hay mỗi lần cân bằng lại lượng máu.
    ///
    /// Chỉ theo dõi nhân vật cục bộ. Trong co-op mỗi máy một màn hình, nên máu của đồng đội
    /// thuộc về phần giao diện của T-20 chứ không nằm ở đây.
    /// </remarks>
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] private RectTransform _pipRow;
        [SerializeField] private Sprite _pipFull;
        [SerializeField] private Sprite _pipEmpty;

        [SerializeField] private Color _fullColor = new Color(0.92f, 0.35f, 0.35f);
        [SerializeField] private Color _emptyColor = new Color(0.32f, 0.22f, 0.26f);

        [SerializeField, Min(8f)] private float _pipSize = 26f;
        [SerializeField, Min(0f)] private float _pipSpacing = 4f;

        private readonly List<Image> _pips = new List<Image>(12);
        private PlayerHealth _tracked;

        private void Update()
        {
            if (_tracked != null) return;

            PlayerHealth found = FindLocalHealth();
            if (found == null) return;

            _tracked = found;
            _tracked.HealthChanged += OnHealthChanged;
            Build(_tracked.MaxHealth);
            Refresh(_tracked.Health);
        }

        private void OnDestroy()
        {
            if (_tracked != null) _tracked.HealthChanged -= OnHealthChanged;
        }

        private static PlayerHealth FindLocalHealth()
        {
            for (int i = 0; i < PlayerRegistry.Count; i++)
            {
                PlayerCharacter player = PlayerRegistry.All[i];
                if (player == null || !player.isLocalPlayer) continue;

                // Máu tối đa chỉ đúng sau khi SyncVar tới nơi; trước đó nó là giá trị mặc
                // định và thanh máu sẽ được dựng với số ô sai.
                if (player.TryGetComponent(out PlayerHealth health) && health.MaxHealth > 1)
                    return health;
            }

            return null;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (_pips.Count != max) Build(max);
            Refresh(current);
        }

        private void Build(int max)
        {
            for (int i = _pips.Count; i < max; i++)
                _pips.Add(CreatePip(i));

            for (int i = 0; i < _pips.Count; i++)
                _pips[i].gameObject.SetActive(i < max);
        }

        private Image CreatePip(int index)
        {
            var go = new GameObject($"Pip_{index}", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_pipRow, worldPositionStays: false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(_pipSize, _pipSize);
            rect.anchoredPosition = new Vector2(index * (_pipSize + _pipSpacing), 0f);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void Refresh(int current)
        {
            for (int i = 0; i < _pips.Count; i++)
            {
                bool filled = i < current;
                _pips[i].sprite = filled ? _pipFull : _pipEmpty;
                _pips[i].color = filled ? _fullColor : _emptyColor;
            }
        }
    }
}
