using System;
using System.Collections.Immutable;
using System.Text;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public sealed record UbiSndSequence(
    string Name,
    UbiInitValueConst<float>? InitVolume,
    UbiInitValueConst<int>? InitNbLoop,
    UbiInitValueConst<float>? InitSpeedFactor,
    ImmutableArray<UbiSndSound> Sounds,
    ImmutableArray<UbiSndFamily> Families) : IUbiDeserializable<UbiSndSequence>
{
    public static ReadOnlySpan<byte> UbiClassName => "snd::Sequence"u8;

    public static UbiSndSequence DeserializeContents(ref UbiBinaryReader reader)
    {
        var name = Encoding.ASCII.GetString(reader.ReadString());

        var initVolume = reader.DeserializeNullable<UbiInitValueConst<float>>();
        var initNbLoop = reader.DeserializeNullable<UbiInitValueConst<int>>();
        var initSpeedFactor = reader.DeserializeNullable<UbiInitValueConst<float>>();
        var sounds = reader.DeserializeList<UbiSndSound>();
        var families = reader.DeserializeList<UbiSndFamily>();

        return new UbiSndSequence(name, initVolume, initNbLoop, initSpeedFactor, sounds, families);
    }
}
