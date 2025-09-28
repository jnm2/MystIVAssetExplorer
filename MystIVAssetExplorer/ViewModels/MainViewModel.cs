using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MystIVAssetExplorer.Formats;
using MystIVAssetExplorer.Formats.UbiObjects;
using MystIVAssetExplorer.Skybox;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly M4bContainingFolder? m4bContainingFolder;
    private readonly Dictionary<(uint SoundId, uint GroupId), string> sequenceSoundNames = new();
    private readonly Dictionary<string, M4bFile>? soundDataFiles;

    public ObservableCollection<AssetBrowserNode> AssetBrowserNodes { get; }

    public AssetBrowserNode? SelectedAssetBrowserNode
    {
        get;
        set
        {
            if (field == value) return;
            this.RaiseAndSetIfChanged(ref field, value);

            SelectedFolderListings.Clear();
            if (field is { FolderListing: [var first, ..] })
                SelectedFolderListings.Add(first);

            UpdateSkyboxModel();
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
            FindInFileName("w1z03n050.m4b");
        }
        else
        {
            AssetBrowserNodes = [];
        }

        ExportCommand = ReactiveCommand.CreateFromTask((DataGrid grid) => ExportAsync(grid));
        PlayCommand = ReactiveCommand.CreateFromTask((DataGrid grid) => PlayAsync(grid));
        OpenCommand = ReactiveCommand.CreateFromTask((DataGridRow row) => OpenAsync(row));
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

        await ExportAsync(window, fileListings);
    }

    private async Task ExportAsync(Window window, IExportableFolderListing[] fileListings)
    {
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

        if (SelectedFolderListings.OfType<AssetFolderListingSoundStream>().FirstOrDefault() is { } soundAsset)
        {
            await PlayAsync(soundAsset);
            return;
        }

        if (SelectedFolderListings.OfType<AssetFolderListingFile>().FirstOrDefault(asset => asset.File.Name.EndsWith(".bik", StringComparison.OrdinalIgnoreCase)) is { } videoAsset)
        {
            await PlayVideoAsync(window, videoAsset);
            return;
        }

        await MessageBoxManager.GetMessageBoxStandard("Play", "Please select one or more sound files to play.", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
            .ShowWindowDialogAsync(window);
    }

    private async Task PlayAsync(AssetFolderListingSoundStream soundAsset)
    {
        var stream = new MemoryStream();
        await soundAsset.ExportToStreamAsync(stream);
        stream.Position = 0;

        AudioPlaybackWindow ??= new();
        AudioPlaybackWindow.SwitchAudioFile(soundAsset.Name, stream, soundAsset.SoundStream.GetExportFormat());
    }

    private async Task PlayVideoAsync(Window window, AssetFolderListingFile videoAsset)
    {
        var player = ExternalVideoPlayer.Detect();
        if (player is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Play", "To play a video file, install VLC media player or ffmpeg.", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(window);
            return;
        }
        await player.PlayAsync(videoAsset.ExportToStreamAsync, options: new() { VideoName = videoAsset.Name });
    }

    private async Task OpenAsync(DataGridRow row)
    {
        var window = (Window)row.GetVisualRoot()!;

        switch (row.DataContext)
        {
            case AssetFolderListingSoundStream soundAsset:
                await PlayAsync(soundAsset);
                break;
            case AssetFolderListingFile videoAsset when videoAsset.File.Name.EndsWith(".bik", StringComparison.OrdinalIgnoreCase):
                await PlayVideoAsync(window, videoAsset);
                break;
            case ISubfolderListing subfolder:
                if (SelectedAssetBrowserNode?.ChildNodes.Contains(subfolder.SubfolderNode) ?? false)
                    SelectedAssetBrowserNode.IsExpanded = true;
                SelectedAssetBrowserNode = subfolder.SubfolderNode;
                break;
            case IExportableFolderListing exportable:
                await ExportAsync(window, [exportable]);
                break;
        }
    }

    public void Dispose()
    {
        m4bContainingFolder?.Dispose();
    }

    public ReactiveCommand<DataGrid, Unit> ExportCommand { get; }

    public ReactiveCommand<DataGrid, Unit> PlayCommand { get; }

    public ReactiveCommand<DataGridRow, Unit> OpenCommand { get; }

    private ILease<SkyboxModel>? nodeViewerBoxModelLease;
    public ReferenceCountedDisposable<SkyboxModel>? NodeViewerSkyboxModel { get; private set => this.RaiseAndSetIfChanged(ref field, value); }

    private ObservableCollection<AssetBrowserNode> CreateNodes(M4bContainingFolder folder)
    {
        var collection = new ObservableCollection<AssetBrowserNode>();

        foreach (var subfolder in folder.Subfolders)
        {
            var childNodes = CreateNodes(subfolder);

            collection.Add(new AssetBrowserNode
            {
                Name = subfolder.Name,
                ChildNodes = childNodes,
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
                .. childNodes.Select(AssetFolderListing (node) => node is FileBasedAssetBrowserNode fileNode
                    ? new AssetFolderListingFileBasedSubfolder(fileNode)
                    : new AssetFolderListingSubfolder(node)),
                .. files],
        };
    }

    private FileBasedAssetBrowserNode CreateSb0Node(M4bFile m4bFile)
    {
        var sb0File = Sb0File.Deserialize(m4bFile.Memory);

        return new FileBasedAssetBrowserNode
        {
            File = m4bFile,
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
                        soundStream.ReferencesExternalDataFile ? soundDataFiles![soundStream.DataFileName] : null);
                })
                ?? [new AssetFolderListingMessage("(Sound stream entry type not yet supported)")]],
        };
    }

    private void UpdateSkyboxModel()
    {
        var originalBoxModelLease = nodeViewerBoxModelLease;

        var imagesFolder = PickSkyboxImagesFolder(SelectedAssetBrowserNode);

        NodeViewerSkyboxModel = imagesFolder is null
            ? null
            : new ReferenceCountedDisposable<SkyboxModel>(
                SkyboxModel.FromImagesFolder(imagesFolder),
                out nodeViewerBoxModelLease);

        originalBoxModelLease?.Dispose();

        static AssetBrowserNode? PickSkyboxImagesFolder(AssetBrowserNode? searchRoot)
        {
            // The folder is literally open (and may not be layer_default)
            if (searchRoot?.Parent?.FolderListing.Any(l => l.Name.Equals("cube.dsc", StringComparison.OrdinalIgnoreCase)) ?? false)
                return searchRoot;

            // A parent folder may have been selected. We should be able to find a unique "set_default" folder under this node.
            var searchPredicates = new Func<AssetBrowserNode, bool>[]
            {
                node => Regex.IsMatch(node.Name, @"^w\dz\d{2}n\d{3}\.m4b$", RegexOptions.IgnoreCase),
                node => node.Name.Equals("cube", StringComparison.OrdinalIgnoreCase),
                node => node.Name.Equals("layer_default.m4b", StringComparison.OrdinalIgnoreCase),
            };

            var searchCandidates = searchRoot!.ChildNodes;

            foreach (var searchPredicate in searchPredicates)
            {
                if (searchCandidates.TrySingle(searchPredicate, out var foundNode))
                    searchCandidates = foundNode.ChildNodes;
            }

            return searchCandidates.TrySingle(n => n.Name.Equals("set_default", StringComparison.OrdinalIgnoreCase), out var setDefaultNode)
                ? setDefaultNode
                : null;
        }
    }
}
