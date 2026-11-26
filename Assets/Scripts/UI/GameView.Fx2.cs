using UnityEngine;

namespace TextSlayers
{
    /// <summary>세부 연출 이펙트 (사용자 2026-11-26): 대검 · 십자가 · 미사일 · 얼음 · 잡기 · 연타 등. 전부 연출용 [임시].</summary>
    public static partial class GameView
    {
        const float FxBigSlash = 0.4f, FxCoin = 0.9f, FxSlantPillar = 0.5f, FxCrossSlam = 0.45f, FxMaw = 0.65f,
                    FxFan = 0.45f, FxLeapOff = 0.3f, FxBladeSwing = 0.4f, FxMissileGap = 0.08f, FxMissileRise = 0.4f,
                    FxMissileFall = 0.35f, FxMissileBoom = 0.3f, FxCrossFallDrop = 0.25f, FxCrossFallAfter = 0.4f;

        static float SpecialLength(FieldEffect e)
        {
            switch (e.Kind)
            {
                case FieldEffectKind.BigSlash: return FxBigSlash;
                case FieldEffectKind.Coin: return FxCoin;
                case FieldEffectKind.SlantPillar: return FxSlantPillar;
                case FieldEffectKind.CrossSlam: return FxCrossSlam;
                case FieldEffectKind.Maw: return FxMaw;
                case FieldEffectKind.FanWave: return FxFan;
                case FieldEffectKind.LeapOff: return FxLeapOff;
                case FieldEffectKind.FireLine: return 0.7f;
                case FieldEffectKind.BladeSwing: return FxBladeSwing;
                case FieldEffectKind.Missiles: return Mathf.Max(1, e.Count) * FxMissileGap + FxMissileRise + FxMissileFall + FxMissileBoom;
                case FieldEffectKind.CrossFall: return e.Duration + FxCrossFallAfter;
                case FieldEffectKind.Landing: return e.Duration + 0.4f;
                case FieldEffectKind.BladeSlam: return Mathf.Max(0.35f, e.Duration);
                default: return e.Duration > 0f ? e.Duration : FxDefaultHold;   // BladeRaise · Whirl · Freeze · Grab · Punches · CrossHold · ForwardSlashes
            }
        }

        static void DrawBlade(Vector2 pivot, float angleDeg, float length, float width, Color main, float alpha)
        {
            Vector2 dir = new Vector2(Mathf.Cos(angleDeg * Mathf.Deg2Rad), Mathf.Sin(angleDeg * Mathf.Deg2Rad));
            Vector2 mid = pivot + dir * (length * 0.5f);
            DrawFxRect(fxLine, mid, length * 1.15f, width * 2.2f, main, 0.5f * alpha, angleDeg);              // 광채
            DrawFxRect(Texture2D.whiteTexture, mid, length, width, Light(main), 0.95f * alpha, angleDeg);     // 직사각형 날
            DrawFxRect(Texture2D.whiteTexture, mid, length, width * 0.18f, Color.white, alpha, angleDeg);     // 날 선
            DrawFxRect(Texture2D.whiteTexture, pivot + dir * (length * 0.08f), width * 0.3f, width * 1.7f, main, alpha, angleDeg);   // 코등이
        }

        static void DrawCross(Vector2 c, float height, Color main, float alpha, float tiltDeg = 0f, bool inverted = false)
        {
            float w = height * 0.2f;
            Vector2 up = new Vector2(Mathf.Sin(tiltDeg * Mathf.Deg2Rad), -Mathf.Cos(tiltDeg * Mathf.Deg2Rad));
            float barAt = (inverted ? -0.2f : 0.2f) * height;
            DrawFxRect(fxDisc, c, height * 1.1f, height * 1.1f, main, 0.35f * alpha);
            DrawFxRect(Texture2D.whiteTexture, c, w, height, Light(main), alpha, tiltDeg);
            DrawFxRect(Texture2D.whiteTexture, c + up * barAt, height * 0.62f, w, Light(main), alpha, tiltDeg);
            DrawFxRect(Texture2D.whiteTexture, c, w * 0.3f, height * 0.9f, Color.white, alpha, tiltDeg);
        }

        /// <summary>필드 안의 점에서 dir 방향으로 필드 가장자리까지의 거리.</summary>
        static float DistanceToEdge(Rect field, Vector2 from, Vector2 dir)
        {
            float len = field.width + field.height;
            if (dir.x > 0.0001f) len = Mathf.Min(len, (field.xMax - from.x) / dir.x);
            if (dir.x < -0.0001f) len = Mathf.Min(len, (field.x - from.x) / dir.x);
            if (dir.y > 0.0001f) len = Mathf.Min(len, (field.yMax - from.y) / dir.y);
            if (dir.y < -0.0001f) len = Mathf.Min(len, (field.y - from.y) / dir.y);
            return Mathf.Max(0f, len);
        }

        static void DrawSpecial(Rect field, float unit, float U, FieldBattle b, FieldEffect e, Vector2 c, Vector2 follow,
                                float t, float age, System.Func<Vector2, Vector2> P)
        {
            Color main = e.Color, light = Light(main), core = Core(main);
            float dirAng = AngleOf(e.Dir);
            float R = Mathf.Max(0.3f, e.Radius) * U;
            switch (e.Kind)
            {
                case FieldEffectKind.BladeSlam:   // 머리 위에서 앞으로 내려찍는 직사각형 대검
                {
                    bool longBeam = e.Radius >= 0.9f && e.Target == null;
                    float length = longBeam ? Mathf.Min(12f * unit, Vector2.Distance(P(e.Origin), P(e.End))) : U * 2.2f;
                    float swing = Mathf.Clamp01(t / 0.6f);
                    float eased = swing * swing;
                    float from = -90f, to = dirAng;
                    float ang = Mathf.LerpAngle(from, to, eased);
                    float a = t < 0.8f ? 1f : (1f - t) / 0.2f;
                    DrawBlade(follow, ang, length, longBeam ? U * 0.75f : U * 0.5f, main, a);
                    if (swing >= 1f)
                    {
                        Vector2 tip = follow + new Vector2(Mathf.Cos(to * Mathf.Deg2Rad), Mathf.Sin(to * Mathf.Deg2Rad)) * (length * 0.85f);
                        float k = (t - 0.6f) / 0.4f;
                        DrawFx(fxRing, tip, U * Mathf.Lerp(0.8f, 3f, k), light, 1f - k);
                        DrawFx(fxDisc, tip, U * 1.6f, main, 0.7f * (1f - k));
                    }
                    break;
                }
                case FieldEffectKind.BladeRaise:  // 수직으로 들어 올린 대검 (모으기)
                {
                    float grow = Ease(Mathf.Clamp01(t * 2f));
                    float length = U * Mathf.Lerp(1.2f, 3.4f, grow);
                    float shake = Mathf.Sin(age * 60f) * 1.5f * t;
                    DrawBlade(follow + new Vector2(shake, -U * 0.3f), -90f, length, U * 0.7f, main, 1f);
                    for (int k = 0; k < 5; k++)
                    {
                        float s = Mathf.Repeat(t * 3f + k / 5f, 1f);
                        DrawFx(fxDisc, follow + new Vector2((Hash(e.Start, k) - 0.5f) * U, -U * 0.4f - s * length), U * 0.3f, light, 1f - s);
                    }
                    break;
                }
                case FieldEffectKind.BladeSwing:  // 대검을 옆으로 휘둘러 베기
                {
                    float sweep = e.Count > 0 ? Mathf.Min(220f, e.Count) : 150f;
                    float ang = dirAng - sweep * 0.5f + sweep * Ease(t);
                    float length = Mathf.Max(U * 2f, R);
                    float a = t < 0.75f ? 1f : (1f - t) / 0.25f;
                    DrawFx(fxArc, follow, length * 2.3f, light, 0.8f * a, ang - 18f);
                    DrawFx(fxArc, follow, length * 2.0f, main, 0.6f * a, ang - 38f);
                    DrawBlade(follow, ang, length, U * 0.42f, main, a);
                    break;
                }
                case FieldEffectKind.BigSlash:    // 큰 베기 한 번
                {
                    float ang = dirAng - 80f + 160f * Ease(t);
                    float size = R * 2.6f;
                    Glow(follow, size * 0.7f, main, 1f - t);
                    DrawFx(fxArc, follow, size, light, 1f - t, ang);
                    DrawFx(fxArc, follow, size * 1.08f, main, 0.8f * (1f - t), ang - 20f);
                    DrawFx(fxArc, follow, size * 0.86f, Color.white, 0.7f * (1f - t), ang + 12f);
                    break;
                }
                case FieldEffectKind.Whirl:       // 자신을 중심으로 화염 참격이 계속 생긴다
                {
                    const float step = 0.1f, one = 0.28f;
                    int last = Mathf.FloorToInt(age / step);
                    for (int k = Mathf.Max(0, last - 3); k <= last; k++)
                    {
                        float s = (age - k * step) / one;
                        if (s < 0f || s >= 1f) continue;
                        float ang = k * 137f;                    // 고르게 돌아가며
                        float rr = R * (0.45f + 0.55f * Hash(e.Start, k));
                        Vector2 pos = follow + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * rr * 0.5f;
                        DrawFx(fxArc, pos, R * 1.5f, new Color(1f, 0.85f, 0.35f), 1f - s, ang + 120f * s);
                        DrawFx(fxArc, pos, R * 1.65f, main, 0.8f * (1f - s), ang - 25f + 120f * s);
                    }
                    DrawFx(fxRing, follow, R * 2.1f, main, 0.35f * Mathf.Min(1f, (1f - t) * 4f));
                    break;
                }
                case FieldEffectKind.Coin:        // 동전이 튀어 올랐다 떨어진다
                {
                    float h = Mathf.Sin(t * Mathf.PI) * unit * 2.4f;
                    Vector2 pos = follow + new Vector2(t * U * 0.6f, -h - U * 0.5f);
                    float flip = Mathf.Abs(Mathf.Cos(age * 18f));
                    DrawFxRect(fxDisc, pos, U * 1.1f, U * 1.1f, new Color(1f, 0.85f, 0.3f), 0.5f);
                    DrawFxRect(fxRing, pos, U * 0.65f * Mathf.Max(0.15f, flip), U * 0.65f, Color.white, 1f);
                    DrawFxRect(fxDisc, pos, U * 0.75f * Mathf.Max(0.15f, flip), U * 0.75f, new Color(1f, 0.82f, 0.2f), 1f);
                    break;
                }
                case FieldEffectKind.Freeze:      // 필드가 얼고 적들이 얼음에 갇힌 듯한 연출
                {
                    float a = Mathf.Min(1f, age / 0.15f) * Mathf.Min(1f, (1f - t) * 2.5f);
                    DrawFxRect(Texture2D.whiteTexture, field.center, field.width, field.height, new Color(0.55f, 0.85f, 1f), 0.22f * a);
                    for (int k = 0; k < 24; k++)  // 바닥 서리
                    {
                        Vector2 pos = new Vector2(field.x + Hash(e.Start, k) * field.width, field.y + Hash(e.Start, k + 40) * field.height);
                        DrawFxRect(fxLine, pos, unit * 1.2f, unit * 0.12f, Color.white, 0.5f * a, Hash(e.Start, k + 80) * 180f);
                        DrawFxRect(fxLine, pos, unit * 0.8f, unit * 0.1f, Color.white, 0.4f * a, Hash(e.Start, k + 80) * 180f + 60f);
                    }
                    if (e.Units == null) break;
                    foreach (var u in e.Units)
                    {
                        if (u == null || !u.Alive) continue;
                        Vector2 pos = P(u.Pos);
                        float s = unit * 1.35f;
                        DrawFxRect(Texture2D.whiteTexture, pos, s, s, new Color(0.7f, 0.92f, 1f), 0.45f * a);
                        DrawFxRect(Texture2D.whiteTexture, pos + new Vector2(-s * 0.18f, -s * 0.2f), s * 0.18f, s * 0.5f, Color.white, 0.6f * a, 30f);
                        DrawFx(fxRing, pos, s * 1.5f, Color.white, 0.5f * a);
                    }
                    break;
                }
                case FieldEffectKind.Grab:        // 잡은 대상을 원으로 감싼다
                {
                    float pulse = 1f + 0.08f * Mathf.Sin(age * 14f);
                    float appear = Mathf.Clamp01(age / 0.15f);
                    DrawFx(fxDisc, c, U * 2.2f * pulse, new Color(0.25f, 0.25f, 0.3f), 0.45f);
                    DrawFx(fxRing, c, U * Mathf.Lerp(3f, 1.8f, appear) * pulse, main, 1f);
                    DrawFx(fxRing, c, U * 1.5f * pulse, light, 0.8f);
                    DrawLine(follow, c, U * 0.3f, main, 0.6f);
                    break;
                }
                case FieldEffectKind.Missiles:    // 케이스를 앞에 놓고 → 미사일이 솟았다가 대상에 포격 (화면 밖으로 나가지 않음)
                {
                    if (e.Points == null) break;
                    Vector2 caseAt = follow + e.Dir * (unit * 1.1f);
                    float total = SpecialLength(e);
                    float ca = Mathf.Min(1f, (total - age) / 0.3f);
                    DrawFxRect(Texture2D.whiteTexture, caseAt, unit * 0.8f, unit * 0.55f, new Color(0.35f, 0.33f, 0.3f), ca);
                    DrawFxRect(Texture2D.whiteTexture, caseAt + new Vector2(0f, -unit * 0.2f), unit * 0.8f, unit * 0.1f, new Color(0.65f, 0.58f, 0.45f), ca);
                    float topY = field.y + unit * 0.6f;
                    for (int k = 0; k < e.Points.Length; k++)
                    {
                        float s = age - k * FxMissileGap;
                        if (s < 0f) continue;
                        Vector2 apex = new Vector2(Mathf.Clamp(caseAt.x + (k - (e.Points.Length - 1) * 0.5f) * unit * 0.45f, field.x + unit * 0.5f, field.xMax - unit * 0.5f),
                                                   Mathf.Max(topY, caseAt.y - unit * (3.2f + Hash(e.Start, k) * 1.6f)));
                        Vector2 hit = P(e.Points[k]) + new Vector2((Hash(e.Start, k + 3) - 0.5f) * unit, (Hash(e.Start, k + 9) - 0.5f) * unit * 0.6f);
                        if (s < FxMissileRise)
                        {
                            float f = Ease(s / FxMissileRise);
                            Vector2 pos = Vector2.Lerp(caseAt, apex, f);
                            DrawLine(Vector2.Lerp(caseAt, apex, Mathf.Max(0f, f - 0.35f)), pos, unit * 0.22f, main, 0.7f);
                            DrawFx(fxDisc, pos, unit * 0.45f, Color.white, 1f);
                        }
                        else if (s < FxMissileRise + FxMissileFall)
                        {
                            float f = (s - FxMissileRise) / FxMissileFall;
                            Vector2 pos = Vector2.Lerp(apex, hit, f * f);
                            DrawLine(Vector2.Lerp(apex, hit, Mathf.Max(0f, f * f - 0.3f)), pos, unit * 0.22f, main, 0.8f);
                            DrawFx(fxDisc, pos, unit * 0.45f, light, 1f);
                            DrawFx(fxRing, hit, unit * 0.9f, new Color(1f, 0.3f, 0.2f), 0.5f);
                        }
                        else if (s < FxMissileRise + FxMissileFall + FxMissileBoom)
                        {
                            float f = (s - FxMissileRise - FxMissileFall) / FxMissileBoom;
                            DrawFx(fxDisc, hit, unit * 2.2f * Mathf.Lerp(0.4f, 1f, Ease(f)), main, 0.85f * (1f - f));
                            DrawFx(fxDisc, hit, unit * 1.0f, Color.white, 0.8f * (1f - f));
                            DrawFx(fxRing, hit, unit * 2.6f * Ease(f), light, 1f - f);
                        }
                    }
                    break;
                }
                case FieldEffectKind.Punches:     // 연속 주먹
                {
                    int n = Mathf.Max(1, e.Count);
                    float step = Mathf.Max(0.05f, e.Duration) / n;
                    for (int k = 0; k < n; k++)
                    {
                        float s = (age - k * step) / (step * 1.4f);
                        if (s < 0f || s >= 1f) continue;
                        Vector2 d = c - follow;
                        Vector2 perp = d.sqrMagnitude > 0.1f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
                        Vector2 to = c + perp * ((k % 2 == 0 ? 1f : -1f) * U * 0.35f);
                        float f = Ease(Mathf.Min(1f, s * 1.8f));
                        Vector2 pos = Vector2.Lerp(follow, to, f);
                        DrawLine(Vector2.Lerp(follow, to, Mathf.Max(0f, f - 0.5f)), pos, U * 0.55f, main, 0.7f * (1f - s));
                        DrawFx(fxDisc, pos, U * 0.8f, light, 1f - s);
                        DrawFxRect(Texture2D.whiteTexture, pos, U * 0.34f, U * 0.3f, Color.white, 0.9f * (1f - s), AngleOf(to - follow));
                        if (s > 0.45f) DrawFx(fxRing, to, U * 1.7f * (s - 0.3f), main, 1f - s);
                    }
                    break;
                }
                case FieldEffectKind.SlantPillar: // 비스듬히 솟는 불기둥
                {
                    float a = 1f - t;
                    float h = U * 3.8f * Ease(Mathf.Min(1f, t * 3f));
                    float lean = e.Dir.x >= 0f ? 28f : -28f;
                    Vector2 up = new Vector2(Mathf.Sin(lean * Mathf.Deg2Rad), -Mathf.Cos(lean * Mathf.Deg2Rad));
                    Vector2 mid = c + up * (h * 0.5f);
                    float ang = AngleOf(up);
                    DrawFxRect(fxLine, mid, h * 1.1f, U * 1.5f, main, 0.55f * a, ang);
                    DrawFxRect(fxLine, mid, h, U * 0.9f, new Color(1f, 0.7f, 0.2f), 0.9f * a, ang);
                    DrawFxRect(fxLine, mid, h * 0.9f, U * 0.35f, Color.white, a, ang);
                    DrawFx(fxDisc, c, U * 1.8f, main, 0.8f * a);
                    break;
                }
                case FieldEffectKind.CrossHold:   // 십자가를 들어 올림
                    DrawCross(follow + new Vector2(e.Dir.x >= 0f ? U * 0.35f : -U * 0.35f, -U * 1.2f), U * 2.2f, main, Mathf.Min(1f, age / 0.1f));
                    DrawFx(fxRing, follow, U * 2f, light, 0.6f);
                    break;
                case FieldEffectKind.CrossSlam:   // 휘둘러 내려찍기
                {
                    float swing = Mathf.Clamp01(t / 0.5f);
                    float tilt = Mathf.Lerp(-110f, 40f, swing * swing) * (e.Dir.x >= 0f ? 1f : -1f);
                    float a = t < 0.7f ? 1f : (1f - t) / 0.3f;
                    Vector2 arm = new Vector2(Mathf.Sin(tilt * Mathf.Deg2Rad), -Mathf.Cos(tilt * Mathf.Deg2Rad)) * (U * 1.2f);
                    DrawCross(c + arm - new Vector2(0f, U * 0.2f), U * 2.4f, main, a, tilt);
                    if (swing >= 1f)
                    {
                        float k = (t - 0.5f) / 0.5f;
                        DrawFx(fxRing, c, U * Mathf.Lerp(1f, 4f, Ease(k)), light, 1f - k);
                        DrawFx(fxDisc, c, U * 2.4f, main, 0.6f * (1f - k));
                    }
                    break;
                }
                case FieldEffectKind.CrossFall:   // 대상 위에 십자가 소환 → 준비가 끝나면 낙하
                {
                    float hold = e.Duration;
                    float dropStart = hold - FxCrossFallDrop;
                    float height = U * 3f;
                    Vector2 hover = c + new Vector2(0f, -unit * 2.6f + Mathf.Sin(age * 5f) * 3f);
                    if (age < dropStart)
                    {
                        float appear = Mathf.Clamp01(age / 0.3f);
                        DrawCross(hover, height * appear, main, appear);
                        DrawFx(fxRing, c, U * Mathf.Lerp(3.6f, 1.6f, age / Mathf.Max(0.1f, dropStart)), light, 0.7f);
                    }
                    else if (age < hold)
                    {
                        float f = (age - dropStart) / FxCrossFallDrop;
                        Vector2 pos = Vector2.Lerp(hover, c + new Vector2(0f, -height * 0.25f), f * f);
                        DrawLine(hover, pos, U * 0.8f, main, 0.5f);
                        DrawCross(pos, height * 1.1f, main, 1f);
                    }
                    else
                    {
                        float f = (age - hold) / FxCrossFallAfter;
                        DrawCross(c + new Vector2(0f, -height * 0.25f), height * 1.1f, main, 1f - f);
                        DrawFx(fxRing, c, U * 5f * Ease(f), light, 1f - f);
                        DrawFx(fxDisc, c, U * 3f, light, 0.8f * (1f - f));
                    }
                    break;
                }
                case FieldEffectKind.Maw:         // 거대한 이빨 입이 벌어졌다가 삼킨다
                {
                    float size = U * 3.4f;
                    float open = t < 0.5f ? Ease(t * 2f) : 1f - Mathf.Clamp01((t - 0.5f) / 0.18f);
                    float a = t < 0.8f ? 1f : (1f - t) / 0.2f;
                    float gap = size * 0.42f * open + size * 0.06f;
                    var jaw = new Color(0.12f, 0.06f, 0.14f);
                    DrawFx(fxDisc, c, size * 1.5f, main, 0.35f * a);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 j = c + new Vector2(0f, side * (gap + size * 0.16f));
                        DrawFxRect(fxDisc, j, size * 1.5f, size * 0.75f, jaw, a);
                        DrawFxRect(Texture2D.whiteTexture, j, size * 0.95f, size * 0.22f, jaw, a);
                        for (int k = -3; k <= 3; k++)   // 이빨
                        {
                            float tooth = size * (k == -3 || k == 3 ? 0.17f : 0.13f);
                            Vector2 tp = c + new Vector2(k * size * 0.145f, side * (gap + Mathf.Abs(k) * size * 0.006f));
                            DrawFxRect(Texture2D.whiteTexture, tp, tooth * 0.7f, tooth * 0.7f, Color.white, a, 45f);
                        }
                    }
                    if (t > 0.68f) DrawFx(fxRing, c, size * 1.8f * (t - 0.4f), light, a);
                    break;
                }
                case FieldEffectKind.ForwardSlashes:  // 전방으로 연속 베기
                {
                    int n = Mathf.Max(1, e.Count);
                    float step = Mathf.Max(0.05f, e.Duration) / n;
                    Vector2 fwd = e.Dir;
                    Vector2 side = new Vector2(-fwd.y, fwd.x);
                    for (int k = 0; k < n; k++)
                    {
                        float s = (age - k * step) / 0.3f;
                        if (s < 0f || s >= 1f) continue;
                        float reach = 0.6f + Hash(e.Start, k) * 2.2f;
                        Vector2 pos = follow + fwd * (reach * unit) + side * ((Hash(e.Start, k + 31) - 0.5f) * unit * reach * 1.1f);
                        float ang = AngleOf(fwd) + (k % 2 == 0 ? -55f : 55f) + (k % 2 == 0 ? 110f : -110f) * Ease(s);
                        Glow(pos, U * 1.6f, main, 1f - s);
                        DrawFx(fxArc, pos, U * 2.4f, light, 1f - s, ang);
                        DrawFx(fxArc, pos, U * 2.6f, main, 0.7f * (1f - s), ang - 22f);
                        DrawFx(fxDisc, pos, U * 0.9f, core, 0.7f * (1f - s));
                    }
                    break;
                }
                case FieldEffectKind.FanWave:     // 부채꼴 충격파
                {
                    float ang = dirAng;
                    for (int k = 0; k < 3; k++)
                    {
                        float s = Mathf.Clamp01(t * 1.6f - k * 0.18f);
                        if (s <= 0f) continue;
                        float size = unit * Mathf.Lerp(1.5f, 8f - k * 1.2f, Ease(s));
                        DrawFx(fxArc, c, size, k == 0 ? Color.white : k == 1 ? light : main, 1f - s, ang);
                        DrawFx(fxArc, c, size * 0.96f, main, 0.7f * (1f - s), ang);
                    }
                    break;
                }
                case FieldEffectKind.FireLine:    // 맵을 꿰뚫는 직선 화염 (필드 안으로 잘라 그린다)
                {
                    // 맵 전체 관통 (사용자 2026-11-26): 시전자를 지나 한쪽 끝에서 반대쪽 끝까지
                    Vector2 origin = P(e.Origin);
                    Vector2 dir = e.Dir;
                    Vector2 from = origin - dir * DistanceToEdge(field, origin, -dir);
                    Vector2 to = origin + dir * DistanceToEdge(field, origin, dir);
                    float a = t < 0.5f ? 1f : (1f - t) * 2f;
                    float w = U * (1.9f + 0.2f * Mathf.Sin(age * 50f));
                    DrawLine(from, to, w * 1.9f, new Color(0.9f, 0.2f, 0f), 0.55f * a);
                    DrawLine(from, to, w * 1.2f, main, 0.95f * a);
                    DrawLine(from, to, w * 0.65f, new Color(1f, 0.85f, 0.3f), a);
                    DrawLine(from, to, w * 0.25f, Color.white, a);
                    for (int k = 0; k < 42; k++)   // 불꽃 혀
                    {
                        float s = (k + 0.5f) / 42f;
                        Vector2 side = new Vector2(-dir.y, dir.x) * ((Hash(e.Start, k) - 0.5f) * w * 1.5f);
                        DrawFx(fxDisc, Vector2.Lerp(from, to, s) + side, U * (0.5f + Hash(e.Start, k + 60) * 0.7f),
                               k % 3 == 0 ? new Color(1f, 0.8f, 0.2f) : main, 0.85f * a);
                    }
                    break;
                }
                case FieldEffectKind.LeapOff:     // 위로 솟구쳐 맵을 벗어남
                {
                    float rise = Ease(t);
                    Vector2 top = new Vector2(c.x, Mathf.Lerp(c.y, field.y, rise));
                    DrawLine(new Vector2(c.x, Mathf.Lerp(c.y, field.y, rise * rise)), top, U * 0.8f, main, 1f - t * 0.5f);
                    DrawLine(new Vector2(c.x, Mathf.Lerp(c.y, field.y, rise * rise)), top, U * 0.3f, Color.white, 1f - t * 0.5f);
                    DrawFx(fxRing, c, U * 3f * Ease(t), light, 1f - t);
                    break;
                }
                case FieldEffectKind.Landing:     // 그림자가 커지다가 낙하 · 충격
                {
                    if (age < e.Duration)
                    {
                        float k = age / Mathf.Max(0.1f, e.Duration);
                        DrawFxRect(fxDisc, c, U * Mathf.Lerp(0.8f, 2.8f, k), U * Mathf.Lerp(0.4f, 1.4f, k), Color.black, 0.8f);
                        DrawFx(fxRing, c, U * 2.6f, main, 0.35f + 0.35f * Mathf.Sin(age * 20f));
                        if (k > 0.7f)
                        {
                            float f = (k - 0.7f) / 0.3f;
                            Vector2 from = new Vector2(c.x, field.y);
                            Vector2 pos = Vector2.Lerp(from, c, f * f);
                            DrawLine(from, pos, U * 0.9f, main, 0.8f);
                            DrawFx(fxDisc, pos, U * 1.3f, light, 1f);
                        }
                    }
                    else
                    {
                        float f = (age - e.Duration) / 0.4f;
                        DrawFx(fxRing, c, R * 3.2f * Ease(f), light, 1f - f);
                        DrawFx(fxDisc, c, R * 2.2f, main, 0.75f * (1f - f));
                    }
                    break;
                }
            }
        }
    }
}
