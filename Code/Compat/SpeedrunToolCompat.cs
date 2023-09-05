using Celeste.Mod.StyleMaskHelper.Entities;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public class SpeedrunToolCompat {

    public static void Initialize() {
        typeof(SaveLoadImports).ModInterop();

        SaveLoadImports.RegisterStaticTypes(typeof(StylegroundMaskRenderer), new string[] { "Instance", "DummyBackdropRenderer" });
    }

    [ModImportName("SpeedrunTool.SaveLoad")]
    private static class SaveLoadImports {
        public static Func<Type, string[], object> RegisterStaticTypes;
    }
}
