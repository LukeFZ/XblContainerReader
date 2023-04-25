using System.Collections.ObjectModel;
using LibXblContainer.Models;

namespace LibXblContainer;

public class ConnectedStorage : IDisposable
{
    public ContainerIndex IndexMetadata { get; }
    public ReadOnlyCollection<Container> Containers => _containers.AsReadOnly();

    private readonly List<Container> _containers;
    private readonly string _basePath;

    public ConnectedStorage(string containerPath)
    {
        _basePath = containerPath;

        var indexPath = Path.Join(_basePath, ContainerIndex.Name);
        if (!File.Exists(indexPath))
            throw new FileNotFoundException("Could not find container index in directory.");

        using var indexReader = new BinaryReader(File.OpenRead(indexPath));
        IndexMetadata = new ContainerIndex(indexReader);

        _containers = new List<Container>();
        foreach (var container in IndexMetadata.Entries.Select(entry => new Container(_basePath, entry)))
        {
            container.Load();
            _containers.Add(container);
        }
    }

    public Container Add(string fileName, string entryName)
    {
        var metadataEntry = new ContainerIndexEntry(fileName, entryName);
        var container = new Container(_basePath, metadataEntry);

        IndexMetadata.Entries.Add(metadataEntry);
        _containers.Add(container);

        Directory.CreateDirectory(Path.Join(_basePath, metadataEntry.ContainerId.ToStringWithoutDashes()));

        return container;
    }

    public void Remove(string name, bool isEntryName = false)
    {
        var container = Get(name, isEntryName);
        if (container == null)
            throw new ArgumentException($"Storage does not contain container with name {name}.");

        Remove(container);
    }

    public void Remove(Container container)
    {
        //container.MetaData.FileSize = 0;
        container.MetaData.SetState(ContainerIndexEntryState.Deleted);
        //Directory.Delete(Path.Join(_basePath, container.MetaData.ContainerId.ToStringWithoutDashes()), true);
    }

    public Container? Get(string name, bool isEntryName = false)
        => _containers.FirstOrDefault(x => isEntryName ? x.MetaData.EntryName == name : x.MetaData.FileName == name);

    public void Write()
    {
        foreach (var container in _containers)
        {
            if (container.MetaData.State is not ContainerIndexEntryState.Synched)
            {
                IndexMetadata.MetaData.Flags = ContainerSyncFlags.FullyDownloaded;
                IndexMetadata.MetaData.LastModified = DateTime.Now;
            }
        }

        var indexPath = Path.Join(_basePath, ContainerIndex.Name);
        using var indexWriter = new BinaryWriter(File.OpenWrite(indexPath));
        IndexMetadata.Write(indexWriter);

        foreach (var container in _containers)
            container.Write();
    }

    public void Dispose()
    {
        Write();
        GC.SuppressFinalize(this);
    }
}