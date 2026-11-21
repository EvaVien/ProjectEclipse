using UnityEngine;

namespace Eclipse.Player
{
    /// <summary>
    /// Настройки движения и уклонения игрока. Значения из Combat Design Document v0.1, разд. 1.4.
    /// Меняй числа прямо в Inspector, в том числе во время Play: изменения ассета сохраняются.
    /// </summary>
    [CreateAssetMenu(menuName = "Eclipse/Player Tuning", fileName = "PlayerTuning")]
    public class PlayerTuning : ScriptableObject
    {
        [Header("Движение")]
        public float moveSpeed = 6f;          // м/с
        public float turnSpeed = 1080f;       // градусов в секунду

        [Header("Уклонение (CDD 1.4)")]
        public int maxCharges = 2;            // заряды уклонения
        public float chargeRecharge = 3.5f;   // секунд на восстановление одного заряда
        public float dodgeDistance = 4.5f;    // метров
        public float dodgeStartup = 0.05f;    // подготовка
        public float dodgeActive = 0.25f;     // рывок = окно i-frames
        public float dodgeRecovery = 0.15f;   // восстановление
    }
}
