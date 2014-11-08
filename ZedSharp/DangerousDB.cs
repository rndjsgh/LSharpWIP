using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace ZedSharp {
    internal class DangerousDB {
        private readonly List<DangerousSpell> dangerousSpells = new List<DangerousSpell>();

        private void populateList() {
            dangerousSpells.Add(new DangerousSpell {ChampName = "Amumu", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell
            {ChampName = "Annie", spell = SpellSlot.R, buff = "Pyromania", danger = 5}); //Check buff name
            dangerousSpells.Add(new DangerousSpell
            {ChampName = "Annie", spell = SpellSlot.Q, buff = "Pyromania", danger = 5}); //Check buff name
            dangerousSpells.Add(new DangerousSpell {ChampName = "Ashe", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Cassiopeia", spell = SpellSlot.R, danger = 4});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Galio", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Gnar", spell = SpellSlot.R, danger = 4});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Gragas", spell = SpellSlot.R, danger = 4});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Leona", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Leona", spell = SpellSlot.Q, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Malphite", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Orianna", spell = SpellSlot.R, danger = 4});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Sona", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Taric", spell = SpellSlot.E, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Alistar", spell = SpellSlot.Q, danger = 4});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Lissandra", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Malzahar", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Riven", spell = SpellSlot.W, danger = 4});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Pantheon", spell = SpellSlot.W, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Sejuani", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell {ChampName = "Skarner", spell = SpellSlot.R, danger = 5});
            dangerousSpells.Add(new DangerousSpell
            {ChampName = "Fiddlesticks", spell = SpellSlot.Q, danger = getDangerLevel("Fiddlesticks", SpellSlot.Q)});
        }

        private bool canGoHam(Obj_AI_Hero target) {
            List<DangerousHero> DangerousheroNear =
                (from enemy in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsEnemy)
                    from dangerousSpell in dangerousSpells
                    where
                        dangerousSpell.ChampName == enemy.ChampionName &&
                        isSpellReady(enemy.ChampionName, dangerousSpell.spell) &&
                        enemy.Distance(target) <= enemy.Spellbook.GetSpell(dangerousSpell.spell).SData.CastRange[0] && //Not sure CastRange[0] Works
                        (dangerousSpell.buff!=null?enemy.HasBuff(dangerousSpell.buff,true):true)
                    select
                        new DangerousHero
                        {dangerLevel = dangerousSpell.danger, hero = enemy, slot = dangerousSpell.spell}).ToList();

            /**
             * foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(e => e.IsEnemy)) {
                foreach (DangerousSpell dangerousSpell in dangerousSpells) {
                    if (dangerousSpell.ChampName == enemy.ChampionName &&
                        isSpellReady(enemy.ChampionName, dangerousSpell.spell) &&
                        enemy.Distance(target) <= enemy.Spellbook.GetSpell(dangerousSpell.spell).SData.CastRange[0]) {
                        DangerousheroNear.Add(new DangerousHero
                        {dangerLevel = dangerousSpell.danger, hero = enemy, slot = dangerousSpell.spell});
                    }
                }
            }*/

            int dangerLevel = DangerousheroNear.Sum(dangerousHero => dangerousHero.dangerLevel);

            return dangerLevel < 15;
        }

        /// <summary>
        ///     Returns true if the hero has the item.
        /// </summary>
        public static bool hasItem(int id, Obj_AI_Hero hero) {
            return hero.InventoryItems.Any(slot => slot.Id == (ItemId) id);
        }


        /// <summary>
        ///     Retruns true if the player has the item and its not on cooldown.
        /// </summary>
        public static bool canUseItem(int id, Obj_AI_Hero target) {
            InventorySlot islot = null;
            foreach (
                InventorySlot slot in target.InventoryItems.Where(slot => slot.Id == (ItemId) id && target.IsEnemy && target.IsValid)) {
                islot = slot;
            }
            if (islot == null) {
                return false;
            }
            int add = Game.Version.Contains("4.19") ? 6 : 4;
            SpellDataInst inst = target.Spellbook.Spells.FirstOrDefault(spell => (int) spell.Slot == islot.Slot + add);
            return inst != null && inst.State == SpellState.Ready;
        }

        private bool isSpellReady(String Champion, SpellSlot slot) {
            return getChampion("Champion").Spellbook.CanUseSpell(slot) == SpellState.Ready;
        }

        private int getDangerLevel(String champ, SpellSlot spell) {
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