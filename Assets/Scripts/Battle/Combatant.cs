namespace TextSlayers
{
    /// <summary>전투 중 한 쪽 (에른 또는 적 1마리).</summary>
    public sealed class Combatant
    {
        public readonly string Name;
        public readonly Stats Stats;
        public readonly bool IsPlayer;
        public readonly EnemyDef Def; // 적일 때만

        public int Hp;
        public int Mp;
        public float Gauge;     // 0 ~ GameData.GaugeMax
        public bool Guarding;   // 다음 피격 피해 감소
        public bool Charging;   // 보스: 다음 행동이 강타

        public Combatant(string name, Stats stats, bool isPlayer, EnemyDef def = null)
        {
            Name = name;
            Stats = stats;
            IsPlayer = isPlayer;
            Def = def;
            Hp = stats.HP;
            Mp = stats.MP;
        }

        public bool Alive => Hp > 0;
        public float HpRatio => Stats.HP > 0 ? (float)Hp / Stats.HP : 0f;
        public float MpRatio => Stats.MP > 0 ? (float)Mp / Stats.MP : 0f;
        public float GaugeRatio => Gauge / GameData.GaugeMax;
    }
}
