
using Monocle;

namespace Celeste.Mod.StyleMaskHelper.Compat;

public static class ZoomOutCompat {
    public static int BufferWidth => GameplayBuffers.Gameplay?.Width ?? 320;
    public static int BufferHeight => GameplayBuffers.Gameplay?.Height ?? 180;

    /// <summary>
    /// Checks if a VirtualRenderTarget has the same dimensions as the gameplay buffer, and resizes it to match them if it doesn't.
    /// </summary>
    /// <param name="target">A VirtualRenderTarget to ensure the dimensions of.</param>
    /// <returns>The VirtualRenderTarget with its dimensions ensured to be the same as the gameplay buffer, for easier use when passing a buffer into GraphicsDevice.SetRenderTarget.</returns>
    public static VirtualRenderTarget EnsureBufferDimensions(VirtualRenderTarget target) {
        if (target is { IsDisposed: false } && (target.Width != BufferWidth || target.Height != BufferHeight)) {
            target.Width = BufferWidth;
            target.Height = BufferHeight;
            target.Reload();
        }

        return target;
    }
}