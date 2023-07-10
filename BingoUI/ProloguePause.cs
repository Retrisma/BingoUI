namespace Celeste.Mod.BingoUI {
    public static class ProloguePauseDisable {
        public static void Load() {
            On.Celeste.CutsceneEntity.Start += DisableProloguePause;
            On.Celeste.CutsceneEntity.EndCutscene += EnableProloguePause;
            On.Celeste.IntroVignette.OpenMenu += PrologueMenu;
        }

        public static void Unload() {
            On.Celeste.CutsceneEntity.Start -= DisableProloguePause;
            On.Celeste.CutsceneEntity.EndCutscene -= EnableProloguePause;
            On.Celeste.IntroVignette.OpenMenu -= PrologueMenu;
        }

        public static void DisableProloguePause(On.Celeste.CutsceneEntity.orig_Start onBegin, CutsceneEntity cutscene) {
            onBegin(cutscene);
            if (cutscene.Level.Session.Area.ID == 0 && BingoModule.Settings.Enabled && BingoModule.Settings.PreventPrologueCutsceneSkips)
                cutscene.Level.PauseLock = true;
        }

        public static void EnableProloguePause(On.Celeste.CutsceneEntity.orig_EndCutscene onEnd, CutsceneEntity cutscene, Level level, bool removeSelf) {
            if (level.Session.Area.ID == 0 && BingoModule.Settings.Enabled && BingoModule.Settings.PreventPrologueCutsceneSkips)
                level.PauseLock = false;
            onEnd(cutscene, level, removeSelf);
        }

        public static void PrologueMenu(On.Celeste.IntroVignette.orig_OpenMenu menu, IntroVignette vignette) {
            if (!BingoModule.Settings.PreventPrologueCutsceneSkips || !BingoModule.Settings.Enabled)
                menu(vignette);
        }
    }
}
