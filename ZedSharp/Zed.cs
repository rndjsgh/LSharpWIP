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

        public static void setSkillshots() {
            Q.SetSkillshot(Qdata.SData.SpellCastTime, Qdata.SData.LineWidth, Qdata.SData.MissileSpeed, false,
                SkillshotType.SkillshotLine);
            //Thats all we need to set
        }

        public static float getComboDmg(Obj_AI_Hero target) {
            float dmg = 0;
            float dist = Player.Distance(target);
            float gapDist = ((W.IsReady()) ? W.Range : 0) + ((R.IsReady()) ? R.Range : 0);

            return dmg;
        }

        public static bool canGoToShadow() {
            // TODO Shadow param W or R
            if (Wdata.Name == "zedw2")
                return true;
            if (Rdata.Name == "ZedR2")
                return true;
            /**
             * switch (RW) {
                case RWEnum.W:
                    if (player.Spellbook.GetSpell(SpellSlot.W).Name == "zedw2") TODO like so
                        return true;
                    break;
                case RWEnum.R:
                    if (player.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR2")
                        return true;
                    break;
            }*/
            return false;
        }

        public static void doCombo() {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (R.IsReady())
                R.Cast(target, true);

            if (W.IsReady() && target.Distance(ObjectManager.Player) < W.Range) {
                Vector3 positionBehind = target.Position +
                                         Vector3.Normalize(target.Position - ObjectManager.Player.Position)*200;
                W.Cast(positionBehind, true);
            }

            if (Q.IsReady() && target.Distance(ObjectManager.Player) < Q.Range) {
                if (Q.GetPrediction(target, true).Hitchance >= HitChance.Medium)
                    Q.Cast(target, true, true); // do packets shit
            }

            if (E.IsReady() && target.Distance(ObjectManager.Player) <= E.Range) {
                E.CastOnUnit(ObjectManager.Player, true);
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

            if (W.IsReady() && target.Distance(ObjectManager.Player) < W.Range) {
                Vector3 positionBehind = target.Position +
                                         Vector3.Normalize(target.Position - ObjectManager.Player.Position)*200;
                W.Cast(positionBehind, true);
            }

            if (Q.IsReady() && target.Distance(ObjectManager.Player) < Q.Range) {
                Q.Cast(target, true, true);
            }

            if (E.IsReady() && target.Distance(ObjectManager.Player) <= E.Range) {
                E.CastOnUnit(ObjectManager.Player, true);
            }
        }
    }
}