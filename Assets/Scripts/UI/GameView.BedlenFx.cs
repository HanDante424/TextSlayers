using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 베들렌 메릴 건틀릿 내지르기. 기본 공격은 중대검이고 건틀릿은 스킬에서 쓸 예정 (사용자 2026-11-28).
        // 스킬이 정해지기 전이라 지금은 부르는 곳이 없다.
        // bedlen_gauntlet: 주먹 자세 원본(Docs/Design/Bedlen_Merill_Cartridge_Gauntlet_64_Fist.png)을 주먹이 오른쪽을 향하게 90도 돌리고
        // 절반(32×32)으로 줄인 것. 캐릭터 도트 속 건틀릿과 비슷한 크기가 된다.
        const float GauntletReachAt = 0.4f;       // 공격 시간 중 주먹이 끝까지 뻗는 시점 (0~1) [임시]
        const float GauntletFadeAt = 0.7f;        // 사라지기 시작하는 시점 [임시]
        const float GauntletStartAhead = 0.15f;   // 내지르기 시작: 몸 중심에서 앞으로 (칸) [임시]
        const float GauntletStopShort = 0.3f;     // 끝: 대상 중심보다 이만큼 앞 (칸) [임시]
        const float GauntletDrop = 6f;            // 손 높이: 몸 중심보다 아래 (도트) [임시]
        static readonly Color CartridgeOrange = new Color(1f, 0.58f, 0.18f);

        /// <summary>건틀릿이 대상 쪽으로 짧게 내질러지고, 닿는 순간 카트리지 색 충격. 도트를 읽지 못해도 충격은 그린다.</summary>
        static void DrawCartridgePunch(FieldEffect e, Vector2 center, Vector2 target, float unit, float U, float t, Color light)
        {
            Vector2 dir = e.Dir.sqrMagnitude > 0.001f ? e.Dir.normalized : Vector2.right;
            bool left = dir.x < 0f;
            float pixel = FieldPixelSize(unit);
            Vector2 drop = new Vector2(0f, GauntletDrop * pixel);
            Vector2 attacker = center - dir * (unit * SwingReachAhead);
            Vector2 from = attacker + dir * (unit * GauntletStartAhead) + drop;
            Vector2 to = target - dir * (unit * GauntletStopShort) + drop;

            float reach = Ease(Mathf.Clamp01(t / GauntletReachAt));
            Vector2 at = Vector2.Lerp(from, to, reach);
            float alpha = t < GauntletFadeAt ? 1f : Mathf.Clamp01((1f - t) / (1f - GauntletFadeAt));

            if (t < GauntletReachAt) DrawLine(from, at, U * 0.35f, CartridgeOrange, 0.55f);   // 뻗는 궤적

            if (t >= GauntletReachAt)
            {
                float k = (t - GauntletReachAt) / (1f - GauntletReachAt);
                Vector2 hit = to + dir * (unit * GauntletStopShort);
                DrawFx(fxDisc, hit, U * 1.1f * Mathf.Lerp(0.6f, 1.3f, Ease(k)), light, 0.9f * (1f - k));
                DrawFx(fxRing, hit, U * 1.8f * Ease(k), CartridgeOrange, 1f - k);
            }

            var texture = left ? UpsideDownWeaponTexture("bedlen_gauntlet") : WeaponTexture("bedlen_gauntlet");
            if (texture != null) DrawFxRect(texture, at, texture.width * pixel, texture.height * pixel, Color.white, alpha, AngleOf(dir));
        }
    }
}
