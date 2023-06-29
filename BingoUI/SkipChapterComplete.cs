using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BingoUI {
    public class Archie : Entity
    {
        private const float CHAPTER_END_DELAY = 1.5f;

        public static void Load() {
            On.Celeste.LevelExit.ctor += SkipChapterComplete;
            On.Celeste.LevelExit.Routine += SHOWBINGO;
        }

        public static void Unload() {
            On.Celeste.LevelExit.ctor -= SkipChapterComplete;
            On.Celeste.LevelExit.Routine -= SHOWBINGO;
        }

        public Archie() : base()
        {
            base.Tag = (Tags.Global | Tags.PauseUpdate |Tags.HUD | Tags.FrozenUpdate);
            //base.Depth = -1000000;
            base.Add(this.icon = new Image(GFX.Game["ARCHIE"]));
            this.icon.Visible = true;
            base.Add(new Coroutine(this.Routine(), true) { UseRawDeltaTime = true });
        }

        private IEnumerator Routine()
        {
            this.icon.Visible = true;
            
            float opacity = 1f;
            
            yield return CHAPTER_END_DELAY;
            
            while(opacity > 0)
            {
                this.icon.SetColor(Color.White * opacity);
                opacity -= .05f;
                yield return null;
            }
            this.icon.Visible = false;
            this.RemoveSelf();

            yield break;
        }

        public override void Render()
        {
            this.icon.CenterOrigin();
            this.icon.Position = new Vector2(960f, 540f);
            base.Render();
        }

        private Image icon;

        private static IEnumerator SHOWBINGO(On.Celeste.LevelExit.orig_Routine orig, LevelExit exit)
        {
            LevelExit.Mode mode = (LevelExit.Mode)BingoUtils.GetInstanceField(typeof(LevelExit), exit, "mode");
            if(!BingoModule.Settings.Enabled || !BingoModule.Settings.SkipChapterComplete || mode != LevelExit.Mode.CompletedInterlude || SaveData.Instance.CurrentSession.Area.ID == 0)
            {
                yield return new SwapImmediately(orig(exit));
            } else
            {
                
                exit.Add(new Archie());
                while ((float)BingoUtils.GetInstanceField(typeof(LevelExit), exit, "timer") < CHAPTER_END_DELAY)
                {
                    yield return null;
                }
                yield return new SwapImmediately(orig(exit));
            }
        }
        
        private static void SkipChapterComplete(On.Celeste.LevelExit.orig_ctor orig, LevelExit self, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            if (BingoModule.Settings.Enabled && BingoModule.Settings.SkipChapterComplete && mode == LevelExit.Mode.Completed)
                mode = LevelExit.Mode.CompletedInterlude;
            orig(self, mode, session, snow);
        }

    }
}
