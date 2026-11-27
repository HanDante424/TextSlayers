using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 필드 전투 스킬 이펙트 · 파티클 (v0.2). 텍스처는 코드로 만든다 (외부 리소스 없음).
    /// 시간은 전투 시간(FieldBattle.Time)이라 배속 · 일시정지를 따른다.
    /// 색은 스킬의 FxColor 에서 밝은 색 · 어두운 색을 만든다. 시간 · 크기는 전부 연출용 [임시] — 전투 결과에 영향 없음.
    /// </summary>
    public static partial class GameView
    {
        // ── 연출 배율 (잘 보이게, 사용자 2026-11-26). 크기는 상단 버튼으로 바꿀 수 있다 ──
        static readonly float[] FxScales = { 1f, 1.3f, 1.6f };
        static int fxScaleIndex = 1;
        static float FxScale => FxScales[fxScaleIndex];
        const float FxTimeMul = 1.4f;           // 짧은 이펙트 지속시간 배율
        const float GlowSize = 1.7f, GlowAlpha = 0.22f;

        const float FxSpin = 0.35f, FxSmash = 0.45f, FxBurst = 0.35f, FxPulse = 0.55f, FxGrasp = 0.9f, FxHarvest = 0.8f;
        const float FxSlash = 0.25f, FxMultiStep = 0.08f, FxMultiOne = 0.2f, FxAfter = 0.3f, FxPillar = 0.45f, FxHeal = 0.8f;
        const float FxBuff = 0.8f, FxRainStep = 0.08f, FxRainFall = 0.25f, FxBite = 0.35f, FxBeam = 0.35f, FxCone = 0.4f;
        const float FxRoar = 0.9f, FxRoarStep = 0.15f, FxLob = 0.35f, FxDefaultHold = 1f, FxCrossBlast = 0.4f, FxChargeBlast = 0.6f;
        const float FxSwing = 0.22f, FxMapSlash = 0.6f, FxBlinkGhost = 0.35f;
        const float FxGraspSpread = 0.35f;     // 손아귀: 어둠이 퍼지는 구간 비율 (이후 모인다)
        const int FxHarvestParticles = 6;      // 거두기: 대상 1명당 파티클 수
        const float FxHarvestStagger = 0.07f;  // 파티클 출발 간격 (비율)

        public static void CycleFxScale() => fxScaleIndex = (fxScaleIndex + 1) % FxScales.Length;
        public static string FxScaleLabel => $"이펙트 ×{FxScale:0.0}";

        static Texture2D fxDisc, fxRing, fxArc, fxLine;

        static void EnsureFx()
        {
            if (fxDisc != null) return;
            fxDisc = MakeFxTex(64, (u, v) => { float r = Mathf.Sqrt(u * u + v * v); float k = Mathf.Clamp01(1f - r); return k * k; });
            fxRing = MakeFxTex(128, (u, v) => { float r = Mathf.Sqrt(u * u + v * v); return r > 1f ? 0f : Mathf.Exp(-Mathf.Pow((r - 0.86f) / 0.07f, 2f)); });
            fxArc = MakeFxTex(128, (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v);
                if (r > 1f) return 0f;
                float band = Mathf.Exp(-Mathf.Pow((r - 0.78f) / 0.10f, 2f));
                float deg = Mathf.Abs(Mathf.Atan2(v, u)) * Mathf.Rad2Deg;
                return band * Mathf.Clamp01(1f - (deg / 75f) * (deg / 75f));
            });
            // 가로 막대: 세로로는 부드럽게, 양 끝은 짧게 흐려진다 (선 · 빔 · 궤적 · 벽)
            fxLine = MakeFxTex(64, (u, v) => Mathf.Exp(-(v / 0.45f) * (v / 0.45f)) * Mathf.Clamp01((1f - Mathf.Abs(u)) / 0.15f));
        }

        /// <summary>흰색 + 알파 텍스처. alpha(u, v): 둘 다 -1 ~ 1 (u 가 +x).</summary>
        static Texture2D MakeFxTex(int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                px[y * size + x] = new Color(1f, 1f, 1f, alpha(u, v));
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static float Ease(float t) => 1f - (1f - t) * (1f - t);
        static float AngleOf(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        static Color Light(Color c) => Color.Lerp(c, Color.white, 0.65f);
        static Color Core(Color c) => new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f);

        static void DrawFx(Texture2D tex, Vector2 center, float size, Color c, float alpha, float angleDeg = 0f)
            => DrawFxRect(tex, center, size, size, c, alpha, angleDeg);

        static void DrawFxRect(Texture2D tex, Vector2 center, float w, float h, Color c, float alpha, float angleDeg = 0f)
        {
            if (w <= 0.5f || h <= 0.5f || alpha <= 0.01f) return;
            Matrix4x4 old = GUI.matrix;
            if (angleDeg != 0f)
            {
                // 가상 좌표(확대 전) 기준으로 회전. GUIUtility.RotateAroundPivot 은 확대된 화면 좌표를 써서 쓰지 않는다.
                Vector3 p = center;
                GUI.matrix = old * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, angleDeg), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
            }
            Color oldColor = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
            GUI.DrawTexture(new Rect(center.x - w / 2f, center.y - h / 2f, w, h), tex, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
            GUI.matrix = old;
        }

        /// <summary>은은한 광채 (선명도 보강).</summary>
        static void Glow(Vector2 center, float size, Color c, float alpha) => DrawFx(fxDisc, center, size * GlowSize, c, alpha * GlowAlpha);

        /// <summary>a → b 선분 (두께 thick).</summary>
        static void DrawLine(Vector2 a, Vector2 b, float thick, Color c, float alpha)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 1f) return;
            DrawFxRect(fxLine, (a + b) * 0.5f, len + thick, thick, c, alpha, AngleOf(d));
        }

        /// <summary>연타 위치 · 각도용 결정적 난수 (프레임마다 같은 값).</summary>
        static float Hash(float seed, int k) => Mathf.Repeat(Mathf.Sin(seed * 12.9898f + k * 78.233f) * 43758.5453f, 1f);

        static float FxLength(FieldEffect e)
        {
            float ern = ErnEffectLength(e);
            if (ern > 0f) return ern;
            switch (e.Kind)
            {
                case FieldEffectKind.Spin: return IsStium(e) ? SheetTime("stiumSpin") : FxSpin * FxTimeMul;
                case FieldEffectKind.Smash: return IsStium(e) ? SheetTime("boomDark") : SheetTime("slam");
                case FieldEffectKind.Burst: return SheetTime(VendettaBurstSheet(e) ?? BurstSheet(e));
                case FieldEffectKind.Pulse: return IsStiumPulse(e) ? SheetTime("darkPulse") : FxPulse * FxTimeMul;
                case FieldEffectKind.Grasp: return IsStium(e) ? SheetTime("grasp") : FxGrasp * FxTimeMul;
                case FieldEffectKind.Harvest: return IsStium(e) ? HarvestLength : FxHarvest * FxTimeMul;
                case FieldEffectKind.Slash: return IsFireSlash(e) ? SheetTime(FireSlashCleave.Sheet) : FxSlash * FxTimeMul;
                case FieldEffectKind.Swing:
                {
                    if (!HasWeaponSwing(e)) return FxSwing * FxTimeMul;
                    var weapon = WeaponOf(e);
                    // 내려베기를 쓰는 무기는 잔흔이 남는 동안
                    var cleave = CleaveOf(e);
                    return cleave != null ? SheetTime(cleave.Sheet) : WeaponSwingTime(weapon);
                }
                case FieldEffectKind.Multi: return MultiStep(e) * Mathf.Max(1, e.Count) + FxMultiOne * FxTimeMul;
                case FieldEffectKind.Trail: return Mathf.Max(0.1f, e.Duration) + FxAfter * FxTimeMul;
                case FieldEffectKind.Leap: return Mathf.Max(0.1f, e.Duration) + SheetTime("landing");
                case FieldEffectKind.Pillar: return e.SkillId == "mel_thunder" ? SheetTime("lightning") : FxPillar * FxTimeMul;
                case FieldEffectKind.Heal: return SheetTime("heal");
                case FieldEffectKind.Buff: return SheetTime("buff");
                case FieldEffectKind.Rain: return Mathf.Max(1, e.Count) * FxRainStep + (FxRainFall + FxBurst) * FxTimeMul;
                case FieldEffectKind.Bite: return FxBite * FxTimeMul;
                case FieldEffectKind.Beam: return FxBeam * FxTimeMul;
                case FieldEffectKind.Cone: return FxCone * FxTimeMul;
                case FieldEffectKind.Roar: return FxRoar * FxTimeMul;
                case FieldEffectKind.Lob: return FxLob;               // 비행 시간과 맞춘다 (보조 이펙트 지연과 같음)
                case FieldEffectKind.MapSlash: return FxMapSlash * FxTimeMul;
                case FieldEffectKind.Cross: return e.Duration + FxCrossBlast * FxTimeMul;
                case FieldEffectKind.Charge: return e.Duration + FxChargeBlast * FxTimeMul;
                case FieldEffectKind.Blink: return Mathf.Max(0.5f, e.Duration) + FxBlinkGhost;
                case FieldEffectKind.Aim: return Mathf.Max(0.1f, e.Duration);
                case FieldEffectKind.Shield:
                case FieldEffectKind.Zone:
                case FieldEffectKind.Wall:
                case FieldEffectKind.Links: return e.Duration > 0f ? e.Duration : FxDefaultHold;
                default: return SpecialLength(e);
            }
        }

        /// <summary>연타 간격: 지속시간이 정해져 있으면 그 안에 고르게 (엑스큐션 2초 10회).</summary>
        static float MultiStep(FieldEffect e) => e.Duration > 0f ? e.Duration / Mathf.Max(1, e.Count) : FxMultiStep;

        static void DrawEffects(Rect field, float unit, FieldBattle b)
        {
            EnsureFx();
            Vector2 P(Vector2 v) => new Vector2(field.x + v.x * unit, field.y + v.y * unit);
            float U = unit * FxScale;      // 이펙트 크기 단위 (배율 적용)

            foreach (var e in b.Effects)
            {
                float age = b.Time - e.Start - e.Delay;
                if (age < 0f) continue;
                if (!e.Spawned) { e.Spawned = true; SpawnForEffect(e, P, unit); }
                float len = FxLength(e);
                float t = age / len;
                if (t >= 1f) continue;

                Color main = e.Color, light = Light(main), core = Core(main);
                Vector2 c = e.Target != null && e.Target.Alive ? P(e.Target.Pos) : P(e.Origin);
                float R = Mathf.Max(0.3f, e.Radius) * U;
                Vector2 follow = e.Source != null && e.Source.Alive ? P(e.Source.Pos) : c;
                // 여러 지점에 같은 이펙트 (모든 적 · 폭격 등)
                Vector2[] spots = e.Points != null && e.Points.Length > 0 && e.Kind != FieldEffectKind.Harvest
                                  && e.Kind != FieldEffectKind.Rain && e.Kind != FieldEffectKind.Links ? ToScreen(e.Points, P) : new[] { c };

                if (e.Theme == FxTheme.Black && (e.Kind == FieldEffectKind.Pulse
                    || e.Kind == FieldEffectKind.Zone || e.Kind == FieldEffectKind.Links || e.Kind == FieldEffectKind.Shield))
                {
                    float da = e.Kind == FieldEffectKind.Zone ? 0.6f : 0.7f * (1f - t);
                    foreach (var sp in spots) DrawFx(fxDisc, e.Kind == FieldEffectKind.Shield ? follow : sp, R * 2.6f, new Color(0.02f, 0f, 0.06f), da);
                }

                if (DrawErnEffect(e, c, follow, unit, U, age, P)) continue;

                switch (e.Kind)
                {
                    case FieldEffectKind.Spin:      // 원형 베기
                    {
                        if (IsStium(e)) { DrawStiumSpin(e, follow, unit, age); break; }
                        float ang = AngleOf(e.Dir) + t * 360f;
                        Glow(c, R * 2.2f, main, 1f - t);
                        DrawFx(fxArc, c, R * 2.3f, light, 1f - t, ang);
                        DrawFx(fxArc, c, R * 2.3f, main, 0.75f * (1f - t), ang - 45f);
                        DrawFx(fxArc, c, R * 2.1f, Color.white, 0.6f * (1f - t), ang + 10f);
                        DrawFx(fxRing, c, R * 2.2f, main, 0.45f * (1f - t));
                        break;
                    }
                    case FieldEffectKind.Smash:     // 원형 파괴: 지면 강타 시트
                        foreach (var sp in spots)
                        {
                            if (IsStium(e)) DrawStiumImpact(sp, unit, age);
                            else DrawSheet("slam", Feet(sp, unit), unit, age, Color.white);
                        }
                        break;
                    case FieldEffectKind.Burst:     // 폭발 · 적중: 테마별 시트 (적중 섬광은 이펙트 색을 입힌다)
                    {
                        if (e.SkillId == DummyBolt) break;     // 더미 원거리 공격: 적중 폭발 없음 (사용자 2026-11-27)
                        var vendetta = VendettaBurstSheet(e);
                        if (vendetta != null)
                        {
                            foreach (var sp in spots) DrawVendettaBurst(vendetta, sp, unit, age, e.Dir);
                            break;
                        }
                        string key = BurstSheet(e);
                        Color tint = key == "hit" ? light : Color.white;
                        foreach (var sp in spots) DrawSheet(key, Feet(sp, unit), unit, age, tint, e.Dir.x < 0f);
                        break;
                    }
                    case FieldEffectKind.Pulse:     // 퍼지는 원
                    {
                        if (IsStiumPulse(e)) { DrawStiumPulse(c, unit, age); break; }
                        float size = R * 2.3f * Ease(t);
                        Glow(c, size, main, 1f - t);
                        DrawFx(fxDisc, c, size, core, 0.35f * (1f - t));
                        DrawFx(fxRing, c, size, main, 1f - t);
                        DrawFx(fxRing, c, size * 0.82f, light, 0.7f * (1f - t));
                        break;
                    }
                    case FieldEffectKind.Grasp:     // 퍼진 원이 한곳으로 모임
                        if (IsStium(e)) break;      // 스티움: 유닛 아래 시트 (DrawGroundEffects)
                        if (t < FxGraspSpread)
                        {
                            float a = Ease(t / FxGraspSpread);
                            DrawFx(fxDisc, c, R * 2.2f * a, core, 0.5f);
                            DrawFx(fxRing, c, R * 2.3f * a, main, 0.75f * a);
                        }
                        else
                        {
                            float s = 1f - Ease((t - FxGraspSpread) / (1f - FxGraspSpread));
                            Vector2 mid = Vector2.Lerp(follow, c, s);
                            DrawFx(fxDisc, mid, R * 2.2f * s, core, 0.5f * s + 0.1f);
                            DrawFx(fxRing, mid, R * 2.3f * s, main, 1f);
                            DrawFx(fxRing, mid, R * 1.6f * s, light, 0.7f);
                        }
                        break;
                    case FieldEffectKind.Harvest:   // 대상들에게서 파티클이 끌려옴
                    {
                        if (IsStium(e)) { DrawStiumHarvest(e, follow, unit, age, P); break; }
                        if (e.Points == null) break;
                        foreach (var pt in e.Points)
                        {
                            Vector2 start = P(pt);
                            DrawFx(fxDisc, start, U * 1.0f, main, 0.7f * (1f - t));
                            Vector2 d = follow - start;
                            Vector2 perp = d.sqrMagnitude > 0.01f ? new Vector2(-d.y, d.x).normalized : Vector2.zero;
                            for (int k = 0; k < FxHarvestParticles; k++)
                            {
                                float s = Mathf.Clamp01((t - k * FxHarvestStagger) / (1f - FxHarvestParticles * FxHarvestStagger));
                                if (s <= 0f || s >= 1f) continue;
                                float side = (k % 2 == 0 ? 1f : -1f) * (0.3f + 0.12f * k) * unit;
                                Vector2 pos = Vector2.Lerp(start, follow, s * s) + perp * (Mathf.Sin(s * Mathf.PI) * side);
                                DrawFx(fxDisc, pos, U * 0.38f, light, 1f);
                                DrawFx(fxDisc, pos, U * 0.75f, main, 0.5f);
                            }
                        }
                        break;
                    }
                    case FieldEffectKind.Slash:     // 짧은 베기
                        if (IsFireSlash(e)) { DrawFireSlash(e, follow, unit, U, age); break; }
                        foreach (var sp in spots)
                        {
                            float ang = AngleOf(e.Dir) - 60f + 120f * Ease(t);
                            float size = Mathf.Max(R, U * 0.9f) * 2.2f;
                            Glow(sp, size * 0.8f, main, 1f - t);
                            DrawFx(fxArc, sp, size, light, 1f - t, ang);
                            DrawFx(fxArc, sp, size * 1.05f, main, 0.7f * (1f - t), ang - 25f);
                        }
                        break;
                    default:                        // 세부 연출 (GameView.Fx2.cs)
                        DrawSpecial(field, unit, U, b, e, c, follow, t, age, P);
                        break;
                    case FieldEffectKind.Aim:       // 저격 조준: 양쪽의 직선 두 개가 대상 쪽으로 모인다
                    {
                        Vector2 from = follow, to = c;
                        Vector2 d = to - from;
                        float spread = 28f * (1f - Ease(t));
                        for (int k = -1; k <= 1; k += 2)
                        {
                            Vector2 end = from + Rotate(d, k * spread * Mathf.Deg2Rad);
                            DrawLine(from, end, U * 0.22f, main, 0.45f);
                            DrawLine(from, end, U * 0.07f, Color.white, 0.9f);
                        }
                        float ring = U * Mathf.Lerp(2.6f, 0.9f, t);
                        DrawFx(fxRing, to, ring, new Color(1f, 0.35f, 0.3f), 0.5f + 0.5f * t);
                        if (t > 0.85f) DrawFx(fxDisc, to, U * 0.5f, new Color(1f, 0.35f, 0.3f), 1f);
                        break;
                    }
                    case FieldEffectKind.Swing:     // 기본 공격 (무기별)
                        DrawSwing(e, c, P(e.End), unit, U, t, age, main, light);
                        break;
                    case FieldEffectKind.Multi:     // 연타: 여러 번의 짧은 베기
                    {
                        int n = Mathf.Max(1, e.Count);
                        float step = MultiStep(e), one = FxMultiOne * FxTimeMul;
                        float spread = Mathf.Max(R, U * 0.8f);
                        for (int k = 0; k < n; k++)
                        {
                            float s = (age - k * step) / one;
                            if (s < 0f || s >= 1f) continue;
                            Vector2 off = new Vector2(Hash(e.Start, k) - 0.5f, Hash(e.Start, k + 50) - 0.5f) * spread;
                            float ang = Hash(e.Start, k + 100) * 360f;
                            foreach (var sp in spots)
                            {
                                DrawFx(fxArc, sp + off, U * 1.8f, light, 1f - s, ang + 90f * s);
                                DrawFx(fxArc, sp + off, U * 1.9f, main, 0.6f * (1f - s), ang + 70f * s);
                                DrawFx(fxDisc, sp + off, U * 0.8f, main, 0.55f * (1f - s));
                            }
                        }
                        break;
                    }
                    case FieldEffectKind.Trail:     // 돌진 궤적
                    case FieldEffectKind.Leap:      // 도약 궤적 (위로 솟는 곡선)
                    {
                        float travel = Mathf.Max(0.1f, e.Duration);
                        float prog = Mathf.Clamp01(age / travel);
                        float fade = age <= travel ? 1f : 1f - (age - travel) / (FxAfter * FxTimeMul);
                        Vector2 a = c, z = P(e.End);
                        const int Steps = 12;
                        Vector2 prev = a;
                        for (int k = 1; k <= Steps; k++)
                        {
                            float s = prog * k / Steps;
                            Vector2 q = Vector2.Lerp(a, z, s);
                            if (e.Kind == FieldEffectKind.Leap) q.y -= Mathf.Sin(s * Mathf.PI) * unit * 1.5f;
                            DrawLine(prev, q, U * 0.9f, main, 0.3f * fade);
                            DrawLine(prev, q, U * 0.55f, main, 0.6f * fade);
                            DrawLine(prev, q, U * 0.22f, light, fade);
                            prev = q;
                        }
                        if (e.Kind == FieldEffectKind.Leap && age > travel)
                            DrawSheet("landing", Feet(z, unit), unit, age - travel, Color.white);
                        break;
                    }
                    case FieldEffectKind.Pillar:    // 위에서 떨어지는 빛 기둥 (번개 · 불기둥 · 빛)
                        if (e.SkillId == "mel_thunder")        // 썬더 브레이커: 벼락
                        {
                            foreach (var sp in spots) DrawSheet("lightning", Feet(sp, unit), unit, age, Color.white);
                            break;
                        }
                        foreach (var sp in spots)
                        {
                            float a = 1f - t;
                            Vector2 top = sp + new Vector2(0f, -U * 1.7f);
                            DrawFxRect(fxLine, top, U * 3.6f, U * 1.2f, main, a * 0.45f, 90f);
                            DrawFxRect(fxLine, top, U * 3.6f, U * 0.8f, main, a * 0.8f, 90f);
                            DrawFxRect(fxLine, top, U * 3.6f, U * 0.3f, light, a, 90f);
                            Glow(sp, R * 2f, main, a);
                            DrawFx(fxDisc, sp, R * 2f * Mathf.Lerp(0.5f, 1.2f, Ease(t)), light, a * 0.85f);
                            DrawFx(fxRing, sp, R * 2.4f * Ease(t), main, a);
                        }
                        break;
                    case FieldEffectKind.Heal:      // 회복
                        DrawSheet("heal", Feet(follow, unit), unit, age, Color.white);
                        break;
                    case FieldEffectKind.Shield:    // 보호막 · 자세: 대상을 따라다니는 구체
                    {
                        float a = Mathf.Min(1f, age / 0.15f) * Mathf.Clamp01((len - age) / 0.25f);
                        Color tint = light; tint.a = a;
                        if (e.SkillId == "mel_divine_shield")   // 디바인 쉴드: 큰 보호막
                            DrawSheet("shieldBig", follow, unit, age, new Color(1f, 1f, 1f, a), false, true);
                        else DrawSheet("shield", Feet(follow, unit), unit, age, tint, false, true);
                        break;
                    }
                    case FieldEffectKind.Buff:      // 강화: 빛줄기
                        DrawSheet("buff", Feet(follow, unit), unit, age, light);
                        break;
                    case FieldEffectKind.Rain:      // 폭격: 위에서 떨어진 뒤 터짐
                    {
                        if (e.Points == null) break;
                        float fall = FxRainFall * FxTimeMul, burst = FxBurst * FxTimeMul;
                        for (int k = 0; k < e.Points.Length; k++)
                        {
                            float s = age - k * FxRainStep;
                            if (s < 0f) continue;
                            Vector2 sp = P(e.Points[k]) + new Vector2((Hash(e.Start, k) - 0.5f) * unit, (Hash(e.Start, k + 9) - 0.5f) * unit);
                            if (s < fall)
                            {
                                float f = s / fall;
                                Vector2 pos = sp + new Vector2(unit * 1.5f * (1f - f), -unit * 4f * (1f - f));
                                DrawFx(fxDisc, pos, U * 0.5f, light, 1f);
                                DrawLine(pos, pos + new Vector2(unit * 0.6f, -unit * 1.6f), U * 0.22f, main, 0.7f);
                            }
                            else if (s < fall + burst)
                            {
                                float f = (s - fall) / burst;
                                Glow(sp, R * 2f, main, 1f - f);
                                DrawFx(fxDisc, sp, R * 2f * Mathf.Lerp(0.4f, 1.2f, Ease(f)), core, 0.85f * (1f - f));
                                DrawFx(fxRing, sp, R * 2.2f * Ease(f), main, 1f - f);
                            }
                        }
                        break;
                    }
                    case FieldEffectKind.Zone:      // 장판
                    {
                        float a = Mathf.Min(1f, age / 0.2f) * Mathf.Min(1f, (len - age) / 0.4f);
                        float pulse = 1f + 0.04f * Mathf.Sin(age * 6f);
                        float r0 = Mathf.Max(0.3f, e.Radius) * unit;   // 장판은 판정 범위 그대로
                        DrawFx(fxDisc, c, r0 * 2.2f * pulse, main, 0.38f * a);
                        DrawFx(fxRing, c, r0 * 2.2f * pulse, light, 0.85f * a);
                        break;
                    }
                    case FieldEffectKind.Wall:      // 벽
                    {
                        float a = Mathf.Min(1f, age / 0.2f) * Mathf.Min(1f, (len - age) / 0.4f);
                        DrawLine(c, P(e.End), U * 0.9f, main, 0.4f * a);
                        DrawLine(c, P(e.End), U * 0.6f, main, 0.8f * a);
                        DrawLine(c, P(e.End), U * 0.25f, light, a);
                        break;
                    }
                    case FieldEffectKind.Cross:     // 십자가: 박힌 뒤(Duration) 폭발. Count 1 = 맵을 꿰뚫는 레이저 십자
                    {
                        if (age < e.Duration)
                        {
                            float a = Mathf.Min(1f, age / 0.15f) * (0.75f + 0.25f * Mathf.Sin(age * 12f));
                            bool inverted = e.Count == 1;
                            float barY = inverted ? U * 0.3f : -U * 0.3f;   // 역십자가는 가로대가 아래
                            Glow(c, U * 1.8f, main, a);
                            DrawFxRect(fxLine, c, U * 0.55f, U * 1.9f, light, a);
                            DrawFxRect(fxLine, c + new Vector2(0f, barY), U * 1.2f, U * 0.5f, light, a);
                            DrawFx(fxRing, c, U * 1.9f, main, 0.6f * a);
                        }
                        else
                        {
                            float f = (age - e.Duration) / (FxCrossBlast * FxTimeMul);
                            if (e.Count == 1)
                            {
                                // 레이저: 필드 끝에서 끝까지
                                Vector2 l = new Vector2(field.x, c.y), r = new Vector2(field.xMax, c.y);
                                Vector2 up = new Vector2(c.x, field.y), dn = new Vector2(c.x, field.yMax);
                                float w = U * Mathf.Lerp(1.2f, 0.4f, f);
                                DrawLine(l, r, w * 1.8f, main, 0.35f * (1f - f)); DrawLine(up, dn, w * 1.8f, main, 0.35f * (1f - f));
                                DrawLine(l, r, w, main, 0.9f * (1f - f)); DrawLine(up, dn, w, main, 0.9f * (1f - f));
                                DrawLine(l, r, w * 0.35f, light, 1f - f); DrawLine(up, dn, w * 0.35f, light, 1f - f);
                            }
                            else
                            {
                                float l = R * 2.2f * Mathf.Lerp(0.3f, 1f, Ease(f));
                                DrawFxRect(fxLine, c, l, U * 0.9f, main, 1f - f);
                                DrawFxRect(fxLine, c, l, U * 0.9f, main, 1f - f, 90f);
                                DrawFxRect(fxLine, c, l, U * 0.35f, light, 1f - f);
                                DrawFxRect(fxLine, c, l, U * 0.35f, light, 1f - f, 90f);
                            }
                            DrawFx(fxDisc, c, U * 2.2f, light, 0.75f * (1f - f));
                        }
                        break;
                    }
                    case FieldEffectKind.Bite:      // 물어뜯기: 위아래 두 호가 닫힌다
                        foreach (var sp in spots)
                        {
                            float size = Mathf.Max(R, U * 0.9f) * 2f;
                            float gap = size * 0.35f * (1f - Ease(Mathf.Min(1f, t * 2f)));
                            float a = 1f - t;
                            Glow(sp, size, main, a);
                            DrawFx(fxArc, sp + new Vector2(0f, -gap), size, light, a, 90f);
                            DrawFx(fxArc, sp + new Vector2(0f, gap), size, light, a, -90f);
                            DrawFx(fxDisc, sp, size * 0.6f, main, 0.5f * a);
                        }
                        break;
                    case FieldEffectKind.Links:     // 사슬: 대상들에게 붙어 따라다니는 선
                    {
                        float a = Mathf.Min(1f, age / 0.2f) * Mathf.Min(1f, (len - age) / 0.4f) * (0.8f + 0.2f * Mathf.Sin(age * 8f));
                        var pts = new List<Vector2>();
                        if (e.Units != null) { foreach (var lu in e.Units) if (lu != null && lu.Alive) pts.Add(P(lu.Pos)); }
                        else if (e.Points != null) pts.AddRange(ToScreen(e.Points, P));
                        pts.Sort((x, y) => x.x.CompareTo(y.x));
                        for (int k = 0; k < pts.Count; k++)
                        {
                            if (k > 0) DrawChain(pts[k - 1], pts[k], U, main, light, a);
                            DrawFx(fxRing, pts[k], U * 1.4f, main, 0.85f * a);
                        }
                        break;
                    }
                    case FieldEffectKind.Charge:    // 심판 준비(Duration): 모여드는 고리 → 일격 + 큰 파동
                        if (age < e.Duration)
                        {
                            float cyc = Mathf.Repeat(age * 2f, 1f);
                            DrawFx(fxRing, c, U * Mathf.Lerp(3.2f, 0.6f, cyc), light, cyc);
                            DrawFx(fxDisc, c, U * Mathf.Lerp(0.5f, 1.8f, age / e.Duration), main, 0.55f);
                        }
                        else
                        {
                            float f = (age - e.Duration) / (FxChargeBlast * FxTimeMul);
                            Glow(c, U * 3f, main, 1f - f);
                            DrawFx(fxDisc, c, U * 2.6f * Mathf.Lerp(0.5f, 1.2f, Ease(f)), light, 1f - f);
                            DrawFx(fxRing, c, R * 2.2f * Ease(f), main, 1f - f);
                            DrawFx(fxRing, c, R * 1.6f * Ease(f), light, 0.7f * (1f - f));
                        }
                        break;
                    case FieldEffectKind.Lob:       // 포션 투척: 포물선으로 날아가는 병
                    {
                        Vector2 a = c, z = P(e.End);
                        Vector2 pos = Vector2.Lerp(a, z, t);
                        pos.y -= Mathf.Sin(t * Mathf.PI) * unit * 1.2f;
                        if (DrawPotion(e.SkillId, pos, unit, age)) break;
                        DrawFx(fxDisc, pos, U * 0.8f, main, 0.65f);
                        DrawFx(fxDisc, pos, U * 0.4f, light, 1f);
                        break;
                    }
                    case FieldEffectKind.Roar:      // 포효: 크게 퍼지는 고리 3겹
                        for (int k = 0; k < 3; k++)
                        {
                            float s = (age - k * FxRoarStep) / (len - 2 * FxRoarStep);
                            if (s < 0f || s >= 1f) continue;
                            DrawFx(fxRing, follow, unit * 16f * Ease(s), k == 0 ? light : main, 1f - s);
                        }
                        break;
                    case FieldEffectKind.Beam:      // 일직선 빔 · 사슬 · 저격선
                    {
                        float width = Mathf.Max(0.3f, e.Radius) * 2f * U;
                        float a = 1f - t;
                        DrawLine(c, P(e.End), width * Mathf.Lerp(1.8f, 1f, t), main, 0.3f * a);
                        DrawLine(c, P(e.End), width * Mathf.Lerp(1.2f, 0.6f, t), main, 0.8f * a);
                        DrawLine(c, P(e.End), width * 0.35f, light, a);
                        break;
                    }
                    case FieldEffectKind.Cone:      // 부채꼴: 커지며 휩쓰는 호 (각도가 넓으면 두 장)
                    {
                        float ang = AngleOf(e.Dir);
                        float size = R * 2.5f * Mathf.Lerp(0.4f, 1f, Ease(t));
                        if (e.Count > 140)
                        {
                            DrawFx(fxArc, c, size, light, 1f - t, ang - 40f);
                            DrawFx(fxArc, c, size, light, 1f - t, ang + 40f);
                        }
                        else DrawFx(fxArc, c, size, light, 1f - t, ang);
                        DrawFx(fxArc, c, size * 0.85f, main, 0.8f * (1f - t), ang);
                        DrawFx(fxArc, c, size * 1.1f, main, 0.35f * (1f - t), ang);
                        break;
                    }
                    case FieldEffectKind.Blink:     // 순간이동 잔상 + 참격 (멸령부)
                    {
                        int n = Mathf.Max(1, e.Count);
                        float dur = Mathf.Max(0.5f, e.Duration);
                        float r0 = Mathf.Max(R, U * 1.1f);
                        for (int k = 0; k < n; k++)
                        {
                            float s = (age - dur * k / n) / FxBlinkGhost;
                            if (s < 0f || s >= 1f) continue;
                            float ang = Hash(e.Start, k) * Mathf.PI * 2f;
                            Vector2 pos = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r0;
                            float body = FieldConst.Radius * 2f * unit;
                            DrawFxRect(Texture2D.whiteTexture, pos, body, body, main, 0.45f * (1f - s));          // 잔상
                            DrawFx(fxArc, c, U * 1.9f, light, 1f - s, ang * Mathf.Rad2Deg + 180f);                  // 참격
                            DrawLine(pos, c, U * 0.2f, light, 0.8f * (1f - s));

                            // 대상을 가로지르는 큰 베기 (사용자 2026-11-27): 순식간에 그어지고 갈라진 선이 남는다
                            float cutAng = ang + Mathf.PI + (Hash(e.Start, k + 20) - 0.5f) * 0.9f;
                            Vector2 cut = new Vector2(Mathf.Cos(cutAng), Mathf.Sin(cutAng));
                            float grow = Ease(Mathf.Clamp01(s / 0.25f));
                            Vector2 head = c - cut * (U * 1.9f), tail = head + cut * (U * 3.8f * grow);
                            float fade = 1f - s;
                            DrawLine(head, tail, U * 0.75f * fade, main, 0.75f * fade);
                            DrawLine(head, tail, U * 0.32f * fade, light, fade);
                            DrawLine(head, tail, U * 0.12f, Color.white, fade);
                            float arcAng = cutAng * Mathf.Rad2Deg - 70f + 140f * Ease(Mathf.Clamp01(s * 2f));
                            DrawFx(fxArc, c, U * 3f, light, 0.9f * fade, arcAng);
                            DrawFx(fxArc, c, U * 3.25f, main, 0.6f * fade, arcAng - 24f);
                            DrawFx(fxDisc, c, U * 1.1f * (1f - s * 0.5f), Color.white, 0.55f * fade * fade);
                        }
                        break;
                    }
                    case FieldEffectKind.MapSlash:  // 맵 전체를 가르는 참격
                    {
                        float a = 1f - t;
                        for (int k = 0; k < 4; k++)
                        {
                            float y0 = field.y + field.height * (0.15f + 0.23f * k);
                            float tilt = field.height * 0.25f * (k % 2 == 0 ? 1f : -1f);
                            Vector2 l = new Vector2(field.x, y0 - tilt), r = new Vector2(field.xMax, y0 + tilt);
                            float prog = Mathf.Clamp01(t * 3f - k * 0.25f);
                            Vector2 head = Vector2.Lerp(l, r, prog);
                            DrawLine(l, head, U * 0.9f, main, 0.5f * a);
                            DrawLine(l, head, U * 0.3f, light, a);
                        }
                        break;
                    }
                }
            }

            DrawWaveRemnants(b, unit, P);

            // 감아오기: 대상을 문 족쇄와 펜리르까지의 사슬
            foreach (var u in b.Players)
                if (u.Alive && u.Reeled != null && u.Reeled.Alive)
                {
                    Vector2 hand = P(u.Pos), bite = P(u.Reeled.Pos);
                    if (!DrawShackleChain(hand, bite, u.Facing, unit))
                        DrawChain(hand, bite, U, SkillColor.Steel, Light(SkillColor.Steel), 1f);
                }

            // 투사체 (모양: 아치 · 구체 · 탄환 · 화살 · 부적 · 얼음 조각 · 포션)
            foreach (var p in b.Projectiles)
            {
                if (p.Delay > 0f) continue;
                Color main = p.Skill.FxColor, light = Light(main);
                float ang = AngleOf(p.Dir);
                Vector2 pos = P(p.Pos);
                if (p.Skill.Id == "fr_chain")             // 족쇄의 사슬: 도트 (읽지 못하면 아래의 도형 사슬 + 구체)
                {
                    Vector2 hand = p.Source.Alive ? P(p.Source.Pos) : pos;
                    if (DrawShackleChain(hand, pos, p.Dir, unit)) continue;
                    DrawChain(hand, pos, U, main, light, 1f);
                }
                if (p.Skill.Id == "stium_darkwave")      // 어둠의 검기: 반복 재생하는 초승달
                {
                    DrawSheet("darkWave", pos, unit, b.Time, Color.white, false, true, ang);
                    continue;
                }
                if (p.Source != null && p.Source.Theme == FxTheme.Black) DrawFx(fxDisc, pos, U * 1.7f, new Color(0.02f, 0f, 0.06f), 0.85f);
                switch (p.Skill.Shot)
                {
                    case ShotStyle.Orb:
                        DrawLine(P(p.Pos - p.Dir * 0.9f), pos, U * 0.45f, main, 0.55f);
                        Glow(pos, U * 0.9f, main, 1f);
                        DrawFx(fxDisc, pos, U * 0.95f, main, 0.8f);
                        DrawFx(fxDisc, pos, U * 0.5f, light, 1f);
                        break;
                    case ShotStyle.Bullet:
                        DrawLine(P(p.Pos - p.Dir * 0.9f), pos, U * 0.2f, main, 0.7f);
                        DrawFxRect(fxLine, pos, U * 0.5f, U * 0.24f, light, 1f, ang);
                        break;
                    case ShotStyle.Arrow:
                        DrawLine(P(p.Pos - p.Dir * 1.3f), pos, U * 0.14f, light, 0.95f);
                        DrawFx(fxDisc, pos, U * 0.4f, light, 1f);
                        break;
                    case ShotStyle.Card:
                    {
                        float spin = b.Time * 720f;
                        DrawFxRect(Texture2D.whiteTexture, pos, U * 0.38f, U * 0.55f, light, 0.95f, spin);
                        DrawFx(fxDisc, pos, U * 0.9f, main, 0.45f);
                        break;
                    }
                    case ShotStyle.Spark:          // 파티클 탄 (원거리 더미): 작게 빛나는 알갱이 + 입자 꼬리 (꼬리는 파티클 쪽)
                        Glow(pos, U * 0.5f, main, 1f);
                        DrawFx(fxDisc, pos, U * 0.5f, main, 0.9f);
                        DrawFx(fxDisc, pos, U * 0.25f, Color.white, 1f);
                        break;
                    case ShotStyle.Shard:          // 얼음 조각: 회전하는 작은 마름모 + 서리 꼬리
                        DrawLine(P(p.Pos - p.Dir * 0.8f), pos, U * 0.25f, main, 0.55f);
                        DrawFxRect(Texture2D.whiteTexture, pos, U * 0.26f, U * 0.26f, light, 0.95f, b.Time * 540f);
                        DrawFx(fxDisc, pos, U * 0.6f, main, 0.5f);
                        break;
                    case ShotStyle.Potion:         // 포션: 포물선
                    {
                        float k = p.Total > 0f ? Mathf.Clamp01(p.Traveled / p.Total) : 0f;
                        Vector2 lifted = pos + new Vector2(0f, -Mathf.Sin(k * Mathf.PI) * unit * 1.1f);
                        if (DrawPotion(p.Skill.Id, lifted, unit, b.Time)) break;
                        DrawFx(fxDisc, lifted, U * 0.6f, main, 0.75f);
                        DrawFx(fxDisc, lifted, U * 0.3f, light, 1f);
                        break;
                    }
                    default: // Arc: 일직선으로 날아가는 아치 (어둠의 검기)
                        Glow(pos, U * 1.4f, main, 1f);
                        for (int i = 3; i >= 1; i--)
                            DrawFx(fxArc, P(p.Pos - p.Dir * (0.35f * i)), U * 1.6f, main, 0.5f / i, ang);
                        DrawFx(fxArc, pos, U * 1.7f, light, 1f, ang);
                        break;
                }
            }
        }

        /// <summary>쇠사슬: 고리를 번갈아 90도씩 돌려 잇는다.</summary>
        static void DrawChain(Vector2 a, Vector2 b, float U, Color main, Color light, float alpha)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 2f) return;
            float link = U * 0.45f;
            int n = Mathf.Max(1, Mathf.FloorToInt(len / link));
            float ang = AngleOf(d);
            for (int i = 0; i <= n; i++)
            {
                Vector2 pos = a + d * (i / (float)n);
                bool flat = i % 2 == 0;
                DrawFxRect(fxRing, pos, flat ? link * 1.3f : link * 0.9f, flat ? link * 0.7f : link * 0.4f, i % 2 == 0 ? light : main, alpha, ang);
            }
        }

        /// <summary>기본 공격 휘두르기. Count = BasicKind.</summary>
        static void DrawSwing(FieldEffect e, Vector2 c, Vector2 target, float unit, float U, float t, float age, Color main, Color light)
        {
            var kind = (BasicKind)e.Count;
            float baseAng = AngleOf(e.Dir);
            float a = 1f - t;
            if (HasWeaponSwing(e)) { DrawWeaponSwing(e, c, unit, U, age, main, light); return; }
            switch (kind)
            {
                case BasicKind.Fist:       // 주먹: 대상에 짧은 충격
                    DrawFx(fxDisc, target, U * 0.9f * Mathf.Lerp(0.5f, 1.2f, Ease(t)), light, 0.9f * a);
                    DrawFx(fxRing, target, U * 1.3f * Ease(t), main, a);
                    break;
                case BasicKind.Claw:       // 손톱: 대각선 세 줄
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector2 off = new Vector2(-e.Dir.y, e.Dir.x) * (k * U * 0.28f);
                        Vector2 p0 = target + off + new Vector2(-U * 0.5f, -U * 0.5f), p1 = target + off + new Vector2(U * 0.5f, U * 0.5f);
                        DrawLine(p0, Vector2.Lerp(p0, p1, Ease(Mathf.Min(1f, t * 2f))), U * 0.18f, light, a);
                    }
                    break;
                case BasicKind.HSlash:     // 횡베기 (근접 더미): 대상 앞을 가로로 납작하게 긋는다
                {
                    float sweep = Ease(Mathf.Min(1f, t * 1.6f));
                    Vector2 left = target + new Vector2(-U * 0.9f, 0f), right = target + new Vector2(U * 0.9f, 0f);
                    Vector2 head = Vector2.Lerp(left, right, sweep);
                    DrawFxRect(fxArc, target, U * 2.2f, U * 0.9f, main, 0.6f * a, 90f);   // 납작한 호 (가로)
                    DrawLine(left, head, U * 0.35f, main, 0.7f * a);
                    DrawLine(left, head, U * 0.12f, light, a);
                    break;
                }
                case BasicKind.Katana:     // 태도: 얇고 빠른 베기 + 반짝임
                    DrawFxRect(fxArc, c, U * 1.9f, U * 1.3f, light, a, baseAng - 50f + 100f * Ease(t));
                    DrawFxRect(fxLine, target, U * 1.4f * Ease(t), U * 0.12f, Color.white, a, baseAng + 35f);
                    break;
                default:
                {
                    // 칼 · 대검 · 할버드 · 십자가 · 화염검 · 어둠검 · 글러트니: 호를 그리며 휘두른다 (무기가 클수록 크고 넓게)
                    float size = kind == BasicKind.Halberd ? 2.7f : kind == BasicKind.GreatSword ? 2.3f
                               : kind == BasicKind.Glutton ? 2.5f : kind == BasicKind.Cross ? 2.0f : 1.75f;
                    float sweep = kind == BasicKind.Halberd ? 150f : kind == BasicKind.GreatSword || kind == BasicKind.Glutton ? 140f : 110f;
                    float ang = baseAng - sweep * 0.5f + sweep * Ease(t);
                    DrawFx(fxArc, c, U * size, light, a, ang);
                    DrawFx(fxArc, c, U * size * 1.05f, main, 0.7f * a, ang - 22f);
                    if (kind == BasicKind.GreatSword || kind == BasicKind.Halberd || kind == BasicKind.Glutton)
                        DrawFx(fxArc, c, U * size * 0.9f, main, 0.5f * a, ang - 40f);
                    if (kind == BasicKind.Cross)
                    {
                        DrawFxRect(fxLine, target, U * 0.3f, U * 1.0f, light, a);
                        DrawFxRect(fxLine, target + new Vector2(0f, -U * 0.15f), U * 0.7f, U * 0.25f, light, a);
                    }
                    break;
                }
            }
        }

        // ── 무기를 직접 휘두르는 기본 공격 (사용자 2026-11-27) ──
        enum Weapon { None, GreatSword, OneHand, Halberd, Katana, Cross }

        const float WeaponScale = 0.65f;   // 무기 크기 배율 (사용자 2026-11-27: 너무 크다 → 1.0 에서 줄임) [임시]
        static readonly Color SteelBlade = new Color(0.82f, 0.84f, 0.92f);
        static readonly Color WoodShaft = new Color(0.45f, 0.32f, 0.2f);

        /// <summary>캐릭터별 기본 공격 무기. 없으면 기존 호 이펙트.</summary>
        static Weapon WeaponOf(FieldEffect e)
        {
            if ((BasicKind)e.Count == BasicKind.Glutton || e.Source == null || e.Source.Def == null) return Weapon.None;
            switch (e.Source.Def.Id)
            {
                case "stium":
                case "c38": return Weapon.GreatSword;   // 레온하르트: 사자왕의 석대검 (사용자 2026-11-27)
                case "ern":                 // 에른
                case "c41":                 // 사하라
                case "c48": return Weapon.OneHand;   // 가니악
                case "c06": return Weapon.Halberd;   // 메일
                case "c19": return Weapon.Katana;    // 요월
                case "c32": return Weapon.Cross;     // 라바다
                default: return Weapon.None;
            }
        }

        static bool HasWeaponSwing(FieldEffect e) => WeaponOf(e) != Weapon.None;

        static float WeaponSwingTime(Weapon w)
        {
            switch (w)
            {
                case Weapon.GreatSword: return 0.51f;
                case Weapon.Halberd: return 0.42f;
                default: return 0.3f;
            }
        }

        static Vector2 Polar(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));

        /// <summary>뒤로 젖혔다가 베고, 날이 지나간 자리에 잔상. 무기 모양은 종류별로 그린다.</summary>
        const float OneHandReach = 1.86f;   // 손 → 칼끝 (U 단위)

        static void DrawOneHandSword(Vector2 hand, float ang, float U, Color trail, float a)
        {
            Vector2 dir = Polar(ang);
            float length = U * 1.55f, width = U * 0.22f;
            DrawFxRect(Texture2D.whiteTexture, hand + dir * (U * 0.08f), U * 0.34f, U * 0.11f, WoodShaft, a, ang);
            DrawFxRect(Texture2D.whiteTexture, hand + dir * (U * 0.27f), U * 0.1f, U * 0.52f, new Color(0.75f, 0.62f, 0.3f), a, ang);        // 코등이
            Vector2 start = hand + dir * (U * 0.32f);
            Vector2 mid = start + dir * (length * 0.42f);
            DrawFxRect(fxLine, mid, length * 1.2f, width * 3.2f, trail, 0.7f * a, ang);                                          // 속성 기운
            DrawFxRect(Texture2D.whiteTexture, mid, length * 0.84f, width, SteelBlade, a, ang);
            DrawFxRect(Texture2D.whiteTexture, mid, length * 0.84f, width * 0.25f, Color.white, a, ang);
            DrawFxRect(Diamond, start + dir * (length * 0.84f), length * 0.3f, width, SteelBlade, a, ang);
        }

        static void DrawWeaponSwing(FieldEffect e, Vector2 c, float unit, float U, float age, Color main, Color light)
        {
            var w = WeaponOf(e);
            float duration = WeaponSwingTime(w);
            float t = Mathf.Min(1f, age / duration);
            var cleave = CleaveOf(e);       // 위에서 아래로 내려베기 + 잔흔 (사용자 2026-11-27)
            if (cleave != null)
            {
                DrawCleave(cleave, e.Dir, c, unit, U * WeaponScale, age, main);
                return;
            }
            float baseAng = AngleOf(e.Dir);
            float side = Hash(e.Start, 3) < 0.5f ? 1f : -1f;          // 휘두르는 방향을 번갈아
            Vector2 pivot = c - e.Dir * (U * 0.45f);                  // 몸 중심 근처
            U *= WeaponScale;                                          // 무기 · 궤적 크기

            float from = -110f, to = 80f, windup = 0.2f, cut = 0.45f, reach = 2.0f;
            Color trail = main;
            switch (w)
            {
                default: from = -95f; to = 75f; windup = 0.16f; cut = 0.4f; reach = 1.8f; break;
            }

            float swing = t < windup ? 0f : Ease(Mathf.Clamp01((t - windup) / cut));
            float offset = t < windup ? Mathf.Lerp(from + 20f, from, t / windup) : Mathf.Lerp(from, to, swing);
            float ang = baseAng + offset * side;
            float a = t < 0.75f ? 1f : (1f - t) / 0.25f;

            // 지나간 궤적
            if (swing > 0.05f)
                for (int k = 1; k <= 4; k++)
                {
                    float back = k * 24f;
                    if (offset - from < back) break;
                    float ga = baseAng + (offset - back) * side;
                    DrawFx(fxArc, pivot, U * reach * 2f, trail, 0.65f * a * (1f - k * 0.2f), ga);
                    DrawFx(fxArc, pivot, U * reach * 1.7f, light, 0.35f * a * (1f - k * 0.22f), ga);
                }

            Vector2 dir = Polar(ang);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 hand = pivot + dir * (U * 0.35f);

            switch (w)
            {
                case Weapon.OneHand:
                    DrawOneHandSword(hand, ang, U, trail, a);
                    break;
            }

            // 베는 순간의 충격
            if (t > windup + cut * 0.7f)
            {
                float k = Mathf.Clamp01((t - windup - cut * 0.7f) / (1f - windup - cut * 0.7f));
                Vector2 hit = c + e.Dir * (U * 0.9f);
                DrawFx(fxRing, hit, U * Mathf.Lerp(0.6f, w == Weapon.OneHand ? 1.7f : 2.2f, Ease(k)), light, 1f - k);
                DrawFx(fxDisc, hit, U * 1.1f, trail, 0.6f * (1f - k));
            }
        }

        static Vector2[] ToScreen(Vector2[] pts, System.Func<Vector2, Vector2> toScreen)
        {
            var r = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) r[i] = toScreen(pts[i]);
            return r;
        }

        // ───────────────────────── 파티클 ─────────────────────────
        // 화면 전용. 전투 시간 기준으로 움직이고, 일시정지하면 멈춘다. 최대 MaxParticles 개.

        struct Particle
        {
            public Vector2 Pos, Vel;     // 화면(가상) 좌표 · 초당 이동
            public float Life, Max;
            public float Size0, Size1;
            public Color C0, C1;
            public float Gravity, Drag;
            public byte Shape;           // 0 원 · 1 줄무늬(속도 방향) · 2 사각 조각
        }

        const int MaxParticles = 2500;
        static readonly List<Particle> particles = new List<Particle>(MaxParticles);
        static FieldBattle particleBattle;
        static float particleTime;
        static System.Random prng = new System.Random(1);
        static float Rnd(float a, float b) => a + (float)prng.NextDouble() * (b - a);

        static void Add(Particle p)
        {
            if (particles.Count >= MaxParticles) return;
            particles.Add(p);
        }

        /// <summary>테마별 입자 한 개. pos 는 화면 좌표, dir 은 퍼지는 기본 방향(없으면 사방).</summary>
        static void Emit(FxTheme theme, Color color, Vector2 pos, float unit, float power = 1f, Vector2? dir = null)
        {
            float ang = Rnd(0f, Mathf.PI * 2f);
            Vector2 d = dir.HasValue && dir.Value.sqrMagnitude > 0.001f
                ? Rotate(dir.Value.normalized, Rnd(-0.6f, 0.6f)) : new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float sp = unit * Rnd(1.5f, 4f) * power;
            Color light = Light(color);
            switch (theme)
            {
                case FxTheme.Fire:     // 불씨: 위로 떠오르며 노랑 → 빨강
                    Add(new Particle { Pos = pos, Vel = d * sp * 0.7f + new Vector2(0f, -unit * 1.2f), Max = Rnd(0.45f, 0.9f),
                        Size0 = unit * Rnd(0.16f, 0.3f), Size1 = unit * 0.04f, C0 = new Color(1f, 0.85f, 0.35f), C1 = new Color(1f, 0.2f, 0f, 0f),
                        Gravity = -unit * 1.5f, Drag = 2f });
                    if (prng.NextDouble() < 0.25)
                        Add(new Particle { Pos = pos, Vel = new Vector2(Rnd(-0.3f, 0.3f), -1f) * unit * 0.8f, Max = Rnd(0.8f, 1.3f),
                            Size0 = unit * 0.3f, Size1 = unit * 0.8f, C0 = new Color(0.35f, 0.3f, 0.3f, 0.45f), C1 = new Color(0.2f, 0.2f, 0.2f, 0f), Drag = 1f });
                    break;
                case FxTheme.Dark:     // 어둠 연기: 커지며 사라짐
                case FxTheme.Black:
                case FxTheme.Shadow:
                case FxTheme.Gray:
                    Add(new Particle { Pos = pos, Vel = d * sp * 0.4f, Max = Rnd(0.6f, 1.1f),
                        Size0 = unit * Rnd(0.25f, 0.4f), Size1 = unit * Rnd(0.7f, 1.1f),
                        C0 = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.55f), C1 = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0f), Drag = 2.5f });
                    if (prng.NextDouble() < 0.35)
                        Add(new Particle { Pos = pos, Vel = d * sp, Max = Rnd(0.3f, 0.5f), Size0 = unit * 0.14f, Size1 = unit * 0.03f,
                            C0 = light, C1 = new Color(light.r, light.g, light.b, 0f), Drag = 3f });
                    break;
                case FxTheme.Moon:     // 날카로운 반사광: 빠른 줄무늬 + 반짝임
                case FxTheme.Steel:
                    Add(new Particle { Pos = pos, Vel = d * sp * 1.6f, Max = Rnd(0.2f, 0.35f), Size0 = unit * 0.5f, Size1 = unit * 0.1f,
                        C0 = Color.white, C1 = new Color(light.r, light.g, light.b, 0f), Drag = 4f, Shape = 1,
                        Gravity = theme == FxTheme.Steel ? unit * 6f : 0f });
                    if (prng.NextDouble() < 0.4)
                        Add(new Particle { Pos = pos + d * unit * Rnd(0.2f, 0.8f), Max = Rnd(0.3f, 0.5f), Size0 = unit * 0.32f, Size1 = 0f,
                            C0 = Color.white, C1 = new Color(1f, 1f, 1f, 0f) });
                    break;
                case FxTheme.Holy:     // 빛 입자: 천천히 떠오름
                case FxTheme.Gold:
                    Add(new Particle { Pos = pos, Vel = d * sp * 0.6f, Max = Rnd(0.6f, 1.1f), Size0 = unit * Rnd(0.12f, 0.22f), Size1 = 0f,
                        C0 = light, C1 = new Color(color.r, color.g, color.b, 0f), Gravity = -unit * 0.8f, Drag = 2f });
                    break;
                case FxTheme.Ice:      // 얼음 조각: 회전하는 사각 조각이 떨어짐
                case FxTheme.Frost:
                    Add(new Particle { Pos = pos, Vel = d * sp, Max = Rnd(0.35f, 0.6f), Size0 = unit * Rnd(0.14f, 0.22f), Size1 = unit * 0.05f,
                        C0 = light, C1 = new Color(color.r, color.g, color.b, 0f), Gravity = unit * 5f, Drag = 2f, Shape = 2 });
                    break;
                case FxTheme.Spirit:   // 투기: 빠른 붉은 불꽃
                    Add(new Particle { Pos = pos, Vel = d * sp * 1.3f, Max = Rnd(0.25f, 0.45f), Size0 = unit * 0.4f, Size1 = unit * 0.08f,
                        C0 = light, C1 = new Color(color.r, 0.1f, 0f, 0f), Drag = 3.5f, Shape = 1 });
                    break;
                case FxTheme.Potion:   // 거품: 흔들리며 떠오름
                    Add(new Particle { Pos = pos, Vel = d * sp * 0.4f + new Vector2(0f, -unit * 0.6f), Max = Rnd(0.5f, 0.9f),
                        Size0 = unit * Rnd(0.1f, 0.2f), Size1 = unit * 0.25f, C0 = light, C1 = new Color(color.r, color.g, color.b, 0f), Drag = 1.5f });
                    break;
                case FxTheme.Talisman: // 부적 조각: 팔랑이는 작은 사각형
                    Add(new Particle { Pos = pos, Vel = d * sp * 0.8f, Max = Rnd(0.5f, 0.9f), Size0 = unit * 0.2f, Size1 = unit * 0.1f,
                        C0 = light, C1 = new Color(color.r, color.g, color.b, 0f), Gravity = unit * 1.5f, Drag = 2.5f, Shape = 2 });
                    break;
                default:               // 스파크
                    Add(new Particle { Pos = pos, Vel = d * sp * 1.2f, Max = Rnd(0.25f, 0.45f), Size0 = unit * 0.35f, Size1 = unit * 0.05f,
                        C0 = light, C1 = new Color(color.r, color.g, color.b, 0f), Drag = 3f, Shape = 1 });
                    break;
            }
        }

        static Vector2 Rotate(Vector2 v, float rad)
        {
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        /// <summary>테마별 입자 양 (화염은 특히 많게, 사용자 2026-11-26).</summary>
        static float ThemeAmount(FxTheme t) => t == FxTheme.Fire ? 2.2f : t == FxTheme.Dark || t == FxTheme.Moon ? 1.6f : 1f;

        static void Burst(FxTheme theme, Color color, Vector2 pos, float unit, int count, float power = 1f, Vector2? dir = null)
        {
            int n = Mathf.RoundToInt(count * ThemeAmount(theme));
            for (int i = 0; i < n; i++) Emit(theme, color, pos, unit, power, dir);
            // 테마와 별개로 밝은 스파크 몇 개 (타격감)
            for (int i = 0; i < count / 3; i++) Emit(FxTheme.None, color, pos, unit, power * 1.2f, dir);
        }

        /// <summary>이펙트가 처음 나타날 때 한 번 뿌리는 입자.</summary>
        static readonly string DummyBolt = "basic_" + BasicKind.Bolt;

        static void SpawnForEffect(FieldEffect e, System.Func<Vector2, Vector2> P, float unit)
        {
            if (e.Kind == FieldEffectKind.Burst && (IsMelBasic(e) || e.SkillId == "mel_fire_buster" || e.SkillId == DummyBolt)) return;
            var th = e.Theme;
            Color col = e.Color;
            Vector2 c = e.Target != null && e.Target.Alive ? P(e.Target.Pos) : P(e.Origin);
            float R = Mathf.Max(0.3f, e.Radius) * unit * FxScale;
            switch (e.Kind)
            {
                case FieldEffectKind.Burst:
                case FieldEffectKind.Smash:
                case FieldEffectKind.Pillar:
                case FieldEffectKind.Bite:
                    if (e.Points != null && e.Points.Length > 0) foreach (var pt in e.Points) Burst(th, col, P(pt), unit, 8);
                    else Burst(th, col, c, unit, 14, 1.2f);
                    break;
                case FieldEffectKind.IceShatter:    // 얼음 파티클: 진행 방향(대상 뒤)으로
                    for (int k = 0; k < 28; k++) Emit(FxTheme.Ice, col, c + Jitter(unit * 0.2f), unit, Rnd(0.9f, 2f), e.Dir);
                    break;
                case FieldEffectKind.FistSlam:
                case FieldEffectKind.Thrust:
                case FieldEffectKind.BigSlash:
                case FieldEffectKind.BladeSwing:
                case FieldEffectKind.SlantPillar:
                case FieldEffectKind.CrossSlam:
                case FieldEffectKind.Maw:
                case FieldEffectKind.FanWave:
                    Burst(th, col, c, unit, 16, 1.4f, e.Kind == FieldEffectKind.FanWave ? e.Dir : (Vector2?)null);
                    break;
                case FieldEffectKind.Slash:
                case FieldEffectKind.Swing:
                    Burst(th, col, e.Kind == FieldEffectKind.Swing ? P(e.End) : c, unit, 6, 0.9f, e.Dir);
                    break;
                case FieldEffectKind.Multi:
                    Burst(th, col, c, unit, 4 * Mathf.Max(1, e.Count) / 2 + 4);
                    break;
                case FieldEffectKind.Pulse:
                case FieldEffectKind.Spin:
                case FieldEffectKind.Roar:
                {
                    int n = Mathf.RoundToInt(18 * ThemeAmount(th));
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)n * Mathf.PI * 2f;
                        Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Emit(th, col, c + d * R * 0.5f, unit, 1.2f, d);
                    }
                    break;
                }
                case FieldEffectKind.Beam:
                case FieldEffectKind.Trail:
                case FieldEffectKind.Leap:
                case FieldEffectKind.Wall:
                {
                    Vector2 a = c, z = P(e.End);
                    int n = Mathf.RoundToInt(Mathf.Clamp((z - a).magnitude / unit * 1.5f, 6, 40) * ThemeAmount(th));
                    Vector2 perp = new Vector2(-(z - a).y, (z - a).x);
                    for (int i = 0; i < n; i++) Emit(th, col, Vector2.Lerp(a, z, (float)prng.NextDouble()), unit, 0.7f, prng.NextDouble() < 0.5 ? perp : -perp);
                    break;
                }
                case FieldEffectKind.Heal:
                case FieldEffectKind.Buff:
                case FieldEffectKind.Shield:
                    for (int i = 0; i < 8; i++) Emit(th == FxTheme.None ? FxTheme.Holy : th, col, c + new Vector2(Rnd(-0.5f, 0.5f), Rnd(-0.5f, 0.5f)) * unit, unit, 0.5f);
                    break;
                case FieldEffectKind.Charge:
                case FieldEffectKind.Cross:
                case FieldEffectKind.Grasp:
                case FieldEffectKind.Harvest:
                    Burst(th, col, c, unit, 8, 0.8f);
                    break;
            }
        }

        /// <summary>계속 나오는 입자 (투사체 꼬리 · 오라 · 장판 · 잠식 · 사슬) 와 입자 이동 · 그리기.</summary>
        static void UpdateAndDrawParticles(Rect field, float unit, FieldBattle b)
        {
            EnsureFx();
            if (particleBattle != b) { particles.Clear(); particleBattle = b; particleTime = b.Time; }
            float dt = Mathf.Clamp(b.Time - particleTime, 0f, 0.1f);
            particleTime = b.Time;
            Vector2 P(Vector2 v) => new Vector2(field.x + v.x * unit, field.y + v.y * unit);

            if (dt > 0f)
            {
                // 투사체 꼬리
                foreach (var p in b.Projectiles)
                {
                    if (p.Delay > 0f) continue;
                    var th = p.Source != null ? p.Source.Theme : FxTheme.None;
                    float rate = p.Skill.Shot == ShotStyle.Spark ? 70f : 25f * ThemeAmount(th);   // 파티클 탄은 꼬리를 많이
                    int n = Mathf.FloorToInt(dt * rate) + (Chance(dt * rate - Mathf.Floor(dt * rate)) ? 1 : 0);
                    for (int k = 0; k < n; k++) Emit(th, p.Skill.FxColor, P(p.Pos), unit, 0.4f, -p.Dir);
                }
                // 넉백: 밀려나는 자리에 먼지
                foreach (var u in b.Players) KnockDust(u, P, unit, dt);
                foreach (var u in b.Enemies) KnockDust(u, P, unit, dt);

                // 돌파(도주 · 견제) 가속: 뒤로 흩날리는 바람 줄
                foreach (var u in b.Players) DashWind(u, P, unit, dt);
                foreach (var u in b.Enemies) DashWind(u, P, unit, dt);

                // 유닛 상태 오라
                foreach (var u in b.Players)
                {
                    if (!u.Alive) continue;
                    Vector2 pos = P(u.Pos);
                    if (u.FireArm && Chance(dt * 45f)) Emit(FxTheme.Fire, SkillColor.Fire, pos + Jitter(unit * 0.4f), unit, 0.6f);
                    if (u.Glutton && Chance(dt * 25f)) Emit(FxTheme.Gray, SkillColor.Gray, pos + Jitter(unit * 0.4f), unit, 0.5f);
                    if (u.MoonShadow && Chance(dt * 30f)) Emit(FxTheme.Shadow, SkillColor.Shadow, pos + Jitter(unit * 0.4f), unit, 0.5f);
                    if (u.Theme == FxTheme.Fire && !u.FireArm && Chance(dt * 6f)) Emit(FxTheme.Fire, SkillColor.Fire, pos + Jitter(unit * 0.3f), unit, 0.3f);
                }
                foreach (var u in b.Enemies)
                {
                    if (!u.Alive || u.DarkErosion <= 0) continue;   // 어둠 잠식: 대상에서 피어오르는 어둠
                    if (Chance(dt * (2f + u.DarkErosion * 1.2f))) Emit(FxTheme.Dark, SkillColor.Dark, P(u.Pos) + Jitter(unit * 0.35f), unit, 0.3f);
                }
                foreach (var e in b.Effects)
                {
                    float age = b.Time - e.Start - e.Delay;
                    if (age < 0f || age > FxLength(e)) continue;
                    // 멸령부: 베는 동안 부적빛 파편
                    if (e.Kind == FieldEffectKind.Blink && e.Target != null && Chance(dt * 35f))
                        for (int k = 0; k < 3; k++) Emit(FxTheme.Moon, e.Color, P(e.Target.Pos) + Jitter(unit * 0.4f), unit, 0.9f);
                    // 계속 입자가 나오는 연출 (화염 참격 · 연속 주먹)
                    if ((e.Kind == FieldEffectKind.Whirl || e.Kind == FieldEffectKind.Punches) && e.Source != null && Chance(dt * 40f))
                    {
                        Vector2 at = e.Kind == FieldEffectKind.Punches && e.Target != null ? e.Target.Pos : e.Source.Pos + new Vector2(Rnd(-1f, 1f), Rnd(-1f, 1f)) * e.Radius;
                        for (int k = 0; k < 3; k++) Emit(e.Theme, e.Color, P(at) + Jitter(unit * 0.4f), unit, 0.8f);
                    }
                    if (e.Kind == FieldEffectKind.Zone && Chance(dt * 12f))
                        Emit(e.Theme, e.Color, P(e.Origin) + Jitter(e.Radius * unit * 0.9f), unit, 0.3f);
                    if (e.Kind == FieldEffectKind.Links && e.Units != null && Chance(dt * 10f))
                        foreach (var lu in e.Units) if (lu != null && lu.Alive && Chance(0.3f)) Emit(e.Theme, e.Color, P(lu.Pos), unit, 0.3f);
                }

                for (int i = particles.Count - 1; i >= 0; i--)
                {
                    var p = particles[i];
                    p.Life += dt;
                    if (p.Life >= p.Max) { particles.RemoveAt(i); continue; }
                    p.Vel *= Mathf.Max(0f, 1f - p.Drag * dt);
                    p.Vel.y += p.Gravity * dt;
                    p.Pos += p.Vel * dt;
                    particles[i] = p;
                }
            }

            foreach (var p in particles)
            {
                float k = p.Life / p.Max;
                Color c = Color.Lerp(p.C0, p.C1, k);
                float size = Mathf.Lerp(p.Size0, p.Size1, k);
                if (p.Shape == 1)
                {
                    float speed = p.Vel.magnitude;
                    DrawFxRect(fxLine, p.Pos, Mathf.Max(size, size * speed / (unit * 2f)), Mathf.Max(1.5f, size * 0.3f), c, c.a, AngleOf(p.Vel));
                }
                else if (p.Shape == 2)
                    DrawFxRect(Texture2D.whiteTexture, p.Pos, size, size, c, c.a, p.Life * 400f + p.Max * 1000f);
                else
                    DrawFx(fxDisc, p.Pos, size * 2f, c, c.a);
            }
        }

        static void KnockDust(FieldUnit u, System.Func<Vector2, Vector2> P, float unit, float dt)
        {
            if (!u.Alive || u.PushTimer <= 0f) return;
            int n = Mathf.FloorToInt(dt * 90f) + (Chance(0.5f) ? 1 : 0);
            for (int i = 0; i < n; i++)
                Add(new Particle { Pos = P(u.Pos) + new Vector2(Rnd(-0.35f, 0.35f), Rnd(0.15f, 0.45f)) * unit,
                    Vel = -u.PushVel.normalized * (unit * Rnd(0.5f, 1.5f)), Max = Rnd(0.3f, 0.55f),
                    Size0 = unit * 0.12f, Size1 = unit * 0.38f,
                    C0 = new Color(0.8f, 0.76f, 0.66f, 0.55f), C1 = new Color(0.5f, 0.47f, 0.4f, 0f), Drag = 3f });
        }

        static void DashWind(FieldUnit u, System.Func<Vector2, Vector2> P, float unit, float dt)
        {
            if (!u.Alive || u.BreakTimer <= 0f || !Chance(dt * 40f)) return;
            Add(new Particle { Pos = P(u.Pos) + Jitter(unit * 0.35f), Vel = -u.KiteDir * (unit * Rnd(2f, 4f)), Max = Rnd(0.2f, 0.35f),
                Size0 = unit * 0.6f, Size1 = unit * 0.15f, C0 = new Color(1f, 1f, 1f, 0.7f), C1 = new Color(1f, 1f, 1f, 0f), Drag = 2f, Shape = 1 });
        }

        static bool Chance(float p) => prng.NextDouble() < p;
        static Vector2 Jitter(float r) => new Vector2(Rnd(-r, r), Rnd(-r, r));
    }
}
