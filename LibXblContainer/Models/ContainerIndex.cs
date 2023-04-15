namespace LibXblContainer.Models;

public class ContainerIndex
{
    public const string Name = "containers.index";

    public ContainerIndexMetaData MetaData { get; }
    public List<ContainerIndexEntry> Entries { get; }

    public ContainerIndex(BinaryReader reader)
    {
        MetaData = new ContainerIndexMetaData(reader);

        Entries = new List<ContainerIndexEntry>(MetaData.EntryCount);
        for (var i = 0; i < MetaData.EntryCount; i++)
        {
            var entry = new ContainerIndexEntry(reader, MetaData.Version);
            Entries.Add(entry);
        }
    }

    public void Write(BinaryWriter writer)
    {
        MetaData.Write(writer);
        foreach (var entry in Entries)
            entry.Write(writer, MetaData.Version);
    }
}