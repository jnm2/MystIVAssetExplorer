using Microsoft.Win32;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MystIVAssetExplorer;

public sealed class ExternalVideoPlayer(string executablePath, Func<ExternalVideoPlayerOptions, ImmutableArray<string>> buildArguments)
{
    public static ExternalVideoPlayer? Detect()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\VLC media player", "InstallLocation", null) is string vlcInstallLocation)
        {
            return new ExternalVideoPlayer(
                Path.Join(vlcInstallLocation, "vlc.exe"),
                options =>
                [
                    "-", "--qt-minimal-view", "--play-and-exit",
                    .. options.VideoName is null ? [] : new[] { "--meta-title", options.VideoName },
                ]);
        }

        if (EnvironmentHelpers.GetEnvironmentPaths().Select(path => Path.Join(path, "ffplay.exe")).FirstOrDefault(File.Exists) is { } ffplayPath)
        {
            return new ExternalVideoPlayer(
                ffplayPath,
                options =>
                [
                    "-i", "-", "-autoexit",
                    .. options.VideoName is null ? [] : new[] { "-window_title", options.VideoName },
                ]);
        }

        return null;
    }

    public async Task PlayAsync(Func<Stream, Task> writeAsync, ExternalVideoPlayerOptions options)
    {
        using var process = Process.Start(new ProcessStartInfo(executablePath, buildArguments(options))
        {
            RedirectStandardInput = true,
            CreateNoWindow = true,
        })!;

        using (process.StandardInput.BaseStream)
        {
            try
            {
                await writeAsync(process.StandardInput.BaseStream);
            }
            catch (IOException ex) when ((ushort)ex.HResult == 109) // ERROR_BROKEN_PIPE "The pipe has been ended."
            {
            }
        }
    }
}
