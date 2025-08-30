using MystIVAssetExplorer.Formats;
using System.IO;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.ViewModels;

public class AssetFolderListingFile(M4bFile file) : AssetFolderListing, IExportableFolderListing
{
    public M4bFile File { get; } = file;

    public override string Name => File.Name;
    public override int? Size => File.Size;

    public async Task ExportToStreamAsync(Stream stream)
    {
        await stream.WriteAsync(File.Memory);
    }

    public string GetExportFileName() => File.Name;
}
