using LibXblContainer.Models.FileNames;

namespace LibXblContainer.Models;

public class Container
{
    public ContainerIndexEntry MetaData { get; }
    public BlobRecords Blobs { get; private set; }

    private readonly string _containerPath;
    private readonly IFileNameFormat _fileNameFormat;

    public Container(string basePath, ContainerIndexEntry info, IFileNameFormat fileNameFormat)
    {
        MetaData = info;
        Blobs = new BlobRecords();

        _containerPath = Path.Join(basePath, fileNameFormat.GetFileName(info.ContainerId));
        _fileNameFormat = fileNameFormat;
    }

    public void Create()
    {
        if (Directory.Exists(_containerPath))
            throw new InvalidDataException("Container directory already exists");

        Directory.CreateDirectory(_containerPath);
    }

    public void Load()
    {
        if (!Directory.Exists(_containerPath))
            throw new InvalidDataException("Container directory does not exist.");

        if (MetaData.State == ContainerIndexEntryState.Deleted)
            return;

        var blobRecordsPath = Path.Join(_containerPath, "container." + MetaData.BlobId);
        if (!File.Exists(blobRecordsPath))
            throw new InvalidDataException($"Blob records file does not exist for container {MetaData.EntryName}.");

        using var reader = new BinaryReader(File.OpenRead(blobRecordsPath));
        Blobs = new BlobRecords(reader);
        if (Blobs.Records.Count <= 0)
            throw new InvalidDataException("Blob records file did not contain any blobs.");
    }

    public Stream Open()
        => OpenInternal(GetBlobPath(Blobs.Records.Single()));

    public Stream Open(string blobName)
        => OpenInternal(GetBlobPath(GetBlob(blobName)));

    public void Update(byte[] newFileData, string? blobName = null)
    {
        var blobPath = UpdateInternal(blobName);
        File.WriteAllBytes(blobPath, newFileData);

        var newFile = new FileInfo(blobPath);
        MetaData.SetModified(newFile.Length);
    }

    public async Task UpdateAsync(Stream newData, string? blobName = null)
    {
        var blobPath = UpdateInternal(blobName);

        var blobStream = File.OpenWrite(blobPath);
        await newData.CopyToAsync(blobStream);
        blobStream.Close();

        var newFile = new FileInfo(blobPath);
        MetaData.SetModified(newFile.Length);
    }

    private string UpdateInternal(string? blobName)
        => GetBlobPath(blobName == null ? Blobs.Records.Single() : GetBlob(blobName));

    public void Add(string? blobName = null, byte[]? blobData = null)
    {
        var name = AddInternal(blobName);
        if (blobData != null)
            Update(blobData, name);
    }

    public async Task AddAsync(string? blobName = null, Stream? blobData = null)
    {
        var name = AddInternal(blobName);
        if (blobData != null)
            await UpdateAsync(blobData, name);
    }

    public void Remove(string? blobName = null)
    {
        if (Blobs.Count == 0)
            throw new InvalidOperationException("Container does not contain any blocks.");

        if (blobName == null && Blobs.Count > 1)
            throw new ArgumentException(
                "Blob name needs to be non-null when the container already contains a record.");

        File.Delete(GetBlobPath(GetBlob(blobName ?? Blobs.Records.Single().Name)));
        Blobs.Remove(blobName ?? Blobs.Records.Single().Name);
    }

    private string AddInternal(string? blobName = null)
    {
        if (blobName == null && Blobs.Count > 0)
            throw new ArgumentException(
                "Blob name needs to be non-null when the container already contains a record.");

        blobName ??= "blob_0";
        Blobs.Add(blobName);
        return blobName;
    }

    private string GetBlobPath(BlobRecord blob)
        => Path.Join(_containerPath, _fileNameFormat.GetFileName(blob.BlobFileId));

    private BlobRecord GetBlob(string name)
    {
        var blob =
            Blobs.Get(name)
            ?? throw new InvalidDataException($"Blob with name {name} was not found in container.");

        return blob;
    }

    private static Stream OpenInternal(string path)
    {
        if (!File.Exists(path))
            throw new InvalidDataException("Blob file does not exist.");

        return File.OpenRead(path);
    }

    public void Write()
    {
        using var fs = File.OpenWrite(Path.Join(_containerPath, "container." + MetaData.BlobId));
        using var writer = new BinaryWriter(fs);
        Blobs.Write(writer);
    }
}