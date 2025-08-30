using System.IO;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.ViewModels;

public interface IExportableFolderListing
{
    string Name { get; }
    string GetExportFileName();
    Task ExportToStreamAsync(Stream stream);
}
