namespace LibXblContainer.Models.FileNames;

public interface IFileNameFormat
{
    string GetFileName(Guid id);
}