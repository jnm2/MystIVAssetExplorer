using MystIVAssetExplorer.Formats;

namespace MystIVAssetExplorer.ViewModels;

public class FileBasedAssetBrowserNode : AssetBrowserNode
{
    public required M4bFile File { get; init; }
}
