using Celeste.Mod.Entities;
using Celeste.Mod.StyleMaskHelper.Compat;
using Celeste.Mod.StyleMaskHelper.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.StyleMaskHelper.Entities;

[Tracked]
[CustomEntity("StyleMaskHelper/StylegroundMask")]
public class StylegroundMask : Mask {

    private ObservableCollection<string> _renderTags;
    public ObservableCollection<string> RenderTags {
        get => _renderTags;
        set {
            if (value != _renderTags) {
                RecacheTags(_renderTags, value);
                value.CollectionChanged += (sender, e) => RecacheTags((IList<string>)e.OldItems, (IList<string>)e.NewItems);
            }
            _renderTags = value;
        }
    }

    public bool Foreground = false;
    public bool EntityRenderer = false;
    public bool BehindForeground = false;

    public float AlphaFrom = 0f;
    public float AlphaTo = 1f;

    public StylegroundMask(Vector2 position, float width, float height, IEnumerable<string> tags = null)
        : base(position, width, height) {

        Depth = 2000000;

        if (tags != null) {
            RenderTags = new ObservableCollection<string>(tags);
        } else {
            RenderTags = new ObservableCollection<string>();
        }
    }

    public StylegroundMask(EntityData data, Vector2 offset) : base(data, offset) {
        Depth = 2000000;
        RenderTags = new ObservableCollection<string>(data.Attr("tag").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        EntityRenderer = data.Bool("entityRenderer");
        BehindForeground = data.Bool("behindFg");
        AlphaFrom = data.Float("alphaFrom", 0f);
        AlphaTo = data.Float("alphaTo", 1f);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        if (EntityRenderer && !Foreground) {
            scene.Add(new StylegroundMask(Position, Width, Height, RenderTags) {
                Depth = -2000000,
                Foreground = true,
                Fade = Fade,
                Flag = Flag,
                NotFlag = NotFlag,
                ScrollX = ScrollX,
                ScrollY = ScrollY,
                EntityRenderer = EntityRenderer,
                BehindForeground = BehindForeground,
                AlphaFrom = AlphaFrom,
                AlphaTo = AlphaTo
            });
        }

        // Special case: Automatically add a core mode Color Grade Mask if we're masking a Heat Wave effect
        if (!Foreground) {
            string needsColorGrade = null;

            var maskRenderer = StylegroundMaskRenderer.GetRendererInLevel(scene as Level);
            if (maskRenderer != null) {
                // Check masked backdrops for a Heat Wave effect
                foreach (var backdrop in maskRenderer.AllBackdrops) {
                    if ((needsColorGrade = GetHeatWaveColorGrade(backdrop)) != null) {
                        break;
                    }
                }
            } else {
                // Styleground renderer not added yet, check for Heat Wave effect in the level
                foreach (var backdrop in (scene as Level).Foreground.Backdrops) {
                    if ((needsColorGrade = GetHeatWaveColorGrade(backdrop)) != null) {
                        break;
                    }
                }
                if (needsColorGrade == null) {
                    foreach (var backdrop in (scene as Level).Background.Backdrops) {
                        if ((needsColorGrade = GetHeatWaveColorGrade(backdrop)) != null) {
                            break;
                        }
                    }
                }
            }

            if (needsColorGrade != null) {
                scene.Add(new ColorGradeMask(Position, Width, Height) {
                    Fade = Fade,
                    FadeMask = FadeMask,
                    Flag = Flag,
                    NotFlag = NotFlag,
                    ScrollX = ScrollX,
                    ScrollY = ScrollY,
                    FadeFrom = 0f,
                    FadeTo = 1f,
                    ColorGradeFrom = "(current)",
                    ColorGradeTo = needsColorGrade,
                });
            }
        }

        RecacheTags(null, RenderTags);
    }

    public override void Removed(Scene scene) {
        RecacheTags(RenderTags, null);

        base.Removed(scene);
    }

    private string GetHeatWaveColorGrade(Backdrop backdrop) {
        if (!IsColorGradeHeatWave(backdrop) || !RenderTags.Any(tag => backdrop.Tags.Contains(StylegroundMaskRenderer.TagPrefix + tag)))
            return null;

        if (backdrop is HeatWaveOneMode heatWaveOneMode) {
            switch (heatWaveOneMode.CoreMode) {
                case Session.CoreModes.Hot: return "hot";
                case Session.CoreModes.Cold: return "cold";
                case Session.CoreModes.None: return "(current)";
            }
        }

        return "(core)";
    }

    private bool IsColorGradeHeatWave(Backdrop backdrop) {
        if (backdrop is not HeatWave)
            return false;

        if (StyleMaskModule.MaddieHelpingHandLoaded && MaddieHelpingHandCompat.IsHeatWaveNoColorGrade(backdrop))
            return false;

        if (backdrop is HeatWaveOneMode heatWaveOneMode && !heatWaveOneMode.ColorGrade)
            return false;

        return true;
    }
    
    public override void Render() {
        base.Render();
        if (EntityRenderer) {
            var bufferDict = StylegroundMaskRenderer.GetBuffers(Foreground);
            foreach (var tag in RenderTags) {
                if (bufferDict.TryGetValue(tag, out var buffer)) {
                    foreach (var slice in GetMaskSlices()) {
                        Draw.SpriteBatch.Draw(buffer, slice.Position, slice.Source, Color.White * slice.GetValue(AlphaFrom, AlphaTo));
                    }
                }
            }
        }
    }

    private void RecacheTags(IEnumerable<string> oldTags, IEnumerable<string> newTags) {
        StylegroundMaskRenderer renderer;

        if (Scene == null || (renderer = StylegroundMaskRenderer.GetRendererInLevel(Scene as Level)) == null)
            return;

        if (oldTags != null) {
            foreach (var tag in oldTags) {
                if (!renderer.Masks.TryGetValue(tag, out var cachedMasks))
                    continue;
                cachedMasks.Remove(this);
            }
        }

        if (newTags != null) {
            foreach (var tag in newTags) {
                if (!renderer.Masks.TryGetValue(tag, out var cachedMasks))
                    continue;
                if (!cachedMasks.Contains(this))
                    cachedMasks.Add(this);
            }
        }
    }
}

public class StylegroundMaskRenderer : Renderer {
    public const string TagPrefix = "mask_";
    public const string MaskBufferFgNamePrefix = "StyleMaskHelper_StylegroundMask_fg_";
    public const string MaskBufferBgNamePrefix = "StyleMaskHelper_StylegroundMask_bg_";
    public const string DynDataRendererName = "StyleMaskHelper_StylegroundMaskRenderer";

    /// <summary>
    /// used internally to render consumed stylegrounds
    /// </summary>
    private static BackdropRenderer DummyBackdropRenderer = new();

    public static Dictionary<string, VirtualRenderTarget> BgBuffers = new();
    public static Dictionary<string, VirtualRenderTarget> FgBuffers = new();

    public bool Foreground;
    public bool Behind;
    public bool SkipBuffers;

    // tag -> backdrops
    public Dictionary<string, List<Backdrop>> FGBackdrops = new();
    public Dictionary<string, List<Backdrop>> BGBackdrops = new();
    public List<Backdrop> AllBackdrops = new();

    public Dictionary<string, List<StylegroundMask>> Masks = new();

    public static Dictionary<string, VirtualRenderTarget> GetBuffers(bool foreground) => foreground ? FgBuffers : BgBuffers;

    public Dictionary<string, List<Backdrop>> GetBackdrops(bool foreground) => foreground ? FGBackdrops : BGBackdrops;

    public List<StylegroundMask> GetMasksWithTag(Level level, string tag) {
        if (Masks.TryGetValue(tag, out var cachedMasks)) {
            return cachedMasks;
        }

        var masks = level.Tracker.GetEntities<StylegroundMask>()
                             .Where(m => (m as StylegroundMask).RenderTags.Contains(tag))
                             .Cast<StylegroundMask>()
                             .ToList();

        return Masks[tag] = masks;
    }

    public static VirtualRenderTarget GetBuffer(string tag, bool foreground) {
        var buffers = GetBuffers(foreground);

        if (!buffers.ContainsKey(tag)) {
            var namePrefix = foreground ? MaskBufferFgNamePrefix : MaskBufferBgNamePrefix;
            buffers.Add(tag, VirtualContent.CreateRenderTarget(namePrefix + tag, 320, 180));
        }

        return buffers[tag];
    }

    private static void UnloadBuffers() {
        foreach (var buffer in FgBuffers.Values)
            buffer.Dispose();

        foreach (var buffer in BgBuffers.Values)
            buffer.Dispose();

        FgBuffers.Clear();
        BgBuffers.Clear();
    }

    private static bool TagIsMaskTag(string tag) => tag.StartsWith(TagPrefix);

    private static string StripTagPrefix(string tag) => tag.Substring(TagPrefix.Length);

    public static StylegroundMaskRenderer GetRendererInLevel(Level level) => DynamicData.For(level).Get<StylegroundMaskRenderer>(DynDataRendererName);

    private void AddBackdrop(string tag, Backdrop backdrop, Dictionary<string, List<Backdrop>> into) {
        if (!into.ContainsKey(tag)) {
            into.Add(tag, new());
        }

        into[tag].Insert(0, backdrop);

        AllBackdrops.Insert(0, backdrop);
    }

    private void ConsumeStylegroundsFrom(List<Backdrop> from, Dictionary<string, List<Backdrop>> into) {
        // reversed loop to allow removing items while iterating
        for (int i = from.Count - 1; i >= 0; i--) {
            var backdrop = from[i];

            foreach (var tag in backdrop.Tags) {
                if (TagIsMaskTag(tag)) {
                    AddBackdrop(StripTagPrefix(tag), backdrop, into);
                    backdrop.Renderer = DummyBackdropRenderer;
                    from.RemoveAt(i);
                }
            }
        }
    }

    public void ConsumeStylegrounds(Level level) {
        ConsumeStylegroundsFrom(level.Foreground.Backdrops, FGBackdrops);
        ConsumeStylegroundsFrom(level.Background.Backdrops, BGBackdrops);
    }

    public static bool IsEntityInView(Level level, Entity entity) {
        Camera camera = level.Camera;
        return new Rectangle((int)entity.X, (int)entity.Y, (int)entity.Width, (int)entity.Height)
               .Intersects(new Rectangle((int)camera.X, (int)camera.Y, 320, 180));
    }

    public bool AnyMaskIsInView(Level level, string tag) {
        foreach (var mask in GetMasksWithTag(level, tag)) {
            if (IsEntityInView(level, mask))
                return true;
        }

        return false;
    }

    public void RenderStylegroundsIntoBuffers(Level level, bool foreground) {
        foreach (var pair in GetBackdrops(foreground)) {
            string tag = pair.Key;
            var backdrops = pair.Value;

            if (AnyMaskIsInView(level, tag)) {
                // since masked stylegrounds are not in the level's styleground renderers at all,
                // we need to go through the whole update-render cycle here
                DummyBackdropRenderer.Backdrops = backdrops;
                if (!level.Paused)
                    DummyBackdropRenderer.Update(level);
                DummyBackdropRenderer.BeforeRender(level);

                Engine.Graphics.GraphicsDevice.SetRenderTarget(GetBuffer(tag, foreground));
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                DummyBackdropRenderer.Render(level);
            }
        }
    }

    public void RenderWith(Scene scene, bool fg, bool behind = false, bool skipBuffers = false) {
        Foreground = fg;
        Behind = behind;
        SkipBuffers = skipBuffers;
        Render(scene);
    }

    private bool ShouldRenderMask(StylegroundMask mask) {
        return !mask.EntityRenderer && (!Foreground || mask.BehindForeground == Behind) && mask.IsVisible();
    }

    public override void Render(Scene scene) {
        var level = scene as Level;

        if (!SkipBuffers) {
            var lastTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
            RenderStylegroundsIntoBuffers(level, Foreground);
            Engine.Graphics.GraphicsDevice.SetRenderTargets(lastTargets);
        }

        var bufferDict = GetBuffers(Foreground);
        if (bufferDict.Count == 0)
            return;

        // fixes bug where custom fade masks would be rendered white if simplified graphics is enabled with CelesteTAS
        if (StyleMaskModule.CelesteTASLoaded && CelesteTASCompat.SimplifiedBackdrop)
            return;

        var backdrops = GetBackdrops(Foreground);

        var masks = scene.Tracker.GetEntities<StylegroundMask>().Cast<StylegroundMask>().Where(ShouldRenderMask);
        var fadeMasks = masks.Where(mask => mask.Fade == Mask.FadeType.Custom);
        var batchMasks = masks.Where(mask => mask.Fade != Mask.FadeType.Custom);

        if (fadeMasks.Any()) {
            var targets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
            
            foreach (var bufferPair in bufferDict) {
                var tag = bufferPair.Key;
                var buffer = bufferPair.Value;

                var drawing = false;

                foreach (var mask in GetMasksWithTag(level, tag)) {
                    if (mask.Fade != Mask.FadeType.Custom || !ShouldRenderMask(mask))
                        continue;

                    if (!drawing) {
                        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.TempA);
                        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

                        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);

                        drawing = true;
                    }

                    mask.DrawFadeMask();
                }

                if (drawing) {
                    Draw.SpriteBatch.End();

                    Engine.Graphics.GraphicsDevice.SetRenderTargets(targets);

                    Engine.Graphics.GraphicsDevice.Textures[1] = buffer;

                    Draw.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, StyleMaskModule.MaskEffect, level.Camera.Matrix);
                    Draw.SpriteBatch.Draw(GameplayBuffers.TempA, level.Camera.Position, Color.White);
                    Draw.SpriteBatch.End();
                }
            }
        }

        if (batchMasks.Any()) {
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, level.Camera.Matrix);
            foreach (var mask in batchMasks) {
                foreach (var tag in mask.RenderTags) {
                    if (bufferDict.TryGetValue(tag, out var buffer)) {
                        foreach (var slice in mask.GetMaskSlices()) {
                            Draw.SpriteBatch.Draw(buffer, slice.Position, slice.Source, Color.White * slice.GetValue(mask.AlphaFrom, mask.AlphaTo));
                        }
                    }
                }
            }
            Draw.SpriteBatch.End();
        }
    }

    public static void RenderHeatWaveDisplacement(Level level) {
        var maskRenderer = GetRendererInLevel(level);

        foreach (var backdropEntry in maskRenderer.FGBackdrops) {
            var tag = backdropEntry.Key;
            var backdrops = backdropEntry.Value;

            if (!backdrops.Any(backdrop => backdrop is HeatWave heatWave && heatWave.heat > 0f))
                continue;

            foreach (var mask in maskRenderer.GetMasksWithTag(level, tag)) {
                foreach (var slice in mask.GetMaskSlices()) {
                    Draw.Rect(slice.Position.X, slice.Position.Y, slice.Source.Width, slice.Source.Height, new Color(0.5f, 0.5f, 0.1f, 1f));
                }
            }
        }
    }

    #region Hooks
    public static void Load() {
        On.Celeste.Level.LoadLevel += Level_LoadLevel;
        IL.Celeste.Level.Render += Level_Render;
        On.Celeste.Level.End += Level_End;
        On.Celeste.HeatWave.Update += HeatWave_Update;
    }

    public static void Unload() {
        On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        IL.Celeste.Level.Render -= Level_Render;
        On.Celeste.Level.End -= Level_End;
        On.Celeste.HeatWave.Update -= HeatWave_Update;
    }

    private static void HeatWave_Update(On.Celeste.HeatWave.orig_Update orig, HeatWave self, Scene scene) {
        if (self.Tags.Any(tag => tag.StartsWith(TagPrefix))) {
            Level level = scene as Level;
            var lastColorGrade = level.lastColorGrade;
            var colorGradeEase = level.colorGradeEase;
            var colorGradeEaseSpeed = level.colorGradeEaseSpeed;
            var colorGrade = level.Session.ColorGrade;
            orig(self, scene);
            level.lastColorGrade = lastColorGrade;
            level.colorGradeEase = colorGradeEase;
            level.colorGradeEaseSpeed = colorGradeEaseSpeed;
            level.Session.ColorGrade = colorGrade;
        } else {
            orig(self, scene);
        }
    }

    private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(self, playerIntro, isFromLoader);

        if (isFromLoader) {
            var renderer = new StylegroundMaskRenderer();
            DynamicData.For(self).Set(DynDataRendererName, renderer);
            self.Add(renderer);
            renderer.ConsumeStylegrounds(self);
        }

        GetRendererInLevel(self).Masks.Clear();
    }

    private static void Level_End(On.Celeste.Level.orig_End orig, Level self) {
        orig(self);
        UnloadBuffers();
    }

    private static void Level_Render(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        while (cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Level>("Background"),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchCallvirt<Renderer>("Render"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Level>>((level) => {
                GetRendererInLevel(level)?.RenderWith(level, false);
            });
        }

        cursor.Index = 0;

        while (cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Level>("Foreground"),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchCallvirt<Renderer>("Render"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Level>>((level) => {
                GetRendererInLevel(level)?.RenderWith(level, true, behind: true);
            });

            cursor.Index += 4;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Level>>((level) => {
                GetRendererInLevel(level)?.RenderWith(level, true, behind: false, skipBuffers: true);
            });
        }
    }
    #endregion
}