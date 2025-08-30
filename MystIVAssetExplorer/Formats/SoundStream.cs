using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace MystIVAssetExplorer.Formats;

public sealed record SoundStream(
    ushort StreamId,
    ushort GroupId,
    ImmutableArray<ushort> SoundIds,
    bool ReferencesExternalDataFile,
    string DataFileName,
    ReadOnlyMemory<byte>? Data,
    int Length,
    int Offset,
    uint ByteRate,
    uint SampleRate,
    uint BitsPerSample,
    uint ChannelCount,
    SoundStreamFormat Format)
{
    public string GetStandaloneFileExtension()
    {
        return Format switch
        {
            SoundStreamFormat.PCM => ".wav",
            SoundStreamFormat.IMA => ".ima",
            SoundStreamFormat.OggVorbis => ".ogg",
        };
    }

    public async Task ExportStandaloneFileAsync(Stream stream, M4bFile? externalDataFile)
    {
        if (externalDataFile is not null)
        {
            if (Data is not null)
                throw new ArgumentException($"External data file must not be provided when {Data} is not null.", nameof(externalDataFile));

            if (!externalDataFile.Name.Equals(DataFileName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Data file name does not match.", nameof(externalDataFile));
        }
        else
        {
            if (Data is null)
                throw new ArgumentException($"External data file must be provided when {Data} is null.", nameof(externalDataFile));
        }

        if (Format is SoundStreamFormat.PCM)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(44);
            var header = buffer.AsMemory(..44);
            var writer = new SpanWriter(header.Span);

            writer.Write("RIFF"u8);
            writer.WriteInt32LittleEndian(36 + Length); // Remaining file size
            writer.Write("WAVE"u8);

            writer.Write("fmt "u8);
            writer.WriteUInt32LittleEndian(16); // Remaining chunk size
            writer.WriteUInt16LittleEndian(1); // PCM format
            writer.WriteUInt16LittleEndian((ushort)ChannelCount);
            writer.WriteUInt32LittleEndian(SampleRate);
            writer.WriteUInt32LittleEndian(ByteRate);
            var blockAlign = BitsPerSample * ChannelCount / 8;
            writer.WriteUInt16LittleEndian((ushort)blockAlign);
            writer.WriteUInt16LittleEndian((ushort)BitsPerSample);

            writer.Write("data"u8);
            writer.WriteInt32LittleEndian(Length);

            await stream.WriteAsync(header);
            ArrayPool<byte>.Shared.Return(buffer);
        }

        await stream.WriteAsync(Data ?? externalDataFile!.Memory.Slice(Offset, Length));
    }
}
