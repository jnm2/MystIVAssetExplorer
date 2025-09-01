using MystIVAssetExplorer.Memory;
using System;
using System.IO.MemoryMappedFiles;

namespace MystIVAssetExplorer.Formats;

public sealed class M4bArchive(MemoryMappedFile memoryMappedFile, MemoryMappedViewMemoryManager memoryManager, string filePath) : M4bDirectory, IDisposable
{
    public string FilePath { get; } = filePath;

    public void Dispose()
    {
        ((IDisposable)memoryManager).Dispose();
        memoryMappedFile.Dispose();
    }
}
