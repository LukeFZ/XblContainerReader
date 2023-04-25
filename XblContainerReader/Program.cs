using LibXblContainer;
using System.Text;

namespace XblContainerReader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                PrintHelp();
                return;
            }

            var containerPath = args[1];
            var secondPath = args[2];
            using var storage = new ConnectedStorage(containerPath);

            switch (args[0])
            {
                case "extract":
                    ExtractCommand(storage, secondPath);
                    break;
                case "update":
                    UpdateCommand(storage, secondPath);
                    break;
                default:
                    PrintHelp();
                    break;
            }
        }

        private static void ExtractCommand(ConnectedStorage storage, string output)
        {
            Directory.CreateDirectory(output);

            Console.WriteLine($"File list for container:");
            foreach (var entry in storage.Containers)
            {
                var entryName = entry.MetaData.EntryName;
                Console.WriteLine("\tFile name: " + entry.MetaData.FileName);
                Console.WriteLine("\tEntry name: " + entryName);
                Console.WriteLine("\tSize: " + entry.MetaData.FileSize);
                Console.WriteLine("\tLast modified: " + entry.MetaData.LastModified);

                if (entry.Blobs.Count > 1)
                {
                    Directory.CreateDirectory(Path.Join(output, entryName));
                    foreach (var blob in entry.Blobs.Records)
                    {
                        Console.WriteLine("\t\tBlob name: " + blob.Name);

                        using var outFile = File.OpenWrite(Path.Join(output, entryName, blob.Name));
                        using var inFile = entry.Open(blob.Name);

                        inFile.CopyTo(outFile);
                    }
                }
                else
                {
                    if (entryName.Contains('/'))
                        Directory.CreateDirectory(Path.Join(output, entryName[..entryName.LastIndexOf('/')]));

                    using var outFile = File.OpenWrite(Path.Join(output, entryName));
                    using var inFile = entry.Open();

                    inFile.CopyTo(outFile);
                }
            }
        }

        private static void UpdateCommand(ConnectedStorage storage, string input)
        {
            Console.WriteLine($"File status for container:");
            var containers = storage.Containers.ToList();
            var newContainers = Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories).Select(x => x.Replace('\\', '/')).ToList();
            Console.WriteLine(string.Join(',', newContainers));

            var containerAdditionEnabled = true;

            foreach (var entry in containers)
            {
                var entryName = entry.MetaData.EntryName;
                var containerPath = Path.Join(input, entryName).Replace('\\', '/');

                if (!newContainers.Contains(containerPath))
                {
                    Console.WriteLine("\tContainer name: " + entryName);
                    Console.WriteLine("\t\tRemoved container.");
                    storage.Remove(entry);
                    continue;
                }

                if (entry.Blobs.Count > 1)
                {
                    containerAdditionEnabled = false;

                    var blobs = entry.Blobs.Records.ToList();
                    var newBlobs = Directory.EnumerateFiles(containerPath).Select(x => x.Replace("\\", "/")).ToList();

                    foreach (var blob in blobs)
                    {
                        Console.WriteLine("\tBlob name: " + blob.Name);
                        var blobPath = Path.Join(containerPath, blob.Name).Replace("\\", "/");
                        if (!newContainers.Contains(blobPath))
                        {
                            entry.Remove(blob.Name);
                            Console.WriteLine("\t\tRemoved blob.");
                            continue;
                        }

                        var newData = File.ReadAllBytes(blobPath);
                        entry.Update(newData, blob.Name);

                        Console.WriteLine("\t\tUpdated blob.");
                        newBlobs.Remove(blobPath);
                        newContainers.Remove(blobPath);
                    }

                    foreach (var file in newBlobs)
                    {
                        var blobName = Path.GetFileName(file);
                        entry.Add(blobName, File.ReadAllBytes(file));
                        Console.WriteLine("\tBlob name: " + blobName);
                        Console.WriteLine("\t\tAdded blob.");
                        newContainers.Remove(file);
                    }
                }
                else
                {
                    Console.WriteLine("\tFile name: " + entryName);
                    var newData = File.ReadAllBytes(containerPath);
                    entry.Update(newData);

                    Console.WriteLine("\t\tUpdated file.");
                }

                newContainers.Remove(containerPath);
            }

            if (containerAdditionEnabled)
            {
                foreach (var newContainer in newContainers)
                {
                    var containerName = newContainer.Replace(input + '/', "");

                    // Important: This is not game agnostic!
                    var container = storage.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(containerName)),
                        containerName);

                    container.Add(null, File.ReadAllBytes(newContainer));

                    Console.WriteLine("\tContainer name: " + containerName);
                    Console.WriteLine("\t\tAdded container.");
                }
            }
            else
            {
                Console.WriteLine("Container addition disabled due to detecting multiple blobs being present in one container.");
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: XblContainerReader.exe <command> <path to container folder> <input/output path>");
            Console.WriteLine("Commands:");
            Console.WriteLine("\textract - extracts files from the container into the output directory");
            Console.WriteLine("\tupdate - updates files inside the container from the input directory");
            Console.WriteLine("\tIMPORTANT: Adding new containers using the \"update\" command is not guaranteed to work, as it does not currently work for games with more than one file per container.");
        }
    }
}