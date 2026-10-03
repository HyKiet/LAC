using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LAC.Cards
{
    /// <summary>Visual-only interaction that keeps animating while card selection pauses gameplay.</summary>
    public sealed class CardHoverVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public const float ConsumeDuration = 1.05f;

        private const float FlightStart = 0.42f;
        private const float RevealDuration = 0.22f;
        private static readonly Color IdleGold = new Color(0.753f, 0.553f, 0.125f, 1f);
        private static readonly Color HoverBlue = new Color(0.184f, 0.455f, 0.502f, 1f);

        private RectTransform _rect;
        private CardFrameGraphic _frame;
        private Image _surface;
        private Image[] _accentImages;
        private CanvasGroup _group;
        private Action<CardHoverVisual> _requestHighlight;
        private Vector2 _restPosition;
        private Vector2 _consumeStart;
        private Transform _consumeTarget;
        private Camera _consumeCamera;
        private RectTransform _consumeParent;
        private float _consumeElapsed;
        private float _revealElapsed = -1f;
        private float _revealDelay;
        private bool _highlighted;
        private bool _dimmed;
        private bool _consuming;
        private bool _interactable = true;

        public void Configure(CardFrameGraphic frame, Image surface, Image[] accentImages,
            Action<CardHoverVisual> requestHighlight)
        {
            _rect = (RectTransform)transform;
            _frame = frame;
            _surface = surface;
            _accentImages = accentImages;
            _requestHighlight = requestHighlight;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _restPosition = _rect.anchoredPosition;
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            _highlighted = false;
            _dimmed = false;
            _consuming = false;
            _consumeTarget = null;
            _consumeCamera = null;
            _consumeParent = null;
            _consumeElapsed = 0f;
            _revealElapsed = -1f;
            _interactable = true;
            if (_rect == null) return;
            _rect.anchoredPosition = _restPosition;
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;
            _group.alpha = 1f;
            ApplyColors(false);
        }

        public void PlayReveal(float delay)
        {
            _revealDelay = Mathf.Max(0f, delay);
            _revealElapsed = 0f;
            _group.alpha = 0f;
        }

        public void SetHighlighted(bool highlighted)
        {
            bool next = _interactable && highlighted;
            if (_highlighted == next) return;
            _highlighted = next;
            ApplyColors(IsActive);
        }

        public void SetDimmed(bool dimmed)
        {
            if (_dimmed == dimmed) return;
            _dimmed = dimmed;
            if (dimmed)
            {
                _highlighted = false;
            }
            ApplyColors(IsActive);
        }

        public void PlayConsume(Transform target)
        {
            _interactable = false;
            _highlighted = false;
            _dimmed = false;
            _consuming = true;
            _revealElapsed = -1f;
            _consumeTarget = target;
            _consumeCamera = Camera.main;
            _consumeParent = _rect.parent as RectTransform;
            _consumeElapsed = 0f;
            _consumeStart = _rect.anchoredPosition;
            transform.SetAsLastSibling();
            ApplyColors(true);
            _frame.PlayGlint();
        }

        public void SetInteractable(bool interactable)
        {
            if (_interactable == interactable) return;
            _interactable = interactable;
            if (!interactable)
            {
                _highlighted = false;
            }
            ApplyColors(IsActive);
        }

        private bool IsActive => _interactable && _highlighted;

        private void Update()
        {
            if (_rect == null) return;
            if (_consuming)
            {
                UpdateConsume();
                return;
            }

            if (_revealElapsed >= 0f && !_dimmed)
            {
                float before = _revealElapsed;
                _revealElapsed += Time.unscaledDeltaTime;
                if (before <= _revealDelay && _revealElapsed > _revealDelay) _frame.PlayGlint();
                float t = Mathf.Clamp01((_revealElapsed - _revealDelay) / RevealDuration);
                _group.alpha = Mathf.SmoothStep(0f, 1f, t);
                if (t >= 1f) _revealElapsed = -1f;
                return;
            }

            float targetAlpha = _dimmed ? 0.24f : 1f;
            if (_group.alpha == targetAlpha) return;
            _group.alpha = Mathf.MoveTowards(_group.alpha, targetAlpha, Time.unscaledDeltaTime * 4f);
        }

        private void UpdateConsume()
        {
            if (_consumeElapsed >= ConsumeDuration) return;
            _consumeElapsed = Mathf.Min(ConsumeDuration, _consumeElapsed + Time.unscaledDeltaTime);
            if (_consumeElapsed < FlightStart)
            {
                _rect.anchoredPosition = _consumeStart;
                // Giữ tên/chỉ số đọc được trong nhịp xác nhận trước khi thẻ bay về nhân vật.
                _rect.localRotation = Quaternion.identity;
                _rect.localScale = Vector3.one;
                _group.alpha = 1f;
            }
            else
            {
                float flyT = Mathf.Clamp01((_consumeElapsed - FlightStart) /
                    (ConsumeDuration - FlightStart));
                float easedFly = flyT * flyT * (3f - 2f * flyT);
                _rect.localRotation = Quaternion.identity;
                _rect.anchoredPosition = Vector2.LerpUnclamped(_consumeStart,
                    ResolveTargetPosition(), easedFly) + Vector2.up * Mathf.Sin(flyT * Mathf.PI) * 64f;
                _rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.04f, easedFly);
                _group.alpha = 1f - Mathf.Clamp01((flyT - 0.76f) / 0.24f);
            }
        }

        private Vector2 ResolveTargetPosition()
        {
            if (_consumeTarget == null || _consumeCamera == null || _consumeParent == null)
                return new Vector2(0f, -320f);

            Vector2 screenPoint = _consumeCamera.WorldToScreenPoint(_consumeTarget.position);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_consumeParent, screenPoint, null,
                out Vector2 localPoint) ? localPoint : new Vector2(0f, -320f);
        }

        private void ApplyColors(bool active)
        {
            if (_frame == null || _surface == null) return;
            Color stateColor = active || _consuming ? HoverBlue : IdleGold;
            _frame.SetHighlighted(active || _consuming);
            _surface.color = active || _consuming
                ? new Color(0.067f, 0.18f, 0.243f, 1f)
                : new Color(0.082f, 0.075f, 0.059f, 1f);

            if (_accentImages == null) return;
            for (int i = 0; i < _accentImages.Length; i++)
            {
                if (_accentImages[i] == null) continue;
                Color accent = stateColor;
                accent.a = _dimmed ? 0.18f : active || _consuming ? 0.92f : 0.48f;
                _accentImages[i].color = accent;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable) return;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);
            _requestHighlight?.Invoke(this);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlighted(false);
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable) return;
            _frame.PlayGlint();
        }
        public void OnPointerUp(PointerEventData eventData) { }
        public void OnSelect(BaseEventData eventData) { if (_interactable) _requestHighlight?.Invoke(this); }
        public void OnDeselect(BaseEventData eventData)
        {
            if (_highlighted) SetHighlighted(false);
        }
    }
}
