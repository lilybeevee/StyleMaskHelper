using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using Monocle;
using Mono.Cecil.Cil;
using Celeste.Mod.Entities;
using Celeste.Mod.StyleMaskHelper.Compat;
using System.Linq;

namespace Celeste.Mod.StyleMaskHelper.Entities;

[Tracked]
[CustomEntity("StyleMaskHelper/BloomMask")]
public class BloomMask : Mask {
    public const string BufferName = "StyleMaskHelper_BloomMask_buffer";
    public const string FadeBufferName = "StyleMaskHelper_BloomMask_fadeBuffer";
    public const string DynDataLastStrengthName = "StyleMaskHelper_BloomMask_lastStrength";
    public const string DynDataMaskRectsName = "StyleMaskHelper_BloomMask_maskRects";

    private static VirtualRenderTarget BloomBuffer;
    private static VirtualRenderTarget FadeBuffer;

    public float BaseFrom;
    public float BaseTo;
    public float StrengthFrom;
    public float StrengthTo;

    public BloomMask(Vector2 position, float width, float height)
        : base(position, width, height) { }

    public BloomMask(EntityData data, Vector2 offset) : base(data, offset) {
        BaseFrom = data.Float("baseFrom", -1f);
        BaseTo = data.Float("baseTo", -1f);
        StrengthFrom = data.Float("strengthFrom", -1f);
        StrengthTo = data.Float("strengthTo", -1f);
    }

    public static void Load() {
        On.Celeste.GameplayBuffers.Create += GameplayBuffers_Create;
        On.Celeste.BloomRenderer.Apply += BloomRenderer_Apply;
        IL.Celeste.BloomRenderer.Apply += BloomRenderer_ApplyIL;
    }

    public static void Unload() {
        On.Celeste.GameplayBuffers.Create -= GameplayBuffers_Create;
        On.Celeste.BloomRenderer.Apply -= BloomRenderer_Apply;
        IL.Celeste.BloomRenderer.Apply -= BloomRenderer_ApplyIL;
    }

    private static void GameplayBuffers_Create(On.Celeste.GameplayBuffers.orig_Create orig) {
        orig();
        BloomBuffer?.Dispose();
        FadeBuffer?.Dispose();
        BloomBuffer = VirtualContent.CreateRenderTarget(BufferName, 320, 180);
        FadeBuffer = VirtualContent.CreateRenderTarget(FadeBufferName, 320, 180);
    }

    private static void BloomRenderer_Apply(On.Celeste.BloomRenderer.orig_Apply orig, BloomRenderer self, VirtualRenderTarget target, Scene scene) {
        DynamicData.For(self).Set(DynDataLastStrengthName, self.Strength);
        if (scene.Tracker.GetEntity<BloomMask>() != null)
            self.Strength = 1f;
        orig(self, target, scene);
    }

    private static void BloomRenderer_ApplyIL(ILContext il) {
        var cursor = new ILCursor(il);

        var textureLoc = 0;
        if (!cursor.TryGotoNext(
            instr => instr.MatchCall(typeof(GaussianBlur), "Blur"),
            instr => instr.MatchStloc(out textureLoc))) {

            Logger.Log("StyleMaskHelper/BloomMask", $"Failed to find local variable 'texture' in BloomRenderer.Apply - Bloom Mask disabled");
            return;
        }

        if (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdcR4(-10f),
            instr => instr.MatchLdcR4(-10f))) {

            if (cursor.TryGotoPrev(MoveType.AfterLabel,
                instr => instr.MatchCall(typeof(Draw), "get_SpriteBatch"))) {

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.Emit(OpCodes.Ldloc_S, (byte)textureLoc);
                cursor.EmitDelegate<Action<BloomRenderer, VirtualRenderTarget, Scene, Texture2D>>((self, target, scene, texture) => {
                    var selfData = DynamicData.For(self);
                    var sliceRects = new List<Rectangle>();
                    var level = scene as Level;
                    
                    var masks = scene.Tracker.GetEntities<BloomMask>().OfType<BloomMask>();

                    if (masks.Any() && !(StyleMaskModule.CelesteTASLoaded && CelesteTASCompat.SimplifiedBloom)) {
                        var bloomMaskLastStrength = selfData.Get<float>(DynDataLastStrengthName);
                        var maxStrength = 0f;

                        var rectMasks = masks.Where(mask => mask.Fade != FadeType.Custom);
                        var fadeMasks = masks.Where(mask => mask.Fade == FadeType.Custom).ToArray();

                        var lastTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();

                        // Bloom Buffer

                        Engine.Instance.GraphicsDevice.SetRenderTarget(BloomBuffer);
                        Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, level.Camera.Matrix);
                        Draw.SpriteBatch.Draw(GameplayBuffers.TempA, Vector2.Transform(Vector2.Zero, -level.Camera.Matrix), Color.White);
                        foreach (var mask in rectMasks) {
                            var baseFrom = (mask.BaseFrom >= 0f ? mask.BaseFrom : self.Base);
                            var baseTo = (mask.BaseTo >= 0f ? mask.BaseTo : self.Base);

                            foreach (var slice in mask.GetMaskSlices())
                                Draw.Rect(slice.Position.X, slice.Position.Y, slice.Source.Width, slice.Source.Height, Color.White * slice.GetValue(baseFrom, baseTo));
                        }
                        Draw.SpriteBatch.End();

                        if (fadeMasks.Length > 0) {
                            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, StyleMaskModule.CustomFadeRange, level.Camera.Matrix);
                            foreach (var mask in fadeMasks) {
                                var baseFrom = (mask.BaseFrom >= 0f ? mask.BaseFrom : self.Base);
                                var baseTo = (mask.BaseTo >= 0f ? mask.BaseTo : self.Base);

                                var strengthFrom = (mask.StrengthFrom >= 0f ? mask.StrengthFrom : bloomMaskLastStrength);
                                var strengthTo = (mask.StrengthTo >= 0f ? mask.StrengthTo : bloomMaskLastStrength);

                                maxStrength = Math.Max(maxStrength, Math.Max(strengthFrom, strengthTo));

                                mask.DrawFadeMask(new Color(baseFrom, baseTo, 1f));
                            }
                            Draw.SpriteBatch.End();
                        }

                        Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BloomRenderer.BlurredScreenToMask);
                        Draw.SpriteBatch.Draw(texture, Vector2.Zero, Color.White);
                        Draw.SpriteBatch.End();

                        // Fade Buffer (Strength)

                        if (fadeMasks.Length > 0) {
                            Engine.Instance.GraphicsDevice.SetRenderTarget(FadeBuffer);
                            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

                            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, StyleMaskModule.CustomFadeRange, level.Camera.Matrix);
                            foreach (var mask in fadeMasks) {
                                var strengthFrom = (mask.StrengthFrom >= 0f ? mask.StrengthFrom : bloomMaskLastStrength);
                                var strengthTo = (mask.StrengthTo >= 0f ? mask.StrengthTo : bloomMaskLastStrength);

                                if (maxStrength > 0f) {
                                    mask.DrawFadeMask(new Color(strengthFrom / maxStrength, strengthTo / maxStrength, 1f));
                                } else {
                                    mask.DrawFadeMask(new Color(strengthFrom, strengthTo, 1f));
                                }
                            }
                            Draw.SpriteBatch.End();
                        }
                        
                        // Target Buffer

                        Engine.Instance.GraphicsDevice.SetRenderTarget(target);

                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BloomRenderer.AdditiveMaskToScreen, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, level.Camera.Matrix);
                        foreach (var mask in masks) {
                            var strengthFrom = (mask.StrengthFrom >= 0f ? mask.StrengthFrom : bloomMaskLastStrength);
                            var strengthTo = (mask.StrengthTo >= 0f ? mask.StrengthTo : bloomMaskLastStrength);

                            foreach (var slice in mask.GetMaskSlices()) {
                                sliceRects.Add(new Rectangle((int)Math.Round(slice.Position.X), (int)Math.Round(slice.Position.Y), slice.Source.Width, slice.Source.Height));

                                if (mask.Fade == FadeType.Custom)
                                    continue;

                                var strength = slice.GetValue(strengthFrom, strengthTo);
                                for (int i = 0; i < strength; i++) {
                                    var scale = (i < strength - 1f) ? 1f : (strength - i);
                                    Draw.SpriteBatch.Draw(BloomBuffer, slice.Position, slice.Source, Color.White * scale);
                                }
                            }
                        }
                        Draw.SpriteBatch.End();

                        if (fadeMasks.Length > 0) {
                            Engine.Graphics.GraphicsDevice.Textures[1] = BloomBuffer;
                            StyleMaskModule.StrengthMask.Parameters["maxStrength"].SetValue(maxStrength);

                            Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BloomRenderer.AdditiveMaskToScreen, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, StyleMaskModule.StrengthMask);
                            for (int i = 0; i < maxStrength; i++) {
                                StyleMaskModule.StrengthMask.Parameters["currentStep"].SetValue(i);
                                Draw.SpriteBatch.Draw(FadeBuffer, Vector2.Zero, Color.White);
                            }
                            Draw.SpriteBatch.End();
                        }

                        Engine.Instance.GraphicsDevice.SetRenderTargets(lastTargets);
                    }

                    selfData.Set(DynDataMaskRectsName, sliceRects);
                });
            }
        }

        if (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchCall<Engine>("get_Instance"),
            instr => instr.MatchCallvirt<Game>("get_GraphicsDevice"),
            instr => instr.MatchLdarg(1))) {

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Action<BloomRenderer, Scene>>((self, scene) => {
                var selfData = DynamicData.For(self);
                var slices = selfData.Get<List<Rectangle>>(DynDataMaskRectsName);
                if (slices.Count > 0) {
                    //Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, (scene as Level).Camera.Matrix);
                    foreach (var slice in slices) {
                        Draw.Rect(slice, Color.Transparent);
                    }
                    Draw.SpriteBatch.End();
                }
                self.Strength = selfData.Get<float>(DynDataLastStrengthName);
            });
        }
    }
}
