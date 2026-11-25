using Eclipse.Player;
using UnityEngine;

namespace Eclipse.DebugTools
{
    /// <summary>Временная отладочная панель (позже заменим на HUD).</summary>
    public class DebugHud : MonoBehaviour
    {
        PlayerController player;
        PlayerCombat combat;
        GUIStyle style;

        void OnGUI()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (combat == null) combat = FindFirstObjectByType<PlayerCombat>();
            if (player == null) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 18;
                style.normal.textColor = Color.white;
            }

            float y = 16f;
            Line("Состояние: " + player.State, ref y);
            Line("Комбо: " + player.ComboIndex + " / 3", ref y);
            Line("Заряды уклонения: " + player.Charges + " / " + player.MaxCharges
                 + "   (" + Mathf.RoundToInt(player.RechargeProgress * 100f) + "%)", ref y);
            if (combat != null)
                Line("Ярость: " + Mathf.RoundToInt(combat.Rage) + " / " + Mathf.RoundToInt(combat.RageMax), ref y);
            Line(player.IsInvulnerable ? "I-FRAMES: ДА" : "I-FRAMES: нет", ref y);
            Line("ЛКМ — Rending · Пробел — уклонение · WASD + мышь", ref y);
        }

        void Line(string text, ref float y)
        {
            GUI.Label(new Rect(16f, y, 900f, 28f), text, style);
            y += 26f;
        }
    }
}
