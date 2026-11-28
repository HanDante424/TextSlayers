using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 레일 루카스 검 ↔ 결합 장병기 (사용자 2026-11-28). 도트는 칼끝이 오른쪽이고 같은 기준점(손 x=12, 축 y=32)을 쓴다.
        // 결합 · 장병기 도트는 원본을 좌우로만 뒤집었다: 장비 도트 그대로 불꽃은 위, 검집 손잡이는 아래.
        //  rail_sword_join    결합 동작의 검 (좌우 반전 · 칼끝 x=59)
        //  rail_scabbard_join 검집 (장병기 속 위치로 옮김)
        //  rail_polearm_swing 결합 장병기 (좌우 반전, 불꽃 포함)
        //  (평소 검 공격의 rail_sword 는 날이 내려베는 쪽을 보도록 180도 돌린 것)
        const string RailPoleSprite = "rail_polearm_swing";
        const float RailPoleZoom = 2f;             // 장병기 도트 배율 [임시]
        const float RailGripY = 32f;               // 도트의 손 위치 가로축 (위에서부터)
        const float RailJoinAt = 0.75f;            // 결합 동작 중 검집이 맞물리는 시점 (0~1) [임시]
        const float RailJoinDistance = 34f;        // 검집이 밀려 들어오는 거리 (도트) [임시]
        const float RailJointAhead = 20f;          // 손 → 결합부 (도트)
        const float RailHoldAngle = -68f;          // 들고 서 있을 때 각도 (오른쪽을 볼 때, 위쪽이 음수) [임시]
        const float RailHandForward = 6f, RailHandDown = 14f;   // 몸 중심에서 손까지 (도트) [임시]

        static bool RailPoleReady(FieldUnit u, float now)
            => u.Alive && u.Polearm && now - u.PolearmSince >= FieldBattle.RailCombineTime;

        /// <summary>결합 동작과, 결합한 뒤 휘두르지 않을 때 들고 있는 장병기.</summary>
        static void DrawRailForms(FieldBattle b, float unit, System.Func<Vector2, Vector2> toScreen)
        {
            foreach (var u in b.Players)
            {
                if (!u.Alive || !u.Polearm) continue;
                bool left = u.Facing.x < 0f;
                float pixel = FieldPixelSize(unit), size = pixel * RailPoleZoom;
                Vector2 hand = toScreen(u.Pos) + new Vector2((left ? -1f : 1f) * RailHandForward * pixel, RailHandDown * pixel);
                float k = (b.Time - u.PolearmSince) / FieldBattle.RailCombineTime;
                if (k < 1f) DrawRailCombine(hand, left, size, unit, Mathf.Max(0f, k));
                else if (!RailSwinging(b, u))
                    DrawRailSprite(RailPoleSprite, hand, left ? 180f - RailHoldAngle : RailHoldAngle, left, size);
            }
        }

        static bool RailSwinging(FieldBattle b, FieldUnit u)
        {
            foreach (var e in b.Effects)
                if (e.Source == u && e.Kind == FieldEffectKind.Swing && b.Time >= e.Start && b.Time - e.Start < RailPoleCleave.WeaponTime) return true;
            return false;
        }

        /// <summary>검을 앞으로 눕혀 들고, 검집이 뒤에서 밀려 들어와 손잡이를 감싼다. 맞물리는 순간 섬광 뒤 장병기가 된다.</summary>
        static void DrawRailCombine(Vector2 hand, bool left, float size, float unit, float k)
        {
            float forward = left ? -1f : 1f;
            float angle = left ? 180f : 0f;
            if (k < RailJoinAt)
            {
                float slide = 1f - Ease(k / RailJoinAt);
                Vector2 back = new Vector2(-forward * RailJoinDistance * size * slide, 0f);
                DrawRailSprite("rail_sword_join", hand, angle, left, size);
                DrawRailSprite("rail_scabbard_join", hand + back, angle, left, size);
                return;
            }
            DrawRailSprite(RailPoleSprite, hand, angle, left, size);
            float flash = (k - RailJoinAt) / (1f - RailJoinAt);
            Vector2 joint = hand + new Vector2(forward * RailJointAhead * size, 0f);
            DrawFx(fxRing, joint, unit * Mathf.Lerp(0.3f, 1.8f, flash), Light(SkillColor.Fire), 1f - flash);
            DrawFx(fxDisc, joint, unit * 0.9f, SkillColor.Fire, 0.8f * (1f - flash));
        }

        /// <summary>왼쪽을 볼 때는 위아래를 뒤집은 도트를 써서 검의 위아래가 오른쪽과 같게 보이게 한다.</summary>
        static void DrawRailSprite(string name, Vector2 hand, float angleDeg, bool left, float size)
        {
            var texture = left ? UpsideDownWeaponTexture(name) : WeaponTexture(name);
            if (texture != null) DrawSwordSprite(texture, hand, angleDeg, size, 1f, left ? SwordCanvas - RailGripY : RailGripY);
        }
    }
}
