using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Player
{
    public enum PlayerState { Free, DodgeStartup, DodgeActive, DodgeRecovery }

    /// <summary>
    /// Шаг 1 прототипа: движение WASD, прицел мышью, уклонение с зарядами и i-frames.
    /// Управление: WASD + мышь + Пробел, либо геймпад (левый стик, правый стик, A).
    /// Спецификация: Docs/Combat_Design_Document_v0.1.md, разд. 1.2-1.4.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        const float InputBufferTime = 0.15f; // CDD 1.2: буфер ввода 150 мс

        [SerializeField] PlayerTuning tuning;
        [SerializeField] Camera cam;

        CharacterController cc;
        Vector3 dodgeDir;
        float stateTimer;
        float verticalVelocity;
        float dodgeBuffer;
        float rechargeTimer;

        public PlayerState State { get; private set; }
        public int Charges { get; private set; }
        public int MaxCharges { get { return tuning != null ? tuning.maxCharges : 0; } }
        public bool IsInvulnerable { get { return State == PlayerState.DodgeActive; } }
        public float RechargeProgress
        {
            get
            {
                if (tuning == null || Charges >= tuning.maxCharges) return 1f;
                return Mathf.Clamp01(rechargeTimer / tuning.chargeRecharge);
            }
        }

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

            TickCharges(dt);

            if (ReadDodgePressed()) dodgeBuffer = InputBufferTime;
            else dodgeBuffer = Mathf.Max(0f, dodgeBuffer - dt);

            Vector3 horizontal = Vector3.zero;

            switch (State)
            {
                case PlayerState.Free:
                {
                    UpdateAim(dt);
                    Vector3 move = InputToWorld(ReadMove());
                    horizontal = move * tuning.moveSpeed;
                    if (dodgeBuffer > 0f && Charges > 0)
                    {
                        StartDodge(move);
                        horizontal = Vector3.zero;
                    }
                    break;
                }
                case PlayerState.DodgeStartup:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) EnterState(PlayerState.DodgeActive, tuning.dodgeActive);
                    break;
                case PlayerState.DodgeActive:
                {
                    float speed = tuning.dodgeDistance / Mathf.Max(0.01f, tuning.dodgeActive);
                    horizontal = dodgeDir * speed;
                    stateTimer -= dt;
                    if (stateTimer <= 0f) EnterState(PlayerState.DodgeRecovery, tuning.dodgeRecovery);
                    break;
                }
                case PlayerState.DodgeRecovery:
                    stateTimer -= dt;
                    if (stateTimer <= 0f) EnterState(PlayerState.Free, 0f);
                    break;
            }

            if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += Physics.gravity.y * dt;
            cc.Move((horizontal + Vector3.up * verticalVelocity) * dt);
        }

        void EnterState(PlayerState next, float duration)
        {
            State = next;
            stateTimer = duration;
        }

        void StartDodge(Vector3 move)
        {
            dodgeDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
            dodgeDir.y = 0f;
            Charges--;
            dodgeBuffer = 0f;
            EnterState(PlayerState.DodgeStartup, tuning.dodgeStartup);
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

        // Перевод ввода в мировое направление относительно камеры.
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
    }
}
