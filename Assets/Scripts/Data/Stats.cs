using System;

namespace TextSlayers
{
    /// <summary>전투 스탯 묶음 (기획 v0.1 4장, v0.2 마력 · 저항력 추가).</summary>
    [Serializable]
    public struct Stats
    {
        public int HP;
        public int MP;
        public int ATK;
        public int DEF;
        public float SPD;
        public float CRIT;    // 치명타 확률 0~1
        public float CRITDMG; // 치명타 배율 (1.5 = 150%)
        public int MAG;       // 마력 (v0.2). 생성자에서는 0, 필요한 곳만 { MAG = .. } 로 채운다
        public int RES;       // 저항력 (v0.2)

        public Stats(int hp, int mp, int atk, int def, float spd, float crit, float critDmg)
        {
            HP = hp; MP = mp; ATK = atk; DEF = def; SPD = spd; CRIT = crit; CRITDMG = critDmg;
            MAG = 0; RES = 0;
        }
    }
}
