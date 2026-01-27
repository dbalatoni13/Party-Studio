using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GCNRenderLibrary.Rendering
{
    public class GLSamplerObject
    {
        public TextureWrapMode WrapU = TextureWrapMode.Repeat;
        public TextureWrapMode WrapV = TextureWrapMode.Repeat;

        public TextureMagFilter MagFilter = TextureMagFilter.Linear;
        public TextureMinFilter MinFilter = TextureMinFilter.Linear;

        public TextureTarget TextureTarget = TextureTarget.Texture2D;

        public int ID { get; set; }

        public string Name { get; set; }

        public GLSamplerObject()
        {

        }

        public GLSamplerObject(GLGXTexture texture)
        {
            Name = texture.Name;
            ID = texture.ID;
            TextureTarget = texture.TextureTarget;
        }

        public void Render()
        {
            GL.BindTexture(TextureTarget, ID);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)MagFilter);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)MinFilter);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)WrapU);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)WrapV);
        }

        public void Dispose()
        {
            GL.DeleteTexture(ID);
        }
    }
}
