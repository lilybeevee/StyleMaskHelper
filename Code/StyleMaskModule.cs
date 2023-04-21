using Celeste.Mod.StyleMaskHelper.Entities;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;

namespace Celeste.Mod.StyleMaskHelper;

public class StyleMaskModule : EverestModule {

    public static bool MaddieHelpingHandLoaded { get; private set; }
    public static bool CelesteTASLoaded { get; private set; }

    public static Effect MaskEffect { get; private set; }

    public override void Initialize() {
        base.Initialize();

        MaddieHelpingHandLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
            Name = "MaxHelpingHand",
            Version = new Version(1, 24, 10)
        });
        CelesteTASLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
            Name = "CelesteTAS",
            Version = new Version(3, 25, 9)
        });
    }

    public override void LoadContent(bool firstLoad) {
        if (!firstLoad) return;

        var maskEffectAsset = Everest.Content.Get("Effects/StyleMaskHelper/Mask.cso");

        MaskEffect = new Effect(Engine.Graphics.GraphicsDevice, maskEffectAsset.Data);
    }

    public override void Load() {
        BloomMask.Load();
        StylegroundMaskRenderer.Load();
        LightingMask.Load();
        ColorGradeMask.Load();
    }

    public override void Unload() {
        BloomMask.Unload();
        StylegroundMaskRenderer.Unload();
        LightingMask.Unload();
        ColorGradeMask.Unload();
    }
}
