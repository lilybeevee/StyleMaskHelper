using ExtendedVariants.Module;
using ExtendedVariants.Variants;
using Microsoft.Xna.Framework;
using Monocle;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public class ExtendedVariantCompat {

    public static float ZoomLevel => (float) ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(Variant.ZoomLevel);

    public static bool UpsideDown => (bool) ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(Variant.UpsideDown);

    // Extended Variant code copied from https://github.com/maddie480/ExtendedVariantMode/blob/750384facbfd83ff6ec7131fe01c34e416bc7d0a/Variants/UpsideDown.cs
    public static void ApplyUpsideDownEffect(ref Vector2 padding, ref Vector2 focus) {
        var zoomLevelVariant = ExtendedVariantsModule.Instance.VariantHandlers[Variant.ZoomLevel] as ZoomLevel;

        padding = zoomLevelVariant.getScreenPosition(padding);

        if (UpsideDown) {
            padding.Y = -padding.Y;
            focus.Y = 90f - (focus.Y - 90f);
        }
    }
}
