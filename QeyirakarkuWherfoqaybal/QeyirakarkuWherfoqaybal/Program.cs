using System;
using System.Linq;
using ComputeSharp;

namespace QeyirakarkuWherfoqaybal
{
    class Program
    {
        static void Main(string[] args)
        {
            // Allocate a writeable buffer on the GPU, with the contents of the array
            ReadWriteBuffer<float> buffer = Gpu.Default.AllocateReadWriteBuffer<float>(1000);

            // Run the shader
            Gpu.Default.For(1000, new MyShader(buffer));

            // Get the data back
            float[] array = buffer.GetData();

            Console.WriteLine(string.Join(",", array.Select(temp => temp.ToString())));
        }
    }

    public readonly struct MyShader : IComputeShader
    {
        // 这是特意的命名，请不要更改
        public readonly ReadWriteBuffer<float> buffer;

        public MyShader(ReadWriteBuffer<float> buffer)
        {
            this.buffer = buffer;
        }

        public void Execute(ThreadIds ids)
        {
            buffer[ids.X] = ids.X;
        }
    }
}
