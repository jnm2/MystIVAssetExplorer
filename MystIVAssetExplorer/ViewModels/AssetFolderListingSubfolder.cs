using System.IO;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.ViewModels;

public class AssetFolderListingSubfolder(AssetBrowserNode node) : AssetFolderListing
{
    public AssetBrowserNode Node { get; } = node;

    public override string Name => Node.Name;
    public override int? Size => null;
}
