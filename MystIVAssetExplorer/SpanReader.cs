using System;
using System.Buffers.Binary;

namespace MystIVAssetExplorer;

internal ref struct SpanReader(ReadOnlySpan<byte> span)
{
    public ReadOnlySpan<byte> Span { get; private set; } = span;

    public byte ReadByte()
    {
        var value = Span[0];
        Span = Span[sizeof(byte)..];
        return value;
    }

    public int ReadInt32LittleEndian()
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(Span);
        Span = Span[sizeof(int)..];
        return value;
    }

    public ReadOnlySpan<byte> ReadSpan(int byteCount)
    {
        var value = Span[..byteCount];
        Span = Span[byteCount..];
        return value;
    }
}
