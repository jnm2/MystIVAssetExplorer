namespace MystIVAssetExplorer.Formats.UbiObjects;

public interface IUbiVirtualDeserializable<TSelf> where TSelf : IUbiVirtualDeserializable<TSelf>
{
    static abstract TSelf Deserialize(ref UbiBinaryReader reader);
}
