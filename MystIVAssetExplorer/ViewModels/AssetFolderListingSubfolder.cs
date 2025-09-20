namespace MystIVAssetExplorer.ViewModels;

public class AssetFolderListingSubfolder(AssetBrowserNode subfolderNode) : AssetFolderListing, ISubfolderListing
{
    public AssetBrowserNode SubfolderNode { get; } = subfolderNode;

    public override string Name => SubfolderNode.Name;
    public override int? Size => null;
}
