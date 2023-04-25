using System.Collections.ObjectModel;

namespace LibXblContainer.Models;

public class BlobRecords
{
    public const uint Version = 4;
    public ReadOnlyCollection<BlobRecord> Records => _records.AsReadOnly();
    public int Count => Records.Count;

    private readonly List<BlobRecord> _records;

    public BlobRecords()
    {
        _records = new List<BlobRecord>();
    }

    public BlobRecords(BinaryReader reader) : this()
    {
        var version = reader.ReadUInt32();
        if (version != Version)
            throw new InvalidDataException("Invalid blob records version.");

        var count = reader.ReadUInt32();
        for (uint i = 0; i < count; i++)
            _records.Add(new BlobRecord(reader));
    }

    public BlobRecord? Get(string name)
        => _records.FirstOrDefault(record => record.Name == name);

    public BlobRecord Add(string name)
    {
        if (_records.Any(x => x.Name == name))
            throw new ArgumentException($"Blob with name {name} already exists in container.");

        var record = new BlobRecord(name);
        _records.Add(record);
        return record;
    }

    public void Remove(string name)
    {
        if (_records.All(x => x.Name != name))
            throw new ArgumentException($"Blob with name {name} does not exist in container.");

        _records.Remove(_records.First(x => x.Name == name));
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Version);
        writer.Write(_records.Count);
        foreach (var record in _records)
            record.Write(writer);
    }
}