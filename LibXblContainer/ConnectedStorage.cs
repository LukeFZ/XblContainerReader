using LibXblContainer.Models;

namespace LibXblContainer
{
    public class ConnectedStorage
    {
        public ContainerIndex IndexMetadata { get; }
        public List<Container> Containers { get; }

        private readonly string _basePath;

        public ConnectedStorage(string containerPath)
        {
            _basePath = containerPath;

            var indexPath = Path.Join(_basePath, ContainerIndex.Name);
            if (!File.Exists(indexPath))
                throw new FileNotFoundException("Could not find container index in directory.");

            using var indexReader = new BinaryReader(File.OpenRead(indexPath));
            IndexMetadata = new ContainerIndex(indexReader);

            Containers = new List<Container>(IndexMetadata.Entries.Count);
            foreach (var metadataEntry in IndexMetadata.Entries)
            {
                Containers.Add(new Container(_basePath, metadataEntry));
            }
        }

        public void Save()
        {
            var indexPath = Path.Join(_basePath, ContainerIndex.Name);
            using var indexWriter = new BinaryWriter(File.OpenWrite(indexPath));
            IndexMetadata.Write(indexWriter);
        }
    }
}