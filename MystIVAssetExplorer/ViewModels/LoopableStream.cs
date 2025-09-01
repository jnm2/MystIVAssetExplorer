using NAudio.Wave;

namespace MystIVAssetExplorer.ViewModels;

public sealed class LoopableStream(WaveStream stream) : WaveStream
{
    public bool EnableLooping { get; set; }

    public override WaveFormat WaveFormat => stream.WaveFormat;

    public override long Length => stream.Length;

    public override long Position { get => stream.Position; set => stream.Position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            var bytesRead = stream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

            if (bytesRead == 0)
            {
                if (stream.Position != stream.Length || !EnableLooping)
                    break;

                stream.Position = 0;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            stream.Dispose();

        base.Dispose(disposing);
    }
}
