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
        } //Easiest

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
        public static bool getRshad = false;
        public static bool getWshad = false;
        public static Obj_AI_Minion shadowR;
        public static HitChance CustomHitChance = HitChance.Medium;
        public static float LastWCast = 0;

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

        public static void normalCombo() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);

            if (Q.IsReady() && ZedSharp.menu.Item("useQC").GetValue<bool>()) {
                if (W.IsReady() && target.Distance(Player) < Q.Range + Q.Range && !canGoToShadow("W"))
                {
                    W.Cast(target.Position, true);
                    ZedSharp.W2 = true;
                    if (Q.GetPrediction(target, true).Hitchance >= HitChance.High)
                    {
                        Q.Cast(target, true, true);
                    }
                    if (E.IsReady() && shadowW.Distance(target) <= E.Range || Player.Distance(target) <= E.Range)
                    {
                        E.Cast(true);
                    }
                }
                else {
                    if (Q.GetPrediction(target, true).Hitchance >= CustomHitChance) {
                        Q.Cast(target, true, true); // Normal 
                    }

                    if (E.IsReady() && Player.Distance(target) <= E.Range) {
                        E.Cast(true);
                    }
                }
            }
        }

        public static void doCombo() {
            //Combo goes here
            if (!canDoCombo(new[] {SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R})) return;
            Obj_AI_Hero target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
            LXOrbwalker.ForcedTarget = target;
            int Shuriken = ZedSharp.menu.Item("minQ").GetValue<Slider>().Value;
            int minE = ZedSharp.menu.Item("minE").GetValue<Slider>().Value;
            Vector3 ShadowPos = shadowR != null ? shadowR.Position : Vector3.Zero;
            if (R.IsReady() && !canGoToShadow("R")) {
                ShadowPos = Player.ServerPosition;
                R.Cast(target, true);
            }
            //Fix
            Vector3 shadowPos = target.Position + Vector3.Normalize(target.Position - shadowR.Position)*W.Range;
            if (!canGoToShadow("W") && W.IsReady() && Player.Distance(target) <= 300) {
                Game.PrintChat("W2 " + ZedSharp.W2);
                W.Cast(new Vector3(shadowPos.X, shadowPos.Y, target.ServerPosition.Z));
                ZedSharp.W2 = true;
                Game.PrintChat("W2 Now: " + ZedSharp.W2);
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
            PredictionOutput CustomQPredictionR = Prediction.GetPrediction(new PredictionInput {
                Unit = target,
                Delay = Q.Delay,
                Radius = Q.Width,
                From = shadowR.Position, //We check for prediction in advance
                Range = Q.Range,
                Collision = false,
                Type = Q.Type,
                RangeCheckFrom = ObjectManager.Player.ServerPosition,
                Aoe = false
            });

            #region QCasting

            if ((QPrediction.Hitchance >= CustomHitChance && CustomQPredictionR.Hitchance >= CustomHitChance &&
                 CustomQPredictionW.Hitchance >= CustomHitChance)) //Triple Q!
            {
                Q.Cast(QPrediction.CastPosition, true);
            }
            else if (((QPrediction.Hitchance >= CustomHitChance && CustomQPredictionR.Hitchance >= CustomHitChance)
                      || (QPrediction.Hitchance >= CustomHitChance && CustomQPredictionW.Hitchance >= CustomHitChance)) &&
                     Shuriken < 3) //Q-R / q-W Prediction
            {
                Q.Cast(QPrediction.CastPosition, true);
            }
            else if ((CustomQPredictionW.Hitchance >= CustomHitChance && CustomQPredictionR.Hitchance >= CustomHitChance) &&
                     Shuriken < 3) //R-W Prediction
            {
                Q.Cast(CustomQPredictionR.CastPosition, true);
            }
            else {
                if (CustomQPredictionR.Hitchance >= CustomHitChance && Shuriken < 2)
                    Q.Cast(CustomQPredictionR.CastPosition, true);
                if (CustomQPredictionW.Hitchance >= CustomHitChance && Shuriken < 2)
                    Q.Cast(CustomQPredictionW.CastPosition, true);
                if (QPrediction.Hitchance >= CustomHitChance && Shuriken < 2)
                    Q.Cast(QPrediction.CastPosition, true);
            }

            #endregion

            #region ECasting

            if (target.Distance(shadowR.Position) <= E.Range && target.Distance(shadowW.Position) <= E.Range &&
                //Triple E
                target.Distance(Player.Position) <= E.Range) {
                E.Cast(true);
            }
            else if ((target.Distance(shadowR.Position) <= E.Range && target.Distance(shadowW.Position) <= E.Range
                //Double E
                      || target.Distance(Player.Position) <= E.Range && target.Distance(shadowW.Position) <= E.Range
                      || target.Distance(Player.Position) <= E.Range && target.Distance(shadowR.Position) <= E.Range)
                     && minE < 3) {
                E.Cast(true);
            }
            else {
                if (target.Distance(shadowR.Position) <= E.Range && minE < 2) //Single E
                    E.Cast(true);
                if (target.Distance(shadowW.Position) <= E.Range && minE < 2)
                    E.Cast(true);
                if (target.Distance(Player.Position) <= E.Range && minE < 2)
                    E.Cast(true);
            }

            #endregion
        }


        public static void doLaneCombo(Obj_AI_Base target) { // TODO kinda works Sometime :^)
            try
            {
                float dist = Player.Distance(target);
                if (R.IsReady() && shadowR == null && dist < R.Range &&
                    canDoCombo(new[] {SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R})) {
                    R.Cast(target);
                }
                //eather casts 2 times or 0 get it to cast 1 time TODO
               // Game.PrintChat("W2 "+ZedSharp.W2);
                
                PredictionOutput p1o = Prediction.GetPrediction(target, 0.350f);
                Vector3 shadowPos = target.Position + Vector3.Normalize(target.Position - shadowR.Position) * E.Range;
                if(Environment.TickCount - LastWCast < 300)return;
                LastWCast = Environment.TickCount;
                if (W.IsReady() && E.IsReady() && shadowW == null && !getWshad) {
                    //V2E(shadowR.Position, po.UnitPosition, E.Range)
                    W.Cast(shadowPos);
                    Console.WriteLine("Cast WWW cmnn");
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
                        Console.WriteLine("Cast QQQQ");

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
            float totalCost = sp.Sum(sp1 => Player.Spellbook.GetManaCost(sp1));
            return Player.Mana >= totalCost;
        }

        public static Vector3 getOptimalShadowPlacement(Vector3 TargetPos) {
            float LastShadowDist = float.MaxValue;
            Vector3 LastVec = Vector3.Zero;
            for (float r = W.Range; r >= 65; r -= 100) {
                double RadAngle = 2*Math.PI*r/100;
                for (int i = 0; i < RadAngle; i++) {
                    double RadAngle2 = i*2*Math.PI/RadAngle;
                    var cos = (float) Math.Cos(RadAngle2);
                    var sin = (float) Math.Cos(RadAngle2);
                    var PossiblePoint = new Vector3(Player.Position.X + r*cos, Player.Position.Y + r*sin,
                        Player.Position.Z);
                    if (!Utility.IsWall(PossiblePoint) && TargetPos.Distance(PossiblePoint) < LastShadowDist) {
                        LastShadowDist = TargetPos.Distance(PossiblePoint);
                        LastVec = PossiblePoint;
                    }
                }
            }
            return LastVec;
        }


        public static void doHarass() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (W.IsReady() && ZedSharp.menu.Item("useWC").GetValue<bool>()) {
                if (LastCastedSpell.LastCastPacketSent.Slot != SpellSlot.W) {
                    W.Cast(target.Position, true);
                    Utility.DelayAction.Add(100, castQ);
                }
            }
            else {
                castQ();
            }

            if (E.IsReady() && ZedSharp.menu.Item("useEC").GetValue<bool>()) {
                if (target.Distance(Player) <= E.Range || target.Distance(shadowW) <= E.Range)
                    E.Cast();
            }
        }

        private static void castQ() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range + Q.Range, SimpleTs.DamageType.Physical);
            if (!Q.IsReady() || ZedSharp.menu.Item("useQC").GetValue<bool>()) return;

            Q.UpdateSourcePosition(Player.Position, Player.Position);

            if (shadowW != null) {
                Q.UpdateSourcePosition(shadowW.Position, shadowW.Position);
                Q.Cast(target, true, true);
                Q.UpdateSourcePosition(Player.Position, Player.Position);
                Q.Cast(target, true, true);
            }
            else if (Q.GetPrediction(target, true).Hitchance >= CustomHitChance) {
                Q.Cast(target, true, true);
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

        private static bool isSafeSwap(Obj_AI_Minion shadow) {
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