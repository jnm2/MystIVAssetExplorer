using MystIVAssetExplorer.Memory;
using System;

namespace MystIVAssetExplorer.Formats;

public sealed class ZapImage
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required ReadOnlyMemory<byte> RgbChannels { get; init; }
    public required ReadOnlyMemory<byte> AlphaChannel { get; init; }

    public static ZapImage Parse(ReadOnlyMemory<byte> zapFileData)
    {
        var reader = new SpanReader(zapFileData.Span);
        if (reader.ReadUInt32LittleEndian() != 32) throw new NotImplementedException();
        if (reader.ReadUInt32LittleEndian() != 2) throw new NotImplementedException();
        if (reader.ReadUInt32LittleEndian() != 10) throw new NotImplementedException();
        if (reader.ReadUInt32LittleEndian() != 10) throw new NotImplementedException();
        var dataLength1 = reader.ReadInt32LittleEndian();
        var dataLength2 = reader.ReadInt32LittleEndian();
        var width = reader.ReadInt32LittleEndian();
        var height = reader.ReadInt32LittleEndian();

        var position = zapFileData.Length - reader.Span.Length;
        var image1 = zapFileData.Slice(position, dataLength1);
        position += dataLength1;
        var image2 = zapFileData.Slice(position, dataLength2);
        position += dataLength2;
        if (position != zapFileData.Length)
            throw new NotImplementedException();

        return new ZapImage { Width = width, Height = height, RgbChannels = image1, AlphaChannel = image2 };
    }
}
