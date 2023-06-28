using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MonoMod.Utils;
using FMOD.Studio;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using static Monocle.MInput;

namespace Celeste.Mod.BingoUI
{
    public class GrablessDisplay : Entity
    {
        private MTexture image;
        public GrablessDisplay() : base()
        {
            image = GFX.Gui["cs_variantmode"];
            Tag = (Tags.HUD | Tags.Global | Tags.PauseUpdate);
            Depth = -99;
            X = 30;
            Y = 30;//1050;
        }

        public override void Update()
        {
            base.Update();
            if(SaveData.Instance != null)
                Visible = SaveData.Instance.Assists.NoGrabbing && BingoModule.mySettings.Enabled;
        }

        public override void Render()
        {
            base.Render();
            if (Visible)
            {
                image.DrawCentered(Position, new Color(255, 255, 255, 70), .7f);
            }
        }
    }

    public class BingoModule : EverestModule
    {
        public static BingoModule Instance;
        public BingoModule() {
            Instance = this;
            
        }
        public override Type SettingsType => typeof(BingoSettings);
        //public override Type SaveDataType => null;
        public static BingoSettings mySettings => (BingoSettings)Instance._Settings;
        private List<TotalCollectableDisplay> DisplayEntities;
        private Level currentLevel = null;
        private List<Binoculars> BinocularsList = new List<Binoculars>();
        private List<String> WingedBerryIDList = new List<string> { "9c:2", "3b:2", "end_3c:13", "06-a:7", "13-b:31", "c-01:26", "b-21:99", "b-04:67", "d-10b:682", "e-09:398", "end:4" };
        private List<String> SeedBerryIDList = new List<string> { "d1:67", "a-10:13", "b-17:10", "e-12:504" };
        private int SeekersHit = 0;
        private int OshiroHits = 0;
        private int SnowballHits = 0;
        private int SkipUsed = -1;
        private static float ChapterEndDelay = 1.5f;
        //private SkipTally skipTally;
        private ILHook customTally;
        private Hook GuiButton;


        private bool testBinos = true;

        public override void Load()
        {
            On.Celeste.Level.LoadLevel += LoadLevel;
            On.Celeste.Level.UnloadLevel += UnloadLevel;
            On.Celeste.Lookout.Interact += Interact;
            On.Celeste.Audio.Play_string_Vector2 += BinoHud;
            On.Celeste.Seeker.RegenerateBegin += RegenerateBegin;
            On.Celeste.AngryOshiro.HurtBegin += HurtBegin;
            On.Celeste.Snowball.OnPlayerBounce += OnPlayerBounce2;
            Everest.Events.Journal.OnEnter += BinocularJournal;
            On.Celeste.CutsceneEntity.Start += DisableProloguePause;
            On.Celeste.CutsceneEntity.EndCutscene += EnableProloguePause;
            On.Celeste.IntroVignette.OpenMenu += PrologueMenu;
            On.Celeste.SaveData.RegisterCompletion += CustomLevelUnlock;
            //On.Celeste.OuiChapterSelect.Added += CustomChapterSelect;
            On.Celeste.OuiChapterSelect.Update += CustomAssistEnable;
            //On.Celeste.OuiChapterSelect.AutoAdvanceRoutine += CustomCh9Unlock;
            //On.Celeste.OuiChapterSelect.Enter += CustomAssistEnter;
            On.Celeste.OuiChapterSelectIcon.AssistModeUnlock += CustomAssist;
            On.Celeste.GameplayStats.Render += ShowBerries;
            //On.Celeste.Strawberry.CollectRoutine += CollectRoutine;
            On.Celeste.Pico8.Classic.room_title.draw += Pico8Timer;
            On.Celeste.LevelExit.ctor += SkipChapterComplete;
            //On.Celeste.OuiChapterSelectIcon.AssistModeUnlock += OuiChapterSelectIcon_AssistModeUnlock;
            On.Celeste.Audio.Play_string += Audio_Play_string;
            On.Celeste.LevelExit.Routine += SHOWBINGO;
            On.Celeste.LevelLoader.LoadingThread += customLoadingThread;
            //On.Celeste.OuiChapterPanel.ctor += CustomChapterPanelCtor;
            //On.Celeste.OuiChapterPanel.IncrementStats += SkipTally;
            //On.Monocle.MInput.KeyboardData.Pressed_Keys += SkipTally2;
            //IL.Celeste.OuiChapterPanel.IncrementStats += SkipTally3;
            customTally = new ILHook(typeof(OuiChapterPanel).GetMethod("IncrementStats", BindingFlags.Instance | BindingFlags.Public).GetStateMachineTarget(), SkipTally3);
            IL.Celeste.OuiChapterPanel.DrawCheckpoint += CustomDrawCheckpoint;
            //GuiButton = new Hook(typeof(Input).GetMethod("GuiButton", BindingFlags.Static | BindingFlags.Public), HideButtons);
            On.Celeste.Level.VariantMode += CustomVariantMode;
            On.Celeste.BirdTutorialGui.Render += CustomTutorialRender;
            On.Celeste.Input.GuiButton_VirtualButton_PrefixMode_string += HideButtons;
            On.Celeste.Input.GuiSingleButton_Buttons_PrefixMode_string += HideSingleButton;
            On.Celeste.Input.GuiTexture += HideButtonTexture;
            //skipTally = new SkipTally();
        }

        


        private void customLoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self)
        {
            orig(self);
            self.Level.Add(new GrablessDisplay());
        }

        public override void Unload()
        {


            //On.Celeste.OuiChapterSelectIcon.AssistModeUnlock -= OuiChapterSelectIcon_AssistModeUnlock;
        }

        public class BingoSettings : EverestModuleSettings
        {
            private bool enabled = true;
            public bool Enabled { get { return enabled; } set { if (value && Instance.currentLevel != null) Instance.LoadGUI(); else Instance.UnloadGUI(); enabled = value; } }

            public bool CustomProgression { get; set; } = true;

            [SettingName("BINGO_UI_PREVENT_PROLOGUE_CUTSCENE_SKIPS")]
            public bool PreventPrologueCutsceneSkips { get; set; } = true;

            public bool SkipChapterComplete { get; set; } = true;

            public bool SkipTallyWithConfirm { get; set; }

            public bool AutoEnableVariants { get; set; } = true;

            public bool ShowChapterBerryCount { get; set; } = true;

            public bool HideVariantsExceptGrabless { get; set; }

            public bool HideControls { get; set; }

            

        }

        

        

        public void LoadGUI()
        {
            if (currentLevel == null)
                return;
            if (DisplayEntities == null)
            {
                CreateDisplayEntities();
            }
            else
            {
                ResetSavedCounts();
            }
            foreach (TotalCollectableDisplay myEntity in DisplayEntities)
            {
                currentLevel.Add(myEntity);
            }
        }

        public void UnloadGUI()
        {
            if (currentLevel == null || DisplayEntities == null)
                return;
            foreach (TotalCollectableDisplay myEntity in DisplayEntities)
            {
                currentLevel.Remove(myEntity);
            }
        }
        
        internal object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            if (instance == null)
                return null;
            return field.GetValue(instance);
        }

        internal object GetInstanceProp(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
            PropertyInfo prop = type.GetProperty(fieldName, bindFlags);
            MethodInfo getter = prop.GetGetMethod(nonPublic: true);
            if (getter == null || prop == null || instance == null)
                return null;

            return getter.Invoke(instance,null);
        }

        internal MethodInfo GetInstanceMethod(Type type, string methodName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
            MethodInfo method = type.GetMethod(methodName, bindFlags);
            return method;
        }

            private void CreateDisplayEntities()
        {
            List<TotalCollectableDisplay> a = new List<TotalCollectableDisplay>();
            a.Add(new TotalCollectableDisplay(96f + 1 * 78f, CheckWingedBerries, true, 0, GFX.Game["wings02"]));
            a.Add(new TotalCollectableDisplay(96f + 2 * 78f, CheckSeedBerries, true, 0, GFX.Game["seed00"]));
            a.Add(new TotalCollectableDisplay(136f + 3 * 78f, CheckCassettes, true, 1, GFX.Gui["collectables/cassette"]));
            a.Add(new TotalCollectableDisplay(136f + 4 * 78f, CheckBlueHearts, true, 1, GFX.Gui["collectables/heartgem/0/spin00"]));
            a.Add(new TotalCollectableDisplay(136f + 5 * 78f, CheckRedHearts, true, 1, GFX.Gui["collectables/heartgem/1/spin00"]));
            a.Add(new TotalCollectableDisplay(176f + 6 * 78f, CheckBinoculars, false, 2, GFX.Game["lookout05"]));
            a.Add(new TotalCollectableDisplay(176f + 7 * 78f, CheckSeekersHit, false, 0, GFX.Game["predator61"]));
            a.Add(new TotalCollectableDisplay(176f + 8 * 78f, CheckOshiroHits, false, 0, GFX.Game["boss35"]));
            a.Add(new TotalCollectableDisplay(176f + 9 * 78f, CheckSnowballHits, false, 0, GFX.Game["snowball00"]));
            DisplayEntities = a;
        }


        private void ResetSavedCounts()
        {
            foreach (TotalCollectableDisplay a in DisplayEntities)
                if (a.TrackedInGame)
                    a.collectables.Amount = a.check();
        }

        private void ResetUnsavedCounts()
        {
            BinocularsList = new List<Binoculars>();
            SeekersHit = 0;
            OshiroHits = 0;
            SnowballHits = 0;

            foreach (TotalCollectableDisplay a in DisplayEntities)
                if (!a.TrackedInGame)
                    a.collectables.Amount = 0;
        }

        public void CustomLevelUnlock(On.Celeste.SaveData.orig_RegisterCompletion register, SaveData saveData, Session session)
        {
            bool clearedCore = false;
            int oldProgress = 0;
            if(session.Area.ID == 9)
            {
                clearedCore = true;
                oldProgress = saveData.UnlockedAreas_Safe;
            }
            register(saveData, session);
            if (session.Area.ID == 5 && mySettings.CustomProgression && mySettings.Enabled)
                saveData.UnlockedAreas_Safe = 7;
            if (mySettings.CustomProgression && mySettings.Enabled && clearedCore && oldProgress != 9)
                saveData.UnlockedAreas_Safe = oldProgress;
        }

        public void CustomTutorialRender(On.Celeste.BirdTutorialGui.orig_Render orig, BirdTutorialGui self)
        {
            if(mySettings.Enabled && mySettings.HideControls && currentLevel.Session.Area.ID <= 10)
            {
                typeof(BirdTutorialGui).GetField("controls", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, new List<object>());
                typeof(BirdTutorialGui).GetField("info", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, "BINGO");
                typeof(BirdTutorialGui).GetField("infoHeight", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, 0f);
            }
            orig(self);
        }

        private void CustomVariantMode(On.Celeste.Level.orig_VariantMode orig, Level self, int returnIndex, bool minimal)
        {
            if (mySettings.Enabled && mySettings.HideVariantsExceptGrabless)
            {
                self.Paused = true;
                TextMenu menu = new TextMenu();
                menu.Add(new TextMenu.Header(Dialog.Clean("MENU_VARIANT_TITLE", null)));
                menu.Add(new TextMenu.SubHeader(Dialog.Clean("MENU_VARIANT_SUBTITLE", null)));
                menu.Add(new TextMenu.OnOff(Dialog.Clean("MENU_VARIANT_NOGRABBING", null), SaveData.Instance.Assists.NoGrabbing).Change(delegate (bool on)
                {
                    SaveData.Instance.Assists.NoGrabbing = on;
                }));
                menu.OnESC = (menu.OnCancel = delegate ()
                {
                    Audio.Play("event:/ui/main/button_back");
                    self.Pause(returnIndex, minimal, false);
                    menu.Close();
                });
                menu.OnPause = delegate ()
                {
                    Audio.Play("event:/ui/main/button_back");
                    self.Paused = false;
                    Engine.FreezeTimer = .15f;
                    //typeof(Level).GetField("unpauseTimer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, .15f);
                    menu.Close();
                };
                self.Add(menu);
                return;
            }
            orig(self, returnIndex, minimal);
        }

        public MTexture HideButtons(On.Celeste.Input.orig_GuiButton_VirtualButton_PrefixMode_string orig, VirtualButton button, Input.PrefixMode mode, string fallback)
        {
            if (mySettings.Enabled && mySettings.HideControls)
                return GFX.Gui["controls/keyboard/oemquestion"];
            return orig(button, mode, fallback);
        }

        public MTexture HideSingleButton(On.Celeste.Input.orig_GuiSingleButton_Buttons_PrefixMode_string orig, Microsoft.Xna.Framework.Input.Buttons button, Input.PrefixMode mode, string fallback)
        {
            if (mySettings.Enabled && mySettings.HideControls)
                return GFX.Gui["controls/keyboard/oemquestion"];
            return orig(button, mode, fallback);
        }

        public MTexture HideButtonTexture(On.Celeste.Input.orig_GuiTexture orig, string prefix, string input)
        {
            if (mySettings.Enabled && mySettings.HideControls)
                return GFX.Gui["controls/keyboard/oemquestion"];
            return orig(prefix, input);
        }


        public void CustomAssistEnable(On.Celeste.OuiChapterSelect.orig_Update update, OuiChapterSelect select)
        {
            
            bool a = (mySettings.Enabled && mySettings.CustomProgression && SaveData.Instance != null && select != null);
            int b = 0;
            
            int i = 1;
            List<OuiChapterSelectIcon> c = new List<OuiChapterSelectIcon>() ;
            bool menuLeft = false;
            bool menuRight = false;
            if (a)
            {
                //bool zzz = SaveData.Instance.Areas_Safe[9].Modes[1].HeartGem;
                
                b = (int)GetInstanceProp(typeof(OuiChapterSelect), select, "area");
                if (b > 10)
                {
                    update(select);
                    return;
                }
                c = GetInstanceField(typeof(OuiChapterSelect), select, "icons") as List<OuiChapterSelectIcon>;
                
                
                while ( i < 9 &&(SaveData.Instance.Areas_Safe[i].Modes[0].Completed || SaveData.Instance.Areas_Safe[i].Modes[1].HeartGem ||
                    i + 1 == SkipUsed || i == 6))
                    i++;
                if (i == 9 && Ch9Unlocked())
                    i = 10;

                if (SaveData.Instance.UnlockedAreas_Safe != i)
                    SaveData.Instance.UnlockedAreas_Safe = i;
            }
            if (a && !SaveData.Instance.AssistMode && SkipUsed == -1)
                SaveData.Instance.AssistMode = true;
            if (a && SkipUsed >= 0)
            {
                
                if (i == 7 && c[7].AssistModeUnlockable && (c[7].IsHidden || c[7].Position == c[7].HiddenPosition))
                {
                    c[7].Show();
                    c[7].AssistModeUnlockable = false;
                    c[7].Position = c[7].IdlePosition;
                }
                for (int j = 2; j < 9; j++)
                    if (c[j].AssistModeUnlockable && j != SkipUsed)
                        c[j].Hide();
            }
            if(a && i < 9)
            {
                if (c[9].IsHidden && !c[0].IsHidden)
                    c[9].Show();
                else if (!c[9].IsHidden && c[0].IsHidden)
                    c[9].Hide();
                if (c[9].AssistModeUnlockable)
                    c[9].AssistModeUnlockable = false;
                if (c[9].Position == c[9].HiddenPosition && !c[9].IsHidden)
                    c[9].Position = c[9].IdlePosition;

                if (Ch9Unlocked())
                {
                    if (c[10].IsHidden && !c[0].IsHidden)
                        c[10].Show();
                    else if (!c[10].IsHidden && c[0].IsHidden)
                        c[10].Hide();
                    if (c[10].AssistModeUnlockable)
                        c[10].AssistModeUnlockable = false;
                    if (c[10].Position == c[10].HiddenPosition && !c[10].IsHidden)
                        c[10].Position = c[10].IdlePosition;
                    if (c[10].HideIcon)
                        c[10].HideIcon = false;
                }
            }
            
            if(a)
            {
                bool disable = (bool)GetInstanceField(typeof(OuiChapterSelect), select, "disableInput");
                bool display = (bool)GetInstanceField(typeof(OuiChapterSelect), select, "display");
                float delay = (float)GetInstanceField(typeof(OuiChapterSelect), select, "inputDelay");

                if (select.Focused && display && !disable && delay <= Engine.DeltaTime)
                {
                    if (Input.MenuLeft.Pressed && b == 9 && c[8].IsHidden)
                    {
                        menuLeft = true;
                    }
                    else if (Input.MenuRight.Pressed && ((b <= 7 && c[b + 1].IsHidden)|| b == 8 || (b == 9 && !c[10].IsHidden)))
                        menuRight = true;
                }
            }
            
            if(a && b >= 9 && c[8].IsHidden && !Input.MenuUp.Pressed && !Input.MenuDown.Pressed)
            {
                select.orig_Update();
            }
            else
            {
                update(select);
            }
            


            if (menuLeft || menuRight)
            {
                int newArea = 0;
                if (menuLeft)
                {
                    Audio.Play("event:/ui/world_map/icon/roll_left");
                    newArea = b - 1;
                    while (c[newArea].IsHidden && newArea > 0)
                        newArea -= 1;
                    c[newArea].Hovered(-1);
                }
                else if (menuRight)
                {
                    if(b == 9)
                    {
                        if (c[10].IsHidden)
                            return;
                        newArea = 10;
                    }
                    else if ((b <= 7 && c[b+1].IsHidden)||b == 8)
                    {
                        newArea = 9;
                    }
                    Audio.Play("event:/ui/world_map/icon/roll_right");
                    c[newArea].Hovered(1);
                }

                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
                FieldInfo delayField = typeof(OuiChapterSelect).GetField("inputDelay", bindFlags);
                delayField.SetValue(select, .15f);
                PropertyInfo setNewArea = typeof(OuiChapterSelect).GetProperty("area", bindFlags);
                setNewArea.SetValue(select, newArea);
                MethodInfo ease = typeof(OuiChapterSelect).GetMethod("EaseCamera", bindFlags);
                ease.Invoke(select, new object[] {});
                
                select.Overworld.Maddy.Hide(true);
            }
            
                
        }

        /*public IEnumerator CustomCh9Unlock(On.Celeste.OuiChapterSelect.orig_AutoAdvanceRoutine setup, OuiChapterSelect select)
        {
            int b = (int)GetInstanceProp(typeof(OuiChapterSelect), select, "area");
            if (Settings.Enabled && Settings.CustomProgression && b >= 9)
            {
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
                FieldInfo adv = typeof(OuiChapterSelect).GetField("autoAdvancing", bindFlags);
                adv.SetValue(select, false);
                FieldInfo dis = typeof(OuiChapterSelect).GetField("disableInput", bindFlags);
                dis.SetValue(select, false);
                FieldInfo foc = typeof(OuiChapterSelect).GetField("Focused", bindFlags);
                foc.SetValue(select, true);
                //this.Overworld.ShowInputUI = true;
                yield break;
            }
            else
            {
                yield return setup(select);
            }
        }*/

        /*public IEnumerator CustomAssistEnter(On.Celeste.OuiChapterSelect.orig_Enter enter, OuiChapterSelect select, Oui from)
        {
            if(Settings.Enabled && Settings.CustomProgression)
            {
                SaveData.Instance.VariantMode = true;
                SaveData.Instance.AssistMode = false;
            }
            return enter(select, from);
        }*/

        public void CustomAssist(On.Celeste.OuiChapterSelectIcon.orig_AssistModeUnlock unlock, OuiChapterSelectIcon icon, Action onComplete)
        {
            if(!mySettings.Enabled || !mySettings.CustomProgression)
            {
                unlock(icon, onComplete);
                return;
            }

            Overworld oui = Engine.Scene as Overworld;
            OuiChapterSelect cselect = oui?.GetUI<OuiChapterSelect>();
            if (cselect == null)
            {
                unlock(icon, onComplete);
                return;
            }

            DynData<OuiChapterSelectIcon> dd = new DynData<OuiChapterSelectIcon>(icon);

            if (dd.Get<bool?>("attemptingSkip") ?? false)
            {
                dd.Set<bool?>("attemptingSkip", false);
                Audio.Play("cas_event:/ui/world_map/icon/assist_skip");
                SaveData.Instance.AssistMode = false;
                SkipUsed = icon.Area;
                unlock(icon, onComplete);
                return;
            }

            dd.Set<bool?>("attemptingSkip", true);
            cselect.Focused = true;
            oui.ShowInputUI = true;

            /*if (Settings.Enabled && Settings.CustomProgression)
            {
                SaveData.Instance.AssistMode = false;
                SkipUsed = icon.Area;
            }
            unlock(icon, onComplete);*/


        }

        private bool Ch9Unlocked()
        {
            return SaveData.Instance == null ? false : SaveData.Instance.Areas_Safe[4].Modes[0].Completed || SaveData.Instance.Areas_Safe[4].Modes[1].HeartGem || 5 == SkipUsed;
            //return true;
            //return SaveData.Instance.Areas_Safe[9].Modes[0].HeartGem;
        }

        private void Pico8Timer(On.Celeste.Pico8.Classic.room_title.orig_draw draw, Pico8.Classic.room_title title)
        {
            draw(title);
            if (title == null || !mySettings.Enabled)
                return;

            /*DynData<Pico8.Classic.room_title> dd = new DynData<Pico8.Classic.room_title>(title);

            if (dd.Get<bool?>("delayStarted") ?? false)
            {*/
            float drawDelay = (float)GetInstanceField(typeof(Pico8.Classic.room_title), title, "delay");
            if (drawDelay < 0f)
            {
                HashSet<int> berries = GetInstanceField(typeof(Pico8.Classic), title.G, "got_fruit") as HashSet<int>;
                title.E.rectfill(4, 10, berries.Count > 9 ? 25 : 21, 19, 0);
                title.E.spr(26, 5, 11, 1, 1, false, false);
                title.E.print("x" + ((float)berries.Count), 14, 14, 7);
            }
        }

        /*private bool SkipTally2(On.Monocle.MInput.KeyboardData.orig_Pressed_Keys orig,  MInput.KeyboardData data, Microsoft.Xna.Framework.Input.Keys key)
        {
            if(mySettings.Enabled && mySettings.SkipTallyWithConfirm && !MInput.Disabled  && key.Equals(Microsoft.Xna.Framework.Input.Keys.Enter))
            {
                foreach (Microsoft.Xna.Framework.Input.Keys key2 in Settings.Instance.Confirm)
                {
                    if (data.CurrentState.IsKeyDown(key2) && !data.PreviousState.IsKeyDown(key2))
                        return true;
                }
                if (MInput.GamePads[0].Pressed(Microsoft.Xna.Framework.Input.Buttons.A))
                    return true;
                //return true;
            }
            return orig(data, key);
        }*/

        private void SkipTally3(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            //Logger.Log("b", $"test {cursor.Index}");
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<KeyboardData>("Pressed")))
            {
                cursor.EmitDelegate<Func<bool, bool>>(checkConfirm);
            }
        }

        private bool checkConfirm(bool a)
        {
            if (mySettings.Enabled && mySettings.SkipTallyWithConfirm)
                return a || Input.MenuConfirm.Pressed;
            return a;
        }

        private void CustomDrawCheckpoint(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<SaveData>("DebugMode")))
                cursor.EmitDelegate<Func<bool, bool>>(checkDrawCheckpoint);
        }

        

        private bool checkDrawCheckpoint(bool a)
        {
            return (mySettings.Enabled && mySettings.ShowChapterBerryCount) ? true : a;
        }

        /*private IEnumerator SkipTally(On.Celeste.OuiChapterPanel.orig_IncrementStats orig, OuiChapterPanel panel, bool shouldAdvance)
        {
            if (!Settings.SkipTallyWithConfirm)
            {
                yield return orig(panel, shouldAdvance);
            }
            else
            {
                if (Input.MenuConfirm.Pressed)
                {

                    GetInstanceMethod(typeof(Microsoft.Xna.Framework.Input.KeyboardState), "InternalSetKey").Invoke(MInput.Keyboard.CurrentState, new Object[] {
                        Microsoft.Xna.Framework.Input.Keys.Enter
                    });
                    //GetInstanceMethod(typeof(MInput.KeyboardData), "Update").Invoke(MInput.Keyboard)
                    //yield return orig(panel, shouldAdvance);
                }
            }


                panel.Focused = false;
                panel.Overworld.ShowInputUI = false;
                bool interlude_Safe = panel.Data.Interlude_Safe;
                if (interlude_Safe)
                {
                    bool flag = shouldAdvance && panel.OverworldStartMode == Overworld.StartMode.AreaComplete;
                    if (flag)
                    {
                        yield return 1.2f;
                        panel.Overworld.Goto<OuiChapterSelect>().AdvanceToNext();
                    }
                    else
                    {
                        panel.Focused = true;
                    }
                    yield return null;
                    yield break;
                }
                AreaData data = panel.Data;
                AreaStats stats = panel.DisplayedStats;
                AreaStats newStats = SaveData.Instance.Areas_Safe[data.ID];
                AreaModeStats modeStats = stats.Modes[(int)panel.Area.Mode];
                AreaModeStats newModeStats = newStats.Modes[(int)panel.Area.Mode];
                bool doStrawberries = newModeStats.TotalStrawberries > modeStats.TotalStrawberries;
                bool doHeartGem = newModeStats.HeartGem && !modeStats.HeartGem;
                bool doDeaths = newModeStats.Deaths > modeStats.Deaths && (panel.Area.Mode != AreaMode.Normal || newModeStats.Completed);
                bool doRemixUnlock = panel.Area.Mode == AreaMode.Normal && data.HasMode(AreaMode.BSide) && newStats.Cassette && !stats.Cassette;
                bool flag2 = doStrawberries || doHeartGem || doDeaths || doRemixUnlock;
                if (flag2)
                {
                    yield return 0.8f;
                }
                bool skipped = false;

                Coroutine routine = new Coroutine(GetInstanceMethod(typeof(OuiChapterPanel), "IncrementStatsDisplay").Invoke(panel, new object[] { modeStats, newModeStats, doHeartGem, doStrawberries, doDeaths, doRemixUnlock }) as IEnumerator, true);
                panel.Add(routine);
                yield return null;
                while (!routine.Finished)
                {
                    bool flag3 = MInput.GamePads[0].Pressed(Microsoft.Xna.Framework.Input.Buttons.Start)
                        || MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.Enter)
                        || Input.MenuConfirm.Pressed;
                    if (flag3)
                    {
                        routine.Active = false;
                        routine.RemoveSelf();
                        skipped = true;
                        EventInstance bside = GetInstanceField(typeof(OuiChapterPanel), panel, "bSideUnlockSfx") as EventInstance;
                        Audio.Stop(bside, true);
                        Audio.Play("event:/new_content/ui/skip_all");
                        break;
                    }
                    yield return null;
                }
                bool flag4 = skipped && doRemixUnlock;
                if (flag4)
                {
                    float spotAlpha = (float)GetInstanceField(typeof(OuiChapterPanel), panel, "spotlightAlpha");
                    float spotRadius = (float)GetInstanceField(typeof(OuiChapterPanel), panel, "spotlightRadius");
                    spotAlpha = 0f;
                    spotRadius = 0f;
                    AreaCompleteTitle remixText = GetInstanceField(typeof(OuiChapterPanel), panel, "remixUnlockText") as AreaCompleteTitle;
                    bool flag5 = remixText != null;
                    if (flag5)
                    {
                        remixText.RemoveSelf();
                        remixText = null;
                    }
                    
                    List<Object> m = GetInstanceField(typeof(OuiChapterPanel), panel, "modes") as List<Object>;
                    bool flag6 = m.Count <= 1 || m[1].ID != "B";
                    if (flag6)
                    {
                        this.AddRemixButton();
                    }
                    else
                    {
                        OuiChapterPanel.Option o = this.modes[1];
                        o.IconEase = 1f;
                        o.Appear = 1f;
                        o.Appeared = false;
                        o = null;
                    }
                }
                this.DisplayedStats = this.RealStats;
                bool flag7 = skipped;
                if (flag7)
                {
                    doStrawberries = (doStrawberries && modeStats.TotalStrawberries != newModeStats.TotalStrawberries);
                    doDeaths &= (modeStats.Deaths != newModeStats.Deaths);
                    doHeartGem = (doHeartGem && !this.heart.Visible);
                    this.UpdateStats(true, new bool?(doStrawberries), new bool?(doDeaths), new bool?(doHeartGem));
                }
                yield return null;
                routine = null;
                bool flag8 = shouldAdvance && this.OverworldStartMode == Overworld.StartMode.AreaComplete;
                if (flag8)
                {
                    bool flag9 = (!doDeaths && !doStrawberries && !doHeartGem) || Settings.Instance.SpeedrunClock > SpeedrunType.Off;
                    if (flag9)
                    {
                        yield return 1.2f;
                    }
                    this.Overworld.Goto<OuiChapterSelect>().AdvanceToNext();
                }
                else
                {
                    this.Focused = true;
                    this.Overworld.ShowInputUI = true;
                }
                yield break;
            }
            
        }*/

        private IEnumerator SHOWBINGO(On.Celeste.LevelExit.orig_Routine orig, LevelExit exit)
        {
            LevelExit.Mode mode = (LevelExit.Mode)GetInstanceField(typeof(LevelExit), exit, "mode");
            if(!mySettings.Enabled || !mySettings.SkipChapterComplete || mode != LevelExit.Mode.CompletedInterlude || currentLevel.Session.Area.ID == 0)
            {
                yield return new SwapImmediately(orig(exit));
            } else
            {
                
                exit.Add(new Bingo());
                while ((float)GetInstanceField(typeof(LevelExit), exit, "timer") < ChapterEndDelay)
                {
                    yield return null;
                }
                yield return new SwapImmediately(orig(exit));
            }
            

        }

        public class Bingo : Entity
        {
            public Bingo() : base()
            {
                this.display = true;
                
                base.Tag = (Tags.Global | Tags.PauseUpdate |Tags.HUD | Tags.FrozenUpdate);
                //base.Depth = -1000000;
                base.Add(this.icon = new Image(GFX.Game["ARCHIE"]));
                this.icon.Visible = true;
                base.Add(new Coroutine(this.Routine(), true) { UseRawDeltaTime = true });
                this.targetA = 1f;
            }

            private IEnumerator Routine()
            {
                this.icon.Visible = true;
                
                float opacity = 1f;
                
                yield return ChapterEndDelay;
                
                while(opacity > 0)
                {
                    this.icon.SetColor(Color.White * opacity);
                    opacity -= .05f;
                    yield return null;
                }
                this.icon.Visible = false;
                this.RemoveSelf();

                //yield return 1000f;
                yield break;
            }

           

            public override void Render()
            {
                this.icon.CenterOrigin();
                this.icon.Position = new Vector2(960f, 540f);
                //base.
                base.Render();
            }
            private bool display;
            private Image icon;
            private float targetA;
            
        }

        
        private void SkipChapterComplete(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            if (mySettings.Enabled && mySettings.SkipChapterComplete && mode == LevelExit.Mode.Completed)
                mode = LevelExit.Mode.CompletedInterlude;
            orig(self, mode, session, snow);
        }

        private int CheckWingedBerries()
        {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int wingedBerryCount = 0;
            foreach (AreaStats myArea in areas)
            {
                foreach (EntityID id in myArea.Modes[(int)AreaMode.Normal].Strawberries)
                    if (WingedBerryIDList.Contains(id.ToString()))
                        wingedBerryCount++;
            }
            return wingedBerryCount;
        }

        private int CheckSeedBerries()
        {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int seedBerryCount = 0;
            foreach (AreaStats myArea in areas)
            {
                foreach (EntityID id in myArea.Modes[(int)AreaMode.Normal].Strawberries)
                    if (SeedBerryIDList.Contains(id.ToString()))
                        seedBerryCount++;
            }
            return seedBerryCount;
        }

        private int CheckCassettes()
        {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int cassetteCount = 0;
            foreach (AreaStats myArea in areas)
                if (myArea.Cassette)
                    cassetteCount++;
            return cassetteCount;
        }

        private int CheckBlueHearts()
        {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int blueHeartCount = 0;
            foreach (AreaStats myArea in areas)
                if (myArea.Modes[(int)AreaMode.Normal].HeartGem)
                    blueHeartCount++;
            return blueHeartCount;
        }

        private int CheckRedHearts()
        {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int redHeartCount = 0;
            foreach (AreaStats myArea in areas)
                if (myArea.Modes[(int)AreaMode.BSide].HeartGem)
                    redHeartCount++;
            return redHeartCount;
        }

        private int CheckBinoculars()
        {
            return BinocularsList.Count;
        }

        private int CheckSeekersHit()
        {
            return SeekersHit;
        }

        private int CheckOshiroHits()
        {
            return OshiroHits;
        }

        private int CheckSnowballHits()
        {
            return SnowballHits;
        }

        public void DisableProloguePause(On.Celeste.CutsceneEntity.orig_Start onBegin, CutsceneEntity cutscene)
        {
            onBegin(cutscene);
            if (cutscene.Level.Session.Area.ID == 0 && mySettings.Enabled && mySettings.PreventPrologueCutsceneSkips)
                cutscene.Level.PauseLock = true;
        }

        public void EnableProloguePause(On.Celeste.CutsceneEntity.orig_EndCutscene onEnd, CutsceneEntity cutscene, Level level, bool removeSelf)
        {
            if (level.Session.Area.ID == 0 && mySettings.Enabled && mySettings.PreventPrologueCutsceneSkips)
                level.PauseLock = false;
            onEnd(cutscene, level, removeSelf);
        }

        public void PrologueMenu(On.Celeste.IntroVignette.orig_OpenMenu menu, IntroVignette vignette)
        {
            if (!mySettings.PreventPrologueCutsceneSkips || !mySettings.Enabled)
                menu(vignette);
        }

        private IEnumerator CollectRoutine(On.Celeste.Strawberry.orig_CollectRoutine orig, Strawberry berry, int collectIndex)
        {

            File.AppendAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt", berry.ID.ToString()+"\n");
            return orig(berry, collectIndex);
        }

        private void Interact(On.Celeste.Lookout.orig_Interact orig, Lookout lookout, Player player)
        {
            orig(lookout,player);
            if (testBinos)
                return;
            bool matchFound = false;
            foreach (Binoculars b in BinocularsList)
                if (b.areaID == currentLevel.Session.Area.ID && b.areaMode == (int)currentLevel.Session.Area.Mode && b.pos.Equals(lookout.Position))
                    matchFound = true;
            if (!matchFound)
                BinocularsList.Add(new Binoculars(currentLevel, lookout.Position));
        }

        private EventInstance BinoHud(On.Celeste.Audio.orig_Play_string_Vector2 orig, string path, Vector2 position)
        {
            if (testBinos && path == "event:/game/general/lookout_use")
            {
                bool matchFound = false;
                foreach (Binoculars b in BinocularsList)
                    if (b.areaID == currentLevel.Session.Area.ID && b.areaMode == (int)currentLevel.Session.Area.Mode && b.pos.Equals(position))
                        matchFound = true;
                if (!matchFound)
                    BinocularsList.Add(new Binoculars(currentLevel, position));
            }
            return orig(path, position);
        }

        private void RegenerateBegin(On.Celeste.Seeker.orig_RegenerateBegin orig, Seeker seeker)
        {

            SeekersHit++;
            orig(seeker);
        }

        private void HurtBegin(On.Celeste.AngryOshiro.orig_HurtBegin orig, AngryOshiro oshiro)
        {
            OshiroHits++;
            orig(oshiro);
        }

        private void OnPlayerBounce2(On.Celeste.Snowball.orig_OnPlayerBounce orig, Snowball snowball, Player player)
        {
            SnowballHits++;
            orig(snowball, player);
        }


        private void LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {


            orig(level, playerIntro, isFromLoader);
            
            currentLevel = level;


            if (DisplayEntities != null && level.Session.Area.ID == 0)
            {
                ResetUnsavedCounts();
                SkipUsed = -1;
            }
            if (mySettings.Enabled)
            {
                LoadGUI();
                if(mySettings.AutoEnableVariants)
                    SaveData.Instance.VariantMode = true;
                if(mySettings.CustomProgression)
                    SaveData.Instance.AssistMode = false;
            }
        }

        private void UnloadLevel(On.Celeste.Level.orig_UnloadLevel orig, Level level)
        {
            orig(level);

            currentLevel = null;
        }

        public void BinocularJournal( OuiJournal journal, Oui from)
        {
            if (!mySettings.Enabled)
                return;
            int newIndex = 2;
            foreach (OuiJournalPage page in journal.Pages)
            {
                if (page.PageIndex >= newIndex)
                    page.PageIndex++;
            }
            OuiJournalBinoculars newPage = new OuiJournalBinoculars(journal, BinocularsList);
            newPage.PageIndex = newIndex;
            journal.Pages.Insert(newIndex, newPage);


            return;

        }

        public void ShowBerries(On.Celeste.GameplayStats.orig_Render render, GameplayStats stats)
        {
            bool flag = stats.DrawLerp <= 0f;
            if (!flag)
            {
                float num = Ease.CubeOut(stats.DrawLerp);
                Level level = stats.Scene as Level;
                AreaKey area = level.Session.Area;
                AreaModeStats areaModeStats = SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode];
                bool flag2 = areaModeStats.Completed || SaveData.Instance.CheatMode || SaveData.Instance.DebugMode || (mySettings.Enabled && mySettings.ShowChapterBerryCount);
                if (flag2)
                {
                    ModeProperties modeProperties = AreaData.Get(area).Mode[(int)area.Mode];
                    int totalStrawberries = modeProperties.TotalStrawberries;
                    int ownedBerries = 0;
                    int num2 = 32;
                    int num3 = (totalStrawberries - 1) * num2;
                    int num4 = (totalStrawberries > 0 && modeProperties.Checkpoints != null) ? (modeProperties.Checkpoints.Length * num2) : 0;
                    Vector2 vector = new Vector2((float)((1920 - num3 - num4) / 2), 1016f + (1f - num) * 80f);
                    bool flag3 = totalStrawberries > 0;
                    if (flag3)
                    {
                        int num5 = (modeProperties.Checkpoints == null) ? 1 : (modeProperties.Checkpoints.Length + 1);
                        for (int i = 0; i < num5; i++)
                        {
                            int num6 = (i == 0) ? modeProperties.StartStrawberries : modeProperties.Checkpoints[i - 1].Strawberries;
                            for (int j = 0; j < num6; j++)
                            {
                                EntityData entityData = modeProperties.StrawberriesByCheckpoint[i, j];
                                bool flag4 = entityData == null;
                                if (!flag4)
                                {
                                    bool flag5 = false;
                                    foreach (EntityID entityID in level.Session.Strawberries)
                                    {
                                        bool flag6 = entityData.ID == entityID.ID && entityData.Level.Name == entityID.Level;
                                        if (flag6)
                                        {
                                            flag5 = true;
                                        }
                                    }
                                    MTexture mtexture = GFX.Gui["dot"];
                                    bool flag7 = flag5;
                                    if (flag7)
                                    {
                                        ownedBerries++;
                                        bool flag8 = area.Mode == AreaMode.CSide;
                                        if (flag8)
                                        {
                                            mtexture.DrawOutlineCentered(vector, Calc.HexToColor("f2ff30"), 1.5f);
                                        }
                                        else
                                        {
                                            mtexture.DrawOutlineCentered(vector, Calc.HexToColor("ff3040"), 1.5f);
                                            
                                        }
                                    }
                                    else
                                    {
                                        bool flag9 = false;
                                        foreach (EntityID entityID2 in areaModeStats.Strawberries)
                                        {
                                            bool flag10 = entityData.ID == entityID2.ID && entityData.Level.Name == entityID2.Level;
                                            if (flag10)
                                            {
                                                flag9 = true;
                                            }
                                        }
                                        bool flag11 = flag9;
                                        if (flag11)
                                        {
                                            mtexture.DrawOutlineCentered(vector, Calc.HexToColor("4193ff"), 1f);
                                            ownedBerries++;
                                        }
                                        else
                                        {
                                            Draw.Rect(vector.X - (float)mtexture.ClipRect.Width * 0.5f, vector.Y - 4f, (float)mtexture.ClipRect.Width, 8f, Color.DarkGray);
                                        }
                                    }
                                    vector.X += (float)num2;
                                }
                            }
                            bool flag12 = modeProperties.Checkpoints != null && i < modeProperties.Checkpoints.Length;
                            if (flag12)
                            {
                                Draw.Rect(vector.X - 3f, vector.Y - 16f, 6f, 32f, Color.DarkGray);
                                vector.X += (float)num2;
                            }
                        }
                        if (currentLevel.Session.Area.ID == 1)
                            foreach (EntityID berry in SaveData.Instance.Areas_Safe[1].Modes[0].Strawberries)
                                if (berry.ToString() == "end:4")
                                    ownedBerries++;
                        if (mySettings.Enabled && mySettings.ShowChapterBerryCount)
                            ActiveFont.DrawOutline(ownedBerries.ToString(), new Vector2(1920f / 2, 1053f + (1f - num) * 80f), new Vector2(0.5f, 0.5f), new Vector2(.7f, .7f), Color.White, 1.5f, Color.Black);
                    }
                    
                }
            }
        }

        

       



        private EventInstance Audio_Play_string(On.Celeste.Audio.orig_Play_string orig, string path)
        {
            if (!mySettings.CustomProgression || !mySettings.Enabled)
                return orig(path);
            if (path == "event:/ui/world_map/icon/assist_skip")
                return null;
            if (path == "cas_event:/ui/world_map/icon/assist_skip")
                return orig("event:/ui/world_map/icon/assist_skip");
            return orig(path);
        }
    }

    
    class TotalCollectableDisplay : Entity
    {
        private const float NumberUpdateDelay = 0.4f;
        private const float ComboUpdateDelay = 0.3f;
        private const float AfterUpdateDelay = 2f;
        private const float LerpInSpeed = 1.2f;
        private const float LerpOutSpeed = 2f;
        public static readonly Color FlashColor = Calc.HexToColor("FF5E76");
        private MTexture bg;
        public float DrawLerp;
        private float strawberriesUpdateTimer;
        private float strawberriesWaitTimer;
        public CollectableCounter collectables;
        private float baseYpos;

        public delegate int CheckVal();
        public CheckVal check;

        public bool TrackedInGame;

        public TotalCollectableDisplay(float Ypos, CheckVal del, bool saveTracked, int playSound, MTexture graphic)
        {
            Y = Ypos;
            baseYpos = Ypos;
            Depth = -100;
            check = del;
            TrackedInGame = saveTracked;
            Tag = (Tags.HUD | Tags.PauseUpdate | Tags.Persistent | Tags.TransitionUpdate);
            bg = GFX.Gui["strawberryCountBG"];
            Add(collectables = new CollectableCounter(false, check(), playSound, graphic, 0, false));
        }

        
        public override void Update()
        {
            base.Update();
            bool flag = check() > collectables.Amount && strawberriesUpdateTimer <= 0f;
            //flag = SaveData.Instance.TotalStrawberries_Safe >= 5;
            if (flag)
            {
                strawberriesUpdateTimer = 0.4f;
            }
            Level level = base.Scene as Level;
            bool flag2 = check() > collectables.Amount || strawberriesUpdateTimer > 0f || strawberriesWaitTimer > 0f || (level.Paused && level.PauseMainMenuOpen);
            if (flag2)
            {
                DrawLerp = Calc.Approach(DrawLerp, 1f, 1.2f * Engine.RawDeltaTime);
            }
            else
            {
                DrawLerp = Calc.Approach(DrawLerp, 0f, 2f * Engine.RawDeltaTime);
            }
            bool flag3 = strawberriesWaitTimer > 0f;
            if (flag3)
            {
                strawberriesWaitTimer -= Engine.RawDeltaTime;
            }
            bool flag4 = strawberriesUpdateTimer > 0f && DrawLerp == 1f;
            if (flag4)
            {
                strawberriesUpdateTimer -= Engine.RawDeltaTime;
                bool flag5 = strawberriesUpdateTimer <= 0f;
                if (flag5)
                {
                    bool flag6 = collectables.Amount < check();
                    if (flag6)
                    {
                        CollectableCounter collectablesCounter = collectables;
                        int amount = collectablesCounter.Amount;
                        collectablesCounter.Amount = amount + 1;
                    }
                    strawberriesWaitTimer = 2f;
                    bool flag7 = collectables.Amount < check();
                    if (flag7)
                    {
                        strawberriesUpdateTimer = 0.3f;
                    }
                }
            }
            bool visible = Visible;
            if (visible)
            {
                float num = baseYpos;
                bool flag8 = Settings.Instance.SpeedrunClock == SpeedrunType.Chapter;
                if (flag8)
                {
                    num += 58f;
                }
                else
                {
                    bool flag9 = Settings.Instance.SpeedrunClock == SpeedrunType.File;
                    if (flag9)
                    {
                        num += 78f;
                    }
                }
                Y = Calc.Approach(Y, num, Engine.DeltaTime * 800f);
            }
            Visible = (DrawLerp > 0f);
        }
        
        public override void Render()
        {
            Vector2 vector = Vector2.Lerp(new Vector2((float)(-(float)bg.Width), Y), new Vector2(32f, Y), Ease.CubeOut(DrawLerp));
            vector = vector.Round();
            bg.DrawJustified(vector + new Vector2(-96, 12f), new Vector2(0f, 0.5f));
            collectables.Position = vector + new Vector2(0f, -Y);
            collectables.Render();
        }

       
        
    }

    public class CollectableCounter : Component
    {
        public static readonly Color FlashColor = Calc.HexToColor("FF5E76");
        private const int IconWidth = 60;
        public bool Golden;
        public Vector2 Position;
        public bool CenteredX;
        public bool CanWiggle;
        public float Scale;
        public float Stroke;
        public float Rotation;
        public Color Color;
        public Color OutOfColor;
        public bool OverworldSfx;
        private int amount;
        private int outOf;
        private Wiggler wiggler;
        private float flashTimer;
        private string sAmount;
        private string sOutOf;
        private MTexture x;
        private bool showOutOf;
        private MTexture graphicID;
        private int playSound;

        public CollectableCounter(bool centeredX, int amount, int sound, MTexture graphic, int outOf = 0, bool showOutOf = false) : base(true,true)
        {
            this.Golden = false;
            this.CanWiggle = true;
            this.Scale = 1f;
            this.Stroke = 2f;
            this.Rotation = 0f;
            this.Color = Color.White;
            this.OutOfColor = Color.LightGray;
            this.outOf = -1;
            this.CenteredX = centeredX;
            this.amount = amount;
            this.outOf = outOf;
            this.showOutOf = showOutOf;
            this.UpdateStrings();
            this.wiggler = Wiggler.Create(0.5f, 3f, null, false, false);
            this.wiggler.StartZero = true;
            this.wiggler.UseRawDeltaTime = true;
            this.x = GFX.Gui["x"];
            playSound = sound;
            graphicID = graphic;
        }

        

        public int Amount
        {
            get
            {
                return amount;
            }
            set
            {
                bool flag = amount != value;
                bool flag2 = amount < value && playSound >= 1;
                if (flag)
                {
                    amount = value;
                    UpdateStrings();
                    bool canWiggle = CanWiggle;
                    if (canWiggle)
                    {
                        bool overworldSfx = OverworldSfx;
                        if (flag2)
                        {
                            if (overworldSfx)
                            {
                                Audio.Play(Golden  ? "event:/ui/postgame/goldberry_count" : "event:/ui/postgame/strawberry_count");
                            }
                            else
                            {
                                Audio.Play(playSound == 2 ? "event:/ui/postgame/goldberry_count" : "event:/ui/game/increment_strawberry");
                            }
                        }
                        wiggler.Start();
                        flashTimer = 0.5f;
                    }
                }
            }
        }
        
        public int OutOf
        {
            get
            {
                return outOf;
            }
            set
            {
                outOf = value;
                UpdateStrings();
            }
        }
        
        public bool ShowOutOf
        {
            get
            {
                return showOutOf;
            }
            set
            {
                bool flag = showOutOf != value;
                if (flag)
                {
                    showOutOf = value;
                    UpdateStrings();
                }
            }
        }
        
        public float FullHeight
        {
            get
            {
                return Math.Max(ActiveFont.LineHeight, (float)GFX.Gui["collectables/strawberry"].Height);
            }
        }
        
        private void UpdateStrings()
        {
            sAmount = amount.ToString();
            bool flag = outOf > -1;
            if (flag)
            {
                sOutOf = "/" + outOf.ToString();
            }
            else
            {
                sOutOf = "";
            }
        }
        
        public void Wiggle()
        {
            wiggler.Start();
            flashTimer = 0.5f;
        }
        
        public override void Update()
        {
            base.Update();
            bool active = wiggler.Active;
            if (active)
            {
                wiggler.Update();
            }
            bool flag = flashTimer > 0f;
            if (flag)
            {
                flashTimer -= Engine.RawDeltaTime;
            }
        }
        
        public override void Render()
        {
            Vector2 value = RenderPosition;
            Vector2 vector = Calc.AngleToVector(Rotation, 1f);
            Vector2 value2 = new Vector2(-vector.Y, vector.X);
            string text = showOutOf ? sOutOf : "";
            float num = ActiveFont.Measure(sAmount).X;
            float num2 = ActiveFont.Measure(text).X;
            float num3 = 62f + (float)x.Width + 2f + num + num2;
            Color color = Color;
            bool flag = flashTimer > 0f && base.Scene != null && base.Scene.BetweenRawInterval(0.05f);
            if (flag)
            {
                color = CollectableCounter.FlashColor;
            }
            bool centeredX = CenteredX;
            if (centeredX)
            {
                value -= vector * (num3 / 2f) * Scale;
            }
            //string id =  graphicID;
            //File.AppendAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt", Scale * (85f / (float)(graphicID.Height)) + "\n" + graphicID.Height + "\n" + graphicID.Height * Scale * (85f / (float)(graphicID.Height)) + "\n\n");
            //graphicID.DrawCentered(value + vector * 60f * 0.5f * Scale, Color.White, Scale);
            float Scale2 = Scale * (78f / (float)(graphicID.Height));
            //graphicID.DrawCentered(value + vector * 60f * 0.5f * Scale , Color.White, Scale * (85f / (float)(graphicID.Height)));
            graphicID.DrawCentered(value + vector * 60f * 0.5f * Scale, Color.White, Scale2);
            x.DrawCentered(value + vector * (62f + (float)x.Width * 0.5f) * Scale + value2 * 2f * Scale, color, Scale);
            ActiveFont.DrawOutline(sAmount, value + vector * (num3 - num2 - num * 0.5f) * Scale + value2 * (wiggler.Value * 18f) * Scale, new Vector2(0.5f, 0.5f), Vector2.One * Scale, color, Stroke, Color.Black);
            bool flag2 = text != "";
            if (flag2)
            {
                ActiveFont.DrawOutline(text, value + vector * (num3 - num2 / 2f) * Scale, new Vector2(0.5f, 0.5f), Vector2.One * Scale, OutOfColor, Stroke, Color.Black);
            }
        }
        
        public Vector2 RenderPosition
        {
            get
            {
                return (((base.Entity != null) ? base.Entity.Position : Vector2.Zero) + Position).Round();
            }
        }
        
        
        
        

        
    }

    public class OuiJournalBinoculars : OuiJournalPage
    {
        private OuiJournalPage.Table table;
        private List<Binoculars> binocularList;

        public OuiJournalBinoculars(OuiJournal journal, List<Binoculars> binocs) : base(journal)
        {
            this.PageTexture = "page";
            binocularList = binocs;
            this.table = new OuiJournalPage.Table().AddColumn(new OuiJournalPage.TextCell("RECORDS:\nBINOCULARS", new Vector2(1f, 0.5f), 0.7f, this.TextColor, 300f, false));
            for (int i = 0; i < SaveData.Instance.UnlockedModes; i++)
            {
                this.table.AddColumn(new OuiJournalPage.TextCell(Dialog.Clean("journal_mode_" + (AreaMode)i, null), this.TextJustify, 0.6f, this.TextColor, 240f, false));
            }
            bool[] array = new bool[]
            {
                true,
                SaveData.Instance.UnlockedModes >= 2,
                SaveData.Instance.UnlockedModes >= 3
            };
            int[] array2 = new int[3];
            foreach (AreaStats areaStats in SaveData.Instance.Areas_Safe)
            {
                AreaData areaData = AreaData.Get(areaStats.ID_Safe);
                bool interlude_Safe = areaData.Interlude_Safe;
                if (!interlude_Safe)
                {
                    bool flag = areaData.ID > SaveData.Instance.UnlockedAreas_Safe;
                    if (flag)
                    {
                        array[0] = (array[1] = (array[2] = false));
                        break;
                    }
                    OuiJournalPage.Row row = this.table.AddRow();
                    row.Add(new OuiJournalPage.TextCell(Dialog.Clean(areaData.Name, null), new Vector2(1f, 0.5f), 0.6f, this.TextColor, 0f, false));
                    for (int j = 0; j < SaveData.Instance.UnlockedModes; j++)
                    {
                        int num = CountBinoculars(areaStats.ID_Safe, j);
                        bool flag2 = num > 0;
                        if (flag2)
                        {
                            foreach (EntityData entityData in AreaData.Areas[areaStats.ID_Safe].Mode[j].MapData.Goldenberries)
                            {
                                EntityID item = new EntityID(entityData.Level.Name, entityData.ID);
                                bool flag3 = areaStats.Modes[j].Strawberries.Contains(item);
                                if (flag3)
                                {
                                    num = 0;
                                }
                            }

                            row.Add(new OuiJournalPage.TextCell(Dialog.Deaths(num), this.TextJustify, 0.5f, this.TextColor, 0f, false));
                            array2[j] += num;

                            

                        }
                        else
                        {
                            row.Add(new OuiJournalPage.IconCell("dot", 0f));
                            array[j] = false;
                        }
                    }
                    
                }
            }

            table.AddRow();
            OuiJournalPage.Row totalsRow = table.AddRow().Add(new OuiJournalPage.TextCell("TOTALS", new Vector2(1f, 0.5f), 0.7f, this.TextColor, 0f, false));
            for (int j = 0; j < SaveData.Instance.UnlockedModes; j++)
                totalsRow.Add(new OuiJournalPage.TextCell(array2[j].ToString(), this.TextJustify, 0.6f, this.TextColor, 0f, false));

            bool flag4 = array[0] || array[1] || array[2];
            if (flag4)
            {
                this.table.AddRow();
                OuiJournalPage.Row row2 = this.table.AddRow();
                row2.Add(new OuiJournalPage.TextCell(Dialog.Clean("journal_totals", null), new Vector2(1f, 0.5f), 0.7f, this.TextColor, 0f, false));
                for (int k = 0; k < SaveData.Instance.UnlockedModes; k++)
                {
                    row2.Add(new OuiJournalPage.TextCell(Dialog.Deaths(array2[k]), this.TextJustify, 0.6f, this.TextColor, 0f, false));
                }
                bool flag5 = array[0] && array[1] && array[2];
                if (flag5)
                {
                    OuiJournalPage.TextCell textCell = new OuiJournalPage.TextCell(Dialog.Deaths(array2[0] + array2[1] + array2[2]), this.TextJustify, 0.6f, this.TextColor, 0f, false);
                    textCell.SpreadOverColumns = 3;
                    this.table.AddRow().Add(new OuiJournalPage.TextCell(Dialog.Clean("journal_grandtotal", null), new Vector2(1f, 0.5f), 0.7f, this.TextColor, 0f, false)).Add(textCell);
                }
            }
        }
        
        private int CountBinoculars(int stage, int mode)
        {
            int binocCount = 0;
            foreach (Binoculars b in binocularList)
                if (b.areaID == stage && b.areaMode == mode)
                    binocCount++;
            return binocCount;
        }

        public override void Redraw(VirtualRenderTarget buffer)
        {
            base.Redraw(buffer);
            Draw.SpriteBatch.Begin();
            this.table.Render(new Vector2(60f, 20f));
            RenderStamps();
            Draw.SpriteBatch.End();
        }

        internal void RenderStamps()
        {
            bool assistMode = SaveData.Instance.AssistMode;
            if (assistMode)
            {
                //GFX.Gui["fileselect/assist"].DrawCentered(new Vector2(1250f, 810f), Color.White * 0.5f, 1f, 0.2f);
            }
            bool cheatMode = SaveData.Instance.CheatMode;
            if (cheatMode)
            {
                //GFX.Gui["fileselect/cheatmode"].DrawCentered(new Vector2(1400f, 860f), Color.White * 0.5f, 1f, 0f);
            }
        }
    }

    public class Binoculars
    {
        public int areaID;
        public int areaMode;
        public Vector2 pos;

        public Binoculars(Level lv, Vector2 myPos)
        {
            areaID = lv.Session.Area.ID;
            areaMode = (int)lv.Session.Area.Mode;
            pos = myPos;
        }
    }



   
}
