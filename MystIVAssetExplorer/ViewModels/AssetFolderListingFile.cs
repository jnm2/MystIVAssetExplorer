using MystIVAssetExplorer.Formats;

namespace MystIVAssetExplorer.ViewModels;

public class AssetFolderListingFile(M4bFile file) : AssetFolderListing
{
    public M4bFile File { get; } = file;

    public override string Name => File.Name;
    public override int? Size => File.Size;
}
