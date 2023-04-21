using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using Monocle;
using Mono.Cecil.Cil;
using Celeste.Mod.Entities;
using System.Drawing.Text;

namespace Celeste.Mod.StyleMaskHelper.Entities;

[Tracked]
[CustomEntity("StyleMaskHelper/ColorGradeMask")]
public class ColorGradeMask : Mask {
    private const string FadeBufferNamePrefix = "StyleMaskHelper_ColorGradeMask_fade";

    private static List<VirtualRenderTarget> FadeBuffers = new List<VirtualRenderTarget>();

    private static BlendState BetterAlphaBlend = new BlendState {
        ColorSourceBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaDestinationBlend = Blend.InverseSourceAlpha,
    };

    /* Special color grades:
     * - (current)
     * - (core)
    */

    public string ColorGradeFrom;
    public string ColorGradeTo;
    public float FadeFrom;
    public float FadeTo;

    public int BufferIndex;

    public ColorGradeMask(Vector2 position, float width, float height)
        : base(position, width, height) { }

    public ColorGradeMask(EntityData data, Vector2 offset) : base(data, offset) {
        ColorGradeFrom = data.Attr("colorGradeFrom", "(current)");
        ColorGradeTo = data.Attr("colorGradeTo", data.Attr("colorGrade", "(current)"));
        FadeFrom = data.Float("fadeFrom", 0f);
        FadeTo = data.Float("fadeTo", 1f);
    }

    public MTexture GetColorGrade(bool from = false) {
        var level = SceneAs<Level>();
        var name = from ? ColorGradeFrom : ColorGradeTo;

        if (name == "(current)") {
            name = from ? level.lastColorGrade : level.Session.ColorGrade;
        } else if (name == "(core)") {
            switch (level.CoreMode) {
                case Session.CoreModes.Cold: name = "cold"; break;
                case Session.CoreModes.Hot: name = "hot"; break;
                case Session.CoreModes.None: name = "none"; break;
            }
        }

        return GFX.ColorGrades.GetOrDefault(name, GFX.ColorGrades["none"]);
    }


    public static void Load() {
        IL.Celeste.Level.Render += Level_Render;
    }

    public static void Unload() {
        IL.Celeste.Level.Render -= Level_Render;
    }

    private static void Level_Render(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdnull(),
            instr => instr.MatchCallOrCallvirt<GraphicsDevice>("SetRenderTarget"))) {

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Level>>(level => {
                var fadeBufferIndexes = new Dictionary<ColorGradeKey, int>();
                var fadeBufferCount = 0;

                var masks = new Dictionary<ColorGradeKey, List<ColorGradeMask>>();

                // Find all masks and group them by color grade
                foreach (var entity in level.Tracker.GetEntities<ColorGradeMask>()) {
                    var mask = entity as ColorGradeMask;

                    if (mask.Fade != FadeType.Custom || !mask.IsVisible())
                        continue;

                    var key = new ColorGradeKey(mask);

                    if (!masks.TryGetValue(key, out var maskList)) {
                        masks.Add(key, maskList = new List<ColorGradeMask>());

                        fadeBufferIndexes.Add(key, fadeBufferCount);
                        fadeBufferCount++;
                    }

                    maskList.Add(mask);
                }

                if (FadeBuffers.Count > fadeBufferCount) {
                    for (var i = fadeBufferCount; i < FadeBuffers.Count; i++)
                        FadeBuffers[i].Dispose();
                    FadeBuffers.RemoveRange(fadeBufferCount, FadeBuffers.Count - fadeBufferCount);
                } else {
                    for (var i = FadeBuffers.Count; i < fadeBufferCount; i++)
                        FadeBuffers.Add(VirtualContent.CreateRenderTarget(FadeBufferNamePrefix + i, 320, 180));
                }

                if (fadeBufferCount > 0) {
                    var renderTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();

                    var currentFrom = GFX.ColorGrades.GetOrDefault(level.lastColorGrade, GFX.ColorGrades["none"]);
                    var currentTo = GFX.ColorGrades.GetOrDefault(level.Session.ColorGrade, GFX.ColorGrades["none"]);
                    var currentValue = ColorGrade.Percent;

                    foreach (var pair in masks) {
                        var colorGrade = pair.Key;

                        // Draw fade masks to canvas
                        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
                        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, level.Camera.Matrix);

                        foreach (var mask in pair.Value)
                            mask.DrawFadeMask();

                        Draw.SpriteBatch.End();

                        // Draw color graded level to canvas
                        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempB);
                        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                        ColorGrade.Set(colorGrade.From, colorGrade.To, colorGrade.ToAmount);

                        Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect);
                        Draw.SpriteBatch.Draw(GameplayBuffers.Level, Vector2.Zero, Color.White);
                        Draw.SpriteBatch.End();
                        
                        // Put them together in the final buffer
                        var bufferIndex = fadeBufferIndexes[colorGrade];

                        Engine.Graphics.GraphicsDevice.SetRenderTarget(FadeBuffers[bufferIndex]);
                        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                        ColorGrade.Set(colorGrade.From, colorGrade.To, colorGrade.FromAmount);

                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect, level.Camera.Matrix);
                        foreach (var mask in pair.Value) {
                            foreach (var slice in mask.GetMaskSlices()) {
                                Draw.SpriteBatch.Draw(GameplayBuffers.Level, slice.Position, slice.Source, Color.White);
                            }
                        }
                        Draw.SpriteBatch.End();

                        Engine.Graphics.GraphicsDevice.Textures[1] = GameplayBuffers.TempB.Target;

                        Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, StyleMaskModule.MaskEffect);
                        Draw.SpriteBatch.Draw(GameplayBuffers.TempA, Vector2.Zero, Color.White);
                        Draw.SpriteBatch.End();
                    }

                    ColorGrade.Set(currentFrom, currentTo, currentValue);

                    Engine.Graphics.GraphicsDevice.SetRenderTargets(renderTargets);
                }

                // Side note:
                //
                // this sucks!!! this is unable to be done after SetRenderTarget(null) because
                // the base render target doesnt seem to get preserved if the render target changes again,
                //
                // and for partial transparency the color grade has to be applied to the level buffer
                // *before* masking it, otherwise the color grade shader messes with the transparency,
                //
                // and even worse since our masks are just using alpha blending for color grade transitions
                // and you can transition to and from a different color grade than the level's own, we need to
                // draw another color graded level underneath the final one,
                //
                // so what we end up having to do is this entire process of:
                // - for each unique color grade (i assumed this was the optimal way to group buffers):
                //   - draw the masks to a temp buffer
                //   - draw the "to" color graded level to a temp buffer
                //   - draw the "from" color graded level in mask positions to the final buffer
                //   - mask the level on top of that buffer
                // - draw those buffers to the game
                //
                // considering all the graphical heckery going on here i can only assume this is a
                // pretty slow process, so if there's any ideas to optimize it let me know
                //
                // steps could be reduced here if i had some celeste-accurate color grade shader code to
                // combine with the mask shader code but i failed to properly recreate that
            });
        }

        int matrixLocal = -1;
        cursor.TryGotoNext(instr => instr.MatchLdcR4(6),
            instr => instr.MatchCall<Matrix>("CreateScale"),
            instr => instr.MatchLdsfld<Engine>("ScreenMatrix"),
            instr => true,
            instr => instr.MatchStloc(out matrixLocal));

        if (matrixLocal == -1) {
            Logger.Log("StyleMaskHelper/ColorGradeMask", $"Failed to find local variable 'matrix' in Level.Render - Color Grade Mask disabled");
            return;
        }

        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Level>("Pathfinder"))) {

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_S, (byte)matrixLocal);
            cursor.EmitDelegate<Action<Level, Matrix>>((level, matrix) => {
                var colorGradeMasks = level.Tracker.GetEntities<ColorGradeMask>();
                if (colorGradeMasks.Count > 0) {
                    var currentFrom = GFX.ColorGrades.GetOrDefault(level.lastColorGrade, GFX.ColorGrades["none"]);
                    var currentTo = GFX.ColorGrades.GetOrDefault(level.Session.ColorGrade, GFX.ColorGrades["none"]);
                    var currentValue = ColorGrade.Percent;

                    var screenSize = new Vector2(320f, 180f);
                    var scaledScreen = screenSize / level.ZoomTarget;
                    var focusOffset = (level.ZoomTarget != 1f) ? ((level.ZoomFocusPoint - scaledScreen / 2f) / (screenSize - scaledScreen) * screenSize) : Vector2.Zero;
                    var paddingOffset = new Vector2(level.ScreenPadding, level.ScreenPadding * 0.5625f);
                    var scale = level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f);

                    var zoomMatrix = Matrix.CreateTranslation(new Vector3(-focusOffset, 0f))
                                   * Matrix.CreateScale(scale)
                                   * Matrix.CreateTranslation(new Vector3(focusOffset + paddingOffset, 0f));

                    if (SaveData.Instance.Assists.MirrorMode) {
                        zoomMatrix *= Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(new Vector3(320f, 0f, 0f));
                    }

                    var batchMasks = colorGradeMasks.Where(mask => (mask as ColorGradeMask).Fade != FadeType.Custom);

                    // Draw normal masks
                    Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect, level.Camera.Matrix * zoomMatrix * matrix);
                    foreach (ColorGradeMask mask in batchMasks) {
                        var from = mask.GetColorGrade(from: true);
                        var to = mask.GetColorGrade(from: false);

                        foreach (var slice in mask.GetMaskSlices()) {
                            var value = Calc.Clamp(mask.FadeFrom + (slice.Value * (mask.FadeTo - mask.FadeFrom)), 0f, 1f);
                            if (value < 1f) {
                                ColorGrade.Set(from, to, value);
                            } else {
                                ColorGrade.Set(to);
                            }

                            Draw.SpriteBatch.Draw(GameplayBuffers.Level, slice.Position, slice.Source, Color.White);
                        }
                    }
                    Draw.SpriteBatch.End();

                    // Draw custom fade masks
                    if (FadeBuffers.Count > 0) {
                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, level.Camera.Matrix * zoomMatrix * matrix);

                        foreach (var buffer in FadeBuffers)
                            Draw.SpriteBatch.Draw(buffer, level.Camera.Position, Color.White);

                        Draw.SpriteBatch.End();
                    }

                    ColorGrade.Set(currentFrom, currentTo, currentValue);
                }
            });
        }
    }


    public struct ColorGradeKey : IEquatable<ColorGradeKey> {
        public MTexture From;
        public MTexture To;
        public float FromAmount;
        public float ToAmount;

        public ColorGradeKey(ColorGradeMask mask) {
            From = mask.GetColorGrade(true);
            To = mask.GetColorGrade(false);
            FromAmount = mask.FadeFrom;
            ToAmount = mask.FadeTo;
        }

        #region Auto-Generated Equality
        public override bool Equals(object obj) {
            return obj is ColorGradeKey key&&Equals(key);
        }

        public bool Equals(ColorGradeKey other) {
            return EqualityComparer<MTexture>.Default.Equals(From, other.From)&&
                   EqualityComparer<MTexture>.Default.Equals(To, other.To)&&
                   FromAmount==other.FromAmount&&
                   ToAmount==other.ToAmount;
        }

        public override int GetHashCode() {
            var hashCode = -1620330566;
            hashCode=hashCode*-1521134295+EqualityComparer<MTexture>.Default.GetHashCode(From);
            hashCode=hashCode*-1521134295+EqualityComparer<MTexture>.Default.GetHashCode(To);
            hashCode=hashCode*-1521134295+FromAmount.GetHashCode();
            hashCode=hashCode*-1521134295+ToAmount.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ColorGradeKey left, ColorGradeKey right) {
            return left.Equals(right);
        }

        public static bool operator !=(ColorGradeKey left, ColorGradeKey right) {
            return !(left==right);
        }
        #endregion
    }
}
