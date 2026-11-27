using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 스티움 대검 도트 (Docs/Design/Stium_Greatsword_64.png 그대로). 원본은 칼끝이 왼쪽을 향한다.
        // 아래 좌표는 좌우를 뒤집어 칼끝이 오른쪽을 향한 상태의 값 (왼쪽 위 기준 도트).
        const int SwordCanvas = 64;
        const float SwordGripX = 12f, SwordGripY = 28f;   // 손으로 쥐는 곳
        const float SwordReach = 47f;                     // 손 → 칼끝

        static Texture2D swordTexture;
        static bool swordLoaded;

        static Texture2D SwordTexture()
        {
            if (swordLoaded) return swordTexture;
            swordLoaded = true;
            var asset = Resources.Load<TextAsset>("FieldSprites/stium_sword");
            if (asset == null) return null;
            var source = NewFieldSpriteTexture("stium sword");
            if (ImageConversion.LoadImage(source, asset.bytes) && source.width == SwordCanvas && source.height == SwordCanvas)
            {
                var pixels = source.GetPixels32();
                var mirrored = new Color32[pixels.Length];
                for (int y = 0; y < SwordCanvas; y++)
                    for (int x = 0; x < SwordCanvas; x++)
                        mirrored[y * SwordCanvas + x] = pixels[y * SwordCanvas + (SwordCanvas - 1 - x)];
                swordTexture = MakeFieldSpriteMask("stium sword", mirrored);
            }
            else Debug.LogWarning("[TextSlayers] 대검 도트를 읽지 못했습니다.");
            Object.Destroy(source);
            return swordTexture;
        }

        // 에른 검 도트 (Docs/Design/Ern_Sword_64_Base.png · _Flame.png). 원본이 이미 오른쪽을 향한다.
        const float ErnSwordGripY = 31f;   // 손잡이 가로축 (손 위치 x 와 칼끝 길이는 대검과 같은 12 · 47)

        static readonly System.Collections.Generic.Dictionary<string, Texture2D> weaponTextures = new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>오른쪽을 향해 그려진 64×64 무기 도트를 그대로 읽는다.</summary>
        static Texture2D WeaponTexture(string name)
        {
            if (weaponTextures.TryGetValue(name, out var texture)) return texture;
            var asset = Resources.Load<TextAsset>("FieldSprites/" + name);
            if (asset != null)
            {
                texture = NewFieldSpriteTexture(name);
                if (!ImageConversion.LoadImage(texture, asset.bytes)) { Object.Destroy(texture); texture = null; }
            }
            if (texture == null) Debug.LogWarning($"[TextSlayers] 무기 도트를 읽지 못했습니다. ({name})");
            weaponTextures.Add(name, texture);
            return texture;
        }

        /// <summary>손 위치 hand 에서 angleDeg 방향으로 무기 도트를 그린다. size = 도트 한 칸의 UI 크기.</summary>
        static void DrawSwordSprite(Texture2D texture, Vector2 hand, float angleDeg, float size, float alpha, float gripY = SwordGripY)
        {
            Vector2 dir = Polar(angleDeg), perp = new Vector2(-dir.y, dir.x);
            Vector2 center = hand + dir * ((SwordCanvas * 0.5f - SwordGripX) * size) + perp * ((SwordCanvas * 0.5f - gripY) * size);
            DrawFxRect(texture, center, SwordCanvas * size, SwordCanvas * size, Color.white, alpha, angleDeg);
        }

        /// <summary>
        /// 스킬의 대검: 스티움은 도트(길이에 가장 가까운 정수배), 다른 캐릭터는 기존 도형 날.
        /// </summary>
        static void DrawSkillBlade(FieldEffect e, float unit, Vector2 pivot, float angleDeg, float length, float width, Color main, float alpha)
        {
            var texture = e.Source != null && e.Source.Def != null && e.Source.Def.Id == "stium" ? SwordTexture() : null;
            if (texture == null)
            {
                DrawBlade(pivot, angleDeg, length, width, main, alpha);
                return;
            }
            float pixel = FieldPixelSize(unit);
            int zoom = Mathf.Max(1, Mathf.RoundToInt(length / (SwordReach * pixel)));
            DrawSwordSprite(texture, pivot, angleDeg, zoom * pixel, alpha);
        }
    }
}
