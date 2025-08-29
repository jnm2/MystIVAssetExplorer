using System;
using System.Collections.Immutable;
using System.Text;

namespace MystIVAssetExplorer.Formats.UbiObjects;

public sealed record UbiSndFamily(
    string Name,
    ImmutableArray<string> SoundList,
    uint PickerType,
    bool SyncOnFamily,
    UbiInitValueConst<float>? InitVolume,
    UbiInitValueConst<int>? InitNbLoop,
    UbiInitValue<float>? InitWaitTimeBegin,
    UbiInitValue<float>? InitWaitTime,
    UbiInitValue<float>? InitWaitTimeEnd,
    uint RulesSize) : IUbiDeserializable<UbiSndFamily>
{
    public static ReadOnlySpan<byte> UbiClassName => "snd::Familly"u8;

    public static UbiSndFamily DeserializeContents(ref UbiBinaryReader reader)
    {
        var name = Encoding.ASCII.GetString(reader.ReadString());

        var soundList = ImmutableArray.CreateBuilder<string>();
        soundList.Capacity = reader.SpanReader.ReadInt32LittleEndian();

        for (var i = 0; i < soundList.Capacity; i++)
            soundList.Add(Encoding.ASCII.GetString(reader.ReadString()));

        var pickerType = reader.SpanReader.ReadUInt32LittleEndian();
        var syncOnFamily = reader.ReadBoolean();
        if (syncOnFamily) throw new NotImplementedException("SyncOnFamilyName");

        var initVolume = reader.DeserializeNullable<UbiInitValueConst<float>>();
        var initNbLoop = reader.DeserializeNullable<UbiInitValueConst<int>>();
        var initWaitTimeBegin = reader.DeserializeNullable<UbiInitValue<float>>();
        var initWaitTime = reader.DeserializeNullable<UbiInitValue<float>>();
        var initWaitTimeEnd = reader.DeserializeNullable<UbiInitValue<float>>();
        var rulesSize = reader.SpanReader.ReadUInt32LittleEndian();

        return new UbiSndFamily(name, soundList.MoveToImmutable(), pickerType, syncOnFamily, initVolume, initNbLoop, initWaitTimeBegin, initWaitTime, initWaitTimeEnd, rulesSize);
    }
}
