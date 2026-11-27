using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 도트 이펙트 시트 재생기. 시트: Assets/Resources/FieldFx/*.bytes (pixelfx 에서 내보냄, MIT — Docs/ThirdParty/pixelfx)
        // 프레임 크기 · 기준점 · fps 는 GameView.FxSheets.Table.cs 한 곳에 있다.

        sealed class FxSheet
        {
            public const int Stride = 6;       // 프레임마다: 시트 x, y, 너비, 높이, 기준점 → 왼쪽 위 dx, dy
            public readonly float Fps;
            public readonly short[] Rects;     // 프레임마다 보이는 영역만 잘라 담았다 (왼쪽 위 기준 도트)
            public int Frames => Rects.Length / Stride;
            public float Time => Frames / Fps;

            public FxSheet(float fps, short[] rects)
            {
                Fps = fps; Rects = rects;
            }
        }

        const float SheetFadeTime = 0.3f;   // 한 번 재생하는 이펙트가 끝에서 사라지는 시간 [임시]

        static readonly Dictionary<string, Texture2D> sheetTextures = new Dictionary<string, Texture2D>();

        static float SheetTime(string key) => FxSheets[key].Time;

        /// <summary>시트의 기준점은 바닥(발 위치)이다. 유닛 중심 좌표를 발 위치로 옮긴다.</summary>
        static Vector2 Feet(Vector2 center, float unit) => center + new Vector2(0f, unit * FieldConst.Radius);

        static Texture2D SheetTexture(string key)
        {
            if (sheetTextures.TryGetValue(key, out var texture)) return texture;
            var asset = Resources.Load<TextAsset>("FieldFx/" + key);
            if (asset != null)
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (!ImageConversion.LoadImage(texture, asset.bytes, true)) { Object.Destroy(texture); texture = null; }
            }
            if (texture == null) Debug.LogWarning($"[TextSlayers] 이펙트 시트를 읽지 못했습니다. ({key})");
            sheetTextures.Add(key, texture);
            return texture;
        }

        /// <summary>
        /// 기준점을 at 에 맞춰 시트를 그린다. age 는 시작 후 흐른 시간. 도트 배율은 캐릭터 스프라이트와 같다.
        /// loop 가 아니면 끝에서 서서히 사라진다. flip 은 좌우 반전 (시트는 오른쪽 방향 기준).
        /// </summary>
        static void DrawSheet(string key, Vector2 at, float unit, float age, Color tint, bool flip = false, bool loop = false, float angleDeg = 0f, float scale = 1f)
        {
            if (Event.current.type != EventType.Repaint || age < 0f) return;
            var s = FxSheets[key];
            if (!loop && age >= s.Time) return;
            var texture = SheetTexture(key);
            if (texture == null) return;

            int frame = (int)(age * s.Fps);
            frame = loop ? frame % s.Frames : Mathf.Min(frame, s.Frames - 1);
            int i = frame * FxSheet.Stride;
            int x = s.Rects[i], y = s.Rects[i + 1], w = s.Rects[i + 2], h = s.Rects[i + 3];
            if (w == 0) return;                                   // 빈 프레임
            int dx = s.Rects[i + 4], dy = s.Rects[i + 5];
            float pixel = FieldPixelSize(unit) * scale;
            var rect = new Rect(at.x + (flip ? -(dx + w) : dx) * pixel, at.y + dy * pixel, w * pixel, h * pixel);
            var uv = new Rect((float)x / texture.width, 1f - (float)(y + h) / texture.height,
                              (float)w / texture.width, (float)h / texture.height);
            if (flip) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);

            if (!loop) tint.a *= Mathf.Clamp01((s.Time - age) / SheetFadeTime);
            var previousColor = GUI.color;
            var previousMatrix = GUI.matrix;
            if (angleDeg != 0f)   // 기준점을 중심으로 회전 (DrawFxRect 와 같은 방식)
            {
                Vector3 p = at;
                GUI.matrix = previousMatrix * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, angleDeg), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
            }
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, texture, uv);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        // ───────── 근접 무기 내려베기 (사용자 2026-11-27: 모든 근거리 무기가 이 방식. 지금은 스티움 · 에른 · 메일 · 요월 · 라바다) ─────────
        // 머리 뒤에서 위로 넘겨 내려베고(-115° → +80°), 초승달 잔흔이 꼬리부터 흩어진다.
        // 값은 시트를 만든 값과 같아야 무기가 잔흔을 따라간다. 좌표는 발 위치 기준 도트, 오른쪽을 볼 때.

        const float CleaveStartAngle = -2.007f, CleaveSpan = 3.403f, CleaveCenterY = -28f;
        const float CleaveSwordZoom = 2f;   // 대검 도트 배율 [임시]

        sealed class CleaveStyle
        {
            public string Sheet;
            public float CenterX, Radius;        // 호의 중심(몸 뒤쪽)과 반지름
            public float Wind, Sweep;            // 시트의 준비 · 휘두르는 시간
            public float WeaponTime;             // 무기를 보여 주는 시간
            public string WeaponSprite;          // 오른쪽을 향한 무기 도트 이름 (없으면 도형 · 대검)
            public float WeaponZoom = 1f;        // 무기 도트 배율
            public float GripY = ErnSwordGripY;  // 무기 도트의 자루 가로축 (위에서부터 도트)
            public bool OneSided;                // 날이 한쪽(아래)에만 있는 무기: 왼쪽을 볼 때 위아래를 뒤집는다
        }

        static readonly CleaveStyle StiumCleave = new CleaveStyle { Sheet = "cleave", CenterX = -32f, Radius = 129f, Wind = 0.04f, Sweep = 0.16f, WeaponTime = 0.51f };
        static readonly CleaveStyle ErnCleave = new CleaveStyle { Sheet = "cleaveErn", CenterX = -22f, Radius = 88f, Wind = 0.03f, Sweep = 0.11f, WeaponTime = 0.36f, WeaponSprite = "ern_sword" };   // [임시]

        // 메일 할버드 (사용자 2026-11-27): 크기 · 속도는 스티움과 같고 색만 강철색 [임시]
        static readonly CleaveStyle MailCleave = new CleaveStyle { Sheet = "cleaveMail", CenterX = -32f, Radius = 129f, Wind = 0.04f, Sweep = 0.16f, WeaponTime = 0.51f, WeaponSprite = "mail_halberd", WeaponZoom = 2f, GripY = 30f, OneSided = true };

        // 요월 달빛 검 (사용자 2026-11-27): 크기 · 속도는 에른과 같고 호는 가늘게, 달빛 색 [임시]
        static readonly CleaveStyle YowolCleave = new CleaveStyle { Sheet = "cleaveYowol", CenterX = -22f, Radius = 88f, Wind = 0.03f, Sweep = 0.11f, WeaponTime = 0.36f, WeaponSprite = "yowol_sword", GripY = 28f, OneSided = true };

        // 라바다 장십자 지팡이 (사용자 2026-11-27): 크기는 에른, 속도는 스티움, 호는 두껍게, 금빛 [임시]. 좌우 대칭이라 뒤집지 않는다
        static readonly CleaveStyle LavadaCleave = new CleaveStyle { Sheet = "cleaveLavada", CenterX = -22f, Radius = 88f, Wind = 0.04f, Sweep = 0.16f, WeaponTime = 0.51f, WeaponSprite = "lavada_staff", GripY = 32f };

        // 파이어 슬래시 (사용자 2026-11-27): 기본 공격 동작에 화염 파티클을 아주 많이
        static readonly CleaveStyle FireSlashCleave = new CleaveStyle { Sheet = "fireSlash", CenterX = -22f, Radius = 104f, Wind = 0.03f, Sweep = 0.13f, WeaponTime = 0.45f, WeaponSprite = "ern_sword_flame" };   // [임시]

        static bool IsFireSlash(FieldEffect e) => e.SkillId == "ern_fire_slash" && e.Kind == FieldEffectKind.Slash && e.Source != null;

        static void DrawFireSlash(FieldEffect e, Vector2 caster, float unit, float U, float age)
            => DrawCleave(FireSlashCleave, e.Dir, caster + e.Dir * (unit * SwingReachAhead), unit, U * WeaponScale, age, SkillColor.Fire);

        /// <summary>이 기본 공격에 쓰는 내려베기. 아직 적용하지 않은 무기는 null.</summary>
        static CleaveStyle CleaveOf(FieldEffect e)
        {
            var weapon = WeaponOf(e);
            if (weapon == Weapon.GreatSword) return StiumCleave;
            if (weapon == Weapon.Halberd) return MailCleave;
            if (weapon == Weapon.Katana) return YowolCleave;
            if (weapon == Weapon.Cross) return LavadaCleave;
            return weapon == Weapon.OneHand && e.Source.Def.Id == "ern" ? ErnCleave : null;
        }

        /// <summary>
        /// 시트는 오른쪽(왼쪽은 좌우 반전)을 향해 만들어졌다. 공격자 몸을 중심으로 돌려 공격 방향에 맞춘다
        /// (사용자 2026-11-27). 위아래를 칠 때도 잔흔 · 무기가 대상 쪽을 향한다.
        /// </summary>
        static void DrawCleave(CleaveStyle style, Vector2 direction, Vector2 center, float unit, float U, float age, Color trail, bool weapon = true)
        {
            bool flip = direction.x < 0f;
            Vector2 attacker = center - direction * (unit * SwingReachAhead);      // 휘두르기 이펙트는 공격자 앞에 생긴다
            float angle = Mathf.DeltaAngle(flip ? 180f : 0f, AngleOf(direction));  // 좌우 방향에서 벗어난 각도 (-90 ~ 90)

            var previousMatrix = GUI.matrix;
            Vector3 p = attacker;
            GUI.matrix = previousMatrix * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, angle), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
            Vector2 level = attacker + new Vector2((flip ? -1f : 1f) * unit * SwingReachAhead, 0f);
            DrawCleaveLevel(style, Feet(level, unit), flip, unit, U, age, trail, weapon);
            GUI.matrix = previousMatrix;
        }

        const float SwingReachAhead = 0.7f;   // FieldBattle 의 기본 공격 이펙트 위치 (공격자 앞 0.7칸) 와 같아야 한다

        /// <summary>좌우로만 향한 상태의 내려베기 (잔흔 + 무기).</summary>
        static void DrawCleaveLevel(CleaveStyle style, Vector2 feet, bool flip, float unit, float U, float age, Color trail, bool weapon)
        {
            DrawSheet(style.Sheet, feet, unit, age, Color.white, flip);
            if (!weapon || age >= style.WeaponTime) return;

            float pixel = FieldPixelSize(unit);
            Vector2 Screened(Vector2 dots) => feet + new Vector2(flip ? -dots.x : dots.x, dots.y) * pixel;
            float k = Mathf.Clamp01((age - style.Wind) / style.Sweep);
            float angle = CleaveStartAngle + CleaveSpan * k * k;
            Vector2 pivot = Screened(new Vector2(style.CenterX, CleaveCenterY));
            Vector2 tip = Screened(new Vector2(style.CenterX + Mathf.Cos(angle) * style.Radius, CleaveCenterY + Mathf.Sin(angle) * style.Radius));
            Vector2 toTip = tip - pivot;
            float t = age / style.WeaponTime;
            float alpha = t < 0.75f ? 1f : (1f - t) / 0.25f;

            // 칼끝이 호의 바깥선에 닿도록 손 위치를 정한다
            var own = style.WeaponSprite != null ? WeaponTexture(style.WeaponSprite) : null;
            var sword = style == StiumCleave ? SwordTexture() : null;
            if (own != null)               // 무기별 도트 (에른 검 1배 · 메일 할버드 2배)
            {
                float zoom = style.WeaponZoom * pixel;
                float gripY = style.GripY;
                if (style.OneSided && flip)
                {
                    own = UpsideDownWeaponTexture(style.WeaponSprite);
                    gripY = SwordCanvas - gripY;
                }
                DrawSwordSprite(own, pivot + toTip.normalized * (toTip.magnitude - SwordReach * zoom), AngleOf(toTip), zoom, alpha, gripY);
            }
            else if (sword != null)             // 스티움 대검 도트 2배 (도트가 고르게 보이는 정수배)
            {
                float size = CleaveSwordZoom * pixel;
                DrawSwordSprite(sword, pivot + toTip.normalized * (toTip.magnitude - SwordReach * size), AngleOf(toTip), size, alpha);
            }
            else                           // 도트가 아직 없는 한손검은 도형 (손 → 칼끝 1.86U)
            {
                float reach = U * OneHandReach;
                DrawOneHandSword(pivot + toTip.normalized * Mathf.Max(0f, toTip.magnitude - reach), AngleOf(toTip), U, trail, alpha);
            }
        }

        /// <summary>폭발 · 적중(Burst)에 쓸 시트. 테마가 먼저고, 없으면 색이 불빛인지 본다.</summary>
        static string BurstSheet(FieldEffect e)
        {
            switch (e.Theme)
            {
                case FxTheme.Fire: return "boom";
                case FxTheme.Dark:
                case FxTheme.Black:
                case FxTheme.Shadow: return "boomDark";
                case FxTheme.Ice:
                case FxTheme.Frost: return "iceShatter";
            }
            Color c = e.Color;
            bool fiery = c.r > 0.8f && c.g < 0.65f && c.b < 0.4f;
            return fiery ? "boom" : "hit";
        }
    }
}
