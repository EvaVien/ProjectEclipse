using UnityEngine;

namespace Eclipse.Player
{
    /// <summary>
    /// Настройки игрока. Числа из CDD v0.1 (разд. 1.4, 2.2, 6.2).
    /// Меняй в Inspector, в том числе в Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Eclipse/Player Tuning", fileName = "PlayerTuning")]
    public class PlayerTuning : ScriptableObject
    {
        [Header("Движение")]
        public float moveSpeed = 6f;
        public float turnSpeed = 1080f;

        [Header("Уклонение (CDD 1.4)")]
        public int maxCharges = 2;
        public float chargeRecharge = 3.5f;
        public float dodgeDistance = 4.5f;
        public float dodgeStartup = 0.05f;
        public float dodgeActive = 0.25f;
        public float dodgeRecovery = 0.15f;

        [Header("Буфер ввода (CDD 1.2)")]
        public float inputBuffer = 0.15f;

        [Header("Ярость — Воин (CDD 2.2)")]
        public float rageMax = 100f;
        public float ragePerLightHit = 4f;
        public float ragePerHeavyHit = 8f;
        public float rageDecayDelay = 4f;
        public float rageDecayPerSec = 5f;

        [Header("Rending Strike — лёгкий удар (CDD 1.4 / 6.2)")]
        public float lightAnticipation = 0.10f;
        public float lightActive = 0.06f;
        public float lightRecovery = 0.14f;
        public float lightHitRadius = 1.8f;
        public float lightHitRange = 2.2f;
        public int lightPoise = 10;

        [Header("Rending Strike — тяжёлый (3-й) удар")]
        public float heavyAnticipation = 0.30f;
        public float heavyActive = 0.10f;
        public float heavyRecovery = 0.30f;
        public float heavyHitRadius = 2.2f;
        public float heavyHitRange = 2.5f;
        public int heavyPoise = 30;

        [Header("Комбо")]
        public int maxComboHits = 3;
        public float comboResetTime = 0.4f; // после recovery, если нет ввода
    }
}
