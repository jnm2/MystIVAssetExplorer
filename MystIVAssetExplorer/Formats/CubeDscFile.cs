using MystIVAssetExplorer.Memory;
using System;

namespace MystIVAssetExplorer.Formats;

public sealed record CubeDscFile
{
    public required (int Width, int Height) FrontSlicing { get; init; }
    public required (int Width, int Height) BackSlicing { get; init; }
    public required (int Width, int Height) LeftSlicing { get; init; }
    public required (int Width, int Height) RightSlicing { get; init; }
    public required (int Width, int Height) TopSlicing { get; init; }
    public required (int Width, int Height) BottomSlicing { get; init; }

    public static CubeDscFile Parse(ReadOnlySpan<byte> cubeDscFileData)
    {
        var reader = new SpanReader(cubeDscFileData);
        reader.ExpectString("""
            version(1)
            {cube_mapping

            """u8);

        return new()
        {
            FrontSlicing = ParseSlicing(ref reader, "front_slicing"u8),
            BackSlicing = ParseSlicing(ref reader, "back_slicing"u8),
            LeftSlicing = ParseSlicing(ref reader, "left_slicing"u8),
            RightSlicing = ParseSlicing(ref reader, "right_slicing"u8),
            TopSlicing = ParseSlicing(ref reader, "top_slicing"u8),
            BottomSlicing = ParseSlicing(ref reader, "bottom_slicing"u8),
        };

        static (int Width, int Height) ParseSlicing(ref SpanReader reader, ReadOnlySpan<byte> expectedName)
        {
            reader.ExpectString("      "u8);
            reader.ExpectString(expectedName);
            reader.ExpectString("("u8);
            var width = reader.ReadByte() - '0';
            reader.ExpectString(","u8);
            var height = reader.ReadByte() - '0';
            reader.ExpectString("""
                )

                """u8);

            return (width, height);
        }
    }
}
