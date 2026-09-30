using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAC.Drum
{
    /// <summary>Chỉ gắn lên đạn ĐỊCH. Bộ bắn đăng ký callback trả về pool khi Launch.</summary>
    public sealed class EnemyProjectileClearable : MonoBehaviour
    {
        private static readonly List<EnemyProjectileClearable> Active = new List<EnemyProjectileClearable>(256);
        private Action _release;
        public void BindRelease(Action release) => _release = release;
        private void OnEnable() => Active.Add(this);
        private void OnDisable() { Active.Remove(this); _release = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();
        public static void ClearAll()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var shot = Active[i];
                if (shot == null) { Active.RemoveAt(i); continue; }
                if (shot._release != null) shot._release();
                else shot.gameObject.SetActive(false);
            }
        }
    }
}
