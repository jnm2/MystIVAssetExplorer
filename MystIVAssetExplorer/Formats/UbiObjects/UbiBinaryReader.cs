using MystIVAssetExplorer.Memory;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public ref struct UbiBinaryReader
{
    public SpanReader SpanReader;

    public UbiBinaryReader(ReadOnlySpan<byte> span)
    {
        if (!span.StartsWith("ubi/b0-l"u8))
            throw new InvalidDataException("The span does not have the correct header.");

        SpanReader = new SpanReader(span[8..]);

        if (SpanReader.ReadInt32LittleEndian() != 4)
            throw new NotSupportedException("Unsupported format version.");
    }

    public ReadOnlySpan<byte> ReadString()
    {
        var length = SpanReader.ReadInt32LittleEndian();
        return SpanReader.ReadSpan(length);
    }

    public void ExpectString(ReadOnlySpan<byte> value)
    {
        var actual = ReadString();
        if (!actual.SequenceEqual(value))
            throw new InvalidDataException($"Expected string '{Encoding.ASCII.GetString(value)}' but was '{Encoding.ASCII.GetString(actual)}'");
    }

    public bool ReadBoolean()
    {
        return SpanReader.ReadByte() switch
        {
            0 => false,
            1 => true,
            var other => throw new InvalidDataException($"Expected boolean but read {other}"),
        };
    }

    public T? DeserializeNullable<T>() where T : class, IUbiVirtualDeserializable<T>
    {
        var validPointer = ReadBoolean();
        return !validPointer ? null : T.Deserialize(ref this);
    }

    public ImmutableArray<T> DeserializeList<T>() where T : IUbiVirtualDeserializable<T>
    {
        var list = ImmutableArray.CreateBuilder<T>();
        list.Capacity = SpanReader.ReadInt32LittleEndian();

        for (var i = 0; i < list.Capacity; i++)
            list.Add(T.Deserialize(ref this));

        return list.MoveToImmutable();
    }
}
