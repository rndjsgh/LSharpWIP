using System;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
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

        private static SpeechSynthesizer sSynth = new SpeechSynthesizer();
        private static readonly SpeechRecognitionEngine sRecognize = new SpeechRecognitionEngine();
        public static Choices speechList = new Choices();
        public static Grammar gr;
        public static Timer speechTimer;
        private static bool wantSpeech;
        private static int oldInterval;
        private static Spell recallSlot = new Spell(SpellSlot.Recall);

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
            menu.SubMenu("combo").AddItem(
                new MenuItem("shadowCoax", "Do Shadow Coax").SetValue(new KeyBind("T".ToCharArray()[0],
                    KeyBindType.Press)));
            //menu.SubMenu("combo").AddItem(new MenuItem("minQ", "Minimum Q to Hit").SetValue(new Slider(2, 1, 3)));
            //menu.SubMenu("combo").AddItem(new MenuItem("minE", "Minimum E to Hit").SetValue(new Slider(2, 1, 3)));

            menu.AddSubMenu(new Menu("Harass Options", "harass"));
            menu.SubMenu("harass").AddItem(
                new MenuItem("harassMode", "Harass Mode").SetValue(new StringList(new[] {"WEQ", "QE", "Q", "E"})));
            menu.SubMenu("harass").AddItem(new MenuItem("harassEnabled", "Enabled").SetValue(true));

            menu.AddSubMenu(new Menu("Laneclear", "laneclear"));
            menu.SubMenu("laneclear").AddItem(new MenuItem("useQLC", "Use Q to laneclear").SetValue(false));
            menu.SubMenu("laneclear").AddItem(new MenuItem("useELC", "Use E to laneclear").SetValue(false));

            menu.AddSubMenu(new Menu("Lasthit", "lasthit"));
            menu.SubMenu("lasthit").AddItem(new MenuItem("useQLH", "Use Q to lasthit").SetValue(false));
            menu.SubMenu("lasthit").AddItem(new MenuItem("useELH", "Use E to lasthit").SetValue(false));

            menu.AddSubMenu(new Menu("Use ultimate on", "ultOn"));
            HeroMenuCreate();

            menu.AddSubMenu(new Menu("Draw Options", "draw"));
            menu.SubMenu("draw").AddItem(new MenuItem("drawHp", "Draw predicted hp").SetValue(true));

            /*menu.AddSubMenu(new Menu("Speech Options", "spoken"));
            menu.SubMenu("spoken").AddItem(new MenuItem("speech", "Enabled").SetValue(true));
            menu.SubMenu("spoken").AddItem(new MenuItem("speechinterval", "delay").SetValue(new Slider(1000, 250, 5000))); 
            */
            menu.AddSubMenu(new Menu("Misc Options", "misc"));
            menu.SubMenu("misc").AddItem(new MenuItem("SwapHPToggle", "Swap R at % HP").SetValue(true)); //dont need %
            menu.SubMenu("misc").AddItem(new MenuItem("SwapHP", "%HP").SetValue(new Slider(5, 1))); //nop
            menu.SubMenu("misc").AddItem(new MenuItem("SwapRKill", "Swap R when target dead").SetValue(true));
            menu.SubMenu("misc").AddItem(new MenuItem("SafeRBack", "Safe swap calculation").SetValue(true));
            menu.SubMenu("misc").AddItem(
                new MenuItem("Flee", "Flee Key").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            menu.AddItem(new MenuItem("sep", "----------"));
            menu.AddItem(new MenuItem("creds", "Iridium, DZ191, DETUKS "));

            Game.PrintChat("Zed by Iridium, DZ191 and DETUKS Loaded.");
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

                // loadSpeech();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        /*private static void loadSpeech() {
            speechList.Add(new[] {"theline", "linecombo", "goham", "recall"});
            gr = new Grammar(new GrammarBuilder(speechList));
            speechTimer = new Timer(TimerCallBack, null, 0, menu.Item("speechinterval").GetValue<Slider>().Value);
        }

        private static void TimerCallBack(object state) {
            if (wantSpeech) {
                try {
                    sRecognize.RequestRecognizerUpdate();
                    sRecognize.LoadGrammar(gr);
                    sRecognize.SpeechRecognized += sRecognize_SpeechRecognized;
                    sRecognize.SetInputToDefaultAudioDevice();
                    sRecognize.RecognizeAsync(RecognizeMode.Multiple);
                    sRecognize.Recognize();
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }*/

        /*private static void sRecognize_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
            var target = SimpleTs.GetTarget(Zed.R.Range, SimpleTs.DamageType.Physical);
            switch (e.Result.Text) {
                case "theline":
                case "linecombo":
                case "goham":
                    Zed.doLineCombo(target);
                    break;
                case "recall":
                    recallSlot.Cast();
                    break;
            }
        }*/

        private static void OnGameProcessPacket(GamePacketEventArgs args) {
            if (args.PacketData[0] == 23) {
                var gp = new GamePacket(args.PacketData) {Position = 1};
                int id = gp.ReadInteger();
                if (id == Zed.Player.NetworkId &&
                    Encoding.UTF8.GetString(args.PacketData, 0, args.PacketData.Length).Contains("ZedW2")) {
                    Zed.serverTookWCast = true;
                    Zed.wIsCasted = false;
                    Console.WriteLine("W.iscasted");
                }
            }
        }

        private static void OnGameSendPacket(GamePacketEventArgs args) {}

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) {
            // if (!sender.IsMe) return;
            //if (args.SData.Name == "ZedShadowDash") Zed.getWshad = true;
            //Game.PrintChat(args.SData.Name);
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args) {
            if (Zed.shadowR != null && sender.NetworkId == Zed.shadowR.NetworkId) {
                Zed.shadowR = null;
                R2 = false;
                Zed.getRshad = false;
            }

            if (Zed.shadowW != null && sender.NetworkId == Zed.shadowW.NetworkId) {
                Zed.shadowW = null;
                W2 = false;
                Zed.getWshad = false;
            }
        }

        private static void OnCreateObject(GameObject sender, EventArgs args) {
            /* if (sender.Name.Equals("Zed_ShadowIndicatorNEARBloop.troy") && Zed.isSafeSwap(Zed.shadowR) &&
                menu.Item("SwapRKill").GetValue<bool>()) {
                if (Zed.canGoToShadow("R")) {
                    Zed.R.Cast(); TODO fixerino
                }
            }*/
            if (sender is Obj_AI_Minion) {
                var min = sender as Obj_AI_Minion;
                if (min.IsAlly && min.BaseSkinName == "ZedShadow") {
                    if (Zed.getRshad) {
                        // Game.PrintChat("R Create");
                        Zed.shadowR = min;
                        Zed.getRshad = false;
                    }
                    if (Zed.getWshad) {
                        //Game.PrintChat("W Created");
                        Zed.shadowW = min;
                        Zed.getWshad = false;
                    }
                }
            }

            var spell = (Obj_SpellMissile) sender;

            Obj_AI_Base unit = spell.SpellCaster;
            string name = spell.SData.Name;
            // Game.PrintChat(name);
            if (unit.IsMe) {
                switch (name) {
                    case "ZedUltMissile":
                        Zed.getRshad = true;
                        R2 = true;
                        break;
                    case "ZedShadowDashMissile":
                        // Game.PrintChat("Yay");
                        Zed.getWshad = true;
                        W2 = true;
                        break;
                }
            }


            //"Zed_Base_R_buf_tell.troy" = killable
        }

        private static void OnGameUpdate(EventArgs args) {
            Zed.checkForSwap("LowHP");

            Zed.Flee();
            Obj_AI_Hero target = SimpleTs.GetTarget(Zed.Q.Range + Zed.W.Range, SimpleTs.DamageType.Physical);
            Obj_AI_Hero target2 = SimpleTs.GetTarget(Zed.R.Range + Zed.Q.Range, SimpleTs.DamageType.Physical);

            if (menu.Item("shadowCoax").GetValue<KeyBind>().Active) {
                Zed.shadowCoax(target2);
            }
            //setInterval();
            switch (LXOrbwalker.CurrentMode) {
                case LXOrbwalker.Mode.Combo:
                    if (Zed.R.IsReady() && Zed.Player.Distance(target) < Zed.R.Range &&
                        menu.Item("useRC").GetValue<bool>() && menu.Item("use" + target.ChampionName).GetValue<bool>())
                        Zed.doLineCombo(target);
                    else
                        Zed.normalCombo(target);
                    break;
                case LXOrbwalker.Mode.Harass:
                    Zed.doHarass(target);
                    break;
                case LXOrbwalker.Mode.LaneClear:
                    Zed.doLaneClear();
                    break;
                case LXOrbwalker.Mode.Lasthit:
                    Zed.doLastHit();
                    break;
            }

            if (LXOrbwalker.CurrentMode != LXOrbwalker.Mode.Combo)
                Zed.serverTookWCast = false;
        }

        /*private static void setInterval() {
            wantSpeech = menu.Item("speech").GetValue<bool>();
            if (wantSpeech) {
                int speechInterval = menu.Item("speechinterval").GetValue<Slider>().Value;
                if (oldInterval == speechInterval) {
                    return;
                }
                if (oldInterval != speechInterval) {
                    speechTimer.Change(0, speechInterval);
                    //Message("New Delay: " + speechInterval / 1000 + " Seconds.");
                    oldInterval = speechInterval;
                }
            }
        }*/


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
            // Obj_AI_Hero pl = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsEnemy);
            // Vector3 shadowPos = pl.Position + Vector3.Normalize(pl.Position - ObjectManager.Player.Position)*Zed.W.Range;
            // Utility.DrawCircle(shadowPos, 100, Color.Yellow);
            foreach (
                Obj_AI_Hero Hero in
                    ObjectManager.Get<Obj_AI_Hero>().Where(
                        h => h.IsEnemy && h.Distance(Zed.shadowW.Position) <= Zed.R.Range && h.IsValidTarget())) {
                if (Zed.isKillableShadowCoax(Hero) && Zed.R.IsReady()) {
                    Vector2 pScreen = Drawing.WorldToScreen(Hero.Position);
                    pScreen[0] -= 20;
                    Drawing.DrawText(pScreen.X - 60, pScreen.Y, Color.Red, "Killable by Shadow Coax");
                    //Utility.DrawCircle(Hero.Position,100f,Color.Blue);
                }
            }
            if (Zed.shadowW != null && !Zed.shadowW.IsDead)
                Utility.DrawCircle(Zed.shadowW.Position, 100, Color.Red);
        }
    }
}