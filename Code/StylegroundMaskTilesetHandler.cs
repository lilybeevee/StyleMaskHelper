using Celeste.Mod.StyleMaskHelper.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Celeste.Mod.StyleMaskHelper;

// credit to ssm24 for starjump tilesets btw !! main inspiration for this implementation
// https://github.com/SSM240/SSMHelper/blob/master/Source/StarjumpTilesetHelper.cs

public static class StylegroundMaskTilesetHandler {
    private const string LogID = "StyleMaskHelper/StylegroundMaskTilesetHandler"; 

    private static Dictionary<string, string> TilesetToStyleMaskTag = [];
    internal static void GetVisibleTags(Level level, HashSet<string> visibleTags) {
        visibleTags.Clear();
        if (TilesetToStyleMaskTag.Count == 0)
            return;

        var tileGrids = level.Tracker.GetComponentsTrackIfNeeded<TileGrid>().Cast<TileGrid>();
        foreach (var tilegrid in tileGrids) {
            if (!tilegrid.Visible || !tilegrid.Entity.Visible || tilegrid.Alpha <= 0f)
                continue;

            tilegrid.ClipCamera ??= level.Camera;

            var clippedRenderTiles = tilegrid.GetClippedRenderTiles();
            int tileWidth = tilegrid.TileWidth;
            int tileHeight = tilegrid.TileHeight;
            var position = tilegrid.Entity.Position + tilegrid.Position;
            var tilePosition = new Vector2(position.X + clippedRenderTiles.Left * tileWidth, position.Y + clippedRenderTiles.Top * tileHeight);
            for (int i = clippedRenderTiles.Left; i < clippedRenderTiles.Right; i++) {
                for (int j = clippedRenderTiles.Top; j < clippedRenderTiles.Bottom; j++) {
                    var mTexture = tilegrid.Tiles[i, j];
                    if (mTexture is not null && TilesetToStyleMaskTag.TryGetValue(mTexture.Parent.AtlasPath, out var styleMaskTag))
                        visibleTags.Add(styleMaskTag);

                    tilePosition.Y += tileHeight;
                }

                tilePosition.X += tileWidth;
                tilePosition.Y += clippedRenderTiles.Top * tileHeight;
            }
        }
    }

    internal static void Load() {
        On.Celeste.LevelLoader.ctor += On_LevelLoader_ctor;
        On.Celeste.Autotiler.ReadInto += On_Autotiler_ReadInto;
        IL.Monocle.TileGrid.RenderAt += IL_TileGrid_RenderAt;
    }

    internal static void Unload() {
        On.Celeste.LevelLoader.ctor -= On_LevelLoader_ctor;
        On.Celeste.Autotiler.ReadInto -= On_Autotiler_ReadInto;
        IL.Monocle.TileGrid.RenderAt -= IL_TileGrid_RenderAt;
    }

    private static void On_LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
        TilesetToStyleMaskTag.Clear();
        orig(self, session, startPosition);
    }

    private static void On_Autotiler_ReadInto(On.Celeste.Autotiler.orig_ReadInto orig, Autotiler self, object data, Tileset tileset, XmlElement xml) {
        orig(self, data, tileset, xml);

        if (xml.HasAttr("styleMaskHelper_stylegroundTag"))
            TilesetToStyleMaskTag["tilesets/" + xml.Attr("path")] = xml.Attr("styleMaskHelper_stylegroundTag");
    }

    // feels so evil to hook this
    private static void IL_TileGrid_RenderAt(ILContext il) {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<SpriteBatch>(nameof(SpriteBatch.Draw)))) {
            Logger.Error(LogID, "Failed to find SpriteBatch.Draw call in tileset rendering!");
            return;
        }

        cursor.Emit(OpCodes.Ldloc_S, (byte)5); // position2
        cursor.Emit(OpCodes.Ldloc_S, (byte)2); // tileWidth
        cursor.Emit(OpCodes.Ldloc_S, (byte)3); // tileHeight
        cursor.Emit(OpCodes.Ldloc_S, (byte)4); // color
        cursor.Emit(OpCodes.Ldloc_S, (byte)8); // mtexture
        cursor.EmitLdarg0(); // tilegrid (for clip camera)
        cursor.EmitDelegate(drawStylemasks);

        static void drawStylemasks(Vector2 position, int tileWidth, int tileHeight, Color color, MTexture mTexture, TileGrid tileGrid) {
            var tilesetPath = mTexture.Parent.AtlasPath;
            if (!TilesetToStyleMaskTag.TryGetValue(tilesetPath, out var styleMaskTag))
                return;

            var camera = tileGrid.ClipCamera;
            var bufferDict = StylegroundMaskRenderer.GetBuffers(foreground: false);
            if (bufferDict.TryGetValue(styleMaskTag, out var buffer)) {
                Draw.SpriteBatch.Draw(buffer, position, new Rectangle((int)(position.X - camera.X), (int)(position.Y - camera.Y), tileWidth, tileHeight), color);
            }
        }
    }
}