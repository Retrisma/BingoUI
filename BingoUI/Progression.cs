using System;
using System.Collections.Generic;
using System.Reflection;
using FMOD.Studio;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.BingoUI {
    public static class CustomProgression {
        public static void Load() {
            On.Celeste.SaveData.RegisterCompletion += CustomLevelUnlock;
            On.Celeste.OuiChapterSelect.Update += CustomAssistEnable;
            On.Celeste.OuiChapterSelectIcon.AssistModeUnlock += CustomAssist;
            On.Celeste.Audio.Play_string += Audio_Play_string;
        }

        public static void Unload() {
            On.Celeste.SaveData.RegisterCompletion -= CustomLevelUnlock;
            On.Celeste.OuiChapterSelect.Update -= CustomAssistEnable;
            On.Celeste.OuiChapterSelectIcon.AssistModeUnlock -= CustomAssist;
            On.Celeste.Audio.Play_string -= Audio_Play_string;
        }

        public static void CustomLevelUnlock(On.Celeste.SaveData.orig_RegisterCompletion register, SaveData saveData, Session session)
        {
            bool clearedCore = false;
            int oldProgress = 0;
            if(session.Area.ID == 9)
            {
                clearedCore = true;
                oldProgress = saveData.UnlockedAreas_Safe;
            }
            register(saveData, session);
            if (session.Area.ID == 5 && BingoModule.Settings.CustomProgression && BingoModule.Settings.Enabled)
                saveData.UnlockedAreas_Safe = 7;
            if (BingoModule.Settings.CustomProgression && BingoModule.Settings.Enabled && clearedCore && oldProgress != 9)
                saveData.UnlockedAreas_Safe = oldProgress;
        }



        public static void CustomAssistEnable(On.Celeste.OuiChapterSelect.orig_Update update, OuiChapterSelect select)
        {
            
            bool a = (BingoModule.Settings.Enabled && BingoModule.Settings.CustomProgression && SaveData.Instance != null && select != null);
            int b = 0;
            
            int i = 1;
            List<OuiChapterSelectIcon> c = new List<OuiChapterSelectIcon>() ;
            bool menuLeft = false;
            bool menuRight = false;
            if (a)
            {
                b = (int)BingoUtils.GetInstanceProp(typeof(OuiChapterSelect), select, "area");
                if (b > 10)
                {
                    update(select);
                    return;
                }
                c = BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "icons") as List<OuiChapterSelectIcon>;
                
                
                while ( i < 9 &&(SaveData.Instance.Areas_Safe[i].Modes[0].Completed || SaveData.Instance.Areas_Safe[i].Modes[1].HeartGem ||
                    i + 1 == BingoModule.SaveData.SkipUsed || i == 6))
                    i++;
                if (i == 9 && Ch9Unlocked())
                    i = 10;

                if (SaveData.Instance.UnlockedAreas_Safe != i)
                    SaveData.Instance.UnlockedAreas_Safe = i;
            }
            if (a && !SaveData.Instance.AssistMode && BingoModule.SaveData.SkipUsed == -1)
                SaveData.Instance.AssistMode = true;
            if (a && BingoModule.SaveData.SkipUsed >= 0)
            {
                
                if (i == 7 && c[7].AssistModeUnlockable && (c[7].IsHidden || c[7].Position == c[7].HiddenPosition))
                {
                    c[7].Show();
                    c[7].AssistModeUnlockable = false;
                    c[7].Position = c[7].IdlePosition;
                }
                for (int j = 2; j < 9; j++)
                    if (c[j].AssistModeUnlockable && j != BingoModule.SaveData.SkipUsed)
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
                bool disable = (bool)BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "disableInput");
                bool display = (bool)BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "display");
                float delay = (float)BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "inputDelay");

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

        public static void CustomAssist(On.Celeste.OuiChapterSelectIcon.orig_AssistModeUnlock unlock, OuiChapterSelectIcon icon, Action onComplete)
        {
            if(!BingoModule.Settings.Enabled || !BingoModule.Settings.CustomProgression)
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
                BingoModule.SaveData.SkipUsed = icon.Area;
                unlock(icon, onComplete);
                return;
            }

            dd.Set<bool?>("attemptingSkip", true);
            cselect.Focused = true;
            oui.ShowInputUI = true;
        }

        private static bool Ch9Unlocked()
        {
            return SaveData.Instance == null ? false : SaveData.Instance.Areas_Safe[4].Modes[0].Completed || SaveData.Instance.Areas_Safe[4].Modes[1].HeartGem || 5 == BingoModule.SaveData.SkipUsed;
        }

        private static EventInstance Audio_Play_string(On.Celeste.Audio.orig_Play_string orig, string path)
        {
            if (!BingoModule.Settings.CustomProgression || !BingoModule.Settings.Enabled)
                return orig(path);
            if (path == "event:/ui/world_map/icon/assist_skip")
                return null;
            if (path == "cas_event:/ui/world_map/icon/assist_skip")
                return orig("event:/ui/world_map/icon/assist_skip");
            return orig(path);
        }
    }
}
