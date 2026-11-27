using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 멜 발렌티나 연출 (사용자 2026-11-27)

        const float DebrisTime = 2.6f;      // 앱솔루트 제로의 얼음 잔해가 남는 시간 (iceDebris 시트 길이) [임시]
        const int DebrisSpots = 22;         // 맵 곳곳에 생기는 잔해 수 [임시]

        static readonly string BasicIce = "basic_" + BasicKind.IceShard;

        static bool IsMelBasic(FieldEffect e) => e.SkillId == BasicIce && e.Source != null && e.Source.Def != null && e.Source.Def.Id == "mel";

        static bool IsAbsoluteZero(FieldEffect e) => e.Kind == FieldEffectKind.Freeze && e.SkillId == "mel_absolute_zero";

        /// <summary>얼음 잔해: 맵 곳곳의 바닥에 솟아 잠시 남는다 (유닛 아래).</summary>
        static void DrawIceDebris(Rect field, float unit, FieldEffect e, float age)
        {
            const int Columns = 6;
            int rows = Mathf.CeilToInt(DebrisSpots / (float)Columns);
            for (int k = 0; k < DebrisSpots; k++)
            {
                // 칸을 나눠 고르게 흩뿌린다
                float x = (k % Columns + 0.15f + Hash(e.Start, k) * 0.7f) / Columns;
                float y = (k / Columns + 0.15f + Hash(e.Start, k + 40) * 0.7f) / rows;
                var at = new Vector2(field.x + x * field.width, field.y + y * field.height);
                float delay = Hash(e.Start, k + 80) * 0.18f;
                DrawSheet(k % 2 == 0 ? "iceDebris" : "iceDebrisB", at, unit, age - delay, Color.white, Hash(e.Start, k + 120) < 0.5f);
            }
        }

        /// <summary>얼어붙은 적의 몸에 달라붙은 얼음 조각.</summary>
        static void DrawIceStuck(FieldEffect e, float unit, float age, float alpha, System.Func<Vector2, Vector2> P)
        {
            if (e.Units == null) return;
            var tint = new Color(1f, 1f, 1f, alpha);
            foreach (var u in e.Units)
                if (u != null && u.Alive) DrawSheet("iceStuck", P(u.Pos), unit, age, tint, false, true);
        }
    }
}
