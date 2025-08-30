using MystIVAssetExplorer.Formats;
using System.IO;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.ViewModels;

public class AssetFolderListingSoundStream(string name, SoundStream soundStream, M4bFile? referencedFile) : AssetFolderListing, IExportableFolderListing
{
    public SoundStream SoundStream { get; } = soundStream;

    public override string Name { get; } = name;

    public override int? Size => SoundStream.Length;

    public async Task ExportToStreamAsync(Stream stream)
    {
        await SoundStream.ExportStandaloneFileAsync(stream, referencedFile);
    }

    public string GetExportFileName()
    {
        return Path.ChangeExtension(Name, SoundStream.GetStandaloneFileExtension());
    }
}
