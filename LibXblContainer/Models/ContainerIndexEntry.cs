namespace LibXblContainer.Models;

public class ContainerIndexEntry
{
    public string FileName { get; }
    public string EntryName { get; }
    public string Etag { get; set; }

    public byte BlobId { get; }
    public ContainerIndexEntryState State { get; set; }
    public Guid ContainerId { get; }
    public DateTime LastModified { get; set; }
    public ContainerEntryType Type { get; }
    public uint UnkInt3 { get; }

    public long FileSize { get; set; }

    public ContainerIndexEntry(BinaryReader reader, uint version)
    {
        FileName = reader.ReadUnicode(255);
        EntryName = version >= 12 ? reader.ReadUnicode(127) : string.Empty;
        Etag = reader.ReadUnicode(256);

        BlobId = reader.ReadByte();
        State = (ContainerIndexEntryState)reader.ReadUInt32();
        if (State == ContainerIndexEntryState.None) State = ContainerIndexEntryState.Synched;

        ContainerId = new Guid(reader.ReadBytes(16));
        LastModified = DateTime.FromFileTimeUtc(reader.ReadInt64());
        Type = (ContainerEntryType)reader.ReadUInt32(); // Does not seem to be used anymore
        UnkInt3 = reader.ReadUInt32(); // Padding?
        FileSize = version > 10 ? reader.ReadInt64() : reader.ReadInt32();
    }

    public ContainerIndexEntry(string fileName, string entryName, Guid? entryGuid = null)
    {
        FileName = fileName;
        EntryName = entryName;
        Etag = string.Empty;

        BlobId = 1;
        ContainerId = entryGuid ?? Guid.NewGuid();
        LastModified = DateTime.Now;
        State = ContainerIndexEntryState.Created;
    }

    public void Write(BinaryWriter writer, uint version)
    {
        writer.WriteUnicode(FileName, 255);
        if (version >= 12) writer.WriteUnicode(EntryName, 127);
        writer.WriteUnicode(Etag, 256);

        writer.Write(BlobId);
        writer.Write((uint)State);

        writer.Write(ContainerId.ToByteArray());
        writer.Write(LastModified.ToFileTime());
        writer.Write((uint)Type);
        writer.Write(UnkInt3);
        if (version > 10) writer.Write(FileSize);
        else writer.Write((uint)FileSize);
    }

    public void SetModified(long fileSize)
    {
        FileSize = fileSize;
        SetState(ContainerIndexEntryState.Modified);
    }

    public void SetState(ContainerIndexEntryState state)
    {
        LastModified = DateTime.Now;
        State = state;
    }
}