namespace LibXblContainer.Models;

public class Container
{
    public ContainerIndexEntry MetaData { get; }
    public BlobRecords Records { get; }

    private readonly string _containerPath;

    public Container(string basePath, ContainerIndexEntry info)
    {
        MetaData = info;

        _containerPath = Path.Join(basePath, info.ContainerId.ToStringWithoutDashes());
        if (!Directory.Exists(_containerPath))
            throw new InvalidDataException("Container directory does not exist.");

        var blobRecordsPath = Path.Join(_containerPath, "container." + info.BlobId);
        if (!File.Exists(blobRecordsPath))
            throw new InvalidDataException("Blob records file does not exist.");

        using var reader = new BinaryReader(File.OpenRead(blobRecordsPath));
        Records = new BlobRecords(reader);
        if (Records.Records.Length <= 0)
            throw new InvalidDataException("Blob records file did not contain any blobs.");
    }

    private string GetBlobPath(BlobRecord blob)
    {
        var blobPath = Path.Join(_containerPath, blob.BlobFileId.ToStringWithoutDashes());
        if (!File.Exists(blobPath))
            throw new InvalidDataException("Blob file does not exist.");

        return blobPath;
    }

    private BlobRecord GetBlob(string name)
    {
        var blob = Records.Get(name);
        if (blob == null)
            throw new InvalidDataException($"Blob with name {name} was not found in container.");

        return blob;
    }

    public Stream Open()
        => OpenInternal(GetBlobPath(Records.Records[0]));

    public Stream Open(string blobName)
        => OpenInternal(GetBlobPath(GetBlob(blobName)));

    private Stream OpenInternal(string path)
    {
        if (!File.Exists(path))
            throw new InvalidDataException("Blob file does not exist.");

        return File.OpenRead(path);
    }

    public void Update(byte[] newFileData, string? blobName = null)
    {
        var blobPath = GetBlobPath(blobName == null ? Records.Records[0] : GetBlob(blobName));
        File.WriteAllBytes(blobPath, newFileData);
        var newFile = new FileInfo(blobPath);
        MetaData.Update(newFile);
    }

    public async Task UpdateAsync(Stream newData, string? blobName = null)
    {
        var blobPath = GetBlobPath(blobName == null ? Records.Records[0] : GetBlob(blobName));

        var blobStream = File.OpenWrite(blobPath);
        await newData.CopyToAsync(blobStream);
        blobStream.Close();

        var newFile = new FileInfo(blobPath);
        MetaData.Update(newFile);
    }
}