using LibXblContainer;
using Spectre.Console;

namespace XblContainerReader;

public static class OutputUtils
{
    public static string Property<T>(string name, T value)
    {
        return $"[white bold]{name}:[/] [green bold]{value}[/]";
    }

    public static void DrawStorage(Tree tree, ConnectedStorage storage)
    {
        var version = storage.IndexMetadata.MetaData.Version;

        var rootNode = tree.AddNode($":file_cabinet: [blue]{storage.BasePath}[/]");
        rootNode.AddNode(Property("Version", version));
        rootNode.AddNode(Property("Entry Count", storage.IndexMetadata.MetaData.EntryCount));

        if (version >= 7)
        {
            rootNode.AddNode(Property("Name", storage.IndexMetadata.MetaData.Name!));
            rootNode.AddNode(Property("AUMID", storage.IndexMetadata.MetaData.Aumid!));

            if (version > 8)
            {
                rootNode.AddNode(Property("Last Modified", storage.IndexMetadata.MetaData.LastModified));
                rootNode.AddNode(Property("Flags", storage.IndexMetadata.MetaData.Flags));

                if (version >= 13)
                {
                    rootNode.AddNode(Property("Root ID", storage.IndexMetadata.MetaData.RootContainerId));

                    if (version >= 14)
                    {
                        rootNode.AddNode(Property("Reserved", Convert.ToHexString(storage.IndexMetadata.MetaData.Reserved!)));
                    }
                }
            }
        }

        var containersNode = rootNode.AddNode("[white bold]Containers:[/]");

        foreach (var container in storage.Containers)
        {
            var node = containersNode.AddNode($":file_folder: [blue]{container.MetaData.ContainerId}[/]");

            node.AddNode(Property("File name", container.MetaData.FileName));
            if (storage.IndexMetadata.MetaData.Version > 12)
                node.AddNode(Property("Entry name", container.MetaData.EntryName));

            node.AddNode(Property("Last Modified", container.MetaData.LastModified));
            node.AddNode(Property("File size", container.MetaData.FileSize));
            node.AddNode(Property("ETag", container.MetaData.Etag));
            node.AddNode(Property("State", container.MetaData.State));
            node.AddNode(Property("Type", container.MetaData.Type));

            var blobsNode = node.AddNode("[white bold]Blobs:[/]");
            foreach (var blob in container.Blobs.Records)
            {
                var blobNode = blobsNode.AddNode($":page_facing_up: [blue]{blob.BlobFileId}[/]");
                blobNode.AddNode(Property("Name", blob.Name));
                blobNode.AddNode(Property("Atom ID", blob.BlobAtomId));
            }
        }
    }
}