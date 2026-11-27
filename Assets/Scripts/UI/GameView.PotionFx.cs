using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 연금술 포션 도트 (사용자 2026-11-27). 원본 Docs/Design/Potion_Colors_64 를 절반 크기로 줄여 64×64 가운데에 둔 것.

        const float PotionSpin = 540f;   // 날아가며 도는 속도 (도/초) [임시]

        /// <summary>스킬에 맞는 병 색. 병 도트를 쓰지 않는 투척이면 null.</summary>
        static string PotionOf(string skillId)
        {
            switch (skillId)
            {
                case "pc_coagulant": return "potion_red";       // 응고제
                case "pc_purify": return "potion_blue";         // 정화 포션
                case "pc_corrode": return "potion_green";       // 부식 플라스크
                case "pc_elixir": return "potion_yellow";       // 만능의 영약
                case "ng_black_solution":
                case "ng_black_sediment": return "potion_black";    // 니그레도: 검은 병 (사용자 추가)
                default: return skillId == "basic_" + BasicKind.Potion ? "potion_purple" : null;   // 파라켈수스 기본 공격
            }
        }

        /// <summary>돌면서 날아가는 병. 그렸으면 true. 도트 배율은 캐릭터와 같다.</summary>
        static bool DrawPotion(string skillId, Vector2 at, float unit, float time)
        {
            var name = PotionOf(skillId);
            if (name == null) return false;
            var texture = WeaponTexture(name);
            if (texture == null) return false;
            float size = FieldPixelSize(unit) * FieldSpriteCanvasSize;
            DrawFxRect(texture, at, size, size, Color.white, 1f, time * PotionSpin);
            return true;
        }
    }
}
