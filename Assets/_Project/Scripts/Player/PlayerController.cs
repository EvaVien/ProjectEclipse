using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Player
{
    public enum PlayerState
    {
        Free,
        DodgeStartup, DodgeActive, DodgeRecovery,
        AttackAnticipation, AttackActive, AttackRecovery
    }

    /// <summary>
    /// Движение, прицел, уклонение и Rending Strike (ЛКМ).
    /// Буфер ввода 150 мс, приоритет: уклонение > атака.
    /// CDD 1.2–1.4, 6.2.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerTuning tuning;
        [SerializeField] Camera cam;

        CharacterController cc;
        Vector3 dodgeDir;
        float stateTimer;
        float verticalVelocity;
        float dodgeBuffer;
        float attackBuffer;
        float rechargeTimer;
        float comboResetTimer;

        int comboIndex; // 0 = нет комбо, 1..3 = текущий удар
        bool hitThisActive; // чтобы hit вызывался один раз за active-окно

        public PlayerState State { get; private set; }
        public int Charges { get; private set; }
        public int MaxCharges => tuning != null ? tuning.maxCharges : 0;
        public bool IsInvulnerable => State == PlayerState.DodgeActive;
        public int ComboIndex => comboIndex;
        public float RechargeProgress
        {
            get
            {
                if (tuning == null || Charges >= tuning.maxCharges) return 1f;
                return Mathf.Clamp01(rechargeTimer / tuning.chargeRecharge);
            }
        }

        // Вызывается из PlayerCombat в момент Active
        public event System.Action<int> OnAttackActive; // comboIndex 1..3

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (tuning == null) tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            Charges = tuning.maxCharges;
        }

        void Update()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }

            float dt = Time.deltaTime;
            float buffer = tuning.inputBuffer;

            TickCharges(dt);
            TickComboReset(dt);

            // Буферы ввода
            if (ReadDodgePressed()) dodgeBuffer = buffer;
            else dodgeBuffer = Mathf.Max(0f, dodgeBuffer - dt);

            if (ReadAttackPressed()) attackBuffer = buffer;
            else attackBuffer = Mathf.Max(0f, attackBuffer - dt);

            Vector3 horizontal = Vector3.zero;

            switch (State)
            {
                case PlayerState.Free:
                {
                    UpdateAim(dt);
                    Vector3 move = InputToWorld(ReadMove());
                    horizontal = move * tuning.moveSpeed;

                    // Приоритет: уклонение > атака
                    if (dodgeBuffer > 0f && Charges > 0)
                    {
                        StartDodge(move);
                        horizontal = Vector3.zero;
                    }
                    else if (attackBuffer > 0f)
                    {
                        StartAttack(1);
                        horizontal = Vector3.zero;
                    }
                    break;
                }

                case PlayerState.DodgeStartup:
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                        EnterState(PlayerState.DodgeActive, tuning.dodgeActive);
                    break;

                case PlayerState.DodgeActive:
                {
                    float speed = tuning.dodgeDistance / Mathf.Max(0.01f, tuning.dodgeActive);
                    horizontal = dodgeDir * speed;
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                        EnterState(PlayerState.DodgeRecovery, tuning.dodgeRecovery);
                    break;
                }

                case PlayerState.DodgeRecovery:
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        EnterState(PlayerState.Free, 0f);
                        if (attackBuffer > 0f)
                            StartAttack(1);
                    }
                    break;

                case PlayerState.AttackAnticipation:
                    UpdateAim(dt);
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        hitThisActive = false;
                        float active = IsHeavyHit() ? tuning.heavyActive : tuning.lightActive;
                        EnterState(PlayerState.AttackActive, active);
                    }
                    if (dodgeBuffer > 0f && Charges > 0)
                    {
                        Vector3 move = InputToWorld(ReadMove());
                        StartDodge(move);
                    }
                    break;

                case PlayerState.AttackActive:
                {
                    stateTimer -= dt;
                    if (!hitThisActive)
                    {
                        hitThisActive = true;
                        OnAttackActive?.Invoke(comboIndex);
                    }
                    if (stateTimer <= 0f)
                    {
                        float rec = IsHeavyHit() ? tuning.heavyRecovery : tuning.lightRecovery;
                        EnterState(PlayerState.AttackRecovery, rec);
                    }
                    if (dodgeBuffer > 0f && Charges > 0)
                    {
                        Vector3 move = InputToWorld(ReadMove());
                        StartDodge(move);
                    }
                    break;
                }

                case PlayerState.AttackRecovery:
                    stateTimer -= dt;
                    if (dodgeBuffer > 0f && Charges > 0)
                    {
                        Vector3 move = InputToWorld(ReadMove());
                        StartDodge(move);
                        break;
                    }
                    if (attackBuffer > 0f && comboIndex < tuning.maxComboHits)
                    {
                        StartAttack(comboIndex + 1);
                        break;
                    }
                    if (stateTimer <= 0f)
                    {
                        comboResetTimer = tuning.comboResetTime;
                        EnterState(PlayerState.Free, 0f);
                    }
                    break;
            }

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * dt;
            cc.Move((horizontal + Vector3.up * verticalVelocity) * dt);
        }

        bool IsHeavyHit() => comboIndex >= 3;

        void StartAttack(int hit)
        {
            comboIndex = hit;
            attackBuffer = 0f;
            comboResetTimer = 0f;
            float ant = IsHeavyHit() ? tuning.heavyAnticipation : tuning.lightAnticipation;
            EnterState(PlayerState.AttackAnticipation, ant);
        }

        void StartDodge(Vector3 move)
        {
            dodgeDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
            dodgeDir.y = 0f;
            Charges--;
            dodgeBuffer = 0f;
            comboIndex = 0;
            EnterState(PlayerState.DodgeStartup, tuning.dodgeStartup);
        }

        void EnterState(PlayerState next, float duration)
        {
            State = next;
            stateTimer = duration;
        }

        void TickCharges(float dt)
        {
            Charges = Mathf.Min(Charges, tuning.maxCharges);
            if (Charges >= tuning.maxCharges)
            {
                rechargeTimer = 0f;
                return;
            }
            rechargeTimer += dt;
            if (rechargeTimer >= tuning.chargeRecharge)
            {
                Charges++;
                rechargeTimer = 0f;
            }
        }

        void TickComboReset(float dt)
        {
            if (comboIndex == 0 || State != PlayerState.Free) return;
            comboResetTimer -= dt;
            if (comboResetTimer <= 0f) comboIndex = 0;
        }

        // ---------- Ввод ----------

        Vector2 ReadMove()
        {
            Vector2 v = Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed) v.y -= 1f;
                if (kb.dKey.isPressed) v.x += 1f;
                if (kb.aKey.isPressed) v.x -= 1f;
            }
            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 s = pad.leftStick.ReadValue();
                if (s.sqrMagnitude > 0.04f) v += s;
            }
            return Vector2.ClampMagnitude(v, 1f);
        }

        bool ReadDodgePressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
            Gamepad pad = Gamepad.current;
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;
            return false;
        }

        bool ReadAttackPressed()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            Gamepad pad = Gamepad.current;
            if (pad != null && pad.rightTrigger.wasPressedThisFrame) return true;
            return false;
        }

        Vector3 InputToWorld(Vector2 input)
        {
            Vector3 f = cam.transform.forward;
            f.y = 0f;
            f.Normalize();
            Vector3 r = cam.transform.right;
            r.y = 0f;
            r.Normalize();
            return Vector3.ClampMagnitude(f * input.y + r * input.x, 1f);
        }

        void UpdateAim(float dt)
        {
            Vector3 dir = Vector3.zero;

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 r = pad.rightStick.ReadValue();
                if (r.sqrMagnitude > 0.25f) dir = InputToWorld(r);
            }

            if (dir == Vector3.zero && Mouse.current != null)
            {
                Vector2 mp = Mouse.current.position.ReadValue();
                Ray ray = cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
                Plane plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                float t;
                if (plane.Raycast(ray, out t))
                {
                    dir = ray.GetPoint(t) - transform.position;
                    dir.y = 0f;
                }
            }

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, tuning.turnSpeed * dt);
            }
        }

        public Vector3 GetHitOrigin(float range)
        {
            return transform.position + Vector3.up * 1f + transform.forward * (range * 0.5f);
        }

        public float GetHitRadius()
        {
            return IsHeavyHit() ? tuning.heavyHitRadius : tuning.lightHitRadius;
        }

        public float GetHitRange()
        {
            return IsHeavyHit() ? tuning.heavyHitRange : tuning.lightHitRange;
        }

        public int GetCurrentPoise()
        {
            return IsHeavyHit() ? tuning.heavyPoise : tuning.lightPoise;
        }

        public PlayerTuning Tuning => tuning;
    }
}
