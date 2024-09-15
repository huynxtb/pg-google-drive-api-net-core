namespace GoogleDriveAPI.Models;

public class GgFileModel
{
    public string Kind { get; set; }
    public string MimeType { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class GoogleDriveFolderModel
{
    public string Kind { get; set; }
    public bool IncompleteSearch { get; set; }
    public List<GgFileModel> Files { get; set; }
}

public class GoogleDriveFileModel
{
    public List<GgFileModel> Files { get; set; }
}

public class GoogleDriveResponseModel
{
    public string Kind { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string MimeType { get; set; }
}