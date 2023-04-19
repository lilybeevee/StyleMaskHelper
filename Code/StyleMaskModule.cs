using Celeste.Mod.StyleMaskHelper.Masks;

namespace Celeste.Mod.StyleMaskHelper;

public class StyleMaskModule : EverestModule {

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
