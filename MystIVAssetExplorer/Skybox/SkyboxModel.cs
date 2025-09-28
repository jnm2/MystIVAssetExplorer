using MystIVAssetExplorer.Formats;
using MystIVAssetExplorer.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;
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

    public static SkyboxModel FromFiles(IReadOnlyCollection<AssetFolderListing> files)
    {
        return new SkyboxModel
        {
            Front = RenderBoxFace(files, "front"),
            Back = RenderBoxFace(files, "back"),
            Left = RenderBoxFace(files, "left"),
            Right = RenderBoxFace(files, "right"),
            Top = RenderBoxFace(files, "top"),
            Bottom = RenderBoxFace(files, "bottom"),
        };
    }

    private static int DetectNumberOfParts(IReadOnlyCollection<AssetFolderListing> files, string face)
    {
        for (var numberOfParts = 6; numberOfParts >= 1; numberOfParts--)
        {
            if (files.OfType<AssetFolderListingFile>().Any(f => f.Name.StartsWith($"{face}_0{numberOfParts}_0{numberOfParts}.", StringComparison.OrdinalIgnoreCase)))
                return numberOfParts;
        }

        return 0;
    }

    private static SKBitmap? RenderBoxFace(IReadOnlyCollection<AssetFolderListing> files, string face)
    {
        var numberOfParts = DetectNumberOfParts(files, face);
        if (numberOfParts == 0)
            return null;

        var imageFiles = new AssetFolderListingFile?[numberOfParts, numberOfParts];

        for (var y = 0; y < numberOfParts; y++)
        {
            for (var x = 0; x < numberOfParts; x++)
            {
                imageFiles[y, x] = files.OfType<AssetFolderListingFile>().SingleOrDefault(
                    f => f.Name.StartsWith($"{face}_0{y + 1}_0{x + 1}.",
                    StringComparison.OrdinalIgnoreCase));
            }
        }

        var anyFile = imageFiles.Cast<AssetFolderListingFile?>().FirstOrDefault(file => file is not null);
        if (anyFile is null)
            return null;

        int partSize;
        if (anyFile.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
        {
            using var image = SKImage.FromEncodedData(anyFile.File.Memory.Span);
            partSize = image.Width;
        }
        else if (anyFile.Name.EndsWith(".zap", StringComparison.OrdinalIgnoreCase))
        {
            partSize = ZapImage.Parse(anyFile.File.Memory).Width;
        }
        else
        {
            throw new NotImplementedException();
        }

        var faceBitmap = new SKBitmap(partSize * numberOfParts, partSize * numberOfParts, SKColorType.Rgba8888, SKAlphaType.Premul);

        using (var faceCanvas = new SKCanvas(faceBitmap))
        {
            using var alphaMaskBitmap = new SKBitmap(partSize * numberOfParts, partSize * numberOfParts, SKColorType.Gray8, SKAlphaType.Opaque);
            using (var alphaMaskCanvas = new SKCanvas(alphaMaskBitmap))
            {
                alphaMaskCanvas.Clear(SKColors.White);

                for (var x = 0; x < numberOfParts; x++)
                {
                    for (var y = 0; y < numberOfParts; y++)
                    {
                        if (imageFiles[y, x] is not { } imageFile)
                            continue;

                        if (imageFile.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            using var image = SKImage.FromEncodedData(imageFile.File.Memory.Span);
                            faceCanvas.DrawImage(image, x * partSize, y * partSize);
                        }
                        else if (imageFile.Name.EndsWith(".zap", StringComparison.OrdinalIgnoreCase))
                        {
                            var zapImage = ZapImage.Parse(imageFile.File.Memory);

                            using (var rgbPartImage = SKImage.FromEncodedData(zapImage.RgbChannels.Span))
                                faceCanvas.DrawImage(rgbPartImage, x * partSize, y * partSize);

                            using (var alphaMaskPartImage = SKImage.FromEncodedData(zapImage.AlphaChannel.Span))
                                alphaMaskCanvas.DrawImage(alphaMaskPartImage, x * partSize, y * partSize);
                        }
                        else
                        {
                            throw new NotImplementedException();
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
}
