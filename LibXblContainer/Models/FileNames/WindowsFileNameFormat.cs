namespace LibXblContainer.Models.FileNames;

public class WindowsFileNameFormat : IFileNameFormat
{
    public string GetFileName(Guid id)
        => id.ToStringWithoutDashes();
}