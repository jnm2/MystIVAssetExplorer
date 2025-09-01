using System.IO.MemoryMappedFiles;

namespace MystIVAssetExplorer.Memory;

internal static class Extensions
{
    public static MemoryMappedViewMemoryManager CreateViewMemoryManager(this MemoryMappedFile memoryMappedFile, MemoryMappedFileAccess access)
    {
        return new MemoryMappedViewMemoryManager(memoryMappedFile, access);
    }
}
