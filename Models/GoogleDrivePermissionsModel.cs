namespace GoogleDriveAPI.Models;

public class DrivePermissionModel
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Kind { get; set; }
    public string Role { get; set; }
    public bool AllowFileDiscovery { get; set; }
}

public class GoogleDrivePermissionsModel
{
    public string Kind { get; set; }
    public List<DrivePermissionModel> Permissions { get; set; }
}