using UnityEngine;

namespace LAC.Director
{
    /// <summary>Lời và ý đồ của Chằn Tinh, biểu diễn quyết định host đã chốt.</summary>
    [CreateAssetMenu(fileName = "DungeonMaster", menuName = "LAC/Dungeon Master")]
    public sealed class DungeonMasterData : ScriptableObject
    {
        [SerializeField] private string _speaker = "Chằn Tinh";
        [Header("Ý đồ và lời thoại")]
        [SerializeField] private string _openingIntent = "Thăm dò";
        [SerializeField, TextArea] private string _openingLine = "Bước vào lãnh địa của ta rồi sao? Hãy thử sống sót!";
        [SerializeField] private string _balancedIntent = "Điều binh";
        [SerializeField, TextArea] private string _balancedLine = "Giữ đội hình! Để xem các ngươi trụ được bao lâu.";
        [SerializeField] private string _reliefIntent = "Giãn binh";
        [SerializeField, TextArea] private string _reliefLine = "Chậm bước! Ta muốn cuộc săn này kéo dài.";
        [SerializeField] private string _varietyIntent = "Đổi thế vây";
        [SerializeField, TextArea] private string _varietyLine = "Đổi hướng bao vây! Đừng để chúng quen đường tiến quân.";
        [SerializeField] private string _bossIntent = "Đối đầu";
        [SerializeField, TextArea] private string _bossLine = "Đủ rồi. Lần này ta sẽ tự tay kết liễu các ngươi!";
        [SerializeField] private string _fixedIntent = "Kế hoạch cố định";
        [SerializeField, TextArea] private string _fixedLine = "Cứ theo kế hoạch. Không đổi nhịp điều binh.";
        [SerializeField] private string _fallbackIntent = "Giữ kế hoạch";
        [SerializeField, TextArea] private string _fallbackLine = "Giữ đội hình cũ. Chờ lệnh tiếp theo.";
        [SerializeField] private string _haltedIntent = "Dừng điều binh";
        [SerializeField, TextArea] private string _haltedLine = "Lệnh điều binh tạm dừng.";
        [Header("Màn chọn chế độ")]
        [SerializeField, TextArea] private string _adaptiveDescription =
            "Chằn Tinh quan sát cả đội và đổi cách điều binh. Vào host dùng chế độ của host.";
        [SerializeField, TextArea] private string _fixedDescription =
            "Chằn Tinh dùng kế hoạch cố định theo bảng đợt. Vào host dùng chế độ của host.";

        public string Speaker => _speaker;
        public string ModeDescription(DirectorMode mode) =>
            mode == DirectorMode.Adaptive ? _adaptiveDescription : _fixedDescription;

        public DungeonMasterCue Describe(DirectorMode mode, DirectorState state, int wave,
            bool finalWave, string source, string fallback)
        {
            if (source == "invalid_baseline") return Cue(_haltedIntent, _haltedLine);
            if (finalWave) return Cue(_bossIntent, _bossLine);
            if (wave == 1) return Cue(_openingIntent, _openingLine);
            if (mode == DirectorMode.Fixed) return Cue(_fixedIntent, _fixedLine);
            if (!string.IsNullOrEmpty(fallback) || source == "fixed_fallback")
                return Cue(_fallbackIntent, _fallbackLine);
            switch (state)
            {
                case DirectorState.Relief: return Cue(_reliefIntent, _reliefLine);
                case DirectorState.Variety: return Cue(_varietyIntent, _varietyLine);
                default: return Cue(_balancedIntent, _balancedLine);
            }
        }

        private DungeonMasterCue Cue(string intent, string line) =>
            new DungeonMasterCue((_speaker + " · " + intent).ToUpperInvariant(), intent, line);
    }

    public readonly struct DungeonMasterCue
    {
        public string Title { get; }
        public string Intent { get; }
        public string Line { get; }

        public DungeonMasterCue(string title, string intent, string line)
        { Title = title; Intent = intent; Line = line; }
    }
}
