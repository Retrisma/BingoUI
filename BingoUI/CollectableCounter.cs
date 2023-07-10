using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BingoUI {
    public class CollectableCounter : Component {
        public static readonly Color FlashColor = Calc.HexToColor("FF5E76");
        private const int IconWidth = 60;
        public bool Golden;
        public Vector2 Position;
        public bool CenteredX;
        public bool CanWiggle;
        public float Scale;
        public float Stroke;
        public float Rotation;
        public Color Color;
        public Color OutOfColor;
        public bool OverworldSfx;
        private int amount;
        private int outOf;
        private Wiggler wiggler;
        private float flashTimer;
        private string sAmount;
        private string sOutOf;
        private MTexture x;
        private bool showOutOf;
        private MTexture graphicID;
        private int playSound;

        public CollectableCounter(bool centeredX, int amount, int sound, MTexture graphic, int outOf = 0, bool showOutOf = false) : base(true, true) {
            this.Golden = false;
            this.CanWiggle = true;
            this.Scale = 1f;
            this.Stroke = 2f;
            this.Rotation = 0f;
            this.Color = Color.White;
            this.OutOfColor = Color.LightGray;
            this.outOf = -1;
            this.CenteredX = centeredX;
            this.amount = amount;
            this.outOf = outOf;
            this.showOutOf = showOutOf;
            this.UpdateStrings();
            this.wiggler = Wiggler.Create(0.5f, 3f, null, false, false);
            this.wiggler.StartZero = true;
            this.wiggler.UseRawDeltaTime = true;
            this.x = GFX.Gui["x"];
            playSound = sound;
            graphicID = graphic;
        }



        public int Amount {
            get {
                return amount;
            }
            set {
                bool flag = amount != value;
                bool flag2 = amount < value && playSound >= 1;
                if (flag) {
                    amount = value;
                    UpdateStrings();
                    bool canWiggle = CanWiggle;
                    if (canWiggle) {
                        bool overworldSfx = OverworldSfx;
                        if (flag2) {
                            if (overworldSfx) {
                                Audio.Play(Golden ? "event:/ui/postgame/goldberry_count" : "event:/ui/postgame/strawberry_count");
                            } else {
                                Audio.Play(playSound == 2 ? "event:/ui/postgame/goldberry_count" : "event:/ui/game/increment_strawberry");
                            }
                        }
                        wiggler.Start();
                        flashTimer = 0.5f;
                    }
                }
            }
        }

        public int OutOf {
            get {
                return outOf;
            }
            set {
                outOf = value;
                UpdateStrings();
            }
        }

        public bool ShowOutOf {
            get {
                return showOutOf;
            }
            set {
                bool flag = showOutOf != value;
                if (flag) {
                    showOutOf = value;
                    UpdateStrings();
                }
            }
        }

        public float FullHeight {
            get {
                return Math.Max(ActiveFont.LineHeight, (float)GFX.Gui["collectables/strawberry"].Height);
            }
        }

        private void UpdateStrings() {
            sAmount = amount.ToString();
            bool flag = outOf > -1;
            if (flag) {
                sOutOf = "/" + outOf.ToString();
            } else {
                sOutOf = "";
            }
        }

        public void Wiggle() {
            wiggler.Start();
            flashTimer = 0.5f;
        }

        public override void Update() {
            base.Update();
            bool active = wiggler.Active;
            if (active) {
                wiggler.Update();
            }
            bool flag = flashTimer > 0f;
            if (flag) {
                flashTimer -= Engine.RawDeltaTime;
            }
        }

        public override void Render() {
            Vector2 value = RenderPosition;
            Vector2 vector = Calc.AngleToVector(Rotation, 1f);
            Vector2 value2 = new Vector2(-vector.Y, vector.X);
            string text = showOutOf ? sOutOf : "";
            float num = ActiveFont.Measure(sAmount).X;
            float num2 = ActiveFont.Measure(text).X;
            float num3 = 62f + (float)x.Width + 2f + num + num2;
            Color color = Color;
            bool flag = flashTimer > 0f && base.Scene != null && base.Scene.BetweenRawInterval(0.05f);
            if (flag) {
                color = CollectableCounter.FlashColor;
            }
            bool centeredX = CenteredX;
            if (centeredX) {
                value -= vector * (num3 / 2f) * Scale;
            }
            //string id =  graphicID;
            //File.AppendAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt", Scale * (85f / (float)(graphicID.Height)) + "\n" + graphicID.Height + "\n" + graphicID.Height * Scale * (85f / (float)(graphicID.Height)) + "\n\n");
            //graphicID.DrawCentered(value + vector * 60f * 0.5f * Scale, Color.White, Scale);
            float Scale2 = Scale * (78f / (float)(graphicID.Height));
            //graphicID.DrawCentered(value + vector * 60f * 0.5f * Scale , Color.White, Scale * (85f / (float)(graphicID.Height)));
            graphicID.DrawCentered(value + vector * 60f * 0.5f * Scale, Color.White, Scale2);
            x.DrawCentered(value + vector * (62f + (float)x.Width * 0.5f) * Scale + value2 * 2f * Scale, color, Scale);
            ActiveFont.DrawOutline(sAmount, value + vector * (num3 - num2 - num * 0.5f) * Scale + value2 * (wiggler.Value * 18f) * Scale, new Vector2(0.5f, 0.5f), Vector2.One * Scale, color, Stroke, Color.Black);
            bool flag2 = text != "";
            if (flag2) {
                ActiveFont.DrawOutline(text, value + vector * (num3 - num2 / 2f) * Scale, new Vector2(0.5f, 0.5f), Vector2.One * Scale, OutOfColor, Stroke, Color.Black);
            }
        }

        public Vector2 RenderPosition {
            get {
                return (((base.Entity != null) ? base.Entity.Position : Vector2.Zero) + Position).Round();
            }
        }
    }
}
