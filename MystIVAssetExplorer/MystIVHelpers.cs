using System;
using System.IO;
using System.IO.Enumeration;
using System.Linq;

namespace MystIVAssetExplorer;

public static class MystIVHelpers
{
    public static string? DetectDataDirectory()
    {
        return (
            from rootDirectory in new[]
            {
                Environment.GetFolderPath(Environment.Is64BitOperatingSystem
                    ? Environment.SpecialFolder.ProgramFilesX86
                    : Environment.SpecialFolder.ProgramFiles),
            }
            from entry in new FileSystemEnumerable<string>(rootDirectory, (ref entry) => entry.ToFullPath(), new EnumerationOptions { RecurseSubdirectories = true })
            {
                ShouldIncludePredicate = (ref entry) => entry.IsDirectory && entry.FileName is "data",
            }
            where File.Exists(Path.Join(entry, "data.m4b"))
            select entry).FirstOrDefault();
    }
}
