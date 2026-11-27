using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 필드 바닥: PixelLab Wang 타일셋 (Docs/Design/Field_Tileset_Wang_64, 사용자 2026-11-27). 그림만이며 전투에 영향 없음.
        // 네 모서리가 풀인지 흙인지(왼위 8 · 오위 4 · 왼아래 2 · 오아래 1)로 타일을 고른다.

        const int GroundTile = 64;
        const float GrassNoiseScale = 0.31f, GrassThreshold = 0.55f;   // 풀 얼룩의 크기 · 비율 [임시]
        static readonly Color GroundTint = new Color(0.72f, 0.72f, 0.74f);   // 캐릭터 · 이펙트가 묻히지 않게 어둡게 [임시]

        // 시트(5×4) 안에서 모서리 조합 0~15 의 타일 위치 (열, 행)
        static readonly int[] GroundColumn = { 1, 3, 4, 1, 3, 2, 3, 2, 4, 4, 0, 0, 1, 2, 0, 1 };
        static readonly int[] GroundRow    = { 1, 0, 0, 2, 1, 1, 2, 2, 1, 2, 1, 2, 0, 0, 0, 3 };

        static Texture2D groundTexture;
        static bool groundLoaded;

        static Texture2D GroundTexture()
        {
            if (groundLoaded) return groundTexture;
            groundLoaded = true;
            var asset = Resources.Load<TextAsset>("Field/ground");
            if (asset == null) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (ImageConversion.LoadImage(texture, asset.bytes, true)) groundTexture = texture;
            else Object.Destroy(texture);
            return groundTexture;
        }

        /// <summary>타일 격자의 꼭짓점이 풀인가. 같은 스테이지는 항상 같은 모양.</summary>
        static bool GrassAt(int x, int y, int seed)
            => Mathf.PerlinNoise(seed * 0.137f + x * GrassNoiseScale, seed * 0.071f + y * GrassNoiseScale) > GrassThreshold;

        /// <summary>필드 바닥을 그린다. 타일이 없으면 false (기존 단색).</summary>
        static bool DrawGround(Rect field, float unit, FieldBattle b)
        {
            var texture = GroundTexture();
            if (texture == null) return false;
            if (Event.current.type != EventType.Repaint) return true;

            int seed = 0;
            foreach (char c in b.Stage.Id) seed = (seed * 31 + c) % 997;
            float size = GroundTile * FieldPixelSize(unit);
            int columns = Mathf.CeilToInt(field.width / size), rows = Mathf.CeilToInt(field.height / size);

            var previousColor = GUI.color;
            GUI.color = GroundTint;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int key = (GrassAt(x, y, seed) ? 8 : 0) | (GrassAt(x + 1, y, seed) ? 4 : 0)
                            | (GrassAt(x, y + 1, seed) ? 2 : 0) | (GrassAt(x + 1, y + 1, seed) ? 1 : 0);
                    // 필드 밖으로 나가는 부분은 잘라 그린다
                    float w = Mathf.Min(size, field.width - x * size), h = Mathf.Min(size, field.height - y * size);
                    float fx = w / size, fy = h / size;
                    float tileW = (float)GroundTile / texture.width, tileH = (float)GroundTile / texture.height;
                    float u = GroundColumn[key] * tileW, top = 1f - GroundRow[key] * tileH;
                    GUI.DrawTextureWithTexCoords(new Rect(field.x + x * size, field.y + y * size, w, h), texture,
                                                 new Rect(u, top - tileH * fy, tileW * fx, tileH * fy));
                }
            GUI.color = previousColor;
            return true;
        }
    }
}
