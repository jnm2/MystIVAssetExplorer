using ReactiveUI;
using System.Collections.ObjectModel;

namespace MystIVAssetExplorer.ViewModels;

public class AssetBrowserNode : ReactiveObject
{
    public required string Name { get; init; }

    public required ObservableCollection<AssetBrowserNode> ChildNodes
    {
        get;
        init
        {
            if (field == value) return;

            if (field is not null)
            {
                foreach (var child in field)
                {
                    if (child.Parent == this)
                        child.Parent = null;
                }
            }

            field = value;

            if (field is not null)
            {
                foreach (var child in field)
                    child.Parent = this;
            }
        }
    }

    public ObservableCollection<AssetFolderListing> FolderListing { get; init; } = [];

    public AssetBrowserNode? Parent { get; private set; }

    public bool IsExpanded { get; set => this.RaiseAndSetIfChanged(ref field, value); }
}
