namespace LibXblContainer.Models;

public readonly struct BlobRecords
{
    public readonly uint Version;
    public readonly uint Count;
    public readonly BlobRecord[] Records;

    public BlobRecords(BinaryReader reader)
    {
        Version = reader.ReadUInt32();
        if (Version != 4)
            throw new InvalidDataException("Invalid blob records version.");

        Count = reader.ReadUInt32();
        Records = new BlobRecord[Count];

        for (var i = 0; i < Count; i++)
        {
            Records[i] = new BlobRecord(reader);
        }
    }

    public BlobRecord? Get(string name)
        => Records.FirstOrDefault(record => record.Name == name);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(Count);
        foreach (var record in Records)
            record.Write(writer);
    }
}