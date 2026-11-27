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

        /// <summary>WeaponTexture 를 위아래로 뒤집은 것.</summary>
        static Texture2D UpsideDownWeaponTexture(string name)
        {
            string key = name + " upside down";
            if (weaponTextures.TryGetValue(key, out var texture)) return texture;
            var source = WeaponTexture(name);
            if (source != null)
            {
                var pixels = source.GetPixels32();
                var flipped = new Color32[pixels.Length];
                int w = source.width, h = source.height;
                for (int y = 0; y < h; y++) System.Array.Copy(pixels, y * w, flipped, (h - 1 - y) * w, w);
                texture = MakeFieldSpriteMask(key, flipped);
            }
            weaponTextures.Add(key, texture);
            return texture;
        }

        // 펜리르 족쇄의 사슬 (사용자 2026-11-27): 날아가는 끝은 족쇄 입, 나머지는 고리.
        // 게임용은 원본(Docs/Design/Fenrir_Shackle_Jaw_64 · Ring_64)을 절반으로 줄인 32×32. 입은 오른쪽을 향하고, 고리는 긴 쪽이 가로.
        const int ShackleCanvas = 32;
        const float ShackleJawBack = 11f;      // 입의 중심 → 사슬이 이어지는 경첩 (도트)
        const float ShackleLinkStep = 13f;     // 고리 중심 간격 (도트) [임시]
        const float ShackleEdgeRatio = 0.35f;  // 하나 걸러 옆으로 선 고리: 납작하게 [임시]

        /// <summary>from(시전자)에서 head(족쇄 입의 중심)까지 사슬을 그린다. 도트를 읽지 못하면 false.</summary>
        static bool DrawShackleChain(Vector2 from, Vector2 head, Vector2 flightDir, float unit)
        {
            var jaw = WeaponTexture("fenrir_jaw");
            var ring = WeaponTexture("fenrir_ring");
            if (jaw == null || ring == null) return false;

            float pixel = FieldPixelSize(unit), size = ShackleCanvas * pixel;
            Vector2 span = head - from;
            Vector2 dir = span.magnitude > 1f ? span.normalized : flightDir.normalized;
            float angle = AngleOf(dir);
            Vector2 hinge = head - dir * (ShackleJawBack * pixel);
            float length = Vector2.Dot(hinge - from, dir), step = ShackleLinkStep * pixel;

            // 고리는 입 쪽부터 센다: 사슬이 입에 끌려 시전자에게서 풀려 나온다. 옆으로 선 고리가 위에 겹친다.
            for (int pass = 0; pass < 2; pass++)
            {
                int k = 0;
                for (float back = step * 0.5f; back < length; back += step, k++)
                    if (k % 2 == pass)
                        DrawFxRect(ring, hinge - dir * back, size, pass == 0 ? size : size * ShackleEdgeRatio, Color.white, 1f, angle);
            }
            DrawFxRect(jaw, head, size, size, Color.white, 1f, angle);
            return true;
        }

        // 레온하르트 사자왕의 석대검 (Docs/Design/Lion_King_Colossal_Sword_64.png 그대로). 원본이 오른쪽을 향한다.
        const string LionSword = "leon_sword";
        const float LionSwordGripY = 32f;

        static bool IsLeon(FieldEffect e) => e.Source != null && e.Source.Def != null && e.Source.Def.Id == "c38";

        /// <summary>스킬의 대검 도트 배율: 원하는 길이에 가장 가까운 정수배.</summary>
        static float SkillSwordPixel(float unit, float length)
        {
            float pixel = FieldPixelSize(unit);
            return Mathf.Max(1, Mathf.RoundToInt(length / (SwordReach * pixel))) * pixel;
        }

        /// <summary>손 위치 hand 에서 angleDeg 방향으로 무기 도트를 그린다. size = 도트 한 칸의 UI 크기.</summary>
        static void DrawSwordSprite(Texture2D texture, Vector2 hand, float angleDeg, float size, float alpha, float gripY = SwordGripY)
        {
            Vector2 dir = Polar(angleDeg), perp = new Vector2(-dir.y, dir.x);
            Vector2 center = hand + dir * ((SwordCanvas * 0.5f - SwordGripX) * size) + perp * ((SwordCanvas * 0.5f - gripY) * size);
            DrawFxRect(texture, center, SwordCanvas * size, SwordCanvas * size, Color.white, alpha, angleDeg);
        }

        /// <summary>
        /// 스킬의 대검: 스티움 · 레온하르트는 도트(길이에 가장 가까운 정수배), 다른 캐릭터는 기존 도형 날.
        /// </summary>
        static void DrawSkillBlade(FieldEffect e, float unit, Vector2 pivot, float angleDeg, float length, float width, Color main, float alpha)
        {
            bool leon = IsLeon(e);
            var texture = leon ? WeaponTexture(LionSword)
                        : e.Source != null && e.Source.Def != null && e.Source.Def.Id == "stium" ? SwordTexture() : null;
            if (texture == null)
            {
                DrawBlade(pivot, angleDeg, length, width, main, alpha);
                return;
            }
            DrawSwordSprite(texture, pivot, angleDeg, SkillSwordPixel(unit, length), alpha, leon ? LionSwordGripY : SwordGripY);
        }
    }
}
