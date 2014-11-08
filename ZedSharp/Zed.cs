using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace ZedSharp {
    internal class Zed {
        public static Obj_AI_Hero Player = ObjectManager.Player;

        public static Spellbook sBook = Player.Spellbook;

        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);
        public static Spell Q = new Spell(SpellSlot.Q, 900);
        public static Spell W = new Spell(SpellSlot.W, 550);
        public static Spell E = new Spell(SpellSlot.E, 290);
        public static Spell R = new Spell(SpellSlot.R, 600);

        public static Obj_AI_Minion shadowW;
        public static bool getRshad = false;
        public static Obj_AI_Minion shadowR;

        public static HitChance CustomHitChance = HitChance.High;

        public static void setSkillshots() {
            Q.SetSkillshot(Qdata.SData.SpellCastTime, Qdata.SData.LineWidth, Qdata.SData.MissileSpeed, false,
                SkillshotType.SkillshotLine);
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
            return dmg;
        }

        public static bool canGoToShadow(string type) {
            // TODO Shadow param W or R
            if (type == "W") {
                if (Wdata.Name == "zedw2")
                    return true;
            }
            if (type == "R") {
                if (Rdata.Name == "ZedR2")
                    return true;
            }
            return false;
        }

        public static void doCombo() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (R.IsReady() && ZedSharp.menu.Item("useRC").GetValue<bool>())
                R.Cast(target);

            if (W.IsReady() && ZedSharp.menu.Item("useWC").GetValue<bool>()) {
                W.Cast(target.Position, true);
            }

            /*if (Player.Distance(target) > LXOrbwalker.GetAutoAttackRange() &&
                shadowW.Distance(target) < LXOrbwalker.GetAutoAttackRange()) {
                //TODO second cast W ?
                if (canGoToShadow("W") && ZedSharp.menu.Item("useWF").GetValue<bool>()) {
                    W.Cast(Player, true); // Check if this works
                }
            }*/ // TODO fix this?


            if (shadowW != null) {
                PredictionOutput CustomQPredictionW = Prediction.GetPrediction(new PredictionInput {
                    Unit = target,
                    Delay = Q.Delay,
                    Radius = Q.Width,
                    From = shadowW.Position, //We check for prediction in advance
                    Range = Q.Range,
                    Collision = false,
                    Type = Q.Type,
                    RangeCheckFrom = ObjectManager.Player.ServerPosition,
                    Aoe = true
                });

                if (shadowR != null) {
                    PredictionOutput CustomQPredictionR = Prediction.GetPrediction(new PredictionInput {
                        Unit = target,
                        Delay = Q.Delay,
                        Radius = Q.Width,
                        From = shadowR.Position, //We check for prediction in advance
                        Range = Q.Range,
                        Collision = false,
                        Type = Q.Type,
                        RangeCheckFrom = ObjectManager.Player.ServerPosition,
                        Aoe = true
                    });

                    if (ZedSharp.menu.Item("useQC").GetValue<bool>()) {
                        if (Q.IsReady() && target.Distance(ObjectManager.Player) <= Q.Range &&
                            Q.GetPrediction(target, true).Hitchance >= CustomHitChance) {
                            Q.Cast(Q.GetPrediction(target, true).CastPosition, true);
                        }
                        if (Q.IsReady() && target.Distance(shadowW.Position) <= Q.Range &&
                            CustomQPredictionW.Hitchance >= CustomHitChance) {
                            Q.Cast(CustomQPredictionW.CastPosition, true);
                        }
                        if (Q.IsReady() && target.Distance(shadowR.Position) <= Q.Range &&
                            CustomQPredictionR.Hitchance >= CustomHitChance) {
                            Q.Cast(CustomQPredictionR.CastPosition, true);
                        }
                    }
                }
            }

            if (E.IsReady() && target.Distance(shadowW) <= E.Range ||
                target.Distance(Player) <= E.Range && ZedSharp.menu.Item("useEC").GetValue<bool>()) {
                // TODO check shadow position with enemy position so we can cast e effectivly.
                E.CastOnUnit(ObjectManager.Player);
            }

            foreach (
                Obj_AI_Hero enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(
                        hero => hero.IsValidTarget() && hero.HasBuff("zedulttargetmark"))) {
                LXOrbwalker.ForcedTarget = enemy;
            }
        }

        public static void doHarass() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (W.IsReady()) {
                Vector3 positionBehind = target.Position +
                                         Vector3.Normalize(target.Position - ObjectManager.Player.Position)*200;
                W.Cast(target.Position, true);
            }
            PredictionOutput QPrediction = Q.GetPrediction(target);
            PredictionOutput CustomQPredictionW = Prediction.GetPrediction(new PredictionInput {
                Unit = target,
                Delay = Q.Delay,
                Radius = Q.Width,
                From = shadowW.Position, //We check for prediction in advance
                Range = Q.Range,
                Collision = false,
                Type = Q.Type,
                RangeCheckFrom = ObjectManager.Player.ServerPosition,
                Aoe = false
            });

            if (Q.IsReady() && target.Distance(ObjectManager.Player) <= Q.Range &&
                QPrediction.Hitchance >= CustomHitChance) {
                Q.Cast(QPrediction.CastPosition, true);
            }
            if (Q.IsReady() && target.Distance(shadowW.Position) <= Q.Range &&
                CustomQPredictionW.Hitchance >= CustomHitChance) {
                Q.Cast(CustomQPredictionW.CastPosition, true);
            }
            if (E.IsReady() && target.Distance(ObjectManager.Player) <= E.Range ||
                target.Distance(shadowW.Position) <= E.Range) {
                E.CastOnUnit(ObjectManager.Player, true);
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
            int enemiesPlayer = ObjectManager.Player.Position.CountEnemysInRange(500);
            if (enemiesShadow < enemiesPlayer) 
                return true;
            return false;
        }
    }
}