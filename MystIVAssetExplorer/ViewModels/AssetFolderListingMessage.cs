namespace MystIVAssetExplorer.ViewModels;

public class AssetFolderListingMessage(string message) : AssetFolderListing
{
    public override string Name => message;
    public override int? Size => null;
}