using MystIVAssetExplorer.Formats;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace MystIVAssetExplorer.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly M4bContainingFolder m4bContainingFolder;

    public ObservableCollection<AssetBrowserNode> AssetBrowserNodes { get; }

    public AssetBrowserNode? SelectedAssetBrowserNode { get; set => this.RaiseAndSetIfChanged(ref field, value); }

    public MainViewModel()
    {
        m4bContainingFolder = M4bReader.OpenM4bFolder(@"C:\Program Files (x86)\GOG Galaxy\Games\Myst 4\data");

        AssetBrowserNodes = CreateNodes(m4bContainingFolder);

        ExportCommand = ReactiveCommand.CreateFromTask((DataGrid grid) => ExportAsync(grid));
    }

    private async Task ExportAsync(DataGrid dataGrid)
    {
        var window = (Window)dataGrid.GetVisualRoot()!;

        var fileListings = dataGrid.SelectedItems.OfType<AssetFolderListingFile>().ToArray();
        if (fileListings is [])
        {
            await MessageBoxManager.GetMessageBoxStandard("Export files", "Please select one or more files to export.", ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(window);
            return;
        }

        var result = (await window.StorageProvider.OpenFolderPickerAsync(new() { Title = "Select destination folder for export" })).SingleOrDefault();
        if (result is not null)
        {
            foreach (var listing in fileListings)
            {
                var file = (await result.CreateFileAsync(listing.File.Name))!;
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(listing.File.Memory);
            }
        }
    }

    public void Dispose()
    {
        m4bContainingFolder.Dispose();
    }

    public ReactiveCommand<DataGrid, Unit> ExportCommand { get; }

    private static ObservableCollection<AssetBrowserNode> CreateNodes(M4bContainingFolder folder)
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

    private static AssetBrowserNode CreateNode(M4bDirectory directory)
    {
        var childNodes = new ObservableCollection<AssetBrowserNode>(directory.Subdirectories.Select(CreateNode));

        return new AssetBrowserNode
        {
            Name = directory.Name,
            ChildNodes = childNodes,
            FolderListing = [
                .. childNodes.Select(node => new AssetFolderListingSubfolder(node)),
                .. directory.Files.Select(file => new AssetFolderListingFile(file))],
        };
    }
}
