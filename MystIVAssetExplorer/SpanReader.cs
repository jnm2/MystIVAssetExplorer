using System;
using System.Buffers.Binary;

namespace MystIVAssetExplorer;

public ref struct SpanReader(ReadOnlySpan<byte> span)
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

    public ushort ReadUInt16LittleEndian()
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(Span);
        Span = Span[sizeof(ushort)..];
        return value;
    }

    public uint ReadUInt32LittleEndian()
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(Span);
        Span = Span[sizeof(uint)..];
        return value;
    }

    public float ReadSingleLittleEndian()
    {
        var value = BinaryPrimitives.ReadSingleLittleEndian(Span);
        Span = Span[sizeof(float)..];
        return value;
    }

    public ReadOnlySpan<byte> ReadSpan(int byteCount)
    {
        var value = Span[..byteCount];
        Span = Span[byteCount..];
        return value;
    }
}
