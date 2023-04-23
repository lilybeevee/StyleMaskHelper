using Celeste.Mod.StyleMaskHelper.Entities;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Linq;

namespace Celeste.Mod.StyleMaskHelper.Effects;

public class HeatWaveOneMode : HeatWave {

    public Session.CoreModes CoreMode;
    public bool ColorGrade;

    public bool IsMasked => Tags != null && Tags.Any(tag => tag.StartsWith(StylegroundMaskRenderer.TagPrefix));
    
    public HeatWaveOneMode(Session.CoreModes coreMode, bool coreColorGrade) {
        CoreMode = coreMode;
        ColorGrade = coreColorGrade;
    }

    public override void Update(Scene scene) {
        var level = scene as Level;
        var masked = IsMasked;

        if (!ColorGrade && !masked) {
            var lastColorGrade = level.lastColorGrade;
            var colorGradeEase = level.colorGradeEase;
            var colorGradeEaseSpeed = level.colorGradeEaseSpeed;
            var colorGrade = level.Session.ColorGrade;

            base.Update(scene);

            level.lastColorGrade = lastColorGrade;
            level.colorGradeEase = colorGradeEase;
            level.colorGradeEaseSpeed = colorGradeEaseSpeed;
            level.Session.ColorGrade = colorGrade;
        } else {
            base.Update(scene);
        }

        var renderer = StylegroundMaskRenderer.GetRendererInLevel(level);

        if (!masked || renderer == null)
            return;

        var maxHeat = 0f;

        foreach (var backdropList in renderer.FGBackdrops.Values) {
            foreach (var backdrop in backdropList) {
                if (backdrop is not HeatWave heatWave)
                    continue;
                maxHeat = Math.Max(maxHeat, heatWave.heat);
            }
        }
        foreach (var backdropList in renderer.BGBackdrops.Values) {
            foreach (var backdrop in backdropList) {
                if (backdrop is not HeatWave heatWave)
                    continue;
                maxHeat = Math.Max(maxHeat, heatWave.heat);
            }
        }

        if (maxHeat > 0f) {
            Distort.WaterSineDirection = -1f;
            Distort.WaterAlpha = maxHeat * 0.5f;
        } else {
            Distort.WaterSineDirection = 1f;
            Distort.WaterAlpha = 1f;
        }
    }


    public static void Load() {
        IL.Celeste.HeatWave.Update += HeatWave_Update;
    }

    public static void Unload() {
        IL.Celeste.HeatWave.Update -= HeatWave_Update;
    }

    private static void HeatWave_Update(ILContext il) {
        var cursor = new ILCursor(il);

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<Level>("get_CoreMode"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Session.CoreModes, HeatWave, Session.CoreModes>>((coreMode, self) =>
                self is HeatWaveOneMode heatWaveOneMode ? heatWaveOneMode.CoreMode : coreMode);
        }
    }
}
