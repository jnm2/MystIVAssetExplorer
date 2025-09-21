using System;
using System.Runtime.InteropServices;

namespace MystIVAssetExplorer;

public static class EnvironmentHelpers
{
    public static readonly char EnvPathSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

    public static string[] GetEnvironmentPaths()
    {
        return Environment.GetEnvironmentVariable("PATH")?.Split(EnvPathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }
}
