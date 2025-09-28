using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace MystIVAssetExplorer.Skybox;

partial class SkyboxControl
{
    private sealed class CheckerboardDrawOperation : ICustomDrawOperation
    {
        private readonly SkyboxControl owner;
        private readonly SKBitmap checkerboardTileBitmap;
        private readonly SKShader checkerboardShader;
        private readonly SKPaint checkerboardPaint;

        public CheckerboardDrawOperation(SkyboxControl owner)
        {
            this.owner = owner;

            checkerboardTileBitmap = new SKBitmap(2, 2);
            checkerboardTileBitmap.SetPixel(0, 0, new SKColor(0x55, 0x55, 0x55));
            checkerboardTileBitmap.SetPixel(1, 0, new SKColor(0x33, 0x33, 0x33));
            checkerboardTileBitmap.SetPixel(0, 1, new SKColor(0x33, 0x33, 0x33));
            checkerboardTileBitmap.SetPixel(1, 1, new SKColor(0x55, 0x55, 0x55));

            checkerboardShader = SKShader.CreateBitmap(checkerboardTileBitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, SKMatrix.CreateScale(8, 8));
            checkerboardPaint = new SKPaint { Shader = checkerboardShader };
        }

        public Rect Bounds => new(owner.Bounds.Size);

        public void Render(ImmediateDrawingContext context)
        {
            using var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()!.Lease();
            var canvas = lease.SkCanvas;

            canvas.DrawRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height), checkerboardPaint);
        }

        public void Dispose()
        {
            checkerboardPaint.Dispose();
            checkerboardShader.Dispose();
            checkerboardTileBitmap.Dispose();
        }

        bool IEquatable<ICustomDrawOperation>.Equals(ICustomDrawOperation? other) => other == this;

        bool ICustomDrawOperation.HitTest(Point p) => Bounds.Contains(p);
    }
}
