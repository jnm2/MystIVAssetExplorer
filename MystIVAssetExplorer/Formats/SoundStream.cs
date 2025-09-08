using System;
using System.Buffers;
using System.Buffers.Binary;
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
    public SoundStreamFormat GetExportFormat() => Format switch
    {
        SoundStreamFormat.IMA => SoundStreamFormat.PCM,
        _ => Format,
    };

    public string GetStandaloneExportFileExtension()
    {
        return Format switch
        {
            SoundStreamFormat.PCM or SoundStreamFormat.IMA => ".wav",
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

        var data = Data ?? externalDataFile!.Memory.Slice(Offset, Length);

        if (Format is SoundStreamFormat.PCM)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(44);
            var header = buffer.AsMemory(..44);

            RiffWave.WritePcmHeader(
                header.Span,
                (ushort)ChannelCount,
                SampleRate,
                (ushort)BitsPerSample,
                (uint)data.Length);

            await stream.WriteAsync(header);
            ArrayPool<byte>.Shared.Return(buffer);

            await stream.WriteAsync(data);
        }
        else if (Format is SoundStreamFormat.IMA)
        {
            const int imaHeaderSize = 28;
            var rawPcmByteCount = 10 << (int)ChannelCount;
            var adpcmData = data[(imaHeaderSize + rawPcmByteCount)..];

            var totalPcmByteCount = rawPcmByteCount + (adpcmData.Length * 4);

            var buffer = ArrayPool<byte>.Shared.Rent(8192);

            var header = buffer.AsMemory(..44);
            RiffWave.WritePcmHeader(
                header.Span,
                (ushort)ChannelCount,
                SampleRate,
                (ushort)BitsPerSample,
                (uint)totalPcmByteCount);
            await stream.WriteAsync(header);

            await stream.WriteAsync(data.Slice(imaHeaderSize, rawPcmByteCount));

            if (ChannelCount > 2)
                throw new NotImplementedException();

            // This previous sample is the same as the raw PCM sample for all single-channel IMA streams with sb0 entry type 1.
            var channelStates = new (short previousSample, byte previousIndex)[ChannelCount];

            channelStates[0] = (
                previousSample: BinaryPrimitives.ReadInt16LittleEndian(data.Span[16..]),
                previousIndex: data.Span[18]);

            if (ChannelCount == 2)
            {
                channelStates[1] = (
                    previousSample: BinaryPrimitives.ReadInt16LittleEndian(data.Span[20..]),
                    previousIndex: data.Span[22]);
            }

            var adpcmDecoder = new ImaAdpcm16BitDecoder(channelStates);

            while (!adpcmData.IsEmpty)
            {
                var (bytesRead, bytesWritten) = adpcmDecoder.Decode(adpcmData.Span, buffer);
                adpcmData = adpcmData[bytesRead..];
                await stream.WriteAsync(buffer.AsMemory(0, bytesWritten));
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }
        else
        {
            await stream.WriteAsync(data);
        }
    }
}
