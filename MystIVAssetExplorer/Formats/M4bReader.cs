using MystIVAssetExplorer.Memory;
using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace MystIVAssetExplorer.Formats;

public static class M4bReader
{
    public static M4bContainingFolder OpenM4bFolder(string folderPath)
    {
        var subfolders = ImmutableArray.CreateBuilder<M4bContainingFolder>();

        foreach (var folder in Directory.GetDirectories(folderPath))
        {
            var subfolder = OpenM4bFolder(folder);
            if (subfolder is not { Subfolders: [], Archives: [] })
                subfolders.Add(subfolder);
        }

        var archives = ImmutableArray.CreateBuilder<M4bArchive>();

        foreach (var file in Directory.GetFiles(folderPath, "*.m4b"))
        {
            archives.Add(OpenM4bFile(file));
        }

        return new M4bContainingFolder
        {
            Name = Path.GetFileName(folderPath),
            FolderPath = folderPath,
            Subfolders = subfolders.DrainToImmutable(),
            Archives = archives.DrainToImmutable(),
        };
    }

    public static M4bArchive OpenM4bFile(string filePath)
    {
        var memoryMapping = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, mapName: null, 0, MemoryMappedFileAccess.Read);
        var memoryManager = memoryMapping.CreateViewMemoryManager(MemoryMappedFileAccess.Read);

        var reader = new SpanReader(memoryManager.GetSpan());
        ReadM4bHeader(ref reader);
        var rootDirectory = ReadFolderDirectory(memoryManager.Memory, ref reader);

        return new M4bArchive(memoryMapping, memoryManager, filePath)
        {
            Name = Path.GetFileName(filePath),
            Subdirectories = rootDirectory.Subdirectories,
            Files = rootDirectory.Files,
        };
    }

    private static void ReadM4bHeader(ref SpanReader reader)
    {
        var headerLength = reader.ReadInt32LittleEndian();
        if (headerLength != 11)
            throw new InvalidDataException("Expected header length of 11");

        var header = reader.ReadSpan(headerLength);
        if (!header.SequenceEqual("UBI_BF_SIG\0"u8))
            throw new InvalidDataException("Expected UBI_BF_SIG header");

        if (reader.ReadInt32LittleEndian() != 1)
            throw new InvalidDataException("Expected 01 00 00 00");

        if (reader.ReadInt32LittleEndian() != 0)
            throw new InvalidDataException("Expected 00 00 00 00");
    }

    private static (ImmutableArray<M4bDirectory> Subdirectories, ImmutableArray<M4bFile> Files) ReadFolderDirectory(ReadOnlyMemory<byte> entireArchive, ref SpanReader reader)
    {
        var subdirectoryCount = reader.ReadByte();
        var subdirectories = ImmutableArray.CreateBuilder<M4bDirectory>(subdirectoryCount);

        for (var i = 0; i < subdirectoryCount; i++)
        {
            var subdirectoryName = ReadName(ref reader);
            var subDirectory = ReadFolderDirectory(entireArchive, ref reader);

            subdirectories.Add(new M4bDirectory
            {
                Name = subdirectoryName,
                Subdirectories = subDirectory.Subdirectories,
                Files = subDirectory.Files,
            });
        }

        var fileCount = reader.ReadInt32LittleEndian();
        var files = ImmutableArray.CreateBuilder<M4bFile>(fileCount);

        for (var i = 0; i < fileCount; i++)
        {
            var fileName = ReadName(ref reader);
            var fileSize = reader.ReadInt32LittleEndian();
            var fileOffset = reader.ReadInt32LittleEndian();

            var fileMemory = entireArchive.Slice(fileOffset, fileSize);

            if (fileName.EndsWith(".m4b", StringComparison.OrdinalIgnoreCase))
            {
                var newReader = new SpanReader(fileMemory.Span);
                ReadM4bHeader(ref newReader);
                var rootDirectory = ReadFolderDirectory(fileMemory, ref newReader);

                subdirectories.Add(new M4bDirectory
                {
                    Name = fileName,
                    Subdirectories = rootDirectory.Subdirectories,
                    Files = rootDirectory.Files,
                });
            }
            else
            {
                files.Add(new M4bFile { Name = fileName, Offset = fileOffset, Memory = fileMemory });
            }
        }

        return (subdirectories.DrainToImmutable(), files.DrainToImmutable());
    }

    private static string ReadName(ref SpanReader reader)
    {
        var length = reader.ReadInt32LittleEndian();
        if (length > 256)
            throw new InvalidDataException("Name length too long");

        var name = reader.ReadSpan(length);
        if (!name.EndsWith([(byte)0]))
            throw new InvalidDataException("Expected null terminator");

        return Encoding.ASCII.GetString(name[..^1]);
    }
}
