using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Các mảnh cartoon và nhịp đi; chỉ biểu diễn, không đổi vật lý/chỉ số.</summary>
    [CreateAssetMenu(fileName = "CartoonCharacter", menuName = "LAC/Cartoon Character")]
    public sealed class CartoonCharacterData : ScriptableObject
    {
        [SerializeField] private Sprite _body;
        [SerializeField] private Sprite _leftFoot;
        [SerializeField] private Sprite _rightFoot;
        [SerializeField] private Sprite _leftHand;
        [SerializeField] private Sprite _rightHand;
        [SerializeField] private Vector2 _leftFootOffset = new Vector2(-.18f, -.34f);
        [SerializeField] private Vector2 _rightFootOffset = new Vector2(.18f, -.34f);
        [SerializeField] private Vector2 _freeHandOffset = new Vector2(-.32f, -.03f);
        [SerializeField, Min(.1f)] private float _strideDistance = 1.8f;
        [SerializeField, Min(0f)] private float _footSwing = .07f;
        [SerializeField, Min(0f)] private float _footLift = .055f;
        [SerializeField, Min(0f)] private float _bodyBob = .02f;
        [SerializeField] private float _leanDegrees = 3f;
        [SerializeField] private Vector2 _downOffset = new Vector2(0f, -.18f);
        [SerializeField] private float _downAngle = -75f;

        public Sprite Body => _body;
        public Sprite LeftFoot => _leftFoot;
        public Sprite RightFoot => _rightFoot;
        public Sprite LeftHand => _leftHand;
        public Sprite RightHand => _rightHand;
        public Vector2 LeftFootOffset => _leftFootOffset;
        public Vector2 RightFootOffset => _rightFootOffset;
        public Vector2 FreeHandOffset => _freeHandOffset;
        public float StrideDistance => _strideDistance;
        public float FootSwing => _footSwing;
        public float FootLift => _footLift;
        public float BodyBob => _bodyBob;
        public float LeanDegrees => _leanDegrees;
        public Vector2 DownOffset => _downOffset;
        public float DownAngle => _downAngle;
    }
}
