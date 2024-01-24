using System.ComponentModel;
using System.Diagnostics;
using LibXblContainer;
using LibXblContainer.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace XblContainerReader
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.AddCommand<InfoCommand>("info").WithDescription("Prints information about a storage.");
                config.AddCommand<ExtractCommand>("extract").WithDescription("Extracts containers/blobs.");
                config.AddCommand<ImportCommand>("import").WithDescription("Clears a storage and imports a folder into it.");
            });
            return await app.RunAsync(args);
        }
    }

    internal class ContainerSettings : CommandSettings
    {
        [CommandArgument(0, "<container>")]
        public required string ContainerPath { get; init; }

        [CommandOption("-t|--type")]
        [Description("Which platform the container is used by. Affects container/blob filenames. Allowed values: Windows, Xbox")]
        [DefaultValue(StoragePlatformType.Windows)]
        public StoragePlatformType StorageType { get; init; }
    }

    internal abstract class ContainerCommand<T> : Command<T> where T : ContainerSettings
    {
        public override ValidationResult Validate(CommandContext context, T settings)
        {
            if (!File.Exists(Path.Join(settings.ContainerPath, ContainerIndex.Name)))
                return ValidationResult.Error("container.index does not exist.");

            return base.Validate(context, settings);
        }
    }

    internal abstract class AsyncContainerCommand<T> : AsyncCommand<T> where T : ContainerSettings
    {
        public override ValidationResult Validate(CommandContext context, T settings)
        {
            if (!File.Exists(Path.Join(settings.ContainerPath, ContainerIndex.Name)))
                return ValidationResult.Error("container.index does not exist.");

            return base.Validate(context, settings);
        }
    }

    internal enum ContainerMode
    {
        SingleFileContainer,
        SingleFileBlob
    }

    internal sealed class InfoCommand : ContainerCommand<InfoCommand.Settings>
    {
        internal sealed class Settings : ContainerSettings
        {
            [CommandOption("-l|--log")]
            public string? LogFilePath { get; init; }
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var treePanel = new Tree("[white bold]Storage contents:[/]");

            if (File.Exists(Path.Join(settings.ContainerPath, ContainerIndex.Name)))
            {
                using var storage = new ConnectedStorage(settings.ContainerPath, settings.StorageType, true);
                OutputUtils.DrawStorage(treePanel, storage);
            }
            else
            {
                foreach (var container in Directory
                             .EnumerateDirectories(settings.ContainerPath, "*", SearchOption.AllDirectories)
                             .Where(x => File.Exists(Path.Join(x, ContainerIndex.Name)))
                        )
                {
                    using var storage = new ConnectedStorage(container, settings.StorageType, true);
                    OutputUtils.DrawStorage(treePanel, storage);
                }
            }

            AnsiConsole.Record();
            AnsiConsole.Write(treePanel);
            var log = AnsiConsole.ExportText();

            if (settings.LogFilePath != null)
                File.WriteAllText(settings.LogFilePath, log);

            return 0;
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (!Directory.Exists(settings.ContainerPath))
                return ValidationResult.Error("Input directory does not exist.");

            return ValidationResult.Success();
        }
    }

    internal sealed class ExtractCommand : ContainerCommand<ExtractCommand.Settings>
    {
        internal sealed class Settings : ContainerSettings
        {
            [CommandArgument(1, "<output>")]
            public required string OutputPath { get; init; }

            [CommandOption("-m|--mode")]
            [Description("Forces parsing as a specific container mode. Allowed values: SingleFileContainer, SingleFileBlob")]
            public ContainerMode? Mode { get; init; }
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            using var storage = new ConnectedStorage(settings.ContainerPath, settings.StorageType, true);

            Directory.CreateDirectory(settings.OutputPath);

            foreach (var container in storage.Containers)
            {
                if (container.MetaData.State == ContainerIndexEntryState.Deleted)
                    continue;

                var containerOutputPath = Path.Join(settings.OutputPath, container.MetaData.FileName);

                if (settings.Mode == ContainerMode.SingleFileContainer || (!settings.Mode.HasValue && container.Blobs.Count == 1))
                {
                    if (container.Blobs.Count != 1)
                        throw new InvalidOperationException(
                            "Cannot extract container with multiple blobs in SingleFileContainer mode");

                    if (container.MetaData.FileName.Contains('/'))
                        Directory.CreateDirectory(Directory.GetParent(containerOutputPath)!.FullName);

                    using var outFile = File.OpenWrite(containerOutputPath);
                    using var inFile = container.Open();

                    inFile.CopyTo(outFile);
                }
                else
                {
                    Directory.CreateDirectory(containerOutputPath);

                    foreach (var blob in container.Blobs.Records)
                    {
                        var blobOutputPath = Path.Join(containerOutputPath, blob.Name);

                        using var output = File.OpenWrite(blobOutputPath);
                        using var input = container.Open(blob.Name);

                        input.CopyTo(output);
                    }
                }
            }

            AnsiConsole.MarkupLine("[green bold]Successfully extracted containers.[/]");

            return 0;
        }
    }

    internal sealed class ImportCommand : AsyncContainerCommand<ImportCommand.Settings>
    {
        internal sealed class Settings : ContainerSettings
        {
            [CommandArgument(1, "<input>")]
            public required string InputPath { get; init; }

            [CommandOption("-m|--mode")]
            [Description("Sets the mode using for importing the files into containers. Allowed values: SingleFileContainer, SingleFileBlob")]
            public ContainerMode Mode { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            using (var storage = new ConnectedStorage(settings.ContainerPath, settings.StorageType))
            {
                foreach (var container in storage.Containers)
                    storage.Remove(container);

                var fullInputPath = Path.GetFullPath(settings.InputPath);

                foreach (var file in Directory.EnumerateFiles(settings.InputPath, "*", SearchOption.AllDirectories))
                {
                    switch (settings.Mode)
                    {
                        case ContainerMode.SingleFileContainer:
                        {
                            var containerName = file[fullInputPath.Length..].Replace("\\", "/");
                            var container = storage.Add(containerName, containerName);
                            await using var input = File.OpenRead(file);
                            await container.AddAsync(blobData: input);
                            break;
                        }
                        case ContainerMode.SingleFileBlob:
                        {
                            var containerName = Directory.GetParent(file)!.FullName[fullInputPath.Length..].Replace("\\", "/");
                            var container = storage.Get(containerName) ?? storage.Add(containerName, containerName);
                            await using var input = File.OpenRead(file);
                            await container.AddAsync(Path.GetFileName(file), input);
                            break;
                        }
                        default:
                            throw new UnreachableException();
                    }
                }
            }

            AnsiConsole.MarkupLine("[green bold]Successfully imported containers.[/]");

            return 0;
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (!Directory.Exists(settings.InputPath))
                return ValidationResult.Error("Input directory does not exist.");

            return base.Validate(context, settings);
        }
    }
}