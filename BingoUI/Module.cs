using System;
using System.Collections.Generic;
using MonoMod.RuntimeDetour;
using Monocle;

namespace Celeste.Mod.BingoUI
{
    public class BingoModule : EverestModule
    {
        public static BingoModule Instance;
        public BingoModule() {
            Instance = this;
        }

        public override Type SettingsType => typeof(BingoSettings);
        public override Type SaveDataType => typeof(BingoSaveData);
        public static BingoSettings Settings => (BingoSettings)Instance._Settings;
        public static BingoSaveData SaveData => (BingoSaveData)Instance._SaveData;
        public static Level CurrentLevel;

        public override void Load()
        {
            OuiJournalBinoculars.Load();
            HideControls.Load();
            ProloguePauseDisable.Load();
            BingoCounting.Load();
            Archie.Load();
            SkipTallyWithConfirm.Load();
            Variants.Load();
            ExtraCounters.Load();
            CustomProgression.Load();

            On.Celeste.Level.LoadLevel += LoadLevel;
            On.Celeste.Level.UnloadLevel += UnloadLevel;
        }

        public override void Unload() {
            OuiJournalBinoculars.Unload();
            HideControls.Unload();
            ProloguePauseDisable.Unload();
            BingoCounting.Unload();
            Archie.Unload();
            SkipTallyWithConfirm.Unload();
            Variants.Unload();
            ExtraCounters.Unload();
            CustomProgression.Unload();

            On.Celeste.Level.LoadLevel -= LoadLevel;
            On.Celeste.Level.UnloadLevel -= UnloadLevel;
        }

        public static void LevelSetup()
        {
            if (CurrentLevel == null) {
                return;
            }
            if (CurrentLevel.Tracker.GetEntity<TotalCollectableDisplay>() == null) {
                BingoCounting.CreateDisplayEntities();
            }
            if (CurrentLevel.Tracker.GetEntity<GrablessDisplay>() == null) {
                CurrentLevel.Add(new GrablessDisplay());
            }
        }

        public static void LevelTeardown()
        {
            if (CurrentLevel == null) {
                return;
            }

            BingoCounting.DestroyDisplayEntities();
            var grabless = CurrentLevel.Tracker.GetEntity<GrablessDisplay>();
            if (grabless != null) {
                CurrentLevel.Remove(grabless);
            }
        }

        private static void LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            CurrentLevel = level;
            orig(level, playerIntro, isFromLoader);
            
            if (BingoModule.Settings.Enabled)
            {
                BingoModule.LevelSetup();
                if(BingoModule.Settings.AutoEnableVariants)
                    global::Celeste.SaveData.Instance.VariantMode = true;
                if(BingoModule.SaveData.CustomProgression != ProgressionType.Vanilla)
                    global::Celeste.SaveData.Instance.AssistMode = false;
            }
        }

        private void UnloadLevel(On.Celeste.Level.orig_UnloadLevel orig, Level level)
        {
            orig(level);
            CurrentLevel = null;
        }
    }    
}
