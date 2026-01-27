using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using GCNRenderLibrary;

namespace GCNRenderLibrary.Rendering
{
    /// <summary>
    /// Texture matrix calculation helper.
    /// </summary>
    public class TexMtx
    {
        public static Matrix4 computeTexSrt(Vector2 scale, float rotate, Vector2 translate, MatrixMode modelTransform)
        {
            Matrix4 matrix = Matrix4.Identity;
            switch (modelTransform)
            {
                case MatrixMode.Max:     return CalcTexMtx_Max(scale, rotate, translate);
                case MatrixMode.Maya:    return CalcTexMtx_Maya(scale, rotate, translate);
                case MatrixMode.XSI:     return CalcTexMtx_XSI(scale, rotate, translate);
                case MatrixMode.Default: return CalcTexMtx_Basic(scale, rotate, translate);
            }
            return matrix;
        }

        static bool IsScaleUniform(Matrix4 mat)
        {
            var scale = mat.ExtractScale();
            return scale.X == scale.Y && scale.Y == scale.Z;
        }

        public static Matrix4 computeNormalMatrix(Matrix4 dst, bool isUniformScale = false)
        {
            //Remove position
            dst[3, 0] = 0;
            dst[3, 1] = 0;
            dst[3, 2] = 0;
            if (!isUniformScale && dst.Determinant != 0)
            {
                dst = Matrix4.Invert(dst);
                dst = Matrix4.Transpose(dst);
            }
            return dst;
        }

        static Matrix4 computeEnvMatrix(float scaleS, float scaleT, float transS, float transT)
        {
            Matrix4 dst = Matrix4.Identity;
            dst[0, 0] = scaleS;
            dst[1, 0] = 0;
            dst[2, 0] = 0;
            dst[3, 0] = transS;

            dst[0, 1] = 0;
            dst[1, 1] = -scaleS;
            dst[2, 1] = 0;
            dst[3, 1] = transT;

            dst[0, 2] = 0;
            dst[1, 2] = 0;
            dst[2, 2] = 0;
            dst[3, 2] = 1.0f;

            return dst;
        }

        static Matrix4 computeInMtx(Matrix4 model, Matrix4 mvp,
            TextureMapMode method)
        {
            //TODO
            Matrix4 matrix = Matrix4.Identity;
        
            return matrix;
        }

        public static Matrix4 computeTexMtx(Matrix4 model, Matrix4 mtxProj, 
            Matrix4 texSRT, Matrix4 effectMatrix,
            TextureMapMode method, CommonMappingOption option)
        {
            bool flipY = false;
            float flipYScale = flipY ? -1.0f : 1.0f;
            var dstPost = texSRT;

            //TODO more modes
            switch (method)
            {
                case TextureMapMode.EnvLight:
                case TextureMapMode.EnvSpec:
                    break;
                case TextureMapMode.TextureCoordinates:
                    return texSRT;
                case TextureMapMode.EnvCamera:
                    dstPost = computeEnvMatrix(0.5f, 0.5f * flipYScale, 0.5f, 0.5f);
                    // Apply effect matrix.
                    dstPost = dstPost * effectMatrix;
                    break;
                case TextureMapMode.Projection:
                    dstPost = mtxProj * Matrix4.CreateScale(0.5f) * Matrix4.CreateTranslation(0.5f, -0.5f * flipYScale, 0.5f);
                    // Apply effect matrix.
                    dstPost = dstPost * effectMatrix;
                    break;
            }

            // Calculate SRT.
            dstPost = dstPost * texSRT;

            return dstPost;
        }

        static Matrix4 CalcTexMtx_Max(Vector2 scale, float rotate, Vector2 translate)
        {
            var theta = MathHelper.DegreesToRadians(rotate);
            var sinR = MathF.Sin(theta);
            var cosR = MathF.Cos(theta);

            Matrix4 dst = Matrix4.Identity;
            dst[0, 0] = scale.X * cosR;
            dst[1, 0] = scale.X * sinR;
            dst[2, 0] = 0.0f;
            dst[3, 0] = -scale.X * cosR * (translate.X + 0.5f) +
                         scale.X * sinR * (translate.Y - 0.5f) + 0.5f;

            dst[0, 1] = -scale.Y * sinR;
            dst[1, 1] = scale.Y * cosR;
            dst[2, 1] = 0.0f;
            dst[3, 1] = scale.Y * sinR * (translate.X + 0.5f) +
                        scale.X * cosR * (translate.Y - 0.5f) + 0.5f;
            return dst;
        }

        static Matrix4 CalcTexMtx_Maya(Vector2 scale, float rotate, Vector2 translate)
        {
            var theta = MathHelper.DegreesToRadians(rotate);
            var sinR = MathF.Sin(theta);
            var cosR = MathF.Cos(theta);

            Matrix4 dst = Matrix4.Identity;
            dst[0, 0] = scale.X * cosR;
            dst[1, 0] = scale.X * sinR;
            dst[2, 0] = 0.0f;
            dst[3, 0] = scale.X * (-0.5f * sinR - 0.5f * cosR + 0.5f - translate.X);

            dst[0, 1] = -scale.Y * sinR;
            dst[1, 1] = scale.Y * cosR;
            dst[2, 1] = 0.0f;
            dst[3, 1] = scale.Y * (0.5f * sinR - 0.5f * cosR - 0.5f + translate.Y) + 1.0f;
            return dst;
        }

        static Matrix4 CalcTexMtx_XSI(Vector2 scale, float rotate, Vector2 translate)
        {
            var theta = MathHelper.DegreesToRadians(rotate);
            var sinR = MathF.Sin(theta);
            var cosR = MathF.Cos(theta);

            Matrix4 dst = Matrix4.Identity;
            dst[0, 0] = scale.X * cosR;
            dst[1, 0] = -scale.X * sinR;
            dst[3, 0] = (scale.X * sinR) - (scale.X * cosR * translate.X) -
                         scale.X * sinR * translate.Y;

            dst[0, 1] = scale.Y * sinR;
            dst[1, 1] = scale.Y * cosR;
            dst[3, 1] = (scale.Y * -cosR) - (scale.Y * cosR * translate.X) +
                         (scale.Y * cosR * translate.Y) + 1.0f;
            return dst;
        }

        static Matrix4 CalcTexMtx_Basic(Vector2 scale, float rotate, Vector2 translate)
        {
            var theta = MathHelper.DegreesToRadians(rotate);
            var sinR = MathF.Sin(theta);
            var cosR = MathF.Cos(theta);

            Matrix4 dst = Matrix4.Identity;
            dst[0, 0] = scale.X * cosR;
            dst[1, 0] = scale.Y * -sinR;
            dst[2, 0] = translate.X;

            dst[1, 0] = scale.X * sinR;
            dst[2, 0] = scale.Y * cosR;
            dst[3, 0] = translate.Y;
            return dst;
        }
    }
}
