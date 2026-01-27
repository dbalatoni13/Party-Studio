using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Represents an opengl state controller for configuring polygonal, depth and blend states.
    /// </summary>
    public class MegaState
    {
        /// <summary>
        /// The cull mode to cull faces. -1 to disable, else cull front face.
        /// </summary>
        public int CullMode; //-1 disabled else CullFrontFace

        /// <summary>
        /// Determines to write depth or not.
        /// </summary>
        public bool DepthWrite = true;

        public BlendingFactor BlendSrcFactor = BlendingFactor.One;
        public BlendingFactor BlendDstFactor = BlendingFactor.Zero;
        public BlendEquationMode BlendMode = BlendEquationMode.FuncAdd;

        public FrontFaceDirection FontFace = FrontFaceDirection.Cw;

        public DepthFunction DepthCompare = DepthFunction.Lequal;

        public PolygonMode PolyMode = PolygonMode.Fill;

        public float PolygonOffsetFactor = 0.0f;
        public float PolygonOffsetUnits = 0.0f;

        /// <summary>
        /// Updates the current GL state with the state settings.
        /// </summary>
        public void SetGLState()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.PolygonOffsetFill);

            GL.BlendFunc(BlendSrcFactor, BlendDstFactor);
            GL.BlendEquation(BlendMode);

            SetGLPolygonState();

            GL.DepthMask(DepthWrite);
        }

        /// <summary>
        /// Updates the GL state of the polygon settings.
        /// </summary>
        public void SetGLPolygonState()
        {
            if (CullMode == -1)
                GL.Disable(EnableCap.CullFace);
            else
            {
                GL.Enable(EnableCap.CullFace);
                GL.CullFace((CullFaceMode)CullMode);
            }
            GL.FrontFace(FontFace);
            GL.PolygonOffset(PolygonOffsetFactor, PolygonOffsetUnits);
        }

        /// <summary>
        /// Updates and resets the GL state using defaults.
        /// </summary>
        public static void SetGLDefaults()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.Disable(EnableCap.Blend);

            GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
            GL.BlendEquation(BlendEquationMode.FuncAdd);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.DepthMask(true);

            GL.PolygonOffset(0, 0);
        }
    }
}
