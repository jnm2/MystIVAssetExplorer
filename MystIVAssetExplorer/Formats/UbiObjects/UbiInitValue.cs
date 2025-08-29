using System;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public abstract record UbiInitValue<T> : IUbiVirtualDeserializable<UbiInitValue<T>>
{
    public static UbiInitValue<T> Deserialize(ref UbiBinaryReader reader)
    {
        reader.ExpectString(""u8);
        var className = reader.ReadString();

        if (className.SequenceEqual(UbiInitValueConst<T>.UbiClassName))
            return UbiInitValueConst<T>.DeserializeContents(ref reader);

        if (className.SequenceEqual(UbiInitValueRange<T>.UbiClassName))
            return UbiInitValueRange<T>.DeserializeContents(ref reader);

        throw new NotSupportedException($"Type '{className.ToString()}' is not supported in UbiInitValue");
    }
}
