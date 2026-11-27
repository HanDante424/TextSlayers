using UnityEngine;

namespace TextSlayers
{
    /// <summary>캐릭터별 이펙트 색 (연출용 [임시]).</summary>
    public static class SkillColor
    {
        public static readonly Color Dark = new Color(0.62f, 0.35f, 1f);      // 스티움 어둠
        public static readonly Color Fire = new Color(1f, 0.5f, 0.15f);       // 에른 · 아그니 화염
        public static readonly Color Gold = new Color(1f, 0.85f, 0.35f);      // 멜 행운 · 레온하르트
        public static readonly Color Thunder = new Color(1f, 0.95f, 0.4f);
        public static readonly Color Ice = new Color(0.55f, 0.9f, 1f);
        public static readonly Color Plasma = new Color(0.6f, 0.75f, 1f);
        public static readonly Color Steel = new Color(0.85f, 0.85f, 0.9f);   // 벤데타 관통 · 저격
        public static readonly Color Potion = new Color(0.45f, 1f, 0.5f);     // 파라켈수스
        public static readonly Color Mercury = new Color(0.8f, 0.85f, 0.95f);
        public static readonly Color Spirit = new Color(1f, 0.35f, 0.25f);    // 메일 투기
        public static readonly Color Moon = new Color(0.75f, 0.88f, 1f);      // 요월 달빛
        public static readonly Color Holy = new Color(1f, 0.93f, 0.6f);       // 라바다 빛
        public static readonly Color Abyss = new Color(0.85f, 0.15f, 0.35f);  // 가니악 심연
        public static readonly Color Frost = new Color(0.6f, 0.8f, 1f);       // 펜리르
        public static readonly Color Talisman = new Color(0.4f, 0.95f, 0.8f); // 사하라 부적
        public static readonly Color Black = new Color(0.5f, 0.22f, 0.72f);   // 니그레도 흑화 (어둠 속성: 검보라, 사용자 2026-11-26)
        public static readonly Color Shadow = new Color(0.42f, 0.36f, 0.62f); // 요월 월영 그림자 (어두운 배경에서도 보이게 약간 밝게)
        public static readonly Color Gray = new Color(0.7f, 0.7f, 0.72f);     // 멜 글러트니
    }

    /// <summary>
    /// 스킬 컨셉 기반 액티브 (스킬 문서 2장, 사용자 2026-11-25 ~ 26). 피해 수치는 전부 미정이라
    /// 캐릭터 쪽 NumbersSet = false 로 피해 0, 쿨타임은 10초 (Decided = false).
    /// 모양 · 대상 · 밀치기 · 기절 · 범위 · 이펙트는 컨셉 문장을 옮긴 것이고, 크기 · 시간 값은 전부 [임시].
    /// 패시브 · 변신(글러트니 · 월영) · 전용 자원(탄환 · 흑화 · 야성 등)은 미구현.
    /// </summary>
    public static class ConceptSkills
    {
        public static readonly FieldSkillDef[] All =
        {
            // ── 에른 스트레이 (화염 검사) ──
            new FieldSkillDef { Id = "ern_fire_slash", KnockLevel = 2, Name = "파이어 슬래시", Shape = SkillShape.Target, Range = 1.8f,
                Fx = FieldEffectKind.Slash, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "ern_fire_stream", KnockLevel = 1, Name = "파이어 스트림", Shape = SkillShape.Dash, Range = 6f, Pierce = true,
                Fx = FieldEffectKind.Trail, Fx2 = FieldEffectKind.Burst, Radius = 0.8f, FxColor = SkillColor.Fire, DashSpeedMul = 0.75f },   // 화염 폭풍을 휘감고 이동 [임시]
            new FieldSkillDef { Id = "ern_fire_buster", KnockLevel = 3, Name = "파이어 버스터", Shape = SkillShape.Around, Radius = 2.5f, Stun = 1f,
                Fx = FieldEffectKind.Pulse, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "ern_fire_execution", Name = "파이어 엑스큐션", Shape = SkillShape.Around, Radius = 2f, Hits = 10, Root = 2f, FxTime = 2f,
                Fx = FieldEffectKind.Whirl, FxColor = SkillColor.Fire },   // 2초간 10회, 이동 불가 (사용자 2026-11-26)
            new FieldSkillDef { Id = "ern_fire_blade", KnockLevel = 1, Name = "파이어 블레이드", Shape = SkillShape.Cone, Range = 2.2f, Radius = 2.8f, Angle = 120f, Combo = true,
                Fx = FieldEffectKind.BigSlash, Fx2 = FieldEffectKind.None, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "ern_fireball", KnockLevel = 1, Name = "파이어 볼", Shape = SkillShape.Projectile, Range = 8f, Shot = ShotStyle.Orb,
                ExplodeRadius = 0.8f, Fx2 = FieldEffectKind.Burst, Radius = 0.8f, FxColor = SkillColor.Fire },   // 매우 좁은 폭발 (사용자 2026-11-26)

            // ── 멜 발렌티나 (도박사: "A ~ B" 는 Roll 로 매번 무작위) ──
            new FieldSkillDef { Id = "mel_thunder", Name = "썬더 브레이커", Shape = SkillShape.Target, Range = 9f, Stun = 2f, Roll = true,
                Fx = FieldEffectKind.Pillar, Radius = 0.9f, FxColor = SkillColor.Thunder },
            new FieldSkillDef { Id = "mel_fire_buster", KnockLevel = 2, Name = "파이어 버스터", Shape = SkillShape.Projectile, Range = 8f, Shot = ShotStyle.Orb,
                ExplodeRadius = 1.6f, Fx2 = FieldEffectKind.Burst, Radius = 1.6f, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "mel_coin_toss", Name = "코인토스", Shape = SkillShape.AllyAll,
                Fx = FieldEffectKind.Buff, Fx2 = FieldEffectKind.Coin, FxColor = SkillColor.Gold },
            new FieldSkillDef { Id = "mel_divine_shield", Name = "디바인 쉴드", Shape = SkillShape.AllyAll, FxTime = 2f,
                Fx = FieldEffectKind.Shield, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "mel_railgun", KnockLevel = 2, Name = "플라즈마 레일건", Shape = SkillShape.Pierce, Range = 14f, Stun = 1f, Roll = true,
                Fx = FieldEffectKind.Beam, Radius = 0.5f, FxColor = SkillColor.Plasma },
            new FieldSkillDef { Id = "mel_absolute_zero", Name = "앱솔루트 제로", Shape = SkillShape.TargetArea, Range = 9f, Radius = 3f, Stun = 1f, Roll = true,
                Fx = FieldEffectKind.Pulse, Fx2 = FieldEffectKind.Freeze, FollowUp = "mel_absolute_zero_shield", FxColor = SkillColor.Ice },
            new FieldSkillDef { Id = "mel_absolute_zero_shield", Name = "앱솔루트 제로 (보호막)", Shape = SkillShape.AllyAll, FxTime = 1.5f,
                Fx = FieldEffectKind.Shield, FxColor = SkillColor.Ice, Quiet = true },

            // ── 벤데타 (권총 특수탄 3발 · 케이스 전용) ──
            new FieldSkillDef { Pistol = true, Id = "vd_incendiary", KnockLevel = 2, Name = "작열탄", Shape = SkillShape.Projectile, Range = 9f, Shot = ShotStyle.Bullet, Shots = 3,
                ExplodeRadius = 1f, Fx2 = FieldEffectKind.Burst, Radius = 1f, FxColor = SkillColor.Fire },
            new FieldSkillDef { Pistol = true, Id = "vd_freeze", Name = "빙결탄", Shape = SkillShape.Projectile, Range = 9f, Shot = ShotStyle.Bullet, Shots = 3,
                Fx2 = FieldEffectKind.IceShatter, Radius = 0.6f, FxColor = SkillColor.Ice },
            new FieldSkillDef { Pistol = true, Id = "vd_pierce", KnockLevel = 1, Name = "관통탄", Shape = SkillShape.Projectile, Range = 12f, Shot = ShotStyle.Bullet, Shots = 3, Pierce = true,
                Fx2 = FieldEffectKind.Burst, Radius = 0.5f, FxColor = SkillColor.Steel },
            new FieldSkillDef { Id = "vd_pilebunker", KnockLevel = 4, Name = "파일벙커 / 벙커버스터", Shape = SkillShape.Dash, Range = 7f, DashReturn = true,
                ImpactCone = true, Fx = FieldEffectKind.Trail, Fx2 = FieldEffectKind.Burst, Radius = 0.9f, FollowUp = "vd_bunkerbuster", FollowDelay = 0.6f,
                FxColor = SkillColor.Steel },
            new FieldSkillDef { Id = "vd_bunkerbuster", KnockLevel = 2, Name = "벙커버스터", Shape = SkillShape.Projectile, Range = 12f, Shot = ShotStyle.Orb,
                ExplodeRadius = 1.8f, Fx2 = FieldEffectKind.Burst, Radius = 1.8f, FxColor = SkillColor.Fire, Quiet = true },
            new FieldSkillDef { Id = "vd_missiles", Name = "다연장 탄두", Shape = SkillShape.RandomEnemies, Shots = 10, Root = 1f,
                Fx = FieldEffectKind.Missiles, Radius = 0.9f, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "vd_sniper", KnockLevel = 1, Name = "저격소총", Shape = SkillShape.Target, Pick = TargetPick.Farthest, Range = 25f,
                Fx = FieldEffectKind.Beam, Fx2 = FieldEffectKind.Burst, Radius = 0.7f, FxColor = SkillColor.Steel },

            // ── 파라켈수스 (포션 투척) ──
            new FieldSkillDef { Id = "pc_coagulant", Name = "응고제", Shape = SkillShape.AllyLowest, FxTime = 3f,
                Fx = FieldEffectKind.Lob, Fx2 = FieldEffectKind.Shield, FxColor = SkillColor.Potion },
            new FieldSkillDef { Id = "pc_purify", Name = "정화 포션", Shape = SkillShape.AllyLowest, NeedsAllyStatus = true, FxTime = 3f,
                Fx = FieldEffectKind.Lob, Fx2 = FieldEffectKind.Buff, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "pc_corrode", Name = "부식 플라스크", Shape = SkillShape.Zone, Pick = TargetPick.HighestMaxHp, Range = 10f, Radius = 1.2f, FxTime = 5f,
                Fx = FieldEffectKind.Lob, Fx2 = FieldEffectKind.Zone, FxColor = SkillColor.Potion },
            new FieldSkillDef { Id = "pc_silver_arrow", KnockLevel = 3, Name = "은화살", Shape = SkillShape.Projectile, Range = 9f, Shot = ShotStyle.Arrow,
                Fx2 = FieldEffectKind.Burst, Radius = 0.6f, FxColor = SkillColor.Mercury },
            new FieldSkillDef { Id = "pc_elixir", Name = "생명의 증류액", Shape = SkillShape.AllyLowest,
                Fx = FieldEffectKind.Lob, Fx2 = FieldEffectKind.Heal, FxColor = SkillColor.Potion },
            new FieldSkillDef { Id = "pc_mercury_wall", Name = "수은 장벽", Shape = SkillShape.Wall, Radius = 2.5f, FxTime = 5f,
                Fx = FieldEffectKind.Wall, FxColor = SkillColor.Mercury },

            // ── 메일 보레아 (투기 · 장병기) ──
            new FieldSkillDef { Id = "mail_shoulder", KnockLevel = 3, Name = "숄더 태클", Shape = SkillShape.Dash, Range = 5f, CollideStun = true,
                Fx = FieldEffectKind.Trail, Fx2 = FieldEffectKind.Burst, Radius = 0.9f, FxColor = SkillColor.Spirit },
            new FieldSkillDef { Id = "mail_stomp", Name = "진각", Shape = SkillShape.Around, Radius = 4f, Knock = -1.5f,
                Fx = FieldEffectKind.Grasp, Fx2 = FieldEffectKind.Pulse, FxColor = SkillColor.Spirit },
            new FieldSkillDef { Id = "mail_smash", Name = "스매시", Shape = SkillShape.Leap, Range = 6f, Stun = 1f, Radius = 0.9f,
                Fx = FieldEffectKind.Leap, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Spirit },
            new FieldSkillDef { Id = "mail_harden", Name = "투기 경화", Shape = SkillShape.Self, FxTime = 5f,
                Fx = FieldEffectKind.Shield, FxColor = SkillColor.Spirit },
            new FieldSkillDef { Id = "mail_slash_blast", KnockLevel = 2, Name = "슬래시 블래스트", Shape = SkillShape.Cone, Radius = 4f, Angle = 120f,
                Fx = FieldEffectKind.Cone, FxColor = SkillColor.Spirit },
            new FieldSkillDef { Id = "mail_overdrive", Name = "오버드라이브", Shape = SkillShape.Self, FxTime = 5f,
                Fx = FieldEffectKind.Buff, FollowUp = "mail_od_blast", FollowDelay = 5f, FxColor = SkillColor.Spirit },
            new FieldSkillDef { Id = "mail_od_blast", KnockLevel = 4, Name = "오버드라이브 슬래시 블래스트", Shape = SkillShape.Cone, Radius = 4.5f, Angle = 140f, CollideStun = true,
                Fx = FieldEffectKind.Cone, Fx2 = FieldEffectKind.Pulse, FxColor = SkillColor.Spirit },

            // ── 아그니 (화염 격투가) ──
            new FieldSkillDef { Id = "agni_kick", KnockLevel = 2, Name = "멸화섬각", Shape = SkillShape.Target, Range = 1.8f,
                Fx = FieldEffectKind.Slash, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "agni_flurry", Name = "염호멸연격", Shape = SkillShape.Target, Range = 1.8f, Hits = 5,
                BarrageTime = 1f, Fx = FieldEffectKind.Punches, Radius = 0.8f, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "agni_rising", KnockLevel = 1, Name = "천멸승화각", Shape = SkillShape.Target, Range = 1.8f, Hits = 2,
                Fx = FieldEffectKind.Slash, Launch = 0.6f, Fx2 = FieldEffectKind.SlantPillar, FxDelay2 = 0.25f, Fx2AtFront = true, Radius = 0.9f, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "agni_meteor", Name = "홍염낙화", Shape = SkillShape.Leap, HangTime = 0.6f, Range = 6f, Radius = 2f,
                Fx = FieldEffectKind.Leap, Fx2 = FieldEffectKind.Spin, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "agni_crusher", KnockLevel = 3, Name = "폭멸파쇄권", Shape = SkillShape.Target, Range = 1.8f, FireArmVariant = "agni_fire_fist",
                Fx = FieldEffectKind.Burst, Radius = 1.1f, FxColor = SkillColor.Fire },
            // 화권: 화염의 팔 보유 시. 캐릭터 폭 일직선으로 맵을 꿰뚫는 화염 파도 (사용자 2026-11-26)
            new FieldSkillDef { Id = "agni_fire_fist", KnockLevel = 3, KnockLevelOthers = 1, Name = "화권", Shape = SkillShape.Pierce, Range = 30f, Radius = 1.2f,
                Fx = FieldEffectKind.Beam, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Fire },
            new FieldSkillDef { Id = "agni_knee", KnockLevel = 1, Name = "염무 - 교정각", Shape = SkillShape.Dash, Range = 5f, ResetShortestCd = true,
                Fx = FieldEffectKind.Trail, Fx2 = FieldEffectKind.Burst, Radius = 0.9f, FxColor = SkillColor.Fire },

            // 옥염의 멸화 - 권: 화염의 팔 + 체력 30% 이하 + 극한 연소 장착일 때 화권 대신. 자해 20% (체력 1 까지만). 처형은 수치 미정이라 미구현
                        // 단일 대상. 맵을 꿰뚫는 직선 화염은 연출이고 피해는 대상만. 적중 시 필드 0.5초 정지 + 색 반전 (사용자 2026-11-26)
new FieldSkillDef { Id = "agni_okyeom", Name = "옥염의 멸화 - 권", Shape = SkillShape.Target, Range = 2f, SelfDamagePct = 0.2f,
                ImpactFrame = 0.5f, Fx = FieldEffectKind.FireLine, Fx2 = FieldEffectKind.Burst, Radius = 1.6f, FxColor = SkillColor.Fire },

            // ── 요월 (달의 검술. 월영 전용 스킬은 악신의 눈 미구현으로 제외) ──
            new FieldSkillDef { Id = "yw_moonlight", KnockLevel = 1, Name = "1식 월광검", Shape = SkillShape.Target, Range = 1.8f,
                Fx = FieldEffectKind.Slash, FollowUp = "yw_moonlight_wave", FollowDelay = 0.2f, FxColor = SkillColor.Moon },
            new FieldSkillDef { Id = "yw_moonlight_wave", KnockLevel = 1, Name = "1식 월광검 (검기)", Shape = SkillShape.Cone, Radius = 4f, Angle = 90f,
                Fx = FieldEffectKind.Cone, FxColor = SkillColor.Moon, Quiet = true },
            new FieldSkillDef { Id = "yw_moonflash", KnockLevel = 1, Name = "2식 월광섬", Shape = SkillShape.Dash, Pick = TargetPick.Farthest, Range = 14f, Pierce = true, DashBehind = true,
                Fx = FieldEffectKind.Trail, Radius = 0.8f, FxColor = SkillColor.Moon },
            new FieldSkillDef { Id = "yw_fullmoon", KnockLevel = 2, Name = "4식 만월참", Shape = SkillShape.Around, Radius = 2.2f,
                Fx = FieldEffectKind.Spin, FxColor = SkillColor.Moon },
            new FieldSkillDef { Id = "yw_moonfall", KnockLevel = 1, Name = "6식 달떨구기", Shape = SkillShape.Leap, Pick = TargetPick.LowestHp, Range = 10f, Radius = 0.9f,
                Fx = FieldEffectKind.Leap, Fx2 = FieldEffectKind.Slash, FxColor = SkillColor.Moon },
            new FieldSkillDef { Id = "yw_eclipse_cut", Name = "9식 월식", Shape = SkillShape.Target, Range = 1.8f, Hits = 4,
                Fx = FieldEffectKind.Multi, Radius = 0.8f, FxColor = SkillColor.Moon },
            new FieldSkillDef { Id = "yw_annular", KnockLevel = 1, Name = "오의 금환일식", Shape = SkillShape.Target, Range = 2.5f, Hits = 2,
                Fx = FieldEffectKind.Multi, Fx2 = FieldEffectKind.Pulse, FxDelay2 = 0.3f, Radius = 1.6f, FxColor = SkillColor.Moon },

            // ── 라바다 (빛 · 십자가) ──
            new FieldSkillDef { Id = "lv_light_pierce", KnockLevel = 1, Name = "빛의 관통", Shape = SkillShape.Pierce, Range = 12f,
                Fx = FieldEffectKind.Beam, Radius = 0.6f, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "lv_blessing", Name = "성광의 축복", Shape = SkillShape.AllyAll,
                Fx = FieldEffectKind.Heal, FollowUp = "lv_blessing_light", FollowDelay = 0.1f, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "lv_blessing_light", Name = "성광의 축복 (빛)", Shape = SkillShape.AllEnemies,
                Fx = FieldEffectKind.Pillar, Radius = 0.8f, FxColor = SkillColor.Holy, Quiet = true },
            new FieldSkillDef { Id = "lv_silence", KnockLevel = 1, Name = "침묵 선언", Shape = SkillShape.Target, Pick = TargetPick.HighestAtk, Range = 10f, FxTime = 3f,
                Fx = FieldEffectKind.Cross, Radius = 1.2f, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "lv_sanctuary", KnockLevel = 3, Name = "성역 선포", Shape = SkillShape.Dash, Range = 6f, Stun = 2f, Radius = 1.6f,
                Fx = FieldEffectKind.Trail, CastFx = FieldEffectKind.CrossHold, Fx2 = FieldEffectKind.CrossSlam, FollowUp = "lv_sanctuary_zone", FollowDelay = 0.3f, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "lv_sanctuary_zone", Name = "성역", Shape = SkillShape.Zone, ZoneAtSelf = true, Radius = 4f, FxTime = 5f,
                Fx2 = FieldEffectKind.Zone, FxColor = SkillColor.Holy, Quiet = true },
            new FieldSkillDef { Id = "lv_bless_judge", Name = "축복과 심판", Shape = SkillShape.AllyLowest,
                Fx = FieldEffectKind.Heal, FollowUp = "lv_bless_judge_light", FollowDelay = 0.2f, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "lv_bless_judge_light", Name = "축복과 심판 (심판)", Shape = SkillShape.Target, Pick = TargetPick.LowestHp, Range = 20f,
                Fx = FieldEffectKind.Pillar, Radius = 0.8f, FxColor = SkillColor.Holy, Quiet = true },
            new FieldSkillDef { Id = "lv_judgement", Name = "심판의 일격", Shape = SkillShape.Target, Range = 2f, Stun = 3f, Root = 3f, FxTime = 3f,
                FollowUp = "lv_judgement_wave", FollowDelay = 3f, Channel = true,
                NoDamage = true, Fx = FieldEffectKind.CrossFall, Radius = 6f, FxColor = SkillColor.Holy },
            new FieldSkillDef { Id = "lv_judgement_wave", Name = "심판의 일격 (빛의 파동)", Shape = SkillShape.AllEnemies, KnockLevel = 4, KnockSkipPrimary = true,
                Fx = FieldEffectKind.None, FxColor = SkillColor.Holy, Quiet = true },   // 주대상 제외 넉백 4 (사용자 2026-11-26)

            // ── 가니악 벤데스트라 (심연 · 마왕) ──
            new FieldSkillDef { Id = "gn_fang", KnockLevel = 2, DashSpeedMul = 1.4f, Name = "어둠의 송곳니", Shape = SkillShape.Dash, Pick = TargetPick.Farthest, Range = 14f,
                Fx = FieldEffectKind.Afterimage, Fx2 = FieldEffectKind.Thrust, Radius = 0.9f, FxColor = SkillColor.Abyss },
            new FieldSkillDef { Id = "gn_wave", KnockLevel = 4, Name = "어둠의 파동", Shape = SkillShape.Around, Radius = 3.5f, 
                Fx = FieldEffectKind.Pulse, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Abyss },
            new FieldSkillDef { Id = "gn_grasp", Name = "어둠의 손아귀", Shape = SkillShape.Target, Pick = TargetPick.Farthest, Range = 16f, PullToSelf = true,
                Fx = FieldEffectKind.Beam, Fx2 = FieldEffectKind.Grasp, Radius = 1f, FxColor = SkillColor.Abyss },
            new FieldSkillDef { Id = "gn_abyss_dance", Name = "심연난무", Shape = SkillShape.Cone, Range = 2f, Radius = 3f, Angle = 90f, Hits = 10, BarrageTime = 2f, BarrageCone = true,
                Fx = FieldEffectKind.ForwardSlashes, FxColor = SkillColor.Abyss },
            new FieldSkillDef { Id = "gn_inverted_cross", KnockLevel = 1, Name = "심판의 역십자가", Shape = SkillShape.Target, Pick = TargetPick.LowestHp, Range = 14f, FxTime = 1f, CrossLaser = true,
                Fx = FieldEffectKind.Cross, Radius = 2f, FxColor = SkillColor.Abyss },
            new FieldSkillDef { Id = "gn_erosion", Name = "심연 잠식", Shape = SkillShape.Self, FxTime = 5f,
                Fx = FieldEffectKind.Shield, FollowUp = "gn_erosion_burst", FollowDelay = 5f, FxColor = SkillColor.Abyss },
            new FieldSkillDef { Id = "gn_erosion_burst", KnockLevel = 1, Name = "심연 잠식 (폭발)", Shape = SkillShape.Around, Radius = 2.5f,
                Fx = FieldEffectKind.Pulse, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Abyss },

            // ── 레온하르트 (사자왕) ──
            new FieldSkillDef { Id = "lh_lion_crash", KnockLevel = 3, KnockSkipPrimary = true, Name = "라이온 크래시", Shape = SkillShape.Dash, Range = 6f, Stun = 3f, Radius = 1.5f,
                Fx = FieldEffectKind.Trail, Fx2 = FieldEffectKind.FistSlam, FxColor = SkillColor.Gold },
            new FieldSkillDef { Id = "lh_roar", Name = "사자후", Shape = SkillShape.AllEnemies, Stun = 1f,
                Fx = FieldEffectKind.None, Fx2 = FieldEffectKind.Roar, FollowUp = "lh_roar_buff", FollowDelay = 0.1f, FxColor = SkillColor.Gold },
            new FieldSkillDef { Id = "lh_roar_buff", Name = "사자후 (아군 강화)", Shape = SkillShape.AllyAll,
                Fx = FieldEffectKind.Buff, FxColor = SkillColor.Gold, Quiet = true },
            new FieldSkillDef { Id = "lh_kings_leap", KnockLevel = 2, KnockSkipPrimary = true, Name = "왕의 도약", Shape = SkillShape.Leap, OffMapTime = 0.8f, Pick = TargetPick.Farthest, Range = 14f, Stun = 2f, Radius = 1.6f,
                Fx = FieldEffectKind.Leap, Fx2 = FieldEffectKind.Pulse, FxColor = SkillColor.Gold },
            new FieldSkillDef { Id = "lh_buster_slash", KnockLevel = 4, Name = "버스터 슬래시", Shape = SkillShape.Cone, Radius = 3.5f, Angle = 180f,
                Fx = FieldEffectKind.BladeSwing, FxColor = SkillColor.Gold },
            new FieldSkillDef { Id = "lh_counter_slash", Name = "카운터 슬래시", Shape = SkillShape.Self, Root = 3f, FxTime = 3f, CounterSkill = "lh_counter_strike",
                Fx = FieldEffectKind.Shield, FxColor = SkillColor.Gold },
            new FieldSkillDef { Id = "lh_counter_strike", KnockLevel = 3, Name = "카운터 슬래시 (반격)", Shape = SkillShape.Around, Radius = 2.5f, 
                Fx = FieldEffectKind.BladeSwing, Fx2 = FieldEffectKind.Pulse, FxColor = SkillColor.Gold },   // 중간 범위 (사용자 2026-11-26), 대검 베기
            new FieldSkillDef { Id = "lh_sky_splitter", KnockLevel = 1, Name = "하늘가르기", Shape = SkillShape.Pierce, Range = 14f,
                CastFx = FieldEffectKind.SwordRaise, Fx = FieldEffectKind.WhiteSlash, Fx2 = FieldEffectKind.SwordCut, Radius = 1f, FxColor = SkillColor.Gold },

            // ── 펜리르 (늑대왕 · 표식) ──
            new FieldSkillDef { Id = "fr_hunt_leap", KnockLevel = 1, Name = "사냥 도약", Shape = SkillShape.Leap, Pick = TargetPick.Farthest, Range = 14f, Marks = true, Radius = 0.9f,
                Fx = FieldEffectKind.Leap, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Frost },
            new FieldSkillDef { Id = "fr_ambush", KnockLevel = 1, Name = "급습", Shape = SkillShape.Dash, Pick = TargetPick.Marked, Range = 14f, Radius = 0.9f,
                Fx = FieldEffectKind.Trail, Fx2 = FieldEffectKind.Bite, FxColor = SkillColor.Frost },
            new FieldSkillDef { Id = "fr_chain", Name = "족쇄의 사슬", Shape = SkillShape.Projectile, Pick = TargetPick.Farthest, Range = 16f, Shot = ShotStyle.Orb, Marks = true, PullToSelf = true,
                Fx2 = FieldEffectKind.Burst, Radius = 0.6f, FxColor = SkillColor.Steel },
            new FieldSkillDef { Id = "fr_eclipse_jaws", KnockLevel = 1, Name = "일식의 아가리", Shape = SkillShape.Target, Range = 1.8f,
                Fx = FieldEffectKind.Maw, Radius = 1f, FxColor = SkillColor.Frost },
            new FieldSkillDef { Id = "fr_pack_hunt", Name = "몰이 사냥", Shape = SkillShape.AllyAll, WolvesOnly = true, Hunt = 5f, FxTime = 5f,
                Fx = FieldEffectKind.Buff, FxColor = SkillColor.Frost },
            new FieldSkillDef { Id = "fr_ground_slam", KnockLevel = 2, Name = "대지 강타", Shape = SkillShape.Around, Radius = 2.5f, Stun = 2f,
                Fx = FieldEffectKind.Pulse, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Frost },

            // ── 사하라 (부적 검사) ──
            new FieldSkillDef { Id = "sh_flash", KnockLevel = 1, Name = "일섬", Teleport = true, Shape = SkillShape.Dash, Pick = TargetPick.Farthest, Range = 14f, Pierce = true,
                Fx = FieldEffectKind.Beam, Fx2 = FieldEffectKind.Slash, Radius = 0.4f, FxColor = SkillColor.Talisman },
            new FieldSkillDef { Id = "sh_seal", Name = "봉인부", Shape = SkillShape.Projectile, Pick = TargetPick.HighestMag, Range = 10f, Shot = ShotStyle.Card,
                Fx2 = FieldEffectKind.Burst, Radius = 0.7f, FxColor = SkillColor.Talisman },
            new FieldSkillDef { Id = "sh_suppress", Name = "억압부", Shape = SkillShape.Projectile, Pick = TargetPick.HighestAtk, Range = 10f, Shot = ShotStyle.Card,
                Fx2 = FieldEffectKind.Burst, Radius = 0.7f, FxColor = SkillColor.Gold },
            new FieldSkillDef { Id = "sh_dark_cut", KnockLevel = 1, Name = "어둠 가르기", Shape = SkillShape.Around, Radius = 3.5f,
                Fx = FieldEffectKind.BladeSwing, FxColor = SkillColor.Talisman },
            new FieldSkillDef { Id = "sh_annihilate", Name = "멸령부", Shape = SkillShape.Dash, Pick = TargetPick.HighestHp, Range = 10f, Hits = 5, Root = 1.2f, FxTime = 1.2f,
                HideWhileUsing = true, Fx = FieldEffectKind.Trail, Fx2 = FieldEffectKind.Blink, Radius = 1.3f, FxColor = SkillColor.Talisman },   // 대상 주변 순간이동 · 잔상 · 참격
            new FieldSkillDef { Id = "sh_ward", Name = "호신부", Shape = SkillShape.AllyLowest, FxTime = 2f, AlsoSelf = true,
                Fx = FieldEffectKind.Lob, Fx2 = FieldEffectKind.Shield, FxColor = SkillColor.Talisman },

            // ── 니그레도 (흑화. 흑화 중첩은 미구현 → 흑화 조건은 충족으로 본다) ──
            new FieldSkillDef { Id = "ng_black_shot", KnockLevel = 1, Name = "흑화탄", Shape = SkillShape.Projectile, Range = 9f, Shot = ShotStyle.Orb,
                Fx2 = FieldEffectKind.Burst, Radius = 0.7f, FxColor = SkillColor.Black },
            new FieldSkillDef { Id = "ng_black_solution", Name = "검은 용액", Shape = SkillShape.Zone, Range = 9f, Radius = 2f, FxTime = 5f,
                Fx = FieldEffectKind.Lob, Fx2 = FieldEffectKind.Zone, FxColor = SkillColor.Black },
            new FieldSkillDef { Id = "ng_black_sediment", Name = "흑색 침전물", Shape = SkillShape.TargetArea, Range = 9f, Radius = 3f,
                Fx = FieldEffectKind.Lob, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Black },
            new FieldSkillDef { Id = "ng_black_chain", Name = "흑색 사슬", Shape = SkillShape.AllEnemies, FxTime = 3f,
                Fx = FieldEffectKind.Links, FxColor = SkillColor.Black },
            new FieldSkillDef { Id = "ng_decompose", Name = "흑화 분해", Shape = SkillShape.AllEnemies,
                Fx = FieldEffectKind.Burst, Radius = 1f, FxColor = SkillColor.Black },
            new FieldSkillDef { Id = "ng_black_guard", Name = "검은 보호", Shape = SkillShape.Target, Pick = TargetPick.HighestMaxHp, Range = 20f,
                Fx = FieldEffectKind.Harvest, Fx2 = FieldEffectKind.None, FollowUp = "ng_black_guard_shield", FollowDelay = 0.6f, FxColor = SkillColor.Black },
            // ── 스티움 보조 ──
            new FieldSkillDef { Id = "stium_grasp_pull", Name = "어둠의 손아귀 (끌어당김)", Shape = SkillShape.Around, Radius = 4.5f, PullToSelf = true,
                FxColor = SkillColor.Dark, Quiet = true },

            // ── 멜 글러트니 전용 (패시브 글러트니 장착 시 액티브 고정) ──
            new FieldSkillDef { Id = "mel_aperitif", KnockLevel = 3, Name = "식 - 아페리티프", Shape = SkillShape.Cone, Radius = 3f, Angle = 140f,
                Fx = FieldEffectKind.Cone, FxColor = SkillColor.Gray },
            new FieldSkillDef { Id = "mel_hors", KnockLevel = 1, Name = "식 - 오르되브르", Shape = SkillShape.TargetArea, Range = 8f, Radius = 1.6f,
                Fx = FieldEffectKind.Pillar, Fx2 = FieldEffectKind.Bite, FxDelay2 = 0.15f, FxColor = SkillColor.Gray },
            new FieldSkillDef { Id = "mel_appetizer", Name = "식 - 에피타이저", Shape = SkillShape.GrabSlam, Range = 4f, Hits = 6, Stun = 1f,
                Fx = FieldEffectKind.Grab, Radius = 1.3f,  FollowDelay = 0.45f, FxColor = SkillColor.Gray },
            new FieldSkillDef { Id = "mel_appetizer_slam", Name = "식 - 에피타이저 (휘두르기)", Shape = SkillShape.Around, Radius = 2.5f, Stun = 1f,
                Fx = FieldEffectKind.Spin, Fx2 = FieldEffectKind.Pulse, FollowUp = "mel_appetizer_smash", FollowDelay = 0.4f, FxColor = SkillColor.Gray, Quiet = true },
            new FieldSkillDef { Id = "mel_appetizer_smash", KnockLevel = 2, Name = "식 - 에피타이저 (내려치기)", Shape = SkillShape.Cone, Radius = 3.5f, Angle = 90f, Stun = 1f,
                Fx = FieldEffectKind.Cone, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Gray, Quiet = true },

            // ── 요월 월영 전용 (패시브 월영 + 악신의 눈일 때 액티브 고정). 사용 후 짝 스킬로 번갈아 바뀜 ──
            new FieldSkillDef { Id = "yw_shadow_open", KnockLevel = 1, Name = "1식 암영 - 개", Shape = SkillShape.Pierce, Range = 18f, Radius = 0.5f,
                Fx = FieldEffectKind.Beam, FxColor = SkillColor.Shadow },
            new FieldSkillDef { Id = "yw_shadow_cut", Name = "오의 암영 - 절", Shape = SkillShape.AllEnemies, Next = "yw_shadow_open",
                Fx = FieldEffectKind.MapSlash, FxColor = SkillColor.Shadow },
            new FieldSkillDef { Id = "yw_shadow_pierce", KnockLevel = 2, Name = "2식 암영 - 추", Shape = SkillShape.Pierce, Range = 18f, Radius = 0.3f, Next = "yw_shadow_needle",
                Fx = FieldEffectKind.Beam, FxColor = SkillColor.Shadow },
            new FieldSkillDef { Id = "yw_shadow_needle", Name = "4식 암영 - 용", Shape = SkillShape.Target, Pick = TargetPick.LowestHp, Range = 30f, Radius = 1.2f, Next = "yw_shadow_pierce",
                Fx = FieldEffectKind.Pillar, FxColor = SkillColor.Shadow },
            new FieldSkillDef { Id = "yw_shadow_tail", KnockLevel = 3, Name = "3식 암영 - 미", Shape = SkillShape.Cone, Radius = 5f, Angle = 150f, Next = "yw_shadow_maw",
                Fx = FieldEffectKind.Cone, FxColor = SkillColor.Shadow },
            new FieldSkillDef { Id = "yw_shadow_maw", KnockLevel = 4, Name = "6식 암영 - 식", Shape = SkillShape.TargetArea, Range = 12f, Radius = 3f, Next = "yw_shadow_tail",
                Fx = FieldEffectKind.Bite, Fx2 = FieldEffectKind.Burst, FxColor = SkillColor.Shadow },

            new FieldSkillDef { Id = "ng_black_guard_shield", Name = "검은 보호 (보호막)", Shape = SkillShape.AllyLowest, FxTime = 2f, AlsoSelf = true,
                Fx = FieldEffectKind.Shield, FxColor = SkillColor.Black, Quiet = true },
        };
    }
}
