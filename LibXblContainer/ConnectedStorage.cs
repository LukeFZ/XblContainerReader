using System.Collections.ObjectModel;
using System.Diagnostics;
using LibXblContainer.Models;
using LibXblContainer.Models.FileNames;

namespace LibXblContainer;

public class ConnectedStorage : IDisposable
{
    public ContainerIndex IndexMetadata { get; }
    public ReadOnlyCollection<Container> Containers => _containers.AsReadOnly();
    public string BasePath { get; }

    private readonly List<Container> _containers;
    private readonly IFileNameFormat _fileNameFormat;
    private readonly bool _readOnly;

    public ConnectedStorage(string containerPath, StoragePlatformType type = StoragePlatformType.Windows, bool readOnly = false)
    {
        BasePath = containerPath;
        _fileNameFormat = type switch
        {
            StoragePlatformType.Windows => new WindowsFileNameFormat(),
            StoragePlatformType.Xbox => new XboxFileNameFormat(),
            _ => throw new UnreachableException()
        };
        _readOnly = readOnly;

        var indexPath = Path.Join(BasePath, ContainerIndex.Name);
        if (!File.Exists(indexPath))
            throw new FileNotFoundException("Could not find container index in directory.");

        using var indexReader = new BinaryReader(File.OpenRead(indexPath));
        IndexMetadata = new ContainerIndex(indexReader);

        _containers = [];
        foreach (var container in IndexMetadata.Entries.Select(entry => new Container(BasePath, entry, _fileNameFormat)))
        {
            container.Load();
            _containers.Add(container);
        }
    }

    public Container Add(string fileName, string entryName)
    {
        if (_readOnly)
            throw new InvalidOperationException("Cannot add containers to a read only storage");

        var metadataEntry = new ContainerIndexEntry(fileName, entryName);
        var container = new Container(BasePath, metadataEntry, _fileNameFormat);
        container.Create();
        
        IndexMetadata.Entries.Add(metadataEntry);
        _containers.Add(container);

        return container;
    }

    public void Remove(string name, bool isEntryName = false)
    {
        if (_readOnly)
            throw new InvalidOperationException("Cannot remove containers in a read only storage");

        var container =
            Get(name, isEntryName) ??
            throw new ArgumentException($"Storage does not contain container with name {name}.");

        Remove(container);
    }

    public void Remove(Container container)
    {
        if (_readOnly)
            throw new InvalidOperationException("Cannot remove containers in a read only storage");

        //container.MetaData.FileSize = 0;
        container.MetaData.SetState(ContainerIndexEntryState.Deleted);
        //Directory.Delete(Path.Join(_basePath, container.MetaData.ContainerId.ToStringWithoutDashes()), true);
    }

    public Container? Get(string name, bool isEntryName = false)
        => _containers.FirstOrDefault(x => isEntryName 
            ? x.MetaData.EntryName == name 
            : x.MetaData.FileName == name);

    public void Write()
    {
        if (_readOnly)
            throw new InvalidOperationException("Cannot write in a read only storage");

        foreach (var container in _containers)
        {
            if (container.MetaData.State is not ContainerIndexEntryState.Synched)
            {
                IndexMetadata.MetaData.Flags = ContainerSyncFlags.FullyDownloaded;
                IndexMetadata.MetaData.LastModified = DateTime.Now;
            }
        }

        var indexPath = Path.Join(BasePath, ContainerIndex.Name);
        using var indexWriter = new BinaryWriter(File.OpenWrite(indexPath));
        IndexMetadata.Write(indexWriter);

        foreach (var container in _containers)
            container.Write();
    }

    public void Dispose()
    {
        if (!_readOnly)
            Write();

        GC.SuppressFinalize(this);
    }
}