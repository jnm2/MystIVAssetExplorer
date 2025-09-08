using System;
using System.Buffers.Binary;

namespace MystIVAssetExplorer.Formats;

/// <summary>
/// Implements <see href="https://www.cs.columbia.edu/~hgs/audio/dvi/IMA_ADPCM.pdf"/>.
/// </summary>
public sealed class ImaAdpcm16BitDecoder(Memory<(short PreviousSample, byte PreviousIndex)> channelStates)
{
    private int channelIndex;

    private static readonly int[] IndexTable = [-1, -1, -1, -1, 2, 4, 6, 8];

    private static readonly int[] StepTable =
    [
        7, 8, 9, 10, 11, 12, 13,
        14, 16, 17, 19, 21, 23, 25, 28,
        31, 34, 37, 41, 45, 50, 55, 60,
        66, 73, 80, 88, 97, 107, 118,
        130, 143, 157, 173, 190, 209, 230,
        253, 279, 307, 337, 371, 408, 449,
        494, 544, 598, 658, 724, 796, 876,
        963, 1060, 1166, 1282, 1411, 1552,
        1707, 1878, 2066, 2272, 2499, 2749,
        3024, 3327, 3660, 4026, 4428, 4871,
        5358, 5894, 6484, 7132, 7845, 8630,
        9493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385,
        24623, 27086, 29794, 32767,
    ];

    public (int BytesRead, int BytesWritten) Decode(ReadOnlySpan<byte> adpcmData, Span<byte> pcmData)
    {
        var bytesWritten = 0;

        foreach (var adpcmByte in adpcmData)
        {
            if (pcmData.Length < bytesWritten + sizeof(short) * 2)
                break;

            DecodeNibble((byte)(adpcmByte >> 4), pcmData[bytesWritten..]);
            DecodeNibble((byte)(adpcmByte & 0xF), pcmData[(bytesWritten + sizeof(short))..]);

            bytesWritten += 4;
        }

        return (bytesWritten >> 2, bytesWritten);
    }

    private void DecodeNibble(byte nibble, Span<byte> pcmData)
    {
        ref var channelState = ref channelStates.Span[channelIndex];
        channelIndex = (channelIndex + 1) % channelStates.Length;

        var stepSize = StepTable[channelState.PreviousIndex];

        var difference = ((((nibble & 0b111) << 1) + 1) * stepSize) >> 3;

        if ((nibble & 8) != 0)
            difference = -difference;

        channelState.PreviousSample = (short)Math.Clamp(channelState.PreviousSample + difference, short.MinValue, short.MaxValue);
        channelState.PreviousIndex = (byte)Math.Clamp(channelState.PreviousIndex + IndexTable[nibble & 7], 0, 88);

        BinaryPrimitives.WriteInt16LittleEndian(pcmData, channelState.PreviousSample);
    }
}
