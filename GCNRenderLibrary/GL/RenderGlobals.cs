using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Global data used for controlling render options.
    /// </summary>
    public class RenderGlobals
    {
        public static GXDebugShading DebugShadingMode = GXDebugShading.Default;

        public static bool MeshPicking = true;

        public static float Bightness = 1.0f;
    }
}
