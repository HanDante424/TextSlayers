using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        const string FieldSpriteResourcePrefix = "FieldSprites/";
        const int FieldSpriteCanvasSize = 64;
        const int FieldSpriteVerticalMargin = 4;
        const float FieldSpriteMarginRatio = (float)FieldSpriteVerticalMargin / FieldSpriteCanvasSize;
        const float FieldSpriteScale = 1.5f;  // [임시] 몸통 지름 대비 목표 크기 (실제 크기는 정수배로 맞춘다)
        const int FieldSpriteOutlineRadius = 2; // [임시] 원본 픽셀 단위
        const string FireArmSpriteSuffix = "_firearm"; // 화염의 팔 상태 전용 도트: <ID>_firearm
        const string MoonSpriteSuffix = "_moon";       // 월영 상태 전용 도트: <ID>_moon

        static readonly Color FieldSpriteOutline = new Color(0.55f, 0.67f, 0.83f, 0.72f); // [임시]
        static readonly Rect FieldSpriteUv = new Rect(0f, 0f, 1f, 1f);
        static readonly Rect FieldSpriteMirroredUv = new Rect(1f, 0f, -1f, 1f);
        static readonly Dictionary<string, FieldSprite> fieldSprites = new Dictionary<string, FieldSprite>();

        sealed class FieldSprite
        {
            public Texture2D Art;
            public Texture2D Outline;
            public Texture2D Silhouette;
            public bool FormArt;           // 변신 모습이 직접 그려진 도트 (상태 색조 · 덧그림을 생략한다)
        }

        /// <summary>유닛의 지금 모습에 맞는 도트. 변신(화염의 팔 · 월영) 전용 도트가 없으면 기본 도트.</summary>
        static FieldSprite UnitFieldSprite(FieldUnit u)
        {
            string id = u.Def?.Id;
            var sprite = GetFieldSprite(id);
            string suffix = u.FireArm ? FireArmSpriteSuffix : u.MoonShadow ? MoonSpriteSuffix : null;
            if (sprite == null || suffix == null) return sprite;
            return GetFieldSprite(id + suffix) ?? sprite;
        }

        static FieldSprite GetFieldSprite(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (fieldSprites.TryGetValue(id, out var sprite)) return sprite;

            // .bytes 로 가져오면 PNG 픽셀을 그대로 읽고 공용 외곽선을 한 번만 만들 수 있다.
            var asset = Resources.Load<TextAsset>(FieldSpriteResourcePrefix + id);
            if (asset != null)
            {
                var art = NewFieldSpriteTexture(id);
                if (ImageConversion.LoadImage(art, asset.bytes) &&
                    art.width == FieldSpriteCanvasSize && art.height == FieldSpriteCanvasSize)
                {
                    var pixels = art.GetPixels32();
                    var outline = new Color32[pixels.Length];
                    var silhouette = new Color32[pixels.Length];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        byte alpha = pixels[i].a;
                        if (alpha > 0) silhouette[i] = new Color32(255, 255, 255, alpha);
                        else if (HasFieldSpriteNeighbor(pixels, i)) outline[i] = new Color32(255, 255, 255, 255);
                    }
                    art.Apply(false, true);
                    sprite = new FieldSprite
                    {
                        Art = art,
                        FormArt = id.EndsWith(FireArmSpriteSuffix) || id.EndsWith(MoonSpriteSuffix),
                        Outline = MakeFieldSpriteMask(id + " outline", outline),
                        Silhouette = MakeFieldSpriteMask(id + " silhouette", silhouette)
                    };
                }
                else
                {
                    Object.Destroy(art);
                    Debug.LogWarning($"[TextSlayers] 필드 스프라이트는 {FieldSpriteCanvasSize}×{FieldSpriteCanvasSize} PNG여야 합니다. ({id})");
                }
            }

            fieldSprites.Add(id, sprite);
            return sprite;
        }

        static bool HasFieldSpriteNeighbor(Color32[] pixels, int index)
        {
            int x = index % FieldSpriteCanvasSize, y = index / FieldSpriteCanvasSize;
            for (int dy = -FieldSpriteOutlineRadius; dy <= FieldSpriteOutlineRadius; dy++)
            for (int dx = -FieldSpriteOutlineRadius; dx <= FieldSpriteOutlineRadius; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= FieldSpriteCanvasSize || ny >= FieldSpriteCanvasSize) continue;
                if (dx * dx + dy * dy > FieldSpriteOutlineRadius * FieldSpriteOutlineRadius) continue;
                if (pixels[ny * FieldSpriteCanvasSize + nx].a > 0) return true;
            }
            return false;
        }

        static Texture2D NewFieldSpriteTexture(string name) =>
            new Texture2D(FieldSpriteCanvasSize, FieldSpriteCanvasSize, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

        static Texture2D MakeFieldSpriteMask(string name, Color32[] pixels)
        {
            var texture = NewFieldSpriteTexture(name);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>도트 한 칸의 UI 크기. 화면 픽셀 정수배(최소 1배). bodyDiameter = 유닛 지름 (UI).</summary>
        static float FieldPixelSize(float bodyDiameter)
        {
            float screenPerUi = Screen.width / vw;
            int zoom = Mathf.Max(1, Mathf.RoundToInt(bodyDiameter * FieldSpriteScale * screenPerUi / FieldSpriteCanvasSize));
            return zoom / screenPerUi;
        }

        /// <summary>도트가 고르게 보이도록 화면 픽셀 기준 정수배(최소 1배)로 키우고, 위치도 화면 픽셀에 맞춘다.</summary>
        static Rect FieldSpriteRect(Rect body)
        {
            float screenPerUi = Screen.width / vw;
            float size = FieldPixelSize(body.height) * FieldSpriteCanvasSize;
            float Snap(float v) => Mathf.Round(v * screenPerUi) / screenPerUi;
            return new Rect(Snap(body.center.x - size * 0.5f), Snap(body.yMax - size * (1f - FieldSpriteMarginRatio)), size, size);
        }

        static float FieldSpriteHead(Rect rect) => rect.y + rect.height * FieldSpriteMarginRatio;

        static void DrawFieldSprite(FieldSprite sprite, Rect rect, bool faceLeft, FieldUnit u, float flash)
        {
            Color outline = FieldSpriteOutline;
            if (u.C.Guarding) outline = GuardColor;
            if (u.SuperArmor) outline = ArmorColor;
            PaintFieldSprite(rect, sprite.Outline, faceLeft, outline);
            Color tint = Color.white;
            if (u.Glutton) tint = GluttonBody;
            if (u.MoonShadow && !sprite.FormArt) tint = ShadowBody;
            if (u.FireArm && !sprite.FormArt) tint = Color.Lerp(tint, SkillColor.Fire, 0.45f);
            PaintFieldSprite(rect, sprite.Art, faceLeft, tint);
            if (flash > 0f) PaintFieldSprite(rect, sprite.Silhouette, faceLeft, new Color(1f, 1f, 1f, 0.7f * flash));
        }

        static void PaintFieldSprite(Rect rect, Texture2D texture, bool faceLeft, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(rect, texture, faceLeft ? FieldSpriteMirroredUv : FieldSpriteUv);
            GUI.color = previousColor;
        }
    }
}
