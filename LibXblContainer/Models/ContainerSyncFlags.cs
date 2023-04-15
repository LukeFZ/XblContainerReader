namespace LibXblContainer.Models;

[Flags]
public enum ContainerSyncFlags
{
    None = 0,
    FullyUploaded = 1 << 0x0,
    FullyDownloaded = 1 << 0x1,
    HasUnresolvedConflicts = 1 << 0x4
}