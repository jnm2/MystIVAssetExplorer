namespace MystIVAssetExplorer.ViewModels;

public class AssetFolderListingFileBasedSubfolder(FileBasedAssetBrowserNode subfolderNode) : AssetFolderListingFile(subfolderNode.File), ISubfolderListing
{
    public AssetBrowserNode SubfolderNode { get; } = subfolderNode;
}
