using TAS.Module;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public class CelesteTASCompat {

    public static bool SimplifiedBackdrop => CelesteTasSettings.Instance.SimplifiedGraphics && CelesteTasSettings.Instance.SimplifiedBackdrop;

    public static bool SimplifiedLighting => CelesteTasSettings.Instance.SimplifiedGraphics && CelesteTasSettings.Instance.SimplifiedLighting.HasValue;

    public static bool SimplifiedBloom => CelesteTasSettings.Instance.SimplifiedGraphics &&
        (CelesteTasSettings.Instance.SimplifiedBloomBase.HasValue || CelesteTasSettings.Instance.SimplifiedBloomStrength.HasValue);
}
