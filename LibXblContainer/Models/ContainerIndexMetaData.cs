namespace LibXblContainer.Models;

public class ContainerIndexMetaData
{
    public readonly uint Version;
    public int EntryCount { get; internal set; }

    // Metadata
    public readonly string? NonAppOwner;
    public readonly string? PackageOwner;

    // Sync Metadata
    public readonly byte[]? UnkData; // Think this is title id but not 100% sure
    public readonly ContainerSyncFlags Flags;
    public readonly Guid RootContainerId;
    public readonly byte[]? UnkData2;

    public ContainerIndexMetaData(BinaryReader stream)
    {
        Version = stream.ReadUInt32();
        EntryCount = stream.ReadInt32();
        if (Version > 14) throw new InvalidDataException("Invalid container index version.");
        if (Version < 7) return;

        NonAppOwner = stream.ReadUnicode();
        PackageOwner = stream.ReadUnicode(130);
        if (Version <= 8) return;

        UnkData = stream.ReadBytes(8);
        if (Version == 9)
        {
            Flags = stream.ReadByte() != 0 ? ContainerSyncFlags.FullyUploaded : ContainerSyncFlags.None;
            return;
        }

        var guid = string.Empty;

        Flags = (ContainerSyncFlags)stream.ReadUInt32();
        if (Version >= 13) guid = stream.ReadUnicode();
        if (Version >= 14) UnkData2 = stream.ReadBytes(8);

        RootContainerId = string.IsNullOrEmpty(guid) ? Guid.Empty : Guid.Parse(guid);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(EntryCount);
        if (Version < 7) return;

        writer.WriteUnicode(NonAppOwner!);
        writer.WriteUnicode(PackageOwner!, 130);
        if (Version <= 8) return;

        writer.Write(UnkData!);
        if (Version == 9)
        {
            writer.Write(Flags == ContainerSyncFlags.FullyUploaded);
            return;
        }

        writer.Write((uint)Flags);
        if (Version >= 13) writer.WriteUnicode(RootContainerId.ToString());
        if (Version >= 14) writer.Write(UnkData2!);
    }
}