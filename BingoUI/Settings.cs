namespace Celeste.Mod.BingoUI
{
    public enum ProgressionType {
        Vanilla,
        Chocolate,
        Strawberry,
    }

    public class BingoSettings : EverestModuleSettings
    {
        private bool enabled = true;
        public bool Enabled { get { return enabled; } set { if (value && BingoModule.CurrentLevel != null) BingoModule.LevelSetup(); else BingoModule.LevelTeardown(); enabled = value; } }

        public ProgressionType CustomProgression { get; set; } = ProgressionType.Chocolate;

        [SettingName("BINGO_UI_PREVENT_PROLOGUE_CUTSCENE_SKIPS")]
        public bool PreventPrologueCutsceneSkips { get; set; } = true;

        public bool SkipChapterComplete { get; set; } = true;

        public bool SkipTallyWithConfirm { get; set; }

        public bool AutoEnableVariants { get; set; } = true;

        public bool ShowChapterBerryCount { get; set; } = true;

        public bool HideVariantsExceptGrabless { get; set; }

        public bool HideControls { get; set; }
    }
}
