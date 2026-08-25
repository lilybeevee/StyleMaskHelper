using Microsoft.Xna.Framework;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public static class FrostHelperCompat {
    public static Color GetFrostHelperBloomColor() => FrostHelperImports.GetBloomColor?.Invoke() ?? Color.White;
    
    public static void Initialize() {
        typeof(FrostHelperImports).ModInterop();
    }

    [ModImportName("FrostHelper")]
    private static class FrostHelperImports {
        public static Func<Color> GetBloomColor;
    }
}