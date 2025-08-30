using System;
using System.Buffers.Binary;

namespace MystIVAssetExplorer;

public ref struct SpanWriter(Span<byte> span)
{
    public Span<byte> Span { get; private set; } = span;

    public void Write(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(Span);
        Span = Span[bytes.Length..];
    }

    public void WriteInt32LittleEndian(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(Span, value);
        Span = Span[sizeof(int)..];
    }

    public void WriteUInt32LittleEndian(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(Span, value);
        Span = Span[sizeof(uint)..];
    }

    public void WriteUInt16LittleEndian(ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(Span, value);
        Span = Span[sizeof(ushort)..];
    }
}
