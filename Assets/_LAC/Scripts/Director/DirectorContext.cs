using UnityEngine;

namespace LAC.Director
{
    public readonly struct DirectorContext
    {
        public bool IsValid { get; }
        public float HealthMin { get; }
        public float DamageRatioMaxRaw { get; }
        public float DamageRatioMax => Mathf.Clamp01(DamageRatioMaxRaw);
        public int Downs { get; }
        public int ConnectedPlayers { get; }
        public int Participants { get; }
        public int LateJoiners { get; }
        public string MissingReason { get; }

        public DirectorContext(bool isValid, float healthMin, float damageRatioMaxRaw,
            int downs, int connectedPlayers, int participants, int lateJoiners = 0, string missingReason = null)
        {
            IsValid = isValid;
            HealthMin = healthMin;
            DamageRatioMaxRaw = damageRatioMaxRaw;
            Downs = downs;
            ConnectedPlayers = connectedPlayers;
            Participants = participants;
            LateJoiners = lateJoiners;
            MissingReason = missingReason ?? string.Empty;
        }

        public static DirectorContext Missing(string reason) => new DirectorContext(false, 0f, 0f, 0, 0, 0, 0, reason);
    }
}
