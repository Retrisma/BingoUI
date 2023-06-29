using Monocle;

namespace Celeste.Mod.BingoUI {
    public static class Variants {
        public static void Load() {
            On.Celeste.Level.VariantMode += CustomVariantMode;
        }
        
        public static void Unload() {
            On.Celeste.Level.VariantMode -= CustomVariantMode;
        }

        private static void CustomVariantMode(On.Celeste.Level.orig_VariantMode orig, Level self, int returnIndex, bool minimal)
        {
            if (BingoModule.Settings.Enabled && BingoModule.Settings.HideVariantsExceptGrabless)
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

    }
}
