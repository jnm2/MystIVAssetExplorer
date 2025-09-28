using MystIVAssetExplorer.Formats;
using MystIVAssetExplorer.ViewModels;
using SkiaSharp;
using System;
using System.Linq;

namespace MystIVAssetExplorer.Skybox;

public sealed class SkyboxModel : IDisposable
{
    public SKBitmap? Front { get; init; }
    public SKBitmap? Back { get; init; }
    public SKBitmap? Left { get; init; }
    public SKBitmap? Right { get; init; }
    public SKBitmap? Top { get; init; }
    public SKBitmap? Bottom { get; init; }

    public void Dispose()
    {
        Front?.Dispose();
        Back?.Dispose();
        Left?.Dispose();
        Right?.Dispose();
        Top?.Dispose();
        Bottom?.Dispose();
    }

    public static SkyboxModel FromImagesFolder(AssetBrowserNode imagesFolder)
    {
        var cubeDscEntry = imagesFolder.Parent!.FolderListing.OfType<AssetFolderListingFile>().Single(file => file.Name.Equals("cube.dsc", StringComparison.OrdinalIgnoreCase));
        var cubeDsc = CubeDscFile.Parse(cubeDscEntry.File.Memory.Span);

        return new SkyboxModel
        {
            Front = RenderBoxFace(imagesFolder, "front", cubeDsc.FrontSlicing),
            Back = RenderBoxFace(imagesFolder, "back", cubeDsc.BackSlicing),
            Left = RenderBoxFace(imagesFolder, "left", cubeDsc.LeftSlicing),
            Right = RenderBoxFace(imagesFolder, "right", cubeDsc.RightSlicing),
            Top = RenderBoxFace(imagesFolder, "top", cubeDsc.TopSlicing),
            Bottom = RenderBoxFace(imagesFolder, "bottom", cubeDsc.BottomSlicing),
        };
    }

    private static SKBitmap? RenderBoxFace(AssetBrowserNode imagesFolder, string face, (int Width, int Height) slicing)
    {
        var images = new (SKImage RgbPartImage, SKImage? AlphaMaskPartImage)?[slicing.Width, slicing.Height];
        try
        {
            var maxPartSize = 0;

            for (var y = 0; y < slicing.Height; y++)
            {
                for (var x = 0; x < slicing.Width; x++)
                {
                    var imageFile = imagesFolder.FolderListing.OfType<AssetFolderListingFile>().SingleOrDefault(
                        f => f.Name.StartsWith($"{face}_0{y + 1}_0{x + 1}.",
                        StringComparison.OrdinalIgnoreCase));

                    if (imageFile is null) continue;

                    if (imageFile.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        var rgbPartImage = SKImage.FromEncodedData(imageFile.File.Memory.Span);

                        if (rgbPartImage.Width > maxPartSize) maxPartSize = rgbPartImage.Width;
                        if (rgbPartImage.Height > maxPartSize) maxPartSize = rgbPartImage.Height;

                        images[y, x] = (rgbPartImage, AlphaMaskPartImage: null);
                    }
                    else if (imageFile.Name.EndsWith(".zap", StringComparison.OrdinalIgnoreCase))
                    {
                        var zapImage = ZapImage.Parse(imageFile.File.Memory);

                        if (zapImage.Width > maxPartSize) maxPartSize = zapImage.Width;
                        if (zapImage.Height > maxPartSize) maxPartSize = zapImage.Height;

                        images[y, x] = (
                            SKImage.FromEncodedData(zapImage.RgbChannels.Span),
                            SKImage.FromEncodedData(zapImage.AlphaChannel.Span));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            if (maxPartSize == 0) return null;

            var faceBitmap = new SKBitmap(maxPartSize * slicing.Width, maxPartSize * slicing.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

            using (var faceCanvas = new SKCanvas(faceBitmap))
            {
                using var alphaMaskBitmap = new SKBitmap(maxPartSize * slicing.Width, maxPartSize * slicing.Height, SKColorType.Gray8, SKAlphaType.Opaque);
                using (var alphaMaskCanvas = new SKCanvas(alphaMaskBitmap))
                {
                    alphaMaskCanvas.Clear(SKColors.White);

                    for (var x = 0; x < slicing.Width; x++)
                    {
                        for (var y = 0; y < slicing.Height; y++)
                        {
                            if (images[y, x] is not var (rgbPartImage, alphaMaskPartImage))
                                continue;

                            faceCanvas.DrawImage(
                                rgbPartImage,
                                SKRect.Create(rgbPartImage.Width, rgbPartImage.Height),
                                SKRect.Create(x * maxPartSize, y * maxPartSize, maxPartSize, maxPartSize));

                            if (alphaMaskPartImage is not null)
                            {
                                alphaMaskCanvas.DrawImage(
                                    alphaMaskPartImage,
                                    SKRect.Create(alphaMaskPartImage.Width, alphaMaskPartImage.Height),
                                    SKRect.Create(x * maxPartSize, y * maxPartSize, maxPartSize, maxPartSize));
                            }
                        }
                    }
                }

                using var alphaMaskPixels = alphaMaskBitmap.PeekPixels();
                using var alphaChannelImage = SKImage.FromPixels(
                    new SKImageInfo(alphaMaskPixels.Info.Width, alphaMaskPixels.Info.Height, SKColorType.Alpha8, SKAlphaType.Unpremul),
                    alphaMaskPixels.GetPixels(),
                    alphaMaskPixels.RowBytes);

                using var alphaMaskShader = alphaChannelImage.ToShader();
                using var paint = new SKPaint { Shader = alphaMaskShader, BlendMode = SKBlendMode.DstIn };

                faceCanvas.DrawRect(new SKRect(0, 0, faceBitmap.Width, faceBitmap.Height), paint);
            }

            return faceBitmap;
        }
        finally
        {
            for (var y = 0; y < slicing.Height; y++)
            {
                for (var x = 0; x < slicing.Width; x++)
                {
                    var imageParts = images[x, y];
                    imageParts?.RgbPartImage.Dispose();
                    imageParts?.AlphaMaskPartImage?.Dispose();
                }
            }
        }
    }
}
