using GoogleDriveAPI.Models;

namespace GoogleDriveAPI.Service;

public interface IGoogleDriveService
{
    Task<GoogleDriveFolderModel> GetListFolderAsync();
    Task<GoogleDriveFileModel> GetListFileAsync();
    Task<GoogleDriveFileModel> GetListFileByFolderIdAsync(string folderId);
    Task<GoogleDriveResponseModel> CreateFolderAsync(string folderName);
    Task<bool> DeleteAsync(string fileId);
    Task<GoogleDriveResponseModel> CreateFileAsync(IFormFile file, string folderId);
    Task<GoogleDriveWebViewLinkModel> GetPublicLinkAsync(string fieldId);
    Task<bool> RevokeShareLinkAsync(string fieldId);
    Task<string> GetAccessTokenAsync();
}