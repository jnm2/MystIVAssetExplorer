using System;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public interface IUbiDeserializable<TSelf> : IUbiVirtualDeserializable<TSelf>
    where TSelf : IUbiDeserializable<TSelf>
{
    static abstract ReadOnlySpan<byte> UbiClassName { get; }
    static abstract TSelf DeserializeContents(ref UbiBinaryReader reader);

    static TSelf IUbiVirtualDeserializable<TSelf>.Deserialize(ref UbiBinaryReader reader)
    {
        reader.ExpectString(""u8);
        reader.ExpectString(TSelf.UbiClassName);
        return TSelf.DeserializeContents(ref reader);
    }
}
