using System;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public sealed record UbiSndSetting(
    UbiInitValueConst<float>? Volume,
    UbiInitValueConst<float>? FadeIn,
    uint FadeInCurve,
    UbiInitValueConst<float>? FadeOut,
    uint FadeOutCurve,
    UbiInitValueConst<float>? Pitch,
    UbiInitValueConst<int>? NbLoop,
    UbiInitValueConst<UbiVector3>? Position,
    int Behavior) : IUbiDeserializable<UbiSndSetting>
{
    public static ReadOnlySpan<byte> UbiClassName => "snd::Setting"u8;

    public static UbiSndSetting DeserializeContents(ref UbiBinaryReader reader)
    {
        var volume = reader.DeserializeNullable<UbiInitValueConst<float>>();
        var fadeIn = reader.DeserializeNullable<UbiInitValueConst<float>>();
        var fadeInCurve = reader.SpanReader.ReadUInt32LittleEndian();
        var fadeOut = reader.DeserializeNullable<UbiInitValueConst<float>>();
        var fadeOutCurve = reader.SpanReader.ReadUInt32LittleEndian();
        var pitch = reader.DeserializeNullable<UbiInitValueConst<float>>();
        var nbLoop = reader.DeserializeNullable<UbiInitValueConst<int>>();

        if (reader.ReadBoolean())
            throw new NotImplementedException("TODO: capture type of 'panning'");

        var position = reader.DeserializeNullable<UbiInitValueConst<UbiVector3>>();

        var behavior = reader.SpanReader.ReadInt32LittleEndian();

        return new UbiSndSetting(volume, fadeIn, fadeInCurve, fadeOut, fadeOutCurve, pitch, nbLoop, position, behavior);
    }
}