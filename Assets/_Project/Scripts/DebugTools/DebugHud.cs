using Eclipse.Player;
using UnityEngine;

namespace Eclipse.DebugTools
{
    /// <summary>Временная отладочная панель в левом верхнем углу экрана (позже заменим на настоящий HUD).</summary>
    public class DebugHud : MonoBehaviour
    {
        PlayerController player;
        GUIStyle style;

        void OnGUI()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 18;
                style.normal.textColor = Color.white;
            }

            float y = 16f;
            Line("Состояние: " + player.State, ref y);
            Line("Заряды уклонения: " + player.Charges + " / " + player.MaxCharges + "   (восстановление " + Mathf.RoundToInt(player.RechargeProgress * 100f) + "%)", ref y);
            Line(player.IsInvulnerable ? "I-FRAMES: ДА" : "I-FRAMES: нет", ref y);
            Line("WASD - движение, мышь - прицел, Пробел - уклонение", ref y);
        }

        void Line(string text, ref float y)
        {
            GUI.Label(new Rect(16f, y, 900f, 28f), text, style);
            y += 26f;
        }
    }
}
