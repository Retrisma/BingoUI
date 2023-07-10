using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BingoUI {
    [Tracked]
    public class GrablessDisplay : Entity {
        private MTexture image;
        public GrablessDisplay() : base() {
            image = GFX.Gui["cs_variantmode"];
            Tag = (Tags.HUD | Tags.Global | Tags.Persistent | Tags.PauseUpdate | Tags.TransitionUpdate);
            Depth = -99;
            X = 30;
            Y = 30;//1050;
        }

        public override void Update() {
            base.Update();
            if (global::Celeste.SaveData.Instance != null)
                Visible = SaveData.Instance.Assists.NoGrabbing && BingoModule.Settings.Enabled;
        }

        public override void Render() {
            base.Render();
            if (Visible) {
                image.DrawCentered(Position, new Color(255, 255, 255, 70), .7f);
            }
        }
    }
}
