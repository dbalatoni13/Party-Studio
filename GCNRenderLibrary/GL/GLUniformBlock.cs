using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace GCNRenderLibrary
{
    internal class GLUniformBlock
    {
        public BufferUsageHint Usage = BufferUsageHint.StaticDraw;

        private int BindingPoint;
        private int Size;

        private int ID;

        public GLUniformBlock(int binding, int size)
        {
            BindingPoint = binding;
            Size = size;
            ID = GL.GenBuffer();
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, ID);
        }

        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        public void BindBlock(int shaderID, string uniformBlockName)
        {
            var blockIndex = GL.GetUniformBlockIndex(shaderID, uniformBlockName);
            if (blockIndex == -1)
                return;

            GL.UniformBlockBinding(shaderID, blockIndex, BindingPoint);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, BindingPoint, ID);
        }

        public void Update<T>(T[] data, int offset, int size) where T : struct
        {
            Bind();
            GL.BufferSubData<T>(BufferTarget.UniformBuffer, (IntPtr)offset, data.Length * size, data);
            Unbind();
        }

        public void Update<T>(T data, int offset, int size) where T : struct
        {
            Bind();
            GL.BufferSubData<T>(BufferTarget.UniformBuffer, (IntPtr)offset, size, ref data);
            Unbind();
        }

        public void Update<T>(T data) where T : struct
        {
            Bind();
            GL.BufferData<T>(BufferTarget.UniformBuffer, Size, ref data, Usage);
            Unbind();
        }

        public void Int()
        {
            Bind();
            GL.BufferData(BufferTarget.UniformBuffer, Size, IntPtr.Zero, Usage);
            Unbind();
        }

        public void Dispose()
        {
            GL.DeleteBuffer(this.ID);
        }
    }
}
