using System.Collections.Generic;
using System.Collections.Immutable;

namespace MystIVAssetExplorer.Formats;

public class M4bDirectory : M4bNode
{
    public ImmutableArray<M4bDirectory> Subdirectories { get; init; }
    public ImmutableArray<M4bFile> Files { get; init; }

    public IEnumerable<M4bFile> EnumerateFilesRecursively()
    {
        foreach (var file in Files)
            yield return file;

        foreach (var subdirectory in Subdirectories)
        {
            foreach (var file in subdirectory.EnumerateFilesRecursively())
                yield return file;
        }
    }
}
