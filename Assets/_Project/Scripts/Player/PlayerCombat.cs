using UnityEngine;

namespace Eclipse.Player
{
    /// <summary>
    /// Rending Strike: hit-detection + Ярость.
    /// Слушает OnAttackActive от PlayerController.
    /// CDD 2.2, 6.2.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        PlayerController player;
        PlayerTuning tuning;

        float rage;
        float timeSinceCombat;

        public float Rage => rage;
        public float RageMax => tuning != null ? tuning.rageMax : 100f;
        public float RageNormalized => RageMax > 0f ? rage / RageMax : 0f;

        static readonly Collider[] HitBuffer = new Collider[16];

        void Awake()
        {
            player = GetComponent<PlayerController>();
            tuning = player.Tuning;
        }

        void OnEnable()
        {
            if (player != null)
                player.OnAttackActive += HandleAttackActive;
        }

        void OnDisable()
        {
            if (player != null)
                player.OnAttackActive -= HandleAttackActive;
        }

        void Update()
        {
            if (tuning == null) return;

            timeSinceCombat += Time.deltaTime;
            if (timeSinceCombat >= tuning.rageDecayDelay && rage > 0f)
            {
                rage = Mathf.Max(0f, rage - tuning.rageDecayPerSec * Time.deltaTime);
            }
        }

        void HandleAttackActive(int comboIndex)
        {
            if (tuning == null) tuning = player.Tuning;

            timeSinceCombat = 0f;

            float radius = player.GetHitRadius();
            float range = player.GetHitRange();
            Vector3 origin = player.GetHitOrigin(range);
            int poise = player.GetCurrentPoise();

            int count = Physics.OverlapSphereNonAlloc(origin, radius, HitBuffer);
            int hits = 0;

            for (int i = 0; i < count; i++)
            {
                Collider col = HitBuffer[i];
                if (col == null || col.transform == transform || col.transform.IsChildOf(transform))
                    continue;

                hits++;
                Debug.Log($"[Combat] Rending #{comboIndex} hit → {col.name}  Poise {poise}");
            }

            float gain = comboIndex >= 3 ? tuning.ragePerHeavyHit : tuning.ragePerLightHit;
            rage = Mathf.Min(tuning.rageMax, rage + gain);

            if (hits == 0)
                Debug.Log($"[Combat] Rending #{comboIndex} — промах. Ярость +{gain:0} → {rage:0}");
        }

        void OnDrawGizmosSelected()
        {
            if (player == null || !Application.isPlaying) return;
            if (player.State != PlayerState.AttackActive) return;

            float radius = player.GetHitRadius();
            float range = player.GetHitRange();
            Vector3 origin = player.GetHitOrigin(range);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(origin, radius);
        }
    }
}
