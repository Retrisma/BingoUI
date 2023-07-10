using System;
using System.Collections.Generic;
using System.Reflection;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.BingoUI {
    public static class CustomProgression {
        public static bool IsAssistSkipping = false; // HACK HACK HACK HACK

        public static void Load() {
            On.Celeste.SaveData.RegisterCompletion += CustomLevelUnlock;
            On.Celeste.OuiChapterSelect.Update += CustomAssistEnable;
            On.Celeste.OuiChapterSelectIcon.AssistModeUnlock += CustomAssist;
            On.Celeste.Audio.Play_string += Audio_Play_string;
            On.Celeste.OuiChapterSelectIcon.Render += CustomIconRender;
            On.Celeste.OuiFileSelectSlot.OnNewGameSelected += AssignFileProgression;
            On.Celeste.OuiFileSelectSlot.Render += ShowBingoIcon;
        }

        public static void Unload() {
            On.Celeste.SaveData.RegisterCompletion -= CustomLevelUnlock;
            On.Celeste.OuiChapterSelect.Update -= CustomAssistEnable;
            On.Celeste.OuiChapterSelectIcon.AssistModeUnlock -= CustomAssist;
            On.Celeste.Audio.Play_string -= Audio_Play_string;
            On.Celeste.OuiChapterSelectIcon.Render -= CustomIconRender;
            On.Celeste.OuiFileSelectSlot.OnNewGameSelected -= AssignFileProgression;
            On.Celeste.OuiFileSelectSlot.Render -= ShowBingoIcon;
        }

        private static void ShowBingoIcon(On.Celeste.OuiFileSelectSlot.orig_Render orig, OuiFileSelectSlot self) {
            if (self.SaveData != null && self.SaveData.HasFlag("BINGO")) {
                var archie = GFX.Game["ARCHIE"];
                var ease = Ease.CubeInOut((float)BingoUtils.GetInstanceField(typeof(OuiFileSelectSlot), self, "highlightEase"));
                archie.Draw(self.Position + new Vector2(0, -10f) + new Vector2(-800f, 0f) * ease, new Vector2(), Color.White, 0.25f, 0f, SpriteEffects.None);
                self.AssistModeEnabled = false;
                self.VariantModeEnabled = false;
            }
            orig(self);
        }

        private static void AssignFileProgression(On.Celeste.OuiFileSelectSlot.orig_OnNewGameSelected orig, OuiFileSelectSlot self) {
            orig(self);
            if (BingoModule.Settings.Enabled) {
                BingoModule.SaveData.CustomProgression = BingoModule.Settings.CustomProgression;
                SaveData.Instance.SetFlag("BINGO");
            }
        }

        private static void CustomIconRender(On.Celeste.OuiChapterSelectIcon.orig_Render orig, OuiChapterSelectIcon self) {
            if (SaveData.Instance == null) {
                orig(self);
            } else {
                var origUnlock = SaveData.Instance.UnlockedAreas_Safe;
                if (BingoModule.Settings.Enabled && BingoModule.SaveData.CustomProgression != ProgressionType.Vanilla) {
                    SaveData.Instance.UnlockedAreas_Safe = self.Area;
                }
                orig(self);
                SaveData.Instance.UnlockedAreas_Safe = origUnlock;
            }
        }

        public static void CustomLevelUnlock(On.Celeste.SaveData.orig_RegisterCompletion register, SaveData saveData, Session session) {
            var areas = BingoModule.SaveData.ClearedAreas;
            if (!areas.Contains(session.Area.ID)) {
                areas.Add(session.Area.ID);
            }
            bool clearedCore = false;
            int oldProgress = 0;
            if (session.Area.ID == 9) {
                clearedCore = true;
                oldProgress = saveData.UnlockedAreas_Safe;
            }
            register(saveData, session);
            if (session.Area.ID == 5 && BingoModule.SaveData.CustomProgression == ProgressionType.Chocolate && BingoModule.Settings.Enabled)
                saveData.UnlockedAreas_Safe = 7;
            if (BingoModule.SaveData.CustomProgression == ProgressionType.Chocolate && BingoModule.Settings.Enabled && clearedCore && oldProgress != 9)
                saveData.UnlockedAreas_Safe = oldProgress;
        }



        public static void CustomAssistEnable(On.Celeste.OuiChapterSelect.orig_Update update, OuiChapterSelect select) {
            if (SaveData.Instance == null || select == null || !BingoModule.Settings.Enabled || BingoModule.SaveData.CustomProgression == ProgressionType.Vanilla) {
                update(select);
                return;
            }

            List<OuiChapterSelectIcon> c = BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "icons") as List<OuiChapterSelectIcon>;
            int b = (int)BingoUtils.GetInstanceProp(typeof(OuiChapterSelect), select, "area");
            if (b > 10 || IsAssistSkipping) {
                update(select);
                return;
            }

            bool menuLeft = false;
            bool menuRight = false;
            int newArea = -1;
            var bestUnlocked = 0;
            SaveData.Instance.AssistMode = false;

            var specialHide = c[0].IsHidden || !((Engine.Scene as Overworld).Current is OuiChapterSelect);

            var statuses = ChapterStatuses();
            for (var i = 1; i <= 10; i++) {
                switch (statuses[i]) {
                    case ChapterIconStatus.Hidden:
                        if (!c[i].IsHidden) {
                            c[i].Hide();
                        }
                        break;
                    case ChapterIconStatus.Skippable:
                        if (c[i].IsHidden && !specialHide) {
                            c[i].Show();
                        }
                        bestUnlocked = i;
                        c[i].New = false;
                        c[i].AssistModeUnlockable = true;
                        SaveData.Instance.AssistMode = true;
                        break;
                    case ChapterIconStatus.Excited:
                        if (c[i].IsHidden && !specialHide) {
                            c[i].Show();
                        }
                        bestUnlocked = i;
                        c[i].New = true;
                        c[i].AssistModeUnlockable = false;
                        break;
                    case ChapterIconStatus.Shown:
                        if (c[i].IsHidden && !specialHide) {
                            c[i].Show();
                        }
                        bestUnlocked = i;
                        c[i].New = false;
                        c[i].AssistModeUnlockable = false;
                        break;
                }
            }

            SaveData.Instance.UnlockedAreas_Safe = bestUnlocked;

            bool disable = (bool)BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "disableInput");
            bool display = (bool)BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "display");
            float delay = (float)BingoUtils.GetInstanceField(typeof(OuiChapterSelect), select, "inputDelay");

            if (select.Focused && display && !disable && delay <= Engine.DeltaTime) {
                if (Input.MenuLeft.Pressed) {
                    for (var i = b - 1; i >= 0; i--) {
                        if (statuses[i] != ChapterIconStatus.Hidden) {
                            menuLeft = true;
                            newArea = i;
                            Audio.Play("event:/ui/world_map/icon/roll_left");
                            break;
                        }
                    }
                }
                if (Input.MenuRight.Pressed) {
                    for (var i = b + 1; i <= 10; i++) {
                        if (statuses[i] != ChapterIconStatus.Hidden) {
                            menuRight = true;
                            newArea = i;
                            Audio.Play("event:/ui/world_map/icon/roll_right");
                            break;
                        }
                    }
                }
            }

            var saved = SaveData.Instance.AssistMode;
            SaveData.Instance.AssistMode = false;
            if (b >= 9 && c[8].IsHidden && !Input.MenuUp.Pressed && !Input.MenuDown.Pressed) {
                select.orig_Update();
            } else {
                update(select);
            }
            SaveData.Instance.AssistMode = saved;



            if (menuLeft || menuRight) {
                c[newArea].Hovered(menuLeft ? -1 : 1);
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
                FieldInfo delayField = typeof(OuiChapterSelect).GetField("inputDelay", bindFlags);
                delayField.SetValue(select, .15f);
                PropertyInfo setNewArea = typeof(OuiChapterSelect).GetProperty("area", bindFlags);
                setNewArea.SetValue(select, newArea);
                MethodInfo ease = typeof(OuiChapterSelect).GetMethod("EaseCamera", bindFlags);
                ease.Invoke(select, new object[] { });

                select.Overworld.Maddy.Hide(true);
            }
        }

        public static void CustomAssist(On.Celeste.OuiChapterSelectIcon.orig_AssistModeUnlock unlock, OuiChapterSelectIcon icon, Action onComplete) {
            if (!BingoModule.Settings.Enabled || BingoModule.SaveData.CustomProgression == ProgressionType.Vanilla) {
                unlock(icon, onComplete);
                return;
            }

            Overworld oui = Engine.Scene as Overworld;
            OuiChapterSelect cselect = oui?.GetUI<OuiChapterSelect>();
            if (cselect == null) {
                unlock(icon, onComplete);
                return;
            }

            DynData<OuiChapterSelectIcon> dd = new DynData<OuiChapterSelectIcon>(icon);

            if (dd.Get<bool?>("attemptingSkip") ?? false) {
                dd.Set<bool?>("attemptingSkip", false);
                Audio.Play("cas_event:/ui/world_map/icon/assist_skip");
                SaveData.Instance.AssistMode = false;
                IsAssistSkipping = true;
                unlock(icon, () => {
                    BingoModule.SaveData.SkipUsed = icon.Area;
                    IsAssistSkipping = false;
                    onComplete();
                });
                return;
            }

            dd.Set<bool?>("attemptingSkip", true);
            cselect.Focused = true;
            oui.ShowInputUI = true;
        }

        private static EventInstance Audio_Play_string(On.Celeste.Audio.orig_Play_string orig, string path) {
            if (SaveData.Instance == null || BingoModule.SaveData.CustomProgression == ProgressionType.Vanilla || !BingoModule.Settings.Enabled)
                return orig(path);
            if (path == "event:/ui/world_map/icon/assist_skip")
                return null;
            if (path == "cas_event:/ui/world_map/icon/assist_skip")
                return orig("event:/ui/world_map/icon/assist_skip");
            return orig(path);
        }

        public static List<ChapterIconStatus> ChapterStatuses() {
            var result = new List<ChapterIconStatus>();
            result.Add(ChapterIconStatus.Shown);
            for (var i = 1; i <= 10; i++) {
                result.Add(ChapterIconStatus.Hidden);
            }
            if (!result.Contains(0)) {
                return result;
            }

            foreach (var i in BingoModule.SaveData.ClearedAreas) {
                result[i] = ChapterIconStatus.Shown;
            }
            var levels = BingoModule.SaveData.ClearedAreas;
            var skipped = BingoModule.SaveData.SkipUsed;
            var canUseCore = levels.Contains(9);

            switch (BingoModule.SaveData.CustomProgression) {
                case ProgressionType.Chocolate:
                    result[0] = ChapterIconStatus.Excited;
                    result[9] = ChapterIconStatus.Shown;
                    if (levels.Contains(0)) {
                        result[0] = ChapterIconStatus.Shown;
                        result[1] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(1) || skipped == 2) {
                        result[1] = ChapterIconStatus.Shown;
                        result[2] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(2) || skipped == 3) {
                        result[2] = ChapterIconStatus.Shown;
                        result[3] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(3) || skipped == 4) {
                        result[3] = ChapterIconStatus.Shown;
                        result[4] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(4) || skipped == 5) {
                        result[4] = ChapterIconStatus.Shown;
                        result[5] = ChapterIconStatus.Excited;
                        result[10] = ChapterIconStatus.Shown;
                    }
                    if (levels.Contains(5) || skipped == 6) {
                        result[5] = ChapterIconStatus.Shown;
                        result[6] = ChapterIconStatus.Excited;
                        result[7] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(7) || skipped == 8) {
                        result[7] = ChapterIconStatus.Shown;
                        result[8] = ChapterIconStatus.Excited;
                    }
                    if (BingoModule.SaveData.SkipUsed == -1) {
                        for (var i = 0; i <= 10; i++) {
                            if (result[i] == ChapterIconStatus.Hidden) {
                                result[i] = ChapterIconStatus.Skippable;
                                break;
                            }
                        }
                    }
                    break;
                case ProgressionType.Strawberry:
                    Func<bool> coreCheck = () => {
                        if (canUseCore) {
                            canUseCore = false;
                            return true;
                        }
                        return false;
                    };
                    result[0] = ChapterIconStatus.Excited;
                    result[9] = ChapterIconStatus.Shown;
                    if (levels.Contains(0) || coreCheck()) {
                        result[0] = ChapterIconStatus.Shown;
                        result[1] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(1) || skipped == 2 || coreCheck()) {
                        result[1] = ChapterIconStatus.Shown;
                        result[2] = ChapterIconStatus.Excited;
                        result[4] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(2) || levels.Contains(4) || skipped == 3 || coreCheck()) {
                        result[2] = ChapterIconStatus.Shown;
                        result[4] = ChapterIconStatus.Shown;
                        result[3] = ChapterIconStatus.Excited;
                        result[5] = ChapterIconStatus.Excited;
                        result[10] = ChapterIconStatus.Shown;
                    }
                    if (levels.Contains(3) || levels.Contains(5) || skipped == 6 || coreCheck()) {
                        result[3] = ChapterIconStatus.Shown;
                        result[5] = ChapterIconStatus.Shown;
                        result[6] = ChapterIconStatus.Excited;
                        result[7] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(6) || levels.Contains(7) || skipped == 8 || coreCheck()) {
                        result[6] = ChapterIconStatus.Shown;
                        result[7] = ChapterIconStatus.Shown;
                        result[8] = ChapterIconStatus.Excited;
                    }
                    if (levels.Contains(8) || skipped == 8) {
                        result[8] = ChapterIconStatus.Shown;
                    }
                    break;
                default:
                    throw new InvalidOperationException("forgot a case");
            }

            return result;
        }
    }

    public enum ChapterIconStatus {
        Hidden,
        Skippable,
        Excited,
        Shown,
    }
}
