using System;
using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using static Monocle.MInput;

namespace Celeste.Mod.BingoUI {
    public static class SkipTallyWithConfirm {
        private static ILHook customTally;

        public static void Load() {
            customTally = new ILHook(typeof(OuiChapterPanel).GetMethod("IncrementStats", BindingFlags.Instance | BindingFlags.Public).GetStateMachineTarget(), SkipTally3);
        }

        public static void Unload() {
            customTally.Dispose();
            customTally = null;
        }

        private static void SkipTally3(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<KeyboardData>("Pressed"))) {
                cursor.EmitDelegate<Func<bool, bool>>(checkConfirm);
            }
        }

        private static bool checkConfirm(bool a) {
            if (BingoModule.Settings.Enabled && BingoModule.Settings.SkipTallyWithConfirm)
                return a || Input.MenuConfirm.Pressed;
            return a;
        }
    }
}
