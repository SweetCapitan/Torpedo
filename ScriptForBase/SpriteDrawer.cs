using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public partial class Program: MyGridProgram
    {
        public class SpriteDrawer
        {
            public IMyTextSurface Lcd { get; }
            List<string> sprites = new List<string>();
            public readonly float textHeight = 28.7f;
            public readonly float textWidht = 19.35f;

            public SpriteDrawer(IMyTextSurface lcd)
            {
                Lcd = lcd;
                Lcd.ContentType = ContentType.SCRIPT;
                Lcd.FontSize = 0.5f;
                Lcd.Font = "DEBUG";
                Lcd.FontColor = new Color(255, 134, 0);

                Lcd.GetSprites(sprites);
            }

            public void DrawSimpleSprite()
            {
                using(MySpriteDrawFrame frame = Lcd.DrawFrame())
                {
                    MySprite Vertical = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(250.0f, 10.0f), new Vector2(1.0f, 70.0f), Color.White,"", TextAlignment.LEFT, 0);
                    frame.Add(Vertical);

                    MySprite SpriteText = new MySprite(SpriteType.TEXT, "Hello World!", new Vector2(250.0f, 10.0f), null, Color.Green, "Monospace", TextAlignment.LEFT, 1.0f);
                    frame.Add(SpriteText);

                    SpriteText.Color = Color.Red;
                    SpriteText.Position = new Vector2(250.0f, 40.0f);
                    SpriteText.Alignment = TextAlignment.CENTER;
                    frame.Add(SpriteText);

                    SpriteText.Color = Color.Blue;
                    SpriteText.Position = new Vector2(250.0f, 70.0f);
                    SpriteText.Alignment = TextAlignment.RIGHT;
                    frame.Add(SpriteText);

                    string Text = $"Sprites count: {sprites.Count}";
                    const float THeight = 20.0f;
                    float Width = MeasureTextWidth(Text, THeight);
                    Vector2 BoxSize = new Vector2(Width + 2.0f, THeight + 2.0f);
                    MySprite Box = new MySprite(SpriteType.TEXTURE, "SquareHollow",
                        new Vector2(100.0f, 150.0f + BoxSize.Y * 0.5f), BoxSize, Color.White, "", TextAlignment.LEFT,
                        0);
                    frame.Add(Box);

                    SpriteText = new MySprite(SpriteType.TEXT, Text, new Vector2(101.0f, 151.0f), null, Color.Green,
                        "Monospace", TextAlignment.LEFT, THeight / textHeight);
                    frame.Add(SpriteText);
                }
            }

            public float MeasureTextWidth(string inText, float textSize)
            {
                float textScale = textSize / textHeight;
                return textWidht * inText.Length * textScale;
            }
        }
    }
}
