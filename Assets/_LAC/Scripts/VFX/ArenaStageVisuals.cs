using System;
using LAC.Core;
using UnityEngine;

namespace LAC.VFX
{
    /// <summary>Trang trí cục bộ theo đợt đã đồng bộ, không thay đổi sân hay va chạm.</summary>
    public sealed class ArenaStageVisuals : MonoBehaviour
    {
        [Serializable]
        private sealed class Stage
        {
            [SerializeField, Min(1)] private int _firstWave = 1;
            [SerializeField] private GameObject _root;
            public int FirstWave => _firstWave;
            public GameObject Root => _root;
        }

        [SerializeField] private Stage[] _stages;
        private RunManager _run;
        private int _current = -1;
        public int CurrentStage => _current;

        private void Start()
        {
            _run = RunManager.Instance;
            Refresh(1);
        }

        private void Update()
        {
            if (_run != null) Refresh(Mathf.Max(1, _run.CurrentWave));
        }

        private void Refresh(int wave)
        {
            if (_stages == null || _stages.Length == 0) return;
            int next = 0;
            for (int i = 1; i < _stages.Length; i++)
                if (wave >= _stages[i].FirstWave) next = i;
            if (next == _current) return;
            _current = next;
            for (int i = 0; i < _stages.Length; i++)
                if (_stages[i].Root != null) _stages[i].Root.SetActive(i == next);
        }
    }
}
