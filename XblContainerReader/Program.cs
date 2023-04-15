using LibXblContainer;

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
            var storage = new ConnectedStorage(containerPath);

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

                if (entry.Records.Count > 1)
                {
                    Directory.CreateDirectory(Path.Join(output, entryName));
                    foreach (var blob in entry.Records.Records)
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
            foreach (var entry in storage.Containers)
            {
                var entryName = entry.MetaData.EntryName;

                if (entry.Records.Count > 1)
                {
                    foreach (var blob in entry.Records.Records)
                    {
                        Console.WriteLine("\tBlob name: " + blob.Name);
                        var blobPath = Path.Join(input, entryName, blob.Name);
                        if (!File.Exists(blobPath)) continue;

                        var newData = File.ReadAllBytes(blobPath);
                        entry.Update(newData, blob.Name);

                        Console.WriteLine("\t\tUpdated blob.");
                    }
                }
                else
                {
                    Console.WriteLine("\tFile name: " + entryName);
                    var containerFilePath = Path.Join(input, entryName);
                    if (!File.Exists(containerFilePath)) continue;

                    var newData = File.ReadAllBytes(containerFilePath);
                    entry.Update(newData);

                    Console.WriteLine("\t\tUpdated file.");
                }
            }

            storage.Save();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: XblContainerReader.exe <command> <path to container folder> <input/output path>");
            Console.WriteLine("Commands:");
            Console.WriteLine("\textract - extracts files from the container into the output directory");
            Console.WriteLine("\tupdate - updates files inside the container from the input directory");
        }
    }
}