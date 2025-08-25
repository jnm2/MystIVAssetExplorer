using System.Collections.ObjectModel;

namespace MystIVAssetExplorer.ViewModels;

public class AssetBrowserNode
{
    public required string Name { get; init; }
    public required ObservableCollection<AssetBrowserNode> ChildNodes { get; init; }
    public bool IsExpanded { get; set; }
    public ObservableCollection<AssetFolderListing> FolderListing { get; init; } = [];
}
