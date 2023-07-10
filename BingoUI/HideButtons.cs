using System.Collections.Generic;
using System.Reflection;
using Monocle;

namespace Celeste.Mod.BingoUI {
    public static class HideControls {
        public static void Load() {
            On.Celeste.BirdTutorialGui.Render += CustomTutorialRender;
            On.Celeste.Input.GuiButton_VirtualButton_PrefixMode_string += HideButtons;
            On.Celeste.Input.GuiSingleButton_Buttons_PrefixMode_string += HideSingleButton;
            On.Celeste.Input.GuiTexture += HideButtonTexture;
        }

        public static void Unload() {
            On.Celeste.BirdTutorialGui.Render -= CustomTutorialRender;
            On.Celeste.Input.GuiButton_VirtualButton_PrefixMode_string -= HideButtons;
            On.Celeste.Input.GuiSingleButton_Buttons_PrefixMode_string -= HideSingleButton;
            On.Celeste.Input.GuiTexture -= HideButtonTexture;
        }

        public static void CustomTutorialRender(On.Celeste.BirdTutorialGui.orig_Render orig, BirdTutorialGui self) {
            if (BingoModule.Settings.Enabled && BingoModule.Settings.HideControls && SaveData.Instance.CurrentSession.Area.ID <= 10) {
                typeof(BirdTutorialGui).GetField("controls", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, new List<object>());
                typeof(BirdTutorialGui).GetField("info", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, "BINGO");
                typeof(BirdTutorialGui).GetField("infoHeight", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, 0f);
            }
            orig(self);
        }

        public static MTexture HideButtons(On.Celeste.Input.orig_GuiButton_VirtualButton_PrefixMode_string orig, VirtualButton button, Input.PrefixMode mode, string fallback) {
            if (BingoModule.Settings.Enabled && BingoModule.Settings.HideControls)
                return GFX.Gui["controls/keyboard/oemquestion"];
            return orig(button, mode, fallback);
        }

        public static MTexture HideSingleButton(On.Celeste.Input.orig_GuiSingleButton_Buttons_PrefixMode_string orig, Microsoft.Xna.Framework.Input.Buttons button, Input.PrefixMode mode, string fallback) {
            if (BingoModule.Settings.Enabled && BingoModule.Settings.HideControls)
                return GFX.Gui["controls/keyboard/oemquestion"];
            return orig(button, mode, fallback);
        }

        public static MTexture HideButtonTexture(On.Celeste.Input.orig_GuiTexture orig, string prefix, string input) {
            if (BingoModule.Settings.Enabled && BingoModule.Settings.HideControls)
                return GFX.Gui["controls/keyboard/oemquestion"];
            return orig(prefix, input);
        }
    }
}
