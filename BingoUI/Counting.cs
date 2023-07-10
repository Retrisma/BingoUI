using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.BingoUI {
    public static class BingoCounting {
        public static void Load() {
            On.Celeste.GameplayStats.Render += ShowBerries;
            On.Celeste.Pico8.Classic.room_title.draw += Pico8Timer;
            IL.Celeste.OuiChapterPanel.DrawCheckpoint += CustomDrawCheckpoint;
        }

        public static void Unload() {
            On.Celeste.GameplayStats.Render -= ShowBerries;
            On.Celeste.Pico8.Classic.room_title.draw -= Pico8Timer;
            IL.Celeste.OuiChapterPanel.DrawCheckpoint -= CustomDrawCheckpoint;
        }

        private static List<string> WingedBerryIDList = new List<string> { "9c:2", "3b:2", "end_3c:13", "06-a:7", "13-b:31", "c-01:26", "b-21:99", "b-04:67", "d-10b:682", "e-09:398", "end:4" };
        private static List<string> SeedBerryIDList = new List<string> { "d1:67", "a-10:13", "b-17:10", "e-12:504" };

        public static void CreateDisplayEntities() {
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(96f + 1 * 78f, CheckWingedBerries, 0, GFX.Game["wings02"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(96f + 2 * 78f, CheckSeedBerries, 0, GFX.Game["seed00"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(136f + 3 * 78f, CheckCassettes, 1, GFX.Gui["collectables/cassette"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(136f + 4 * 78f, CheckBlueHearts, 1, GFX.Gui["collectables/heartgem/0/spin00"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(136f + 5 * 78f, CheckRedHearts, 1, GFX.Gui["collectables/heartgem/1/spin00"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(176f + 6 * 78f, CheckBinoculars, 2, GFX.Game["lookout05"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(176f + 7 * 78f, CheckSeekersHit, 0, GFX.Game["predator61"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(176f + 8 * 78f, CheckOshiroHits, 0, GFX.Game["boss35"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(176f + 9 * 78f, CheckSnowballHits, 0, GFX.Game["snowball00"]));
            BingoModule.CurrentLevel.Add(new TotalCollectableDisplay(176f + 10 * 78f, CheckKeys, 0, GFX.Game["key00"]));
        }

        public static void DestroyDisplayEntities() {
            var entities = BingoModule.CurrentLevel.Tracker.GetEntities<TotalCollectableDisplay>();
            foreach (var entity in entities) {
                BingoModule.CurrentLevel.Remove(entity);
            }
        }

        private static int CheckWingedBerries() {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int wingedBerryCount = 0;
            foreach (AreaStats myArea in areas) {
                foreach (EntityID id in myArea.Modes[(int)AreaMode.Normal].Strawberries)
                    if (WingedBerryIDList.Contains(id.ToString()))
                        wingedBerryCount++;
            }
            return wingedBerryCount;
        }

        private static int CheckSeedBerries() {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int seedBerryCount = 0;
            foreach (AreaStats myArea in areas) {
                foreach (EntityID id in myArea.Modes[(int)AreaMode.Normal].Strawberries)
                    if (SeedBerryIDList.Contains(id.ToString()))
                        seedBerryCount++;
            }
            return seedBerryCount;
        }

        private static int CheckCassettes() {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int cassetteCount = 0;
            foreach (AreaStats myArea in areas)
                if (myArea.Cassette)
                    cassetteCount++;
            return cassetteCount;
        }

        private static int CheckBlueHearts() {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int blueHeartCount = 0;
            foreach (AreaStats myArea in areas)
                if (myArea.Modes[(int)AreaMode.Normal].HeartGem)
                    blueHeartCount++;
            return blueHeartCount;
        }

        private static int CheckRedHearts() {
            List<AreaStats> areas = SaveData.Instance.Areas_Safe;
            int redHeartCount = 0;
            foreach (AreaStats myArea in areas)
                if (myArea.Modes[(int)AreaMode.BSide].HeartGem)
                    redHeartCount++;
            return redHeartCount;
        }

        private static int CheckBinoculars() {
            return BingoModule.SaveData.BinocularsList.Count;
        }

        private static int CheckSeekersHit() {
            return BingoModule.SaveData.SeekersHit;
        }

        private static int CheckOshiroHits() {
            return BingoModule.SaveData.OshiroHits;
        }

        private static int CheckSnowballHits() {
            return BingoModule.SaveData.SnowballHits;
        }

        private static int CheckKeys() {
            return BingoModule.SaveData.KeysList.Count;
        }

        private static void Pico8Timer(On.Celeste.Pico8.Classic.room_title.orig_draw draw, Pico8.Classic.room_title title) {
            draw(title);
            if (title == null || !BingoModule.Settings.Enabled)
                return;

            float drawDelay = (float)BingoUtils.GetInstanceField(typeof(Pico8.Classic.room_title), title, "delay");
            if (drawDelay < 0f) {
                HashSet<int> berries = BingoUtils.GetInstanceField(typeof(Pico8.Classic), title.G, "got_fruit") as HashSet<int>;
                title.E.rectfill(4, 10, berries.Count > 9 ? 25 : 21, 19, 0);
                title.E.spr(26, 5, 11, 1, 1, false, false);
                title.E.print("x" + ((float)berries.Count), 14, 14, 7);
            }
        }

        public static void ShowBerries(On.Celeste.GameplayStats.orig_Render render, GameplayStats stats) {
            bool flag = stats.DrawLerp <= 0f;
            if (!flag) {
                float num = Ease.CubeOut(stats.DrawLerp);
                Level level = stats.Scene as Level;
                AreaKey area = level.Session.Area;
                AreaModeStats areaModeStats = SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode];
                bool flag2 = areaModeStats.Completed || SaveData.Instance.CheatMode || SaveData.Instance.DebugMode || (BingoModule.Settings.Enabled && BingoModule.Settings.ShowChapterBerryCount);
                if (flag2) {
                    ModeProperties modeProperties = AreaData.Get(area).Mode[(int)area.Mode];
                    int totalStrawberries = modeProperties.TotalStrawberries;
                    int ownedBerries = 0;
                    int num2 = 32;
                    int num3 = (totalStrawberries - 1) * num2;
                    int num4 = (totalStrawberries > 0 && modeProperties.Checkpoints != null) ? (modeProperties.Checkpoints.Length * num2) : 0;
                    Vector2 vector = new Vector2((float)((1920 - num3 - num4) / 2), 1016f + (1f - num) * 80f);
                    bool flag3 = totalStrawberries > 0;
                    if (flag3) {
                        int num5 = (modeProperties.Checkpoints == null) ? 1 : (modeProperties.Checkpoints.Length + 1);
                        for (int i = 0; i < num5; i++) {
                            int num6 = (i == 0) ? modeProperties.StartStrawberries : modeProperties.Checkpoints[i - 1].Strawberries;
                            for (int j = 0; j < num6; j++) {
                                EntityData entityData = modeProperties.StrawberriesByCheckpoint[i, j];
                                bool flag4 = entityData == null;
                                if (!flag4) {
                                    bool flag5 = false;
                                    foreach (EntityID entityID in level.Session.Strawberries) {
                                        bool flag6 = entityData.ID == entityID.ID && entityData.Level.Name == entityID.Level;
                                        if (flag6) {
                                            flag5 = true;
                                        }
                                    }
                                    MTexture mtexture = GFX.Gui["dot"];
                                    bool flag7 = flag5;
                                    if (flag7) {
                                        ownedBerries++;
                                        bool flag8 = area.Mode == AreaMode.CSide;
                                        if (flag8) {
                                            mtexture.DrawOutlineCentered(vector, Calc.HexToColor("f2ff30"), 1.5f);
                                        } else {
                                            mtexture.DrawOutlineCentered(vector, Calc.HexToColor("ff3040"), 1.5f);

                                        }
                                    } else {
                                        bool flag9 = false;
                                        foreach (EntityID entityID2 in areaModeStats.Strawberries) {
                                            bool flag10 = entityData.ID == entityID2.ID && entityData.Level.Name == entityID2.Level;
                                            if (flag10) {
                                                flag9 = true;
                                            }
                                        }
                                        bool flag11 = flag9;
                                        if (flag11) {
                                            mtexture.DrawOutlineCentered(vector, Calc.HexToColor("4193ff"), 1f);
                                            ownedBerries++;
                                        } else {
                                            Draw.Rect(vector.X - (float)mtexture.ClipRect.Width * 0.5f, vector.Y - 4f, (float)mtexture.ClipRect.Width, 8f, Color.DarkGray);
                                        }
                                    }
                                    vector.X += (float)num2;
                                }
                            }
                            bool flag12 = modeProperties.Checkpoints != null && i < modeProperties.Checkpoints.Length;
                            if (flag12) {
                                Draw.Rect(vector.X - 3f, vector.Y - 16f, 6f, 32f, Color.DarkGray);
                                vector.X += (float)num2;
                            }
                        }
                        if (SaveData.Instance.CurrentSession.Area.ID == 1)
                            foreach (EntityID berry in SaveData.Instance.Areas_Safe[1].Modes[0].Strawberries)
                                if (berry.ToString() == "end:4")
                                    ownedBerries++;
                        if (BingoModule.Settings.Enabled && BingoModule.Settings.ShowChapterBerryCount)
                            ActiveFont.DrawOutline(ownedBerries.ToString(), new Vector2(1920f / 2, 1053f + (1f - num) * 80f), new Vector2(0.5f, 0.5f), new Vector2(.7f, .7f), Color.White, 1.5f, Color.Black);
                    }

                }
            }
        }

        private static void CustomDrawCheckpoint(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<SaveData>("DebugMode")))
                cursor.EmitDelegate<Func<bool, bool>>(CheckDrawCheckpoint);
        }

        private static bool CheckDrawCheckpoint(bool a) {
            return (BingoModule.Settings.Enabled && BingoModule.Settings.ShowChapterBerryCount) ? true : a;
        }
    }
}
