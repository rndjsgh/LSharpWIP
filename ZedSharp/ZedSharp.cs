using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace ZedSharp {
    //TODO idea, use EvadeSpellDatabase or .dll to have an option to use ultimate to dodge dangeruous spells like Grag ult when evade can't dodge, so it doesn't waste ur R ? 
    //TODO - reply here.
    //TODO - when hes played more we will finish this tbh, i doubt he can carry solo q anyway too team orientated..

    /* 
     * In combo it should Cast R then Items (Bork/Hydra/etc) after that everything is variable. 
     * If the enemy dashes/blinks away use W-E-Double Q. If not Zed should try to save his W shadow 
     * in case the enemy is saving his Escape for your double Q. If the enemy doesnt try to get away 
     * at all Zed should just either save his W or throw it in last second to get the double Q for his Death Mark Proc.
     * Also dodging important spells with Death Mark and Shadow Swaps should be an option confirguable spell by spell 
     * and integrated into Evade. With Shadow Swaps it should check if a specific number of enemys is around before switching 
     * and also check how far away/how close the shadow is from your target (assuming you are holding combo key down) and a check 
     * if the spell would kill you if you dont dodge it etc etc I could continue talking about such features for, well, forever.
     * At comboing put shadow w at best position to escape over wall or stuff
     */

    internal class ZedSharp {
        public const string CharName = "Zed";

        public static Menu menu;

        public static HpBarIndicator hpi = new HpBarIndicator();
        public static bool W2;
        public static bool R2;

        public ZedSharp() {
            Console.WriteLine("Zed sharp starting...");
            try {
                // if (ObjectManager.Player.BaseSkinName != CharName)
                //    return;
                /* CallBAcks */
                CustomEvents.Game.OnGameLoad += onLoad;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private static void HeroMenuCreate() {
            foreach (Obj_AI_Hero Enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy)) {
                menu.SubMenu("ultOn").AddItem(new MenuItem("use" + Enemy.ChampionName, Enemy.ChampionName).SetValue(true));
            }
        }

        private static void loadMenu() {
            menu = new Menu("Zed Sharp", "zedSharp", true);

            var targetSelector = new Menu("Target Selector", "Target Selector"); //TODO new target selector ofc.
            SimpleTs.AddToMenu(targetSelector);
            menu.AddSubMenu(targetSelector);

            var orbwalkerMenu = new Menu("LX Orbwalker", "my_Orbwalker");
            LXOrbwalker.AddToMenu(orbwalkerMenu);
            menu.AddSubMenu(orbwalkerMenu);

            menu.AddSubMenu(new Menu("Combo Options", "combo"));
            menu.SubMenu("combo").AddItem(new MenuItem("useQC", "Use Q in combo").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("useWC", "Use W in combo").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("useEC", "Use E in combo").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("useRC", "Use R in combo").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("useWF", "Use W to follow").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("minQ", "Minimum Q to Hit").SetValue(new Slider(2, 1, 3)));
            menu.SubMenu("combo").AddItem(new MenuItem("minE", "Minimum E to Hit").SetValue(new Slider(2, 1, 3)));

            menu.AddSubMenu(new Menu("Harass Options", "harass"));
            menu.SubMenu("harass").AddItem(new MenuItem("useQH", "Use Q in harass").SetValue(true));
            menu.SubMenu("harass").AddItem(new MenuItem("useWH", "Use W in harass").SetValue(false));
            menu.SubMenu("harass").AddItem(new MenuItem("useEH", "Use E in harass").SetValue(false));

            menu.AddSubMenu(new Menu("Use ultimate on", "ultOn"));
            HeroMenuCreate();

            menu.AddSubMenu(new Menu("Draw Options", "draw"));
            menu.SubMenu("draw").AddItem(new MenuItem("drawHp", "Draw predicted hp").SetValue(true));

            menu.AddSubMenu(new Menu("Misc Options", "misc"));
            menu.SubMenu("misc").AddItem(new MenuItem("SwapHPToggle", "Swap R at % HP").SetValue(true)); //dont need %
            menu.SubMenu("misc").AddItem(new MenuItem("SwapHP", "%HP").SetValue(new Slider(5, 1))); //nop
            menu.SubMenu("misc").AddItem(new MenuItem("SwapRKill", "Swap R when target dead").SetValue(true));
            menu.SubMenu("misc").AddItem(new MenuItem("SafeRBack", "Safe swap calculation").SetValue(true));
            menu.SubMenu("misc").AddItem(
                new MenuItem("Flee", "Flee Key").SetValue(new KeyBind("S".ToCharArray()[0], KeyBindType.Press)));

            Game.PrintChat("Zed by iJava,DZ191 and DETUKS Loaded.");
        }

        private static void onLoad(EventArgs args) {
            try {
                loadMenu();
                menu.AddToMainMenu();

                Drawing.OnDraw += onDraw;
                Drawing.OnEndScene += OnEndScene;
                Game.OnGameUpdate += OnGameUpdate;

                GameObject.OnCreate += OnCreateObject;
                GameObject.OnDelete += OnDeleteObject;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

                Game.OnGameSendPacket += OnGameSendPacket;
                Game.OnGameProcessPacket += OnGameProcessPacket;

                Zed.setSkillshots();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private static void OnGameProcessPacket(GamePacketEventArgs args) {}

        private static void OnGameSendPacket(GamePacketEventArgs args) {}

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) {}

        private static void OnDeleteObject(GameObject sender, EventArgs args) {
            if (Zed.shadowR != null && sender.NetworkId == Zed.shadowR.NetworkId) {
                Zed.shadowR = null;
                R2 = false;
            }

            if (Zed.shadowW != null && sender.NetworkId == Zed.shadowW.NetworkId) {
                Zed.shadowW = null;
                W2 = false;
            }
        }

        private static void OnCreateObject(GameObject sender, EventArgs args) {
            if (sender is Obj_AI_Minion) {
                var min = sender as Obj_AI_Minion;
                if (min.IsAlly && min.BaseSkinName == "ZedShadow") {
                    if (Zed.getRshad) {
                        Zed.shadowR = min;
                        R2 = true;
                    }
                    else {
                        Zed.shadowW = min;
                        W2 = true;
                    }
                }
            }

            if (sender.Name == "Zed_Base_R_buf_tell.troy" && sender.IsEnemy) {
                //TODO check if this works it means the enemy is killable with ult and you can then leave him and return to ult shadow if it is still active.
                //TODO targetKillable = true - alos check hp and if below % go back to RShadow?
                Zed.checkForSwap("OnKill");
            }

            var spell = (Obj_SpellMissile) sender;

            Obj_AI_Base unit = spell.SpellCaster;
            string name = spell.SData.Name;
            if (unit.IsMe) {
                switch (name) {
                    case "ZedUltMissile":
                        Zed.getRshad = true;
                        R2 = true;
                        break;
                }
            }
            //"Zed_Base_R_buf_tell.troy" = killable
        }

        private static void OnGameUpdate(EventArgs args) {
            //  Game.PrintChat(Zed.canGoToShadow("W").ToString());
            Zed.checkForSwap("LowHP");
            Zed.Flee();
            Obj_AI_Hero target = SimpleTs.GetTarget(Zed.R.Range, SimpleTs.DamageType.Physical);
            switch (LXOrbwalker.CurrentMode) {
                case LXOrbwalker.Mode.Combo:
                    if (Zed.R.IsReady())
                        Zed.doLaneCombo(target);
                    else
                        Zed.normalCombo();
                    break;
                case LXOrbwalker.Mode.Harass:
                    Zed.doHarass();
                    break;
            }
        }


        private static void OnEndScene(EventArgs args) {
            if (menu.Item("drawHp").GetValue<bool>()) {
                foreach (
                    Obj_AI_Hero enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible)) {
                    hpi.unit = enemy;
                    hpi.drawDmg(Zed.getFullComboDmg(enemy));
                }
            }
        }

        private static void onDraw(EventArgs args) {
            Obj_AI_Hero pl = ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy).FirstOrDefault();
            Vector3 shadowPos = pl.Position + Vector3.Normalize(pl.Position - ObjectManager.Player.Position)*Zed.W.Range;
            Utility.DrawCircle(shadowPos, 100, Color.Yellow);
            if (Zed.shadowW != null && !Zed.shadowW.IsDead)
                Utility.DrawCircle(Zed.shadowW.Position, 100, Color.Red);
        }
    }
}