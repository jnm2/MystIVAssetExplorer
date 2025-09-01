using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MystIVAssetExplorer.Formats;
using MystIVAssetExplorer.Formats.UbiObjects;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly M4bContainingFolder m4bContainingFolder;
    private readonly Dictionary<(uint SoundId, uint GroupId), string> sequenceSoundNames = new();
    private readonly Dictionary<string, M4bFile> soundDataFiles;

    public ObservableCollection<AssetBrowserNode> AssetBrowserNodes { get; }

    public AssetBrowserNode? SelectedAssetBrowserNode
    {
        get;
        set
        {
            var isChanging = field != value;
            this.RaiseAndSetIfChanged(ref field, value);
            if (isChanging)
            {
                SelectedFolderListings.Clear();
                if (value is { FolderListing: [var first, ..] })
                    SelectedFolderListings.Add(first);
            }
        }
    }

    public ObservableCollection<AssetFolderListing> SelectedFolderListings { get; } = [];

    public AudioPlaybackViewModel? AudioPlaybackWindow
    {
        get;
        set
        {
            if (value is null) field?.Dispose();
            this.RaiseAndSetIfChanged(ref field, value);
        }
    }

    public string WindowTitle { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    public MainViewModel()
    {
        var dataDirectory = MystIVHelpers.DetectDataDirectory();
        WindowTitle = "Myst IV Asset Explorer - " + (dataDirectory ?? "Myst IV data files not found");

        if (dataDirectory is not null)
        {
            m4bContainingFolder = M4bReader.OpenM4bFolder(dataDirectory);

            var soundM4b = m4bContainingFolder.Archives.Single(a => a.Name == "sound.m4b");

            foreach (var seqFile in soundM4b.Subdirectories.Single(d => d.Name == "sequence").Files)
            {
                var binaryReader = new UbiBinaryReader(seqFile.Memory.Span);
                binaryReader.ExpectString(UbiSndSequence.UbiClassName);
                var sequence = UbiSndSequence.DeserializeContents(ref binaryReader);

                foreach (var sound in sequence.Sounds)
                    sequenceSoundNames.Add((sound.SoundId, sound.GroupId), sound.Name);
            }

            var language = m4bContainingFolder.Subfolders.Single();

            soundDataFiles = (
                from directory in new[]
                {
                soundM4b.Subdirectories.Single(d => d.Name == "data"),
                language.Archives.Single(a => a.Name == "sound.m4b")
                    .Subdirectories.Single(d => d.Name == "data")
                    .Subdirectories.Single(d => d.Name == language.Name),
                }
                from file in directory.Files
                where !file.Name.EndsWith(".sb0", StringComparison.OrdinalIgnoreCase)
                select file).ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            AssetBrowserNodes = CreateNodes(m4bContainingFolder);
            FindInFileName("mu_music.sb0 stream 0");
        }
        else
        {
            AssetBrowserNodes = [];
        }

        ExportCommand = ReactiveCommand.CreateFromTask((DataGrid grid) => ExportAsync(grid));
        PlayCommand = ReactiveCommand.CreateFromTask((DataGrid grid) => PlayAsync(grid));
    }

    private void FindInFileName(string text)
    {
        Search(AssetBrowserNodes);

        bool Search(IEnumerable<AssetBrowserNode> nodes)
        {
            foreach (var node in nodes)
            {
                var matchedFile = node.FolderListing
                    .FirstOrDefault(f => f.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

                if (matchedFile is not null)
                {
                    SelectedAssetBrowserNode = node;
                    SelectedFolderListings.Clear();
                    SelectedFolderListings.Add(matchedFile);
                    return true;
                }

                if (Search(node.ChildNodes))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }
    }

    private async Task ExportAsync(DataGrid dataGrid)
    {
        var window = (Window)dataGrid.GetVisualRoot()!;

        var fileListings = dataGrid.SelectedItems.OfType<IExportableFolderListing>().ToArray();
        if (fileListings is [])
        {
            await MessageBoxManager.GetMessageBoxStandard("Export files", "Please select one or more files to export.", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(window);
            return;
        }

        if (fileListings is [var singleListing])
        {
            var exportFileName = singleListing.GetExportFileName();
            var extension = Path.GetExtension(exportFileName);

            var result = await window.StorageProvider.SaveFilePickerAsync(new()
            {
                Title = "Export " + singleListing.Name,
                SuggestedFileName = exportFileName,
                FileTypeChoices =
                [
                    .. extension is not "" ? new[] { new FilePickerFileType(extension[1..].ToUpper() + " file") { Patterns = ["*" + extension] } } : [],
                    new FilePickerFileType("All files") { Patterns = ["*.*"] },
                ],
            });

            if (result is not null)
            {
                await using var stream = await result.OpenWriteAsync();
                await singleListing.ExportToStreamAsync(stream);
            }
        }
        else
        {
            var result = (await window.StorageProvider.OpenFolderPickerAsync(new() { Title = "Select destination folder for export" })).SingleOrDefault();
            if (result is not null)
            {
                foreach (var listing in fileListings)
                {
                    var file = (await result.CreateFileAsync(listing.GetExportFileName()))!;
                    await using var stream = await file.OpenWriteAsync();
                    await listing.ExportToStreamAsync(stream);
                }
            }
        }
    }

    private async Task PlayAsync(DataGrid dataGrid)
    {
        var window = (Window)dataGrid.GetVisualRoot()!;

        var soundAsset = SelectedFolderListings.OfType<AssetFolderListingSoundStream>().FirstOrDefault();
        if (soundAsset is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Play", "Please select one or more sound files to play.", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(window);
            return;
        }

        if (soundAsset.SoundStream.Format is not (SoundStreamFormat.PCM or SoundStreamFormat.OggVorbis))
        {
            await MessageBoxManager.GetMessageBoxStandard("Play", "This audio format is not currently supported.", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(window);
            return;
        }

        var stream = new MemoryStream();
        await soundAsset.ExportToStreamAsync(stream);
        stream.Position = 0;

        AudioPlaybackWindow ??= new();
        AudioPlaybackWindow.SwitchAudioFile(soundAsset.Name, stream, soundAsset.SoundStream.Format);
    }

    public void Dispose()
    {
        m4bContainingFolder.Dispose();
    }

    public ReactiveCommand<DataGrid, Unit> ExportCommand { get; }

    public ReactiveCommand<DataGrid, Unit> PlayCommand { get; }

    private ObservableCollection<AssetBrowserNode> CreateNodes(M4bContainingFolder folder)
    {
        var collection = new ObservableCollection<AssetBrowserNode>();

        foreach (var subfolder in folder.Subfolders)
        {
            var childNodes = CreateNodes(subfolder);

            collection.Add(new AssetBrowserNode
            {
                Name = subfolder.Name,
                ChildNodes = CreateNodes(subfolder),
                FolderListing = [.. childNodes.Select(node => new AssetFolderListingSubfolder(node))],
                IsExpanded = true,
            });
        }

        foreach (var archive in folder.Archives)
        {
            collection.Add(CreateNode(archive));
        }

        return collection;
    }

    private AssetBrowserNode CreateNode(M4bDirectory directory)
    {
        var childNodes = new ObservableCollection<AssetBrowserNode>(directory.Subdirectories.Select(CreateNode));
        var files = new List<AssetFolderListingFile>();

        foreach (var file in directory.Files)
        {
            if (file.Name.EndsWith(".sb0", StringComparison.OrdinalIgnoreCase))
                childNodes.Add(CreateSb0Node(file));
            else
                files.Add(new AssetFolderListingFile(file));
        }

        return new AssetBrowserNode
        {
            Name = directory.Name,
            ChildNodes = childNodes,
            FolderListing = [
                .. childNodes.Select(node => new AssetFolderListingSubfolder(node)),
                .. files],
        };
    }

    private AssetBrowserNode CreateSb0Node(M4bFile m4bFile)
    {
        var sb0File = Sb0File.Deserialize(m4bFile.Memory);

        return new AssetBrowserNode
        {
            Name = m4bFile.Name,
            ChildNodes = [],
            FolderListing = [..
                sb0File?.SoundStreams.Select(soundStream =>
                {
                    var name =
                        (!soundStream.ReferencesExternalDataFile ? soundStream.DataFileName : null)
                        ?? soundStream.SoundIds
                            .Select(soundId => sequenceSoundNames.GetValueOrDefault((soundId, soundStream.GroupId)))
                            .FirstOrDefault(name => name is not null)
                        ?? $"{m4bFile.Name} stream {soundStream.StreamId} → {Path.GetFileNameWithoutExtension(soundStream.DataFileName)}";

                    return (AssetFolderListing)new AssetFolderListingSoundStream(
                        name,
                        soundStream,
                        soundStream.ReferencesExternalDataFile ? soundDataFiles[soundStream.DataFileName] : null);
                })
                ?? [new AssetFolderListingMessage("(Sound stream entry type not yet supported)")]],
        };
    }
}
