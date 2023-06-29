using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.BingoUI
{
    [Tracked]
    class TotalCollectableDisplay : Entity
    {
        private const float NumberUpdateDelay = 0.4f;
        private const float ComboUpdateDelay = 0.3f;
        private const float AfterUpdateDelay = 2f;
        private const float LerpInSpeed = 1.2f;
        private const float LerpOutSpeed = 2f;
        public static readonly Color FlashColor = Calc.HexToColor("FF5E76");
        private MTexture bg;
        public float DrawLerp;
        private float strawberriesUpdateTimer;
        private float strawberriesWaitTimer;
        public CollectableCounter collectables;
        private float baseYpos;

        public delegate int CheckVal();
        public CheckVal check;

        public TotalCollectableDisplay(float Ypos, CheckVal del, int playSound, MTexture graphic)
        {
            Y = Ypos;
            baseYpos = Ypos;
            Depth = -100;
            check = del;
            Tag = (Tags.HUD | Tags.PauseUpdate | Tags.Persistent | Tags.TransitionUpdate);
            bg = GFX.Gui["strawberryCountBG"];
            Add(collectables = new CollectableCounter(false, check(), playSound, graphic, 0, false));
        }

        
        public override void Update()
        {
            base.Update();
            bool flag = check() > collectables.Amount && strawberriesUpdateTimer <= 0f;
            //flag = SaveData.Instance.TotalStrawberries_Safe >= 5;
            if (flag)
            {
                strawberriesUpdateTimer = 0.4f;
            }
            Level level = base.Scene as Level;
            bool flag2 = check() > collectables.Amount || strawberriesUpdateTimer > 0f || strawberriesWaitTimer > 0f || (level.Paused && level.PauseMainMenuOpen);
            if (flag2)
            {
                DrawLerp = Calc.Approach(DrawLerp, 1f, 1.2f * Engine.RawDeltaTime);
            }
            else
            {
                DrawLerp = Calc.Approach(DrawLerp, 0f, 2f * Engine.RawDeltaTime);
            }
            bool flag3 = strawberriesWaitTimer > 0f;
            if (flag3)
            {
                strawberriesWaitTimer -= Engine.RawDeltaTime;
            }
            bool flag4 = strawberriesUpdateTimer > 0f && DrawLerp == 1f;
            if (flag4)
            {
                strawberriesUpdateTimer -= Engine.RawDeltaTime;
                bool flag5 = strawberriesUpdateTimer <= 0f;
                if (flag5)
                {
                    bool flag6 = collectables.Amount < check();
                    if (flag6)
                    {
                        CollectableCounter collectablesCounter = collectables;
                        int amount = collectablesCounter.Amount;
                        collectablesCounter.Amount = amount + 1;
                    }
                    strawberriesWaitTimer = 2f;
                    bool flag7 = collectables.Amount < check();
                    if (flag7)
                    {
                        strawberriesUpdateTimer = 0.3f;
                    }
                }
            }
            bool visible = Visible;
            if (visible)
            {
                float num = baseYpos;
                bool flag8 = Settings.Instance.SpeedrunClock == SpeedrunType.Chapter;
                if (flag8)
                {
                    num += 58f;
                }
                else
                {
                    bool flag9 = Settings.Instance.SpeedrunClock == SpeedrunType.File;
                    if (flag9)
                    {
                        num += 78f;
                    }
                }
                Y = Calc.Approach(Y, num, Engine.DeltaTime * 800f);
            }
            Visible = (DrawLerp > 0f);
        }
        
        public override void Render()
        {
            Vector2 vector = Vector2.Lerp(new Vector2((float)(-(float)bg.Width), Y), new Vector2(32f, Y), Ease.CubeOut(DrawLerp));
            vector = vector.Round();
            bg.DrawJustified(vector + new Vector2(-96, 12f), new Vector2(0f, 0.5f));
            collectables.Position = vector + new Vector2(0f, -Y);
            collectables.Render();
        }
    }
}
