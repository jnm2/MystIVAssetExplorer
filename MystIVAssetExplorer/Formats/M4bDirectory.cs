using System.Collections.Immutable;

namespace MystIVAssetExplorer.Formats;

public class M4bDirectory : M4bNode
{
    public ImmutableArray<M4bDirectory> Subdirectories { get; init; }
    public ImmutableArray<M4bFile> Files { get; init; }
}
