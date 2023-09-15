using Celeste.Mod.StyleMaskHelper.Compat;
using Celeste.Mod.StyleMaskHelper.Effects;
using Celeste.Mod.StyleMaskHelper.Entities;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;

namespace Celeste.Mod.StyleMaskHelper;

public class StyleMaskModule : EverestModule {

    public static bool MaddieHelpingHandLoaded { get; private set; }
    public static bool CelesteTASLoaded { get; private set; }
    public static bool SpeedrunToolLoaded { get; private set; }
    public static bool ExtendedVariantsLoaded { get; private set; }

    public static Effect MaskEffect { get; private set; }
    public static Effect StrengthMask { get; private set; }
    public static Effect CustomFadeRange { get; private set; }

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
        SpeedrunToolLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
            Name = "SpeedrunTool",
            Version = new Version(3, 21, 0)
        });
        ExtendedVariantsLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
            Name = "ExtendedVariantMode",
            Version = new Version(0, 28, 1)
        });

        if (SpeedrunToolLoaded)
            SpeedrunToolCompat.Initialize();
    }

    public override void LoadContent(bool firstLoad) {
        if (!firstLoad) return;

        var maskEffectAsset = Everest.Content.Get("Effects/StyleMaskHelper/Mask.cso");
        var strengthMaskAsset = Everest.Content.Get("Effects/StyleMaskHelper/StrengthMask.cso");
        var customFadeRangeAsset = Everest.Content.Get("Effects/StyleMaskHelper/CustomFadeRange.cso");

        MaskEffect = new Effect(Engine.Graphics.GraphicsDevice, maskEffectAsset.Data);
        StrengthMask = new Effect(Engine.Graphics.GraphicsDevice, strengthMaskAsset.Data);
        CustomFadeRange = new Effect(Engine.Graphics.GraphicsDevice, customFadeRangeAsset.Data);
    }

    public override void Load() {
        StyleMaskCommonHooks.Load();
        StylegroundLightingHandler.Load();

        BloomMask.Load();
        StylegroundMaskRenderer.Load();
        LightingMask.Load();
        ColorGradeMask.Load();

        HeatWaveOneMode.Load();

        Everest.Events.Level.OnLoadBackdrop += OnLoadBackdrop;
    }

    public override void Unload() {
        StyleMaskCommonHooks.Unload();
        StylegroundLightingHandler.Unload();

        BloomMask.Unload();
        StylegroundMaskRenderer.Unload();
        LightingMask.Unload();
        ColorGradeMask.Unload();

        HeatWaveOneMode.Unload();

        Everest.Events.Level.OnLoadBackdrop -= OnLoadBackdrop;
    }

    private Backdrop OnLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above) {
        if (child.Name.Equals("StyleMaskHelper/HeatWaveOneMode", StringComparison.OrdinalIgnoreCase))
            return new HeatWaveOneMode((Session.CoreModes)Enum.Parse(typeof(Session.CoreModes), child.Attr("coreMode", "None")), child.AttrBool("colorGrade"));

        return null;
    }
}