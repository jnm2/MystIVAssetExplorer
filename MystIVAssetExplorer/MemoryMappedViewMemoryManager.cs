using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace MystIVAssetExplorer;

public sealed unsafe class MemoryMappedViewMemoryManager : MemoryManager<byte>
{
    private readonly MemoryMappedViewAccessor accessor;
    private readonly byte* pointer;

    public MemoryMappedViewMemoryManager(MemoryMappedFile memoryMappedFile, MemoryMappedFileAccess access)
    {
        accessor = memoryMappedFile.CreateViewAccessor(0, 0, access);
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
    }

    public override Span<byte> GetSpan()
    {
        return new(pointer, checked((int)accessor.Capacity));
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        var pointer = (byte*)null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
        return new MemoryHandle(pointer, default, this);
    }

    public override void Unpin()
    {
        accessor.SafeMemoryMappedViewHandle.ReleasePointer();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            accessor.Dispose();
        }
    }
}
