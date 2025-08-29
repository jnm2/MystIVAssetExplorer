using System;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public sealed record UbiInitValueRange<T>(T Min, T Max) : UbiInitValue<T>, IUbiDeserializable<UbiInitValueRange<T>>
{
    public static ReadOnlySpan<byte> UbiClassName =>
        typeof(T) == typeof(float) ? "snd::InitValueRange < ubiF32 >"u8 :
        throw new NotSupportedException($"Type '{typeof(T)}' is not supported in UbiInitValueRange");

    public static UbiInitValueRange<T> DeserializeContents(ref UbiBinaryReader reader)
    {
        if (typeof(T) == typeof(float))
        {
            var min = reader.SpanReader.ReadSingleLittleEndian();
            var max = reader.SpanReader.ReadSingleLittleEndian();
            return new UbiInitValueRange<T>((T)(object)min, (T)(object)max);
        }
        throw new NotSupportedException($"Type '{typeof(T)}' is not supported in UbiInitValueRange");
    }
}
