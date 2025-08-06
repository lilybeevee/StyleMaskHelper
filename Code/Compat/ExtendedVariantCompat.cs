using ExtendedVariants.Module;
using ExtendedVariants.Variants;
using Microsoft.Xna.Framework;
using Monocle;
using static ExtendedVariants.Module.ExtendedVariantsModule;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public class ExtendedVariantCompat {
    public static bool UpsideDown => (bool) ExtendedVariantsModule.Instance.TriggerManager.GetCurrentVariantValue(Variant.UpsideDown);
}
