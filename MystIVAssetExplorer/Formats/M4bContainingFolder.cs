using System;
using System.Collections.Immutable;

namespace MystIVAssetExplorer.Formats;

public sealed class M4bContainingFolder : M4bNode, IDisposable
{
    public required string FolderPath { get; init; }
    public required ImmutableArray<M4bContainingFolder> Subfolders { get; init; }
    public required ImmutableArray<M4bArchive> Archives { get; init; }

    public void Dispose()
    {
        foreach (var archive in Archives)
            archive.Dispose();
    }
}
