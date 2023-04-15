namespace LibXblContainer.Models;

public class ContainerIndexEntry
{
    public string FileName { get; }
    public string EntryName { get; }
    public string Etag { get; set; }

    public byte BlobId { get; }
    public uint UnkInt { get; }
    public Guid ContainerId { get; }
    public DateTime LastModified { get; set; }
    public uint UnkInt2 { get; }
    public uint UnkInt3 { get; }

    public long FileSize { get; set; }

    public ContainerIndexEntry(BinaryReader reader, uint version)
    {
        FileName = reader.ReadUnicode(255);
        EntryName = reader.ReadUnicode(127);
        Etag = reader.ReadUnicode(256);

        BlobId = reader.ReadByte();
        UnkInt = reader.ReadUInt32();
        if (UnkInt == 0) UnkInt = 1;

        ContainerId = new Guid(reader.ReadBytes(16));
        LastModified = DateTime.FromFileTime(reader.ReadInt64());
        UnkInt2 = reader.ReadUInt32();
        UnkInt3 = reader.ReadUInt32();
        FileSize = version > 10 ? reader.ReadInt64() : reader.ReadInt32();
    }

    public void Write(BinaryWriter writer, uint version)
    {
        writer.WriteUnicode(FileName, 255);
        writer.WriteUnicode(EntryName, 127);
        writer.WriteUnicode(Etag, 256);

        writer.Write(BlobId);
        writer.Write(UnkInt);

        writer.Write(ContainerId.ToByteArray());
        writer.Write(LastModified.ToFileTime());
        writer.Write(UnkInt2);
        writer.Write(UnkInt3);
        if (version > 10) writer.Write(FileSize);
        else writer.Write((uint)FileSize);
    }

    public void Update(FileInfo newFileInfo)
    {
        LastModified = newFileInfo.LastWriteTime;
        FileSize = newFileInfo.Length;
    }
}