using System.Text;

namespace LibXblContainer.Models;

public record BlobRecord
{
    public readonly string Name;
    public readonly Guid BlobAtomId; // TODO: Unsure about this
    public readonly Guid BlobFileId;

    public BlobRecord(string name, Guid atomId = default, Guid fileId = default)
    {
        Name = name;
        BlobFileId = fileId == Guid.Empty ? Guid.NewGuid() : fileId;
        BlobAtomId = atomId == Guid.Empty ? BlobFileId : atomId;
    }

    public BlobRecord(BinaryReader reader)
    {
        var blobData = reader.ReadBytes(0x80);

        Name = string.Empty;
        int blobNameLen;
        for (blobNameLen = 0; blobData[blobNameLen * 2] != 0; blobNameLen++) { }

        if (blobNameLen != 0)
            Name = Encoding.Unicode.GetString(blobData, 0, blobNameLen * 2);

        BlobAtomId = new Guid(reader.ReadBytes(16));
        BlobFileId = new Guid(reader.ReadBytes(16));
    }

    public void Write(BinaryWriter writer)
    {
        var blobData = new byte[0x80];
        var nameBytes = Encoding.Unicode.GetBytes(Name);
        if (nameBytes.Length > 0x80)
            throw new InvalidDataException("Blob name too long.");

        Buffer.BlockCopy(nameBytes, 0, blobData, 0, nameBytes.Length);
        writer.Write(blobData);
        writer.Write(BlobAtomId.ToByteArray());
        writer.Write(BlobFileId.ToByteArray());
    }
}