using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 캐릭터별 컨셉 연출 · 규칙 (사용자 2026-11-26): 기본 공격 모양 · 파티클 테마 · 고유 패시브 3 · 태그(총잡이 · 늑대 · 이클립스 · 화염의 팔).
    /// FieldData 의 정적 생성자가 Roster 에 적용한다. 패시브는 이름 · 설명만 있고 효과는 아직 없다
    /// (예외: 월영 · 글러트니는 변신으로 반영, 어둠 거두기의 잠식은 액티브 쪽에서 처리).
    /// </summary>
    public static class ConceptCharacters
    {
        sealed class Style
        {
            public string Id;
            public BasicKind Basic;
            public Color Color;
            public FxTheme Theme;
            public string[] Passives;
            public int Knock;
        }

        /// <param name="knock">기본 공격 넉백 단계 (사용자 2026-11-26)</param>
        static Style S(string id, int knock, BasicKind basic, Color color, FxTheme theme, params string[] passives)
            => new Style { Id = id, Knock = knock, Basic = basic, Color = color, Theme = theme, Passives = passives };

        static readonly Style[] Styles =
        {
            S("stium", 2, BasicKind.GreatSword, SkillColor.Steel, FxTheme.Dark, "stium_mana_barrier", "stium_deep_mana", "stium_awaken_dark"),
            S("ern", 1, BasicKind.FireSword, SkillColor.Fire, FxTheme.Fire, "ern_flame_sword", "ern_okyeom", "ern_flame_armor"),
            S("mel", 0, BasicKind.IceShard, SkillColor.Ice, FxTheme.Ice, "mel_luck", "mel_share", "mel_glutton"),
            S("vendetta", 1, BasicKind.Pistol, SkillColor.Steel, FxTheme.Steel, "vd_aim", "vd_case_ready", "vd_ap_round"),
            S("paracelsus", 0, BasicKind.Potion, SkillColor.Potion, FxTheme.Potion, "pc_stone", "pc_mass", "pc_magnum"),
            S("c06", 2, BasicKind.Halberd, SkillColor.Spirit, FxTheme.Spirit, "mail_one_hundred", "mail_spirit", "mail_all_in"),
            S("c07", 1, BasicKind.Anchor, SkillColor.Steel, FxTheme.Steel),   // 모모로이: 패시브 미정. 넉백은 근거리 기본값 [임시]
            S("c08", 1, BasicKind.Fist, SkillColor.Steel, FxTheme.Steel),      // 투견: 패시브 미정. 주먹 · 넉백은 근거리 기본값 [임시]
            S("c13", 1, BasicKind.Fist, SkillColor.Fire, FxTheme.Fire, "agni_giant", "agni_cycle", "agni_burn"),
            S("c19", 1, BasicKind.Katana, SkillColor.Moon, FxTheme.Moon, "yw_bright_moon", "yw_moon_cut", "yw_moon_shadow"),
            S("c32", 2, BasicKind.Cross, SkillColor.Holy, FxTheme.Holy, "lv_divine", "lv_radiance", "lv_overwhelm"),
            S("c38", 2, BasicKind.GreatSword, SkillColor.Gold, FxTheme.Gold, "lh_unyielding", "lh_kings_call", "lh_sword_saint"),
            S("c40", 1, BasicKind.Claw, SkillColor.Frost, FxTheme.Frost, "fr_sun_eater", "fr_kingship", "fr_gleipnir"),
            S("c41", 1, BasicKind.Sword, SkillColor.Talisman, FxTheme.Talisman, "sh_union", "sh_life_charm", "sh_chakra"),
            S("c43", 0, BasicKind.DarkOrb, SkillColor.Black, FxTheme.Black, "ng_start", "ng_black_settle", "ng_residue"),   // 기본 공격 미지정 → 흑화 구슬 [해석]
            S("c48", 1, BasicKind.DarkSword, SkillColor.Abyss, FxTheme.Dark, "gn_sign", "gn_erosion_p", "gn_vengeance"),
        };

        static readonly string[] Wolves = { "c08", "c18", "c39", "c40" };          // 투견 · 이게르투스 · 드로미 · 펜리르
        static readonly string[] EclipseMembers = { "c19", "c20", "c21", "c22", "c23" }; // 요월 · 환백 · 흑영 · 야월 · 분진
        static readonly string[] Gunners = { "vendetta", "c12" };                  // 벤데타 · 릭

        public const string MoonShadowPassive = "yw_moon_shadow";
        public const string GluttonPassive = "mel_glutton";
        public static readonly string[] GluttonSkills = { "mel_aperitif", "mel_hors", "mel_appetizer" };
        public static readonly string[] MoonShadowSkills = { "yw_shadow_open", "yw_shadow_pierce", "yw_shadow_tail" };

        /// <summary>고유 패시브 (이름 · 설명만. 효과 미구현 [임시]).</summary>
        public static readonly FieldPassiveDef[] Passives =
        {
            P("stium_mana_barrier", "어둠의 마나 장벽", SkillTag.Defense), P("stium_deep_mana", "깊은 어둠의 마나", SkillTag.Defense), P("stium_awaken_dark", "각성 - 어둠", SkillTag.Support),
            P("ern_flame_sword", "화염의 검", SkillTag.Attack), P("ern_okyeom", "옥염", SkillTag.Attack), P("ern_flame_armor", "화염 갑주", SkillTag.Defense),
            P("mel_luck", "압도적 행운", SkillTag.Support), P("mel_share", "운의 분배", SkillTag.Support), P(GluttonPassive, "글러트니", SkillTag.Attack, "변신: 액티브가 식 3종으로 고정, 근거리, 회색 기운"),
            P("vd_aim", "정조준", SkillTag.Attack), P("vd_case_ready", "케이스 준비", SkillTag.Support), P("vd_ap_round", "철갑탄", SkillTag.Attack),
            P("pc_stone", "현자의 돌", SkillTag.Support), P("pc_mass", "양산화", SkillTag.Support), P("pc_magnum", "마그눔 오푸스", SkillTag.Support),
            P("mail_one_hundred", "일당백", SkillTag.Defense), P("mail_spirit", "압도적 투기", SkillTag.Defense), P("mail_all_in", "전심전력", SkillTag.Attack),
            P("agni_giant", "거인의 잔열", SkillTag.Attack), P("agni_cycle", "화력 순환", SkillTag.Attack), P("agni_burn", "극한 연소", SkillTag.Attack),
            P("yw_bright_moon", "밝은 달의 검", SkillTag.Attack), P("yw_moon_cut", "월광절식", SkillTag.Support), P(MoonShadowPassive, "월영", SkillTag.Attack, "악신의 눈이 있을 때 변신: 암영 스킬, 기본 공격 · 이동 안 함"),
            P("lv_divine", "신의 가호", SkillTag.Defense), P("lv_radiance", "광휘", SkillTag.Heal), P("lv_overwhelm", "빛의 압도", SkillTag.Support),
            P("lh_unyielding", "불굴의 왕", SkillTag.Defense), P("lh_kings_call", "왕의 부름", SkillTag.Support), P("lh_sword_saint", "검성", SkillTag.Attack),
            P("fr_sun_eater", "해를 먹는 늑대", SkillTag.Attack), P("fr_kingship", "왕의 자질", SkillTag.Support), P("fr_gleipnir", "글레이프니르", SkillTag.Defense),
            P("sh_union", "기와 마나의 합일", SkillTag.Attack), P("sh_life_charm", "생명의 부적", SkillTag.Defense), P("sh_chakra", "차크라 주입", SkillTag.Attack),
            P("ng_start", "공정 시작", SkillTag.Support), P("ng_black_settle", "검은 침전", SkillTag.Attack), P("ng_residue", "잔류 시료", SkillTag.Attack),
            P("gn_sign", "징표 - 절단", SkillTag.Attack), P("gn_erosion_p", "어둠 침식", SkillTag.Attack), P("gn_vengeance", "복수의 어둠", SkillTag.Defense),
        };

        static FieldPassiveDef P(string id, string name, SkillTag tag, string desc = "효과 미구현 [임시]")
            => new FieldPassiveDef { Id = id, Name = name, Tag = tag, Desc = desc };

        public static void Apply(CharacterDef[] roster)
        {
            foreach (var c in roster)
            {
                foreach (var st in Styles)
                {
                    if (st.Id != c.Id) continue;
                    c.Basic = st.Basic;
                    c.BasicColor = st.Color;
                    c.Theme = st.Theme;
                    c.Passives = st.Passives;
                    c.Passive = st.Passives.Length > 0 ? st.Passives[0] : null;   // 패시브 미정 캐릭터는 없음
                    c.BasicKnock = st.Knock;
                }
                c.Wolf = System.Array.IndexOf(Wolves, c.Id) >= 0;
                c.Eclipse = System.Array.IndexOf(EclipseMembers, c.Id) >= 0;
                c.Gunner = System.Array.IndexOf(Gunners, c.Id) >= 0;
                c.ShowWeapons = c.Id == "vendetta";
                c.FireArmTrait = c.Id == "c13";

                // 선택 화면 전술은 4종뿐 → 호위 · 진형 유지 기본값은 사거리로 바꾼다 [해석]
                if (c.DefaultStrategy == Strategy.Escort || c.DefaultStrategy == Strategy.Hold)
                    c.DefaultStrategy = c.Range >= FieldConst.RangedThreshold ? Strategy.Kite : Strategy.Charge;
            }
        }
    }
}
