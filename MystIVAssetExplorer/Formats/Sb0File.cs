using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MystIVAssetExplorer.Formats;

public sealed record Sb0File(ImmutableArray<SoundStream> SoundStreams)
{
    public static Sb0File Deserialize(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);

        if (reader.ReadUInt16LittleEndian() != 6) throw new NotImplementedException("Expected 6");
        if (reader.ReadUInt16LittleEndian() != 18) throw new NotImplementedException("Expected 18");
        var soundCount = reader.ReadInt32LittleEndian();
        var streamCount = reader.ReadInt32LittleEndian();

        var containsOwnAudioData = reader.ReadInt32LittleEndian() switch
        {
            0 => false,
            1 => true,
            _ => throw new NotImplementedException("Expected 0 or 1"),
        };

        _ = reader.ReadInt32LittleEndian();
        if (reader.ReadInt32LittleEndian() != -1) throw new NotImplementedException("Expected -1");
        if (reader.ReadInt32LittleEndian() != -1) throw new NotImplementedException("Expected -1");

        var soundIdsByStreamIndex = new List<(ushort SoundId, ushort GroupId)>?[streamCount];

        for (var i = 0; i < soundCount; i++)
        {
            var soundId = reader.ReadUInt16LittleEndian();
            var groupId = reader.ReadUInt16LittleEndian();
            if (reader.ReadInt32LittleEndian() != 9) throw new NotImplementedException("Expected 9");
            var streamIndex = reader.ReadInt32LittleEndian();
            _ = reader.ReadSpan(96);

            var ids = soundIdsByStreamIndex[streamIndex] ??= [];
            ids.Add((soundId, groupId));
        }

        var soundStreams = ImmutableArray.CreateBuilder<SoundStream>();
        soundStreams.Capacity = streamCount;

        for (var i = 0; i < streamCount; i++)
        {
            var streamId = reader.ReadUInt16LittleEndian();
            var groupId = reader.ReadUInt16LittleEndian();
            var entryType = reader.ReadInt32LittleEndian();
            if (entryType != 1) throw new Exception($"Unexpected entry type {entryType}");
            var length = reader.ReadInt32LittleEndian();
            if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");
            var offset = reader.ReadInt32LittleEndian();
            if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");
            if (reader.ReadInt32LittleEndian() != 1) throw new NotImplementedException("Expected 1");
            if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");
            if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");

            var referencesExternalDataFile = reader.ReadInt32LittleEndian() switch
            {
                0 => false,
                1 => true,
                _ => throw new NotImplementedException("Expected 0 or 1"),
            };

            var unknown2 = reader.ReadInt32LittleEndian();
            if (unknown2 is not (0 or 1)) throw new NotImplementedException("Expected 0 or 1");

            if (reader.ReadInt32LittleEndian() != 1) throw new NotImplementedException("Expected 1");
            if (unknown2 == 1)
            {
                if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");
                if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");
            }
            _ = reader.ReadInt32LittleEndian();
            var repeatedLength = reader.ReadInt32LittleEndian();
            if (repeatedLength != length) throw new NotImplementedException("Expected repeated length");
            if (unknown2 == 0)
            {
                if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");
                if (reader.ReadInt32LittleEndian() != 0) throw new NotImplementedException("Expected 0");
            }
            var byteRate = reader.ReadUInt32LittleEndian();
            var sampleRate = reader.ReadUInt32LittleEndian();
            var bitsPerSample = reader.ReadUInt32LittleEndian();
            if (bitsPerSample != 16) throw new NotImplementedException("Expected 16 bits per sample");
            var channelCount = reader.ReadUInt32LittleEndian();
            if (channelCount is not (1 or 2)) throw new NotImplementedException("Expected 1 or 2 channels");
            var format = (SoundStreamFormat)reader.ReadUInt32LittleEndian();
            if (!Enum.IsDefined(format))
                throw new NotImplementedException("Unrecognized format");
            var name = Encoding.ASCII.GetString(reader.ReadSpan(72).TrimEnd((byte)0));
            if (reader.ReadInt32LittleEndian() != 1) throw new NotImplementedException("Expected 1");
            if (reader.ReadInt32LittleEndian() != -1) throw new NotImplementedException("Expected -1");

            if (!referencesExternalDataFile && !containsOwnAudioData)
                throw new NotImplementedException("The header claims the file has no audio data of its own, but the stream entry claims to reference some.");

            var soundIds = soundIdsByStreamIndex[i];
            if (soundIds is null)
                throw new NotImplementedException("Unused stream");

            if (soundIds.Any(id => id.GroupId != groupId))
                throw new NotImplementedException("Sounds and stream are in different groups");

            soundStreams.Add(new SoundStream(streamId, groupId, [.. soundIds.Select(id => id.SoundId)], referencesExternalDataFile, name, Data: null, length, offset, byteRate, sampleRate, bitsPerSample, channelCount, format));
        }

        if (containsOwnAudioData)
        {
            var containedDataStreamCount = reader.ReadInt32LittleEndian();
            if (containedDataStreamCount != 1)
                throw new NotImplementedException("More than one contained data stream");

            var dataLength = reader.ReadInt32LittleEndian();
            var currentOffset = memory.Length - reader.Span.Length;
            var containedDataStreams = memory.Slice(currentOffset, dataLength);

            for (var i = 0; i < soundStreams.Count; i++)
            {
                var stream = soundStreams[i];
                if (!stream.ReferencesExternalDataFile)
                    soundStreams[i] = stream with { Data = containedDataStreams.Slice(stream.Offset, stream.Length) };
            }
        }

        return new Sb0File(soundStreams.MoveToImmutable());
    }
}
