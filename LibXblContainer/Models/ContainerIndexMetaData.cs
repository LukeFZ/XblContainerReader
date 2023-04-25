namespace LibXblContainer.Models;

public class ContainerIndexMetaData
{
    public readonly uint Version;
    public int EntryCount { get; internal set; }

    // Metadata
    public readonly string? Name;
    public readonly string? Aumid;

    // Sync Metadata
    public DateTime LastModified { get; internal set; }
    public ContainerSyncFlags Flags { get; internal set; }
    public readonly Guid RootContainerId;
    public readonly byte[]? Reserved;

    public ContainerIndexMetaData(BinaryReader stream)
    {
        Version = stream.ReadUInt32();
        EntryCount = stream.ReadInt32();
        if (Version > 14) throw new InvalidDataException("Invalid container index version.");
        if (Version < 7) return;

        Name = stream.ReadUnicode();
        Aumid = stream.ReadUnicode(130);
        if (Version <= 8) return;

        LastModified = DateTime.FromFileTime(stream.ReadInt64());
        if (Version == 9)
        {
            Flags = stream.ReadByte() != 0 ? ContainerSyncFlags.FullyUploaded : ContainerSyncFlags.None;
            return;
        }

        var guid = string.Empty;

        Flags = (ContainerSyncFlags)stream.ReadUInt32();
        if (Version >= 13) guid = stream.ReadUnicode();
        if (Version >= 14) Reserved = stream.ReadBytes(8);

        RootContainerId = string.IsNullOrEmpty(guid) ? Guid.Empty : Guid.Parse(guid);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(EntryCount);
        if (Version < 7) return;

        writer.WriteUnicode(Name!);
        writer.WriteUnicode(Aumid!, 130);
        if (Version <= 8) return;

        writer.Write(LastModified.ToFileTime());
        if (Version == 9)
        {
            writer.Write(Flags == ContainerSyncFlags.FullyUploaded);
            return;
        }

        writer.Write((uint)Flags);
        if (Version >= 13) writer.WriteUnicode(RootContainerId.ToString());
        if (Version >= 14) writer.Write(Reserved!);
    }
}