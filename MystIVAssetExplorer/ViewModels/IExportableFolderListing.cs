using System.IO;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.ViewModels;

public interface IExportableFolderListing
{
    string GetExportFileName();
    Task ExportToStreamAsync(Stream stream);
}
