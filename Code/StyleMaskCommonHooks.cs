using Celeste.Mod.StyleMaskHelper.Effects;
using Celeste.Mod.StyleMaskHelper.Entities;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;

namespace Celeste.Mod.StyleMaskHelper;

public class StyleMaskCommonHooks {
    
    public static void Load() {
        IL.Celeste.DisplacementRenderer.BeforeRender += DisplacementRenderer_BeforeRender;
    }

    public static void Unload() {
        IL.Celeste.DisplacementRenderer.BeforeRender -= DisplacementRenderer_BeforeRender;
    }

    private static void DisplacementRenderer_BeforeRender(ILContext il) {
        var cursor = new ILCursor(il);

        var getHeatWaveMethod = typeof(BackdropRenderer)
            .GetMethod("Get", BindingFlags.Instance | BindingFlags.Public)
            .MakeGenericMethod(typeof(HeatWave));

        int levelArg = -1;
        if (!cursor.TryGotoNext(MoveType.After,
            instr => instr.MatchLdarg(out levelArg),
            instr => instr.MatchIsinst<Level>(),
            instr => instr.MatchLdfld<Level>("Foreground"),
            instr => instr.MatchCallvirt(getHeatWaveMethod))) {

            Logger.Log("StyleMaskHelper/StylegroundMaskRenderer", $"Failed to find heat wave code in DisplacementRenderer.BeforeRender - Heat Wave displacement disabled");
            return;
        }

        cursor.Emit(OpCodes.Ldarg, levelArg);
        cursor.Emit(OpCodes.Isinst, typeof(Level));
        cursor.EmitDelegate<Func<HeatWave, Level, HeatWave>>((heatWave, level) => {
            HeatWave firstHeatWave = null;

            foreach (var backdrop in level.Foreground.Backdrops) {

                if (backdrop is HeatWaveOneMode heatWaveOneMode) {
                    if (backdrop.Visible)
                        heatWaveOneMode.RenderDisplacement(level);

                } else if (backdrop is HeatWave otherHeatWave && firstHeatWave == null) {
                    firstHeatWave = otherHeatWave;
                }
            }

            return (heatWave is HeatWaveOneMode) ? firstHeatWave : heatWave;
        });

        cursor.Emit(OpCodes.Ldarg, levelArg);
        cursor.Emit(OpCodes.Isinst, typeof(Level));
        cursor.EmitDelegate(StylegroundMaskRenderer.RenderHeatWaveDisplacement);
    }
}
