using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MystIVAssetExplorer.Formats;

public sealed class M4bContainingFolder : M4bNode, IDisposable
{
    public required string FolderPath { get; init; }
    public required ImmutableArray<M4bContainingFolder> Subfolders { get; init; }
    public required ImmutableArray<M4bArchive> Archives { get; init; }

    public IEnumerable<M4bFile> EnumerateFilesRecursively()
    {
        foreach (var archive in Archives)
        {
            foreach (var file in archive.EnumerateFilesRecursively())
                yield return file;
        }

        foreach (var subfolder in Subfolders)
        {
            foreach (var file in subfolder.EnumerateFilesRecursively())
                yield return file;
        }
    }

    public void Dispose()
    {
        foreach (var archive in Archives)
            archive.Dispose();
    }
}
