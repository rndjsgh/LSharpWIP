using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace ZedSharp {
    internal class Zed {
        public enum ZedComboMove {
//Hardest
            LineCombo = 1,
            ShadowCoax = 2,
            Poke = 3,
            StickyUlt = 4,
            WQkill = 5,
            WEaaKill = 6,
            Qkill = 7,
            Ekill = 8,
        }

        public const HitChance customHitchance = HitChance.Medium;
        //Easiest

        public static Obj_AI_Hero Player = ObjectManager.Player;

        public static Spellbook sBook = Player.Spellbook;

        public static SummonerItems sumItems;

        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);
        public static Spell Q = new Spell(SpellSlot.Q, 900);
        public static Spell W = new Spell(SpellSlot.W, 550);
        public static Spell E = new Spell(SpellSlot.E, 290);
        public static Spell R = new Spell(SpellSlot.R, 600);

        public static Obj_AI_Minion shadowW;
        public static bool getRshad;
        public static bool getWshad;
        public static Obj_AI_Minion shadowR;
        public static float LastWCast;

        public static bool wIsCasted = false;
        public static bool serverTookWCast = false;
        public static float recast = 0;

        public static bool test = false;

        public static void setSkillshots() {
            Q.SetSkillshot(Qdata.SData.SpellCastTime, Qdata.SData.LineWidth, Qdata.SData.MissileSpeed, false,
                SkillshotType.SkillshotLine);
            sumItems = new SummonerItems(Player);
            //Thats all we need to set
        }

        public static float getFullComboDmg(Obj_AI_Hero target) {
            float dmg = 0;
            PredictionOutput po = Prediction.GetPrediction(target, 0.5f);
            float dist = Player.Distance(po.UnitPosition);
            float gapDist = ((W.IsReady()) ? W.Range : 0);
            float distAfterGap = dist - gapDist;

            if (distAfterGap < Player.AttackRange)
                dmg += (float) Player.GetAutoAttackDamage(target);
            if (Q.IsReady() && distAfterGap < Q.Range)
                dmg += Q.GetDamage(target);
            if (Q.IsReady() && W.IsReady() && distAfterGap < Q.Range && dist < Q.Range)
                dmg += Q.GetDamage(target)/2;
            if (distAfterGap < E.Range)
                dmg += E.GetDamage(target);
            if (R.IsReady() && distAfterGap < R.Range) {
                dmg += R.GetDamage(target);
                dmg += (float) Player.CalcDamage(target, Damage.DamageType.Physical, (dmg*(5 + 15*R.Level)/100));
            }
            if (Items.HasItem(3153)) // botrk
                dmg += (float) Player.GetItemDamage(target, Damage.DamageItems.Botrk);
            if (Items.HasItem(3074))
                dmg += (float) Player.GetItemDamage(target, Damage.DamageItems.Hydra);
            if (Items.HasItem(3077))
                dmg += (float) Player.GetItemDamage(target, Damage.DamageItems.Tiamat);
            if (Items.HasItem(3144))
                dmg += (float) Player.GetItemDamage(target, Damage.DamageItems.Bilgewater);

            return dmg;
        }

        public static bool canGoToShadow(string type) {
            // TODO Shadow param W or R
            if (type == "W") {
                if (ZedSharp.W2)
                    return true;
            }
            if (type == "R") {
                if (ZedSharp.R2)
                    return true;
            }
            return false;
        }

        public static void normalCombo(Obj_AI_Hero target) {
            if (target.IsValidTarget(W.Range + Q.Range)) {
                if (W.IsReady() && shadowW == null &&
                    ((!getWshad && recast <= Environment.TickCount && !serverTookWCast))) {
                    // Throw W
                    W.Cast(target.Position, true);
                    serverTookWCast = false;
                    wIsCasted = true;
                    recast = Environment.TickCount + 320;
                }
            }
            if (E.IsReady() && target.Distance(shadowW) <= E.Range ||
                Player.Distance(shadowW) <= E.Range && ZedSharp.menu.Item("useEC").GetValue<bool>()) {
                // Cast E..
                E.Cast(true);
            }
            if (ZedSharp.menu.Item("useQC").GetValue<bool>() && target.IsValidTarget(Q.Range)) {
                if (shadowW != null) {
                    // if shadow is not null update source position.
                    if (Q.IsReady()) {
                        if (target.Distance(shadowW) <= Q.Range) {
                            Q.UpdateSourcePosition(shadowW.ServerPosition, shadowW.ServerPosition);
                            Q.Cast(target);
                        }
                        else if (target.Distance(Player) <= Q.Range) {
                            Q.UpdateSourcePosition(Player.Position, Player.Position);
                            PredictionOutput QPrediction = Q.GetPrediction(target);
                            if (QPrediction.Hitchance >= HitChance.Medium)
                                Q.Cast(QPrediction.UnitPosition);
                        }
                    }
                    return;
                }
                    if (Q.IsReady()) {
                        if (Q.GetPrediction(target).Hitchance >= customHitchance) {
                            Q.Cast(target);
                        }
                    }
            }
        }

        public static bool isKillableShadowCoax(Obj_AI_Hero target) {
            float health = target.Health;
            int igniteDMG = sumItems.isIgniteReady() ? 50 + 20*Player.Level : 0;
            float botrkDmg = Items.HasItem(3153) && Items.CanUseItem(3153) ? target.MaxHealth*0.1f : 0f;
            float cutLassDmg;
            return Q.GetDamage(target) + E.GetDamage(target) + Player.GetAutoAttackDamage(target)*2 + igniteDMG +
                   botrkDmg >=
                   health;
        }

        public static void shadowCoax(Obj_AI_Hero target) {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            // Game.PrintChat("Wut");
            if (shadowW == null || !canGoToShadow("W") || !isKillableShadowCoax(target)) return;
            //var target =
            //    ObjectManager.Get<Obj_AI_Hero>().First(h => h.IsEnemy && h.IsValidTarget() && h.Distance(shadowW) <= R.Range && isKillableShadowCoax(h));
            if (target == null || !canDoCombo(new[] {SpellSlot.Q, SpellSlot.E, SpellSlot.R})) return;
            if (canGoToShadow("W") && shadowW != null) {
                W.Cast();
            }
            ;
            if (R.IsReady() && shadowR == null) {
                R.Cast(target);
            }
            if (E.IsReady() && shadowR != null) {
                E.Cast();
            }

            if (Q.IsReady() && shadowR != null) {
                Q.Cast(target, true);
            }
            LXOrbwalker.ForcedTarget = target;
            if (LXOrbwalker.CanAttack()) Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            sumItems.castIgnite(target);
            castItemsFull(target);
            // if (canGoToShadow("R") && shadowR != null && !Player.IsAutoAttacking) {
            //     R.Cast();
            //  }
        }

        /*public static void doShadowCoax(Obj_AI_Hero target) {
            if (E.IsReady() && Q.IsReady() && shadowW != null && LXOrbwalker.CanAttack() && canGoToShadow("W") &&
                    isKillableShadowCoax(target) && target.Distance(shadowW.Position) <= R.Range) {
                shadowCoax(target);
            }
        }*/

        public static void doLineCombo(Obj_AI_Base target) {
            try {
                //Tried to Add shadow Coax
                float dist = Player.Distance(target);
                if (R.IsReady() && shadowR == null && dist < R.Range &&
                    canDoCombo(new[] {SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R})) {
                    R.Cast(target);
                }
                //eather casts 2 times or 0 get it to cast 1 time TODO
                // Game.PrintChat("W2 "+ZedSharp.W2);
                /*foreach (
                Obj_AI_Hero newtarget in
                ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(Q.Range)).Where(
                enemy => enemy.HasBuff("zedulttargetmark") && enemy.IsEnemy && !enemy.IsMinion)) {
                target = newtarget;
                }*/
                //PredictionOutput p1o = Prediction.GetPrediction(target, 0.350f);
                Vector3 shadowPos = target.Position + Vector3.Normalize(target.Position - shadowR.Position)*E.Range;
                if (W.IsReady() && shadowW == null &&
                    ((!getWshad && recast < Environment.TickCount && !serverTookWCast))) {
                    //V2E(shadowR.Position, po.UnitPosition, E.Range)
                    Console.WriteLine("cast WWW");
                    W.Cast(shadowPos);
                    serverTookWCast = false;
                    wIsCasted = true;
                    recast = Environment.TickCount + 300;
                }
                if (E.IsReady() && shadowW != null || shadowR != null) {
                    E.Cast();
                }
                if (Q.IsReady() && shadowW != null && shadowR != null) {
                    float midDist = dist;
                    midDist += target.Distance(shadowR);
                    midDist += target.Distance(shadowW);
                    float delay = midDist/(Q.Speed*3);
                    PredictionOutput po = Prediction.GetPrediction(target, delay*1.1f);
                    if (po.Hitchance > HitChance.Low) {
                        // Console.WriteLine("Cast QQQQ");
                        Q.Cast(po.UnitPosition);
                    }
                }
                if (shadowR != null) {
                    castItemsFull(target);
                }
            }
            catch (Exception ex) {
                //Console.WriteLine(ex);
            }
        }


        private static void castItemsFull(Obj_AI_Base target) {
            if (target.Distance(Player) < 500) {
                sumItems.cast(SummonerItems.ItemIds.Ghostblade);
                sumItems.castIgnite((Obj_AI_Hero) target);
            }
            if (target.Distance(Player) < 500) {
                sumItems.cast(SummonerItems.ItemIds.BotRK, target);
                sumItems.cast(SummonerItems.ItemIds.Cutlass, target);
            }
            if (target.Distance(Player.ServerPosition) < (400 + target.BoundingRadius - 20)) {
                sumItems.cast(SummonerItems.ItemIds.Tiamat);
                sumItems.cast(SummonerItems.ItemIds.Hydra);
            }
        }


        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance) {
            return (from + distance*Vector3.Normalize(direction - from)).To2D();
        }

        public static void Flee() {
            bool isEnabled = ZedSharp.menu.Item("Flee").GetValue<KeyBind>().Active;
            if (!isEnabled) return;
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (W.IsReady()) W.Cast(Game.CursorPos, true);
            List<Obj_AI_Hero> ListOfEnemies =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(he => he.IsValidTarget() && he.Distance(shadowW.Position) <= E.Range).ToList();
            if (ListOfEnemies.Count > 0 && E.IsReady()) E.Cast(true);
        }

        private static bool canDoCombo(IEnumerable<SpellSlot> sp) {
            float delay = sp.Sum(sp1 => Player.Spellbook.GetSpell(sp1).SData.SpellCastTime); //Hope it is correct
            float totalCost = sp.Sum(sp1 => Player.Spellbook.GetManaCost(sp1));
            return Player.Mana + delay*5 >= totalCost;
        }

        public static void doHarass(Obj_AI_Hero target) {
            if (ZedSharp.menu.Item("harassEnabled").GetValue<bool>()) {
                switch (ZedSharp.menu.Item("harassMode").GetValue<StringList>().SelectedIndex) {
                    case 0: //WEQ
                        if (target.IsValidTarget(W.Range + Q.Range)) {
                            if (W.IsReady() && shadowW == null &&
                                ((!getWshad && recast <= Environment.TickCount && !serverTookWCast))) {
                                // Throw W
                                W.Cast(target.Position, true);
                                serverTookWCast = false;
                                wIsCasted = true;
                                recast = Environment.TickCount + 320;
                            }
                        }
                            if (E.IsReady() && target.Distance(shadowW) <= E.Range ||
                                Player.Distance(shadowW) <= E.Range) {
                                // Cast E..
                                E.Cast(true);
                            }
                        
                        if (target.IsValidTarget(Q.Range)) {
                            if (shadowW != null) {
                                // if shadow is not null update source position.
                                if (Q.IsReady()) {
                                    if (target.Distance(shadowW) <= Q.Range) {
                                        Q.UpdateSourcePosition(shadowW.ServerPosition, shadowW.ServerPosition);
                                        Q.Cast(target, true);
                                    }
                                    else if (target.Distance(Player) <= Q.Range) {
                                        Q.UpdateSourcePosition(Player.Position, Player.Position);
                                        PredictionOutput QPrediction = Q.GetPrediction(target);
                                        if (QPrediction.Hitchance >= HitChance.Medium)
                                            Q.Cast(QPrediction.UnitPosition);
                                    }
                                }
                            }
                            if (shadowW == null) {
                                if (Q.IsReady()) {
                                    if (Q.GetPrediction(target).Hitchance >= customHitchance) {
                                        Q.UpdateSourcePosition(Player.Position, Player.Position);
                                        Q.Cast(target);
                                    }
                                }
                            }
                        }
                        break;
                    case 1: //QE
                        if (target.Distance(Player) <= Q.Range && target.IsValidTarget(Q.Range)) {
                            PredictionOutput QPrediction = Q.GetPrediction(target);
                            if (QPrediction.Hitchance >= HitChance.Medium && Q.IsReady())
                                Q.Cast(QPrediction.UnitPosition);
                        }
                        if (target.Distance(Player) <= E.Range && target.IsValidTarget(W.Range)) {
                            if (E.IsReady()) {
                                E.Cast(true);
                            }
                        }
                        break;
                    case 2: //Q
                        if (target.Distance(Player) <= Q.Range && target.IsValidTarget(Q.Range) && Q.IsReady()) {
                            PredictionOutput QPrediction = Q.GetPrediction(target);
                            if (QPrediction.Hitchance >= HitChance.Medium)
                                Q.Cast(QPrediction.UnitPosition);
                        }
                        break;
                    case 3: // E
                        if (target.Distance(Player) <= E.Range && target.IsValidTarget(W.Range)) {
                            if (E.IsReady()) {
                                E.Cast(true);
                            }
                        }
                        break;
                }
            }
        }

        public static void checkForSwap(string mode) {
            switch (mode) {
                case "LowHP":
                    int HPPerc = ZedSharp.menu.Item("SwapHP").GetValue<Slider>().Value;
                    float myHP = (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100;
                    if (myHP <= HPPerc && ZedSharp.menu.Item("SwapHPToggle").GetValue<bool>()) {
                        if (canGoToShadow("R") && isSafeSwap(shadowR))
                            R.Cast();
                    }
                    break;
                case "OnKill":
                    if (canGoToShadow("R") && isSafeSwap(shadowR))
                        R.Cast();
                    break;
            }
        }

        public static bool isSafeSwap(Obj_AI_Minion shadow) {
            if (!ZedSharp.menu.Item("SafeRBack").GetValue<bool>()) return true;
            //Idk if 500 is ok, maybe we can increment it a little bit more
            int enemiesShadow = shadow.Position.CountEnemysInRange(500);
            int enemiesPlayer = Player.Position.CountEnemysInRange(500);
            if (enemiesShadow < enemiesPlayer)
                return true;
            return false;
        }

        public static void doLastHit() {
            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(Player.Position, Q.Range);
            foreach (Obj_AI_Base minion in allMinions) {
                if (Q.GetDamage(minion) >= minion.Health && ZedSharp.menu.Item("useQLH").GetValue<bool>()) {
                    if (Q.IsReady() && minion.Distance(Player) <= Q.Range) {
                        Q.Cast(minion, true, true);
                    }
                }
                if (E.GetDamage(minion) >= minion.Health && ZedSharp.menu.Item("useELH").GetValue<bool>()) {
                    if (E.IsReady() && minion.Distance(Player) <= E.Range) {
                        E.Cast(true);
                    }
                }
            }
        }

        public static void doLaneClear() {
            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            if (ZedSharp.menu.Item("useQLC").GetValue<bool>()) {
                MinionManager.FarmLocation bestLocation = Q.GetLineFarmLocation(allMinions.ToList(), Q.Width);
                Q.Cast(bestLocation.Position);
            }
            if (ZedSharp.menu.Item("useELC").GetValue<bool>()) {
                MinionManager.FarmLocation bestLocation = E.GetCircularFarmLocation(allMinions.ToList(), E.Width);
                if (Player.Distance(bestLocation.Position) <= E.Range) {
                    E.Cast(true);
                }
            }
        }
    }
}