using Celeste.Mod.Entities;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using System.Linq;
using Celeste.Mod.StyleMaskHelper.Compat;

namespace Celeste.Mod.StyleMaskHelper.Entities;

[Tracked]
[CustomEntity("StyleMaskHelper/LightingMask")]
public class LightingMask : Mask {

    public static BlendState SubtractAlpha = new BlendState {
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        ColorBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.ReverseSubtract
    };

    public static BlendState InvertAlpha = new BlendState {
        ColorSourceBlend = Blend.InverseDestinationAlpha,
        ColorDestinationBlend = Blend.Zero,
        ColorBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Subtract
    };

    public static BlendState DestinationTransparencySubtractAlpha = new BlendState {
        ColorSourceBlend = Blend.InverseSourceAlpha,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };

    public float LightingFrom;
    public float LightingTo;
    public bool AddBase;

    public int BufferIndex;

    public LightingMask(Vector2 position, float width, float height)
        : base(position, width, height) { }

    public LightingMask(EntityData data, Vector2 offset) : base(data, offset) {
        LightingFrom = data.Float("lightingFrom", -1f);
        LightingTo = data.Float("lightingTo", 0f);
        AddBase = data.Bool("addBase", true);
    }


    public static void Load() {
        On.Celeste.LightingRenderer.Render += LightingRenderer_Render;
    }

    public static void Unload() {
        On.Celeste.LightingRenderer.Render -= LightingRenderer_Render;
    }

    private static void LightingRenderer_Render(On.Celeste.LightingRenderer.orig_Render orig, LightingRenderer self, Scene scene) {
        var lightingMasks = scene.Tracker.GetEntities<LightingMask>();

        if (scene is Level level && lightingMasks.Count > 0 && !(StyleMaskModule.CelesteTASLoaded && CelesteTASCompat.SimplifiedLighting)) {
            var lastTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
            var lightingRects = new List<Rectangle>();

            var fadeMasks = lightingMasks.OfType<LightingMask>().Where(mask => mask.Fade == FadeType.Custom).ToArray();

            if (fadeMasks.Length > 0) {
                Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempB);
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, StyleMaskModule.CustomFadeRange, level.Camera.Matrix);
                foreach (var mask in fadeMasks) {
                    var lightingTo = (mask.LightingTo >= 0f ? ((mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingTo) : level.BaseLightingAlpha + level.Session.LightingAlphaAdd);
                    var lightingFrom = (mask.LightingFrom >= 0f ? ((mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingFrom) : level.BaseLightingAlpha + level.Session.LightingAlphaAdd);

                    mask.DrawFadeMask(new Color(lightingFrom, lightingTo, 1f));
                }
                Draw.SpriteBatch.End();

                Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
                Engine.Graphics.GraphicsDevice.Clear(Color.White);

                Engine.Graphics.GraphicsDevice.Textures[1] = GameplayBuffers.Light;

                Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, StyleMaskModule.MaskEffect);
                Draw.SpriteBatch.Draw(GameplayBuffers.TempB, Vector2.Zero, Color.White);
                Draw.SpriteBatch.End();

                Engine.Graphics.GraphicsDevice.SetRenderTargets(lastTargets);
            }

            GFX.FxDither.CurrentTechnique = GFX.FxDither.Techniques["InvertDither"];
            GFX.FxDither.Parameters["size"].SetValue(new Vector2(GameplayBuffers.Light.Width, GameplayBuffers.Light.Height));
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, GFX.DestinationTransparencySubtract, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, GFX.FxDither, level.Camera.Matrix);

            foreach (LightingMask mask in lightingMasks) {
                var lightingTo = (mask.LightingTo >= 0f ? ((mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingTo) : level.BaseLightingAlpha + level.Session.LightingAlphaAdd);
                var lightingFrom = (mask.LightingFrom >= 0f ? ((mask.AddBase ? level.BaseLightingAlpha : 0f) + mask.LightingFrom) : level.BaseLightingAlpha + level.Session.LightingAlphaAdd);

                foreach (var slice in mask.GetMaskSlices()) {
                    var lighting = MathHelper.Clamp(slice.GetValue(lightingFrom, lightingTo), 0f, 1f);
                    if (mask.Fade != FadeType.Custom)
                        Draw.SpriteBatch.Draw(GameplayBuffers.Light, slice.Position, slice.Source, Color.White * lighting);
                    lightingRects.Add(new Rectangle((int)slice.Position.X, (int)slice.Position.Y, slice.Source.Width, slice.Source.Height));
                }
            }

            if (fadeMasks.Length > 0)
                Draw.SpriteBatch.Draw(GameplayBuffers.TempA, Vector2.Transform(Vector2.Zero, -level.Camera.Matrix), Color.White);

            Draw.SpriteBatch.End();

            Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Light);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
            foreach (var rect in lightingRects) {
                Draw.Rect(rect, Color.White);
            }
            Draw.SpriteBatch.End();
            Engine.Graphics.GraphicsDevice.SetRenderTargets(lastTargets);
        }
        orig(self, scene);
    }
}
