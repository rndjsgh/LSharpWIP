using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace ZedSharp {
    internal class DangerousDB {
        private readonly List<DangerousSpell> dangerousDB = new List<DangerousSpell>();

        private void compileDB() {
            dangerousDB.Add(new DangerousSpell {ChampName = "Amumu", spell = SpellSlot.R});
            dangerousDB.Add(new DangerousSpell
            {ChampName = "Annie", spell = SpellSlot.R, buff = "Pyromania", danger = 5}); //Check buff name
            dangerousDB.Add(new DangerousSpell
            {ChampName = "Annie", spell = SpellSlot.Q, buff = "Pyromania", danger = 5}); //Check buff name
            dangerousDB.Add(new DangerousSpell {ChampName = "Ashe", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Cassiopeia", spell = SpellSlot.R, danger = 4});
            dangerousDB.Add(new DangerousSpell {ChampName = "Galio", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Gnar", spell = SpellSlot.R, danger = 4});
            dangerousDB.Add(new DangerousSpell {ChampName = "Gragas", spell = SpellSlot.R, danger = 4});
            dangerousDB.Add(new DangerousSpell {ChampName = "Leona", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Leona", spell = SpellSlot.Q, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Malphite", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Orianna", spell = SpellSlot.R, danger = 4});
            dangerousDB.Add(new DangerousSpell {ChampName = "Sona", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Taric", spell = SpellSlot.E, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Alistar", spell = SpellSlot.Q, danger = 4});
            dangerousDB.Add(new DangerousSpell {ChampName = "Lissandra", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Malzahar", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Riven", spell = SpellSlot.W, danger = 4});
            dangerousDB.Add(new DangerousSpell {ChampName = "Pantheon", spell = SpellSlot.W, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Sejuani", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell {ChampName = "Skarner", spell = SpellSlot.R, danger = 5});
            dangerousDB.Add(new DangerousSpell
            {ChampName = "Fiddlesticks", spell = SpellSlot.Q, danger = getDangerLevelSkill("Fiddlesticks", SpellSlot.Q)});
        }

        private bool canGoIn(Obj_AI_Hero target) {
            var DangerousheroNear = new List<DangerousHero>();
            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsEnemy)) {
                foreach (DangerousSpell DP in dangerousDB) {
                    if (DP.ChampName == enemy.ChampionName && spellUp(enemy.ChampionName, DP.spell) &&
                        enemy.Distance(target) <= enemy.Spellbook.GetSpell(DP.spell).SData.CastRange[0]) {
                        DangerousheroNear.Add(new DangerousHero {dangerLevel = DP.danger, hero = enemy, slot = DP.spell});
                    }
                }
            }
            var dangerLevel = 0;
            foreach (var DH in DangerousheroNear)
            {
                dangerLevel += DH.dangerLevel;
            }
            
            return dangerLevel >= 15;
        }

        private bool spellUp(String Champion, SpellSlot slot) {
            return getChampion("Champion").Spellbook.CanUseSpell(slot) == SpellState.Ready;
        }

        private int getDangerLevelSkill(String champ, SpellSlot spell) {
            if (champ == "Fiddlesticks" && spell == SpellSlot.Q)
                return 1*getChampion(champ).Spellbook.GetSpell(spell).Level;
            return 5;
        }

        private Obj_AI_Hero getChampion(String champ) {
            return ObjectManager.Get<Obj_AI_Hero>().First(ch => ch.IsEnemy && ch.ChampionName == champ);
        }
    }

    internal class DangerousSpell {
        public String ChampName { get; set; }
        public SpellSlot spell { get; set; }
        public String buff { get; set; }
        public int danger { get; set; }
    }

    internal class DangerousHero {
        public Obj_AI_Hero hero { get; set; }
        public int dangerLevel { get; set; }
        public SpellSlot slot { get; set; }
    }
}
