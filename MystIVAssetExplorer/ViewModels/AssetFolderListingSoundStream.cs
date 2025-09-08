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
        // Only find three- or four-character extensions
        var extensionSeparator = Name.LastIndexOf('.', Name.Length - 4, count: 2);
        var withoutExtension = extensionSeparator != -1
            ? Name[..extensionSeparator]
            : Name;

        return withoutExtension + SoundStream.GetStandaloneExportFileExtension();
    }
}
