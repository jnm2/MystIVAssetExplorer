using System;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public sealed record UbiInitValueConst<T>(T Value) : UbiInitValue<T>, IUbiDeserializable<UbiInitValueConst<T>>
{
    public static ReadOnlySpan<byte> UbiClassName =>
        typeof(T) == typeof(float) ? "snd::InitValueConst < ubiF32 >"u8 :
        typeof(T) == typeof(int) ? "snd::InitValueConst < ubiS32 >"u8 :
        typeof(T) == typeof(UbiVector3) ? "snd::InitValueConst < ubi::Vector3 >"u8 :
        throw new NotSupportedException($"Type '{typeof(T)}' is not supported in UbiInitValueConst");

    public static UbiInitValueConst<T> DeserializeContents(ref UbiBinaryReader reader)
    {
        if (typeof(T) == typeof(float))
        {
            var value = reader.SpanReader.ReadSingleLittleEndian();
            return new UbiInitValueConst<T>((T)(object)value);
        }
        if (typeof(T) == typeof(int))
        {
            var value = reader.SpanReader.ReadInt32LittleEndian();
            return new UbiInitValueConst<T>((T)(object)value);
        }
        if (typeof(T) == typeof(UbiVector3))
        {
            var x = reader.SpanReader.ReadSingleLittleEndian();
            var y = reader.SpanReader.ReadSingleLittleEndian();
            var z = reader.SpanReader.ReadSingleLittleEndian();
            return new UbiInitValueConst<T>((T)(object)new UbiVector3(x, y, z));
        }
        throw new NotSupportedException($"Type '{typeof(T)}' is not supported in UbiInitValueConst");
    }
}
