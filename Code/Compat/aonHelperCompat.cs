using Celeste.Mod.StyleMaskHelper.Entities;
using Monocle;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public static class aonHelperCompat {

    /// <summary>
    /// Retrieves the bloom tag of the current Fg Stryleground Bloom Controller.
    /// </summary>
    /// <param name="level">The current <see cref="Level"/> instance to use.</param>
    /// <returns>The current controller's bloom tag, or null if it is either empty or no controller is present.</returns>
    public static string GetCurrentFgStylegroundBloomTag(Level level) => FgStylegroundBloomControllerCompat.GetCurrentBloomTag(level);

    public static void Initialize() {
        typeof(FgStylegroundBloomControllerCompat).ModInterop();

        FgStylegroundBloomControllerCompat.AddBeforeForegroundRenderAction(StylegroundMaskRenderer.aonHelperCompat_BeforeRender);
        FgStylegroundBloomControllerCompat.AddAfterForegroundRenderAction(StylegroundMaskRenderer.aonHelperCompat_AfterRender);
    }

    public static void Uninitialize() {
        FgStylegroundBloomControllerCompat.RemoveBeforeForegroundRenderAction(StylegroundMaskRenderer.aonHelperCompat_BeforeRender);
        FgStylegroundBloomControllerCompat.RemoveAfterForegroundRenderAction(StylegroundMaskRenderer.aonHelperCompat_AfterRender);
    }

    [ModImportName("aonHelper.FgStylegroundBloomControllerCompat")]
    public static class FgStylegroundBloomControllerCompat {
        public static Action<Action<Level, bool>> AddBeforeForegroundRenderAction;
        public static Action<Action<Level, bool>> AddAfterForegroundRenderAction;
        public static Action<Action<Level, bool>> RemoveBeforeForegroundRenderAction;
        public static Action<Action<Level, bool>> RemoveAfterForegroundRenderAction;
        public static Func<Level, string> GetCurrentBloomTag;
    }
}
