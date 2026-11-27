// 자동 생성: 시트 내보내기 스크립트가 만든 프레임 표. 손으로 고치지 않는다.
using System.Collections.Generic;

namespace TextSlayers
{
    public static partial class GameView
    {
        static readonly Dictionary<string, FxSheet> FxSheets = new Dictionary<string, FxSheet>
        {
            { "boom", new FxSheet("boom", 28, 5, 189, 166, 89, 141, 20f) },   // explosions/small-explosion
            { "boomDark", new FxSheet("boomDark", 28, 5, 189, 166, 89, 141, 20f) },   // explosions/small-explosion
            { "hit", new FxSheet("hit", 12, 3, 161, 99, 65, 75, 20f) },   // impacts/crit-hit
            { "slam", new FxSheet("slam", 22, 4, 206, 140, 100, 101, 18f) },   // impacts/ground-slam
            { "landing", new FxSheet("landing", 16, 3, 136, 41, 70, 28, 18f) },   // impacts/heavy-landing
            { "iceShatter", new FxSheet("iceShatter", 18, 3, 175, 82, 48, 64, 18f) },   // ice/ice-shatter
            { "frostNova", new FxSheet("frostNova", 26, 4, 218, 116, 109, 88, 16f) },   // ice/frost-nova
            { "heal", new FxSheet("heal", 20, 5, 90, 87, 43, 61, 14f) },   // holy/heal-aoe
            { "shield", new FxSheet("shield", 24, 5, 66, 68, 33, 57, 10f) },   // holy/divine-shield
            { "buff", new FxSheet("buff", 18, 7, 45, 106, 22, 102, 12f) },   // holy/blessing
            { "stun", new FxSheet("stun", 24, 4, 45, 29, 23, 15, 12f) },   // status/stunned
            { "cleave", new FxSheet("cleave", 30, 7, 200, 274, 100, 165, 30f) },   // melee/cleave-reap
            { "stiumSpin", new FxSheet("stiumSpin", 26, 6, 228, 229, 115, 143, 30f) },   // stium_fx
            { "stiumSmash", new FxSheet("stiumSmash", 23, 6, 134, 174, 20, 146, 30f) },   // stium_fx
            { "darkWave", new FxSheet("darkWave", 6, 3, 61, 80, 44, 40, 20f) },   // stium_fx
            { "waveDust", new FxSheet("waveDust", 10, 5, 23, 39, 11, 21, 18f) },   // stium_fx
            { "darkPillar", new FxSheet("darkPillar", 20, 9, 32, 114, 16, 102, 24f) },   // stium_fx
            { "darkSlam", new FxSheet("darkSlam", 22, 4, 206, 140, 100, 101, 18f) },   // impacts/ground-slam
        };
    }
}
