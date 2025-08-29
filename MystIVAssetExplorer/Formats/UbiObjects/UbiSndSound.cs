using System;
using System.Text;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public sealed record UbiSndSound(
    string Name,
    ushort SoundId,
    ushort GroupId,
    UbiSndSetting Default,
    uint NbSettings,
    uint Type,
    int VolumeLine) : IUbiDeserializable<UbiSndSound>
{
    public static ReadOnlySpan<byte> UbiClassName => "snd::Sound"u8;

    public static UbiSndSound DeserializeContents(ref UbiBinaryReader reader)
    {
        var name = Encoding.ASCII.GetString(reader.ReadString());
        var soundId = reader.SpanReader.ReadUInt16LittleEndian();
        var groupId = reader.SpanReader.ReadUInt16LittleEndian();
        var defaultSetting = UbiSndSetting.DeserializeContents(ref reader);
        var nbSettings = reader.SpanReader.ReadUInt32LittleEndian();
        var type = reader.SpanReader.ReadUInt32LittleEndian();
        var volumeLine = reader.SpanReader.ReadInt32LittleEndian();

        return new UbiSndSound(name, soundId, groupId, defaultSetting, nbSettings, type, volumeLine);
    }
}
