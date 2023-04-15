# XblContainerReader

*CLI tool and library (LibXblContainer) to parse and interact with UWP save games (containers.index).*

## Requirements

- [.NET 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) installed

## Usage

1. Download the latest release from the [Releases](https://github.com/LukeFZ/XblContainerReader/releases) tab.
2. Extract the downloaded archive.
3. Launch the executable and follow the instructions.
```
Usage: XblContainerReader.exe <command> <path to container folder> <input/output path>
Commands:
        extract - extracts files from the container into the output directory
        update - updates files inside the container from the input directory
```

## For Developers
The library also targets .NET 7, and has no other dependencies.  
Microsoft's own implementation can be found in XblGameSave.dll in your System32 folder, as well as some APIs in the `Windows.Gaming.XboxLive.Storage` namespace. 
