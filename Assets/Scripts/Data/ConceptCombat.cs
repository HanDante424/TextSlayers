namespace TextSlayers
{
    /// <summary>
    /// 스킬별 전투 동작 세부 (사용자 2026-11-26): 슈퍼아머 · 전진 · 대상에게 접근 · 시전 시간 · 사용 중 천천히 이동.
    /// 거리 · 시간 값은 전부 [임시]. FieldData.Skill 목록을 만들 때 한 번 적용한다.
    /// </summary>
    public static class ConceptCombat
    {
        public const float LungeDistance = 1.0f;    // "앞으로 살짝 돌진 · 이동"
        public const float LungeTime = 0.15f;
        public const float ApproachRange = 6f;      // "대상에게 돌진 후 사용" 거리
        public const float PileWindup = 0.25f;      // 파일벙커 대기 (사용자)
        public const float SniperCast = 1f;         // 저격소총 시전 (사용자)

        // 사용 중 슈퍼아머
        static readonly string[] Armor =
        {
            "stium_spin", "stium_smash", "stium_pulse", "stium_grasp",
            "ern_fire_stream", "ern_fire_buster", "ern_fire_execution",
            "mel_railgun",
            "vd_pilebunker",
            "pc_silver_arrow",
            "mail_shoulder", "mail_stomp", "mail_smash", "mail_slash_blast", "mail_od_blast",
            "agni_kick", "agni_flurry", "agni_rising", "agni_meteor", "agni_crusher", "agni_fire_fist", "agni_okyeom",
            "yw_moonflash", "yw_fullmoon", "yw_moonfall", "yw_eclipse_cut", "yw_annular",
            "lv_sanctuary", "lv_judgement",
            "lh_lion_crash", "lh_kings_leap", "lh_buster_slash", "lh_sky_splitter",
            "fr_eclipse_jaws",
            "sh_dark_cut",
            "gn_fang", "gn_wave", "gn_grasp", "gn_abyss_dance",
        };

        // 앞으로 살짝 이동하며 사용
        static readonly string[] Lunge =
        {
            "stium_spin", "stium_smash",
            "ern_fire_slash",
            "mail_stomp", "mail_smash", "mail_slash_blast", "mail_od_blast",
            "agni_kick", "agni_rising", "agni_crusher", "agni_fire_fist",
            "yw_moonlight", "yw_annular",
            "lh_counter_slash",
        };

        // 대상에게 돌진한 뒤 사용
        static readonly string[] Approach = { "agni_okyeom", "lv_judgement", "fr_eclipse_jaws" };

        public static void Apply(FieldSkillDef s)
        {
            if (System.Array.IndexOf(Armor, s.Id) >= 0) s.Armor = true;
            if (System.Array.IndexOf(Lunge, s.Id) >= 0) s.Lunge = LungeDistance;
            if (System.Array.IndexOf(Approach, s.Id) >= 0) s.Approach = ApproachRange;
            switch (s.Id)
            {
                case "stium_smash": s.CastTime = 0.35f; s.CastFx = FieldEffectKind.BladeSlam; break;   // 대검 내려찍기 모션
                case "lh_sky_splitter": s.CastTime = 1f; break;             // 1초 들어 올린 뒤 내려베기
                case "lv_sanctuary": s.CastTime = 0.25f; break;             // 십자가를 들고 0.25초 대기
                case "gn_abyss_dance": s.Creep = 2f; break;
                // 사용 중 대상에게 천천히 이동 (연타 시간)
                case "agni_flurry": s.Creep = 1.0f; break;
                case "yw_eclipse_cut": s.Creep = 0.8f; break;
                case "vd_pilebunker": s.CastTime = PileWindup; break;
                case "vd_sniper": s.CastTime = SniperCast; s.Aim = true; break;
                case "mail_harden": s.ToughUp = 20f; s.ToughUpLowHp = 35f; break;   // 강인함 +20 (체력 30% 이하 +35), 5초
                case "pc_silver_arrow": s.ToughDown = 20f; break;                     // 적 강인함 -20, 5초
            }
        }
    }
}
