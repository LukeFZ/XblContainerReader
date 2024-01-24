namespace LibXblContainer.Models.FileNames;

public class XboxFileNameFormat : IFileNameFormat
{
    public string GetFileName(Guid id)
        => $"{{{id.ToString().ToUpper()}}}";
}