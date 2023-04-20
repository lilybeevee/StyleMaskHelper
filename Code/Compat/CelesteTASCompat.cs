using TAS.Module;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public class CelesteTASCompat {

    public static bool SimplifiedBackdrop => CelesteTasSettings.Instance.SimplifiedGraphics && CelesteTasSettings.Instance.SimplifiedBackdrop;
}
