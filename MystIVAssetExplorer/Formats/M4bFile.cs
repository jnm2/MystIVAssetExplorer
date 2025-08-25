using System;

namespace MystIVAssetExplorer.Formats;

public class M4bFile : M4bNode
{
    public int Size => Memory.Length;
    public required int Offset { get; init; }

    public required ReadOnlyMemory<byte> Memory { get; init; }
}
