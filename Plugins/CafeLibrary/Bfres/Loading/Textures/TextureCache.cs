using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using Toolbox.Core;
using GLFrameworkEngine;

namespace CafeLibrary.Rendering
{
    public class TextureCache
    {
        public static bool CacheTexturesToDisk = false;

        public static string TextueCacheFolder = "TextueCache";

        public static bool HasTextueCached(string dir, string name)
        {
            return File.Exists($"{TextueCacheFolder}/{dir}/{name}.png");
        }

        public static void SaveTextureToDisk(string dir, STGenericTexture texture)
        {
            if (!Directory.Exists($"{TextueCacheFolder}/{dir}"))
                Directory.CreateDirectory($"{TextueCacheFolder}/{dir}");

            texture.SaveBitmap($"{TextueCacheFolder}/{dir}/{texture.Name}.png", new TextureExportSettings());
        }
    }
}
