using MystIVAssetExplorer.Memory;
using System;

namespace MystIVAssetExplorer.Formats;

public static class RiffWave
{
    public static void WritePcmHeader(
        Span<byte> buffer,
        ushort channelCount,
        uint sampleRate,
        ushort bitsPerSample,
        uint dataLength)
    {
        var writer = new SpanWriter(buffer);

        writer.Write("RIFF"u8);
        writer.WriteUInt32LittleEndian(36 + dataLength); // Remaining file size
        writer.Write("WAVE"u8);

        writer.Write("fmt "u8);
        writer.WriteUInt32LittleEndian(16); // Remaining chunk size
        writer.WriteUInt16LittleEndian(1); // PCM format
        writer.WriteUInt16LittleEndian(channelCount);
        writer.WriteUInt32LittleEndian(sampleRate);
        var blockAlign = (ushort)(bitsPerSample * channelCount / 8);
        var byteRate = sampleRate * blockAlign;
        writer.WriteUInt32LittleEndian(byteRate);
        writer.WriteUInt16LittleEndian(blockAlign);
        writer.WriteUInt16LittleEndian(bitsPerSample);

        writer.Write("data"u8);
        writer.WriteUInt32LittleEndian(dataLength);
    }
}
