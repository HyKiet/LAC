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

        private const float SpinDuration = 0.38f;
        private const float FlightStart = 0.42f;
        private static readonly Color IdleGold = new Color(0.753f, 0.553f, 0.125f, 1f);
        private static readonly Color HoverBlue = new Color(0.184f, 0.455f, 0.502f, 1f);
        private static readonly Color HoverBlueLight = new Color(0.612f, 0.812f, 0.753f, 1f);

        private RectTransform _rect;
        private Image _glow;
        private Image _border;
        private Image _surface;
        private Image[] _accentImages;
        private CanvasGroup _group;
        private Action<CardHoverVisual> _requestHighlight;
        private Vector2 _restPosition;
        private Vector2 _consumeStart;
        private Transform _consumeTarget;
        private float _heartbeatPhase;
        private float _consumeElapsed;
        private bool _highlighted;
        private bool _pressed;
        private bool _dimmed;
        private bool _consuming;
        private bool _interactable = true;

        public void Configure(Image glow, Image border, Image surface, Image[] accentImages,
            float heartbeatPhase, Action<CardHoverVisual> requestHighlight)
        {
            _rect = (RectTransform)transform;
            _glow = glow;
            _border = border;
            _surface = surface;
            _accentImages = accentImages;
            _requestHighlight = requestHighlight;
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _heartbeatPhase = heartbeatPhase;
            _restPosition = _rect.anchoredPosition;
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            _highlighted = false;
            _pressed = false;
            _dimmed = false;
            _consuming = false;
            _consumeTarget = null;
            _consumeElapsed = 0f;
            _interactable = true;
            if (_rect == null) return;
            _rect.anchoredPosition = _restPosition;
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;
            _group.alpha = 1f;
            ApplyColors(false, 0f);
        }

        public void SetHighlighted(bool highlighted)
        {
            _highlighted = _interactable && highlighted;
            if (!_highlighted) _pressed = false;
            ApplyColors(IsActive, 0f);
        }

        public void SetDimmed(bool dimmed)
        {
            _dimmed = dimmed;
            if (dimmed)
            {
                _highlighted = false;
                _pressed = false;
            }
        }

        public void PlayConsume(Transform target)
        {
            _interactable = false;
            _highlighted = false;
            _pressed = false;
            _dimmed = false;
            _consuming = true;
            _consumeTarget = target;
            _consumeElapsed = 0f;
            _consumeStart = _rect.anchoredPosition;
            transform.SetAsLastSibling();
            ApplyColors(true, 1f);
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (!interactable)
            {
                _highlighted = false;
                _pressed = false;
            }
            ApplyColors(IsActive, 0f);
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

            bool active = IsActive;
            float beat = Heartbeat();
            float lift = active ? 13f + beat * 1.5f : beat * 1.5f;
            float scale = _pressed ? 1.025f : active ? 1.045f + beat * 0.012f : 1f + beat * 0.018f;
            float blend = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition,
                _restPosition + Vector2.up * lift, blend);
            _rect.localScale = Vector3.Lerp(_rect.localScale, Vector3.one * scale, blend);
            _rect.localRotation = Quaternion.Slerp(_rect.localRotation, Quaternion.identity, blend);
            _group.alpha = Mathf.MoveTowards(_group.alpha, _dimmed ? 0.24f : 1f,
                Time.unscaledDeltaTime * 4f);
            ApplyColors(active, beat);
        }

        private void UpdateConsume()
        {
            _consumeElapsed += Time.unscaledDeltaTime;
            float spinT = Mathf.Clamp01(_consumeElapsed / SpinDuration);
            float easedSpin = 1f - Mathf.Pow(1f - spinT, 3f);
            if (_consumeElapsed < FlightStart)
            {
                _rect.anchoredPosition = _consumeStart;
                _rect.localRotation = Quaternion.Euler(0f, 0f, easedSpin * 360f);
                _rect.localScale = Vector3.one * (1f + Mathf.Sin(spinT * Mathf.PI) * 0.12f);
                _group.alpha = 1f;
            }
            else
            {
                float flyT = Mathf.Clamp01((_consumeElapsed - FlightStart) /
                    (ConsumeDuration - FlightStart));
                float easedFly = flyT * flyT * (3f - 2f * flyT);
                _rect.localRotation = Quaternion.identity;
                _rect.anchoredPosition = Vector2.LerpUnclamped(_consumeStart,
                    ResolveTargetPosition(), easedFly);
                _rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.04f, easedFly);
                _group.alpha = 1f - Mathf.Clamp01((flyT - 0.76f) / 0.24f);
            }
            ApplyColors(true, 1f);
        }

        private Vector2 ResolveTargetPosition()
        {
            if (_consumeTarget == null || Camera.main == null || _rect.parent is not RectTransform parent)
                return new Vector2(0f, -320f);

            Vector2 screenPoint = Camera.main.WorldToScreenPoint(_consumeTarget.position);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null,
                out Vector2 localPoint) ? localPoint : new Vector2(0f, -320f);
        }

        private float Heartbeat()
        {
            float cycle = Mathf.Repeat(Time.unscaledTime * 0.82f + _heartbeatPhase, 1f);
            float first = Mathf.Exp(-Mathf.Pow((cycle - 0.10f) / 0.055f, 2f));
            float second = 0.55f * Mathf.Exp(-Mathf.Pow((cycle - 0.25f) / 0.075f, 2f));
            return Mathf.Clamp01(first + second);
        }

        private void ApplyColors(bool active, float beat)
        {
            if (_glow == null || _border == null || _surface == null) return;

            Color stateColor = active || _consuming ? HoverBlue : IdleGold;
            Color glow = stateColor;
            glow.a = _dimmed ? 0.05f : active || _consuming ? 0.72f : 0.16f + beat * 0.20f;
            _glow.color = glow;

            Color border = _consuming ? HoverBlueLight : stateColor;
            border.a = _dimmed ? 0.22f : active || _consuming ? 1f : 0.90f;
            _border.color = border;
            _surface.color = active || _consuming
                ? new Color(0.055f, 0.13f, 0.16f, 1f)
                : new Color(0.055f, 0.07f, 0.075f, 1f);

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
            _pressed = false;
            SetHighlighted(false);
        }
        public void OnPointerDown(PointerEventData eventData) { if (_interactable) _pressed = true; }
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
        public void OnSelect(BaseEventData eventData) { if (_interactable) _requestHighlight?.Invoke(this); }
        public void OnDeselect(BaseEventData eventData)
        {
            if (_highlighted) SetHighlighted(false);
        }
    }
}
