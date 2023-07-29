using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;

namespace Celeste.Mod.StyleMaskHelper;

public class StylegroundLightingHandler {

    public const string BufferName = "StyleMaskHelper_StylegroundLightingHandler_buffer";

    public static VirtualRenderTarget Buffer;

    public static void Load() {
        On.Celeste.GameplayBuffers.Create += GameplayBuffers_Create;
        IL.Celeste.BackdropRenderer.Render += BackdropRenderer_Render;
    }

    public static void Unload() {
        On.Celeste.GameplayBuffers.Create -= GameplayBuffers_Create;
        IL.Celeste.BackdropRenderer.Render -= BackdropRenderer_Render;
    }

    private static void GameplayBuffers_Create(On.Celeste.GameplayBuffers.orig_Create orig) {
        orig();
        Buffer?.Dispose();
        Buffer = VirtualContent.CreateRenderTarget(BufferName, 320, 180);
    }

    private static void BackdropRenderer_Render(ILContext il) {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdfld<Backdrop>("Visible"))) {
            Logger.Log(LogLevel.Error, "StyleMaskHelper/StylegroundLightingHandler", $"Failed to hook BackdropRenderer.Render - Styleground Lighting disabled");
            return;
        }

        cursor.Emit(OpCodes.Dup);
        cursor.Index++;
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);

        cursor.EmitDelegate<Func<Backdrop, bool, BackdropRenderer, Scene, bool>>((backdrop, visible, renderer, scene) => {
            if (!visible)
                return false;

            if (!backdrop.Tags.Contains("renderlighting"))
                return true;

            if (!(scene is Level level))
                return true; // Probably not possible

            renderer.EndSpritebatch();

            var lastTargets = Engine.Instance.GraphicsDevice.GetRenderTargets();
            Engine.Instance.GraphicsDevice.SetRenderTarget(Buffer);
            Engine.Instance.GraphicsDevice.Clear(Color.Transparent);

            if (backdrop.UseSpritebatch && !renderer.usingSpritebatch) {
                if (backdrop is Parallax parallax) {
                    renderer.StartSpritebatchLooping(parallax.BlendState);
                } else {
                    renderer.StartSpritebatch(BlendState.AlphaBlend);
                }
            }

            backdrop.Render(scene);
            renderer.EndSpritebatch();

            level.Lighting.Render(scene);

            Engine.Instance.GraphicsDevice.SetRenderTargets(lastTargets);

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Draw.SpriteBatch.Draw(Buffer, Vector2.Zero, Color.White);
            Draw.SpriteBatch.End();

            return false;
        });
    }
}
