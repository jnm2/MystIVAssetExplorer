using ReactiveUI;
using System.Collections.ObjectModel;

namespace MystIVAssetExplorer.ViewModels;

public class AssetBrowserNode : ReactiveObject
{
    public required string Name { get; init; }
    public required ObservableCollection<AssetBrowserNode> ChildNodes { get; init; }
    public ObservableCollection<AssetFolderListing> FolderListing { get; init; } = [];

    public bool IsExpanded { get; set => this.RaiseAndSetIfChanged(ref field, value); }
}
