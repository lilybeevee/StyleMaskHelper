using Celeste.Mod.StyleMaskHelper.Entities;
using Monocle;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public static class aonHelperCompat {
    public static void Initialize() {
        typeof(TaggedFgStylegroundBloomRenderCompat).ModInterop();

        TaggedFgStylegroundBloomRenderCompat.AddBeforeForegroundRenderAction?.Invoke(StylegroundMaskRenderer.aonHelperCompat_RenderBefore);
        TaggedFgStylegroundBloomRenderCompat.AddAfterForegroundRenderAction?.Invoke(StylegroundMaskRenderer.aonHelperCompat_RenderAfter);
    }

    public static void Uninitialize() {
        TaggedFgStylegroundBloomRenderCompat.RemoveBeforeForegroundRenderAction?.Invoke(StylegroundMaskRenderer.aonHelperCompat_RenderBefore);
        TaggedFgStylegroundBloomRenderCompat.RemoveAfterForegroundRenderAction?.Invoke(StylegroundMaskRenderer.aonHelperCompat_RenderAfter);
    }

    [ModImportName("aonHelper.TaggedFgStylegroundBloomRenderCompat")]
    public static class TaggedFgStylegroundBloomRenderCompat {
        public static Action<Action<Level, bool>> AddBeforeForegroundRenderAction;
        public static Action<Action<Level, bool>> AddAfterForegroundRenderAction;
        public static Action<Action<Level, bool>> RemoveBeforeForegroundRenderAction;
        public static Action<Action<Level, bool>> RemoveAfterForegroundRenderAction;
        public static Func<Level, string> GetCurrentBloomTag;
    }
}
