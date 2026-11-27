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
            public readonly string Name;
            public readonly int Frames, Columns, Width, Height, PivotX, PivotY;   // 기준점은 프레임 왼쪽 위 기준
            public readonly float Fps;
            public float Time => Frames / Fps;

            public FxSheet(string name, int frames, int columns, int width, int height, int pivotX, int pivotY, float fps)
            {
                Name = name; Frames = frames; Columns = columns; Width = width; Height = height;
                PivotX = pivotX; PivotY = pivotY; Fps = fps;
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
        static void DrawSheet(string key, Vector2 at, float unit, float age, Color tint, bool flip = false, bool loop = false, float angleDeg = 0f)
        {
            if (Event.current.type != EventType.Repaint || age < 0f) return;
            var s = FxSheets[key];
            if (!loop && age >= s.Time) return;
            var texture = SheetTexture(key);
            if (texture == null) return;

            int frame = (int)(age * s.Fps);
            frame = loop ? frame % s.Frames : Mathf.Min(frame, s.Frames - 1);
            int column = frame % s.Columns, row = frame / s.Columns;
            float pixel = FieldPixelSize(unit);
            float pivotX = flip ? s.Width - s.PivotX : s.PivotX;
            var rect = new Rect(at.x - pivotX * pixel, at.y - s.PivotY * pixel, s.Width * pixel, s.Height * pixel);
            var uv = new Rect((float)column * s.Width / texture.width, 1f - (float)(row + 1) * s.Height / texture.height,
                              (float)s.Width / texture.width, (float)s.Height / texture.height);
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

        // 스티움 내려베기 (cleave-reap) 의 호. 시트를 만든 값과 같아야 대검이 잔흔을 따라간다.
        // 좌표는 발 위치 기준 도트 단위, 오른쪽을 볼 때.
        // 크기는 이전 대검 휘두르기와 같게 (사용자 2026-11-27): 중심은 몸 뒤쪽, 각도 -115° → +80°.
        const float CleaveCenterX = -32f, CleaveCenterY = -28f, CleaveRadius = 129f,
                    CleaveStartAngle = -2.007f, CleaveSpan = 3.403f;
        const float CleaveWind = 0.04f, CleaveSweep = 0.16f;
        const float CleaveSwordZoom = 2f;   // 대검 도트 배율 [임시]

        /// <summary>호 위에서 u 라디안 지나간 점 (발 위치 기준 도트 단위).</summary>
        static Vector2 CleavePoint(float u)
        {
            float a = CleaveStartAngle + u;
            return new Vector2(CleaveCenterX + Mathf.Cos(a) * CleaveRadius, CleaveCenterY + Mathf.Sin(a) * CleaveRadius);
        }

        /// <summary>대검 기본 공격: 머리 뒤에서 위로 넘겨 내려베고, 두꺼운 초승달 잔흔이 꼬리부터 흩어진다.</summary>
        static void DrawCleave(FieldEffect e, Vector2 center, float unit, float U, float age, float duration)
        {
            Vector2 feet = Feet(center, unit);
            bool flip = e.Dir.x < 0f;
            DrawSheet("cleave", feet, unit, age, Color.white, flip);
            if (age >= duration) return;

            float pixel = FieldPixelSize(unit);
            Vector2 Screened(Vector2 dots) => feet + new Vector2(flip ? -dots.x : dots.x, dots.y) * pixel;
            float k = Mathf.Clamp01((age - CleaveWind) / CleaveSweep);
            Vector2 pivot = Screened(new Vector2(CleaveCenterX, CleaveCenterY));
            Vector2 toTip = Screened(CleavePoint(CleaveSpan * k * k)) - pivot;
            float t = age / duration;
            float alpha = t < 0.75f ? 1f : (1f - t) / 0.25f;
            // 칼끝이 호의 바깥선에 닿도록 손 위치를 정한다. 대검 도트는 2배 (도트가 고르게 보이는 정수배)
            var sword = SwordTexture();
            if (sword != null)
            {
                float size = CleaveSwordZoom * pixel;
                Vector2 hand = pivot + toTip.normalized * (toTip.magnitude - SwordReach * size);
                DrawSwordSprite(sword, hand, AngleOf(toTip), size, alpha);
            }
            else DrawBlade(pivot + toTip * 0.13f, AngleOf(toTip), toTip.magnitude * 0.87f, U * 0.55f, SkillColor.Dark, alpha);
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
