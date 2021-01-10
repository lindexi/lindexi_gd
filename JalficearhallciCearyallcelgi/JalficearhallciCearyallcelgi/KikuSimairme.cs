using System;
using System.Drawing;
using SharpDX.Windows;

namespace JalficearhallciCearyallcelgi
{
    class KikuSimairme : IDisposable
    {
        public KikuSimairme()
        {
            _renderForm = new RenderForm
            {
                ClientSize = new Size(Width, Height)
            };
        }

        private const int Width = 1280;

        private const int Height = 720;

        public void Run()
        {
            RenderLoop.Run(_renderForm, RenderCallback);
        }

        private readonly RenderForm _renderForm;

        private void RenderCallback()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _renderForm?.Dispose();
        }
    }
}