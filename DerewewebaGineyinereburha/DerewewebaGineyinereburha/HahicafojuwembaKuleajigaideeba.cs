using System;

namespace DerewewebaGineyinereburha
{
    class HahicafojuwembaKuleajigaideeba
    {
        public uint Width { get; }
        public uint Height { get; }

        /// <inheritdoc />
        public HahicafojuwembaKuleajigaideeba(uint width, uint height)
        {
            Width = width;
            Height = height;
            World = new bool[width * height];
        }

        public void Build()
        {
            // 10
            for (int j = 0; j < Height; j++)
            {
                var line = World.AsSpan((int) (j * Width), (int) Width);
                for (int i = 0; i < Width; i++)
                {
                    if(Ran.Next(3)==1)
                    {
                        line[i] = true;
                    }
                }
            }
            
        }

        public void Proc()
        {

        }

        public bool[] World { get; }

        private static readonly Random Ran = new Random();
    }
}