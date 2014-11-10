using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        public static void normalCombo() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range + W.Range, SimpleTs.DamageType.Physical);

            if (Q.IsReady() && ZedSharp.menu.Item("useQC").GetValue<bool>()) {
                if (W.IsReady() && target.Distance(Player) < W.Range) {
                    W.Cast(target.Position, true);
                }
                else {
                    if (Q.GetPrediction(target, true).Hitchance >= CustomHitChance) {
                        Q.Cast(target, true, true);
                    }
                }
            }
        }

        public static void doCombo() {
            //Combo goes here
            if(!canDoCombo(new SpellSlot[] {SpellSlot.Q,SpellSlot.W,SpellSlot.E,SpellSlot.R}))return;
            Obj_AI_Hero target = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Physical);
            LXOrbwalker.ForcedTarget = target;
            var Shuriken = ZedSharp.menu.Item("minQ").GetValue<Slider>().Value;
            var minE = ZedSharp.menu.Item("minE").GetValue<Slider>().Value;
            if (R.IsReady() && !canGoToShadow("R"))
            {
                R.Cast(target,true);
            }
            var shadowPos = getOptimalShadowPlacement(target.Position);
            if (shadowPos != Vector3.Zero && !canGoToShadow("W") && W.IsReady())
            {
                W.Cast(shadowPos,true);
            }
            PredictionOutput QPrediction = Q.GetPrediction(target);
            PredictionOutput CustomQPredictionW = Prediction.GetPrediction(new PredictionInput
            {
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
            PredictionOutput CustomQPredictionR = Prediction.GetPrediction(new PredictionInput
            {
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
                Q.Cast(QPrediction.CastPosition,true);

            }else if (((QPrediction.Hitchance >= CustomHitChance && CustomQPredictionR.Hitchance >= CustomHitChance) 
                || (QPrediction.Hitchance >= CustomHitChance && CustomQPredictionW.Hitchance>=CustomHitChance)) && Shuriken<3) //Q-R / q-W Prediction
            {
                
                Q.Cast(QPrediction.CastPosition,true);
            }
            else if ((CustomQPredictionW.Hitchance >= CustomHitChance && CustomQPredictionR.Hitchance >= CustomHitChance) && Shuriken<3) //R-W Prediction
            {
                
                Q.Cast(CustomQPredictionR.CastPosition,true);
            }
            else
            {
                if (CustomQPredictionR.Hitchance >= CustomHitChance && Shuriken < 2)
                    Q.Cast(CustomQPredictionR.CastPosition, true);
                if (CustomQPredictionW.Hitchance >= CustomHitChance && Shuriken < 2)
                    Q.Cast(CustomQPredictionW.CastPosition, true);
                if (QPrediction.Hitchance >= CustomHitChance && Shuriken < 2)
                    Q.Cast(QPrediction.CastPosition, true);
            }
            #endregion

            #region ECasting

            if (target.Distance(shadowR.Position) <= E.Range && target.Distance(shadowW.Position) <= E.Range && //Triple E
                target.Distance(Player.Position) <= E.Range)
            {
                E.Cast(true);
            }else if ((target.Distance(shadowR.Position) <= E.Range && target.Distance(shadowW.Position) <= E.Range //Double E
                || target.Distance(Player.Position)<= E.Range && target.Distance(shadowW.Position)<=E.Range
                || target.Distance(Player.Position) <= E.Range && target.Distance(shadowR.Position)<=E.Range)
                && minE < 3)
            {
                E.Cast(true);
            }
            else
            {
                if (target.Distance(shadowR.Position) <= E.Range && minE <2) //Single E
                    E.Cast(true);
                if (target.Distance(shadowW.Position) <= E.Range && minE <2)
                    E.Cast(true);
                if (target.Distance(Player.Position) <= E.Range && minE <2)
                    E.Cast(true);
            }
            #endregion


        }

        public static void Flee()
        {
            var isEnabled = ZedSharp.menu.Item("Flee").GetValue<KeyBind>().Active;
            if (!isEnabled) return;
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                if(W.IsReady())W.Cast(Game.CursorPos,true);
            var ListOfEnemies =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(he => he.IsValidTarget() && he.Distance(shadowW.Position) <= E.Range).ToList();
            if (ListOfEnemies.Count > 0 && E.IsReady()) E.Cast(true);
        }
        public static bool canDoCombo(SpellSlot[] sp)
        {
            var totalCost = sp.Sum(sp1 => Player.Spellbook.GetManaCost(sp1));
            return Player.Mana >= totalCost;
        }

        public static Vector3 getOptimalShadowPlacement(Vector3 TargetPos)
        {
            var LastShadowDist = float.MaxValue;
            var LastVec = Vector3.Zero;
            for (float r = W.Range; r >= 65; r -= 100)
            {
                var RadAngle = (int)2*Math.PI*r/100;
                for (int i = 0; i < RadAngle; i++)
                {
                    var RadAngle2 = i*2*Math.PI/RadAngle;
                    float cos = (float)Math.Cos(RadAngle2);
                    float sin = (float)Math.Cos(RadAngle2);
                    Vector3 PossiblePoint = new Vector3(Player.Position.X+r*cos,Player.Position.Y+r*sin,Player.Position.Z);
                    if (!Utility.IsWall(PossiblePoint) && TargetPos.Distance(PossiblePoint) < LastShadowDist)
                    {
                        LastShadowDist = TargetPos.Distance(PossiblePoint);
                        LastVec = PossiblePoint;
                    }
                }
            }
            return LastVec;
        }
        public static void doHarass() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (W.IsReady() && !canGoToShadow("W")) {
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