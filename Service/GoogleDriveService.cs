using System.Net;
using System.Net.Http.Headers;
using GoogleDriveAPI.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace GoogleDriveAPI.Service;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly IOptions<GoogleDriveApiOptionModel> _driveOption;

    public GoogleDriveService(IOptions<GoogleDriveApiOptionModel> driveOption)
    {
        _driveOption = driveOption;
    }

    public async Task<GoogleDriveFolderModel> GetListFolderAsync()
    {
        var client = new RestClient(_driveOption.Value.DriveApiUrl + "/drive/v3/files");
        var request = new RestRequest() { Method = Method.Get };
        var accessToken = await GetAccessTokenAsync();

        request.AddHeader("Authorization", "Bearer " + accessToken);
        request.AddParameter("q", "mimeType='application/vnd.google-apps.folder' and 'me' in owners");
        request.AddParameter("fields", "files(id, name, size, mimeType, createdTime)");
        
        var response = await client.ExecuteAsync(request);

        return JsonConvert.DeserializeObject<GoogleDriveFolderModel>(response.Content ?? "{}") ?? new GoogleDriveFolderModel();
    }

    public async Task<GoogleDriveFileModel> GetListFileAsync()
    {
        var client = new RestClient(_driveOption.Value.DriveApiUrl + "/drive/v3/files");

        var request = new RestRequest() { Method = Method.Get };
        var accessToken = await GetAccessTokenAsync();

        request.AddHeader("Authorization", "Bearer " + accessToken);
        request.AddParameter("q", "mimeType!='application/vnd.google-apps.folder' and 'me' in owners");
        request.AddParameter("fields", "files(id, name, size, mimeType, createdTime)");

        var response = await client.ExecuteAsync(request);

        return JsonConvert.DeserializeObject<GoogleDriveFileModel>(response.Content ?? "{}") ?? new GoogleDriveFileModel();
    }

    public async Task<GoogleDriveFileModel> GetListFileByFolderIdAsync(string folderId)
    {
        var q = $"?q='{folderId}'+in+parents and mimeType!='application/vnd.google-apps.folder' and 'me' in owners";
        const string fields = "&fields=files(id, name, size, mimeType, createdTime)";

        var client = new RestClient(_driveOption.Value.DriveApiUrl + "/drive/v3/files" + q + fields);
        var request = new RestRequest() { Method = Method.Get };
        var accessToken = await GetAccessTokenAsync();

        request.AddHeader("Authorization", "Bearer " + accessToken);

        var response = await client.ExecuteAsync(request);

        return JsonConvert.DeserializeObject<GoogleDriveFileModel>(response.Content ?? "{}") ?? new GoogleDriveFileModel();
    }

    public async Task<GoogleDriveResponseModel> CreateFolderAsync(string folderName)
    {
        var client = new RestClient(_driveOption.Value.DriveApiUrl + "/drive/v3/files");

        var request = new RestRequest() { Method = Method.Post };
        var accessToken = await GetAccessTokenAsync();

        request.AddHeader("Authorization", "Bearer " + accessToken);
        request.AddJsonBody(new { name = folderName, mimeType = "application/vnd.google-apps.folder" });

        var response = await client.ExecuteAsync(request);

        return JsonConvert.DeserializeObject<GoogleDriveResponseModel>(response.Content ?? "{}") ?? new GoogleDriveResponseModel();
    }

    public async Task<bool> DeleteAsync(string fileId)
    {
        var client = new RestClient(_driveOption.Value.DriveApiUrl + $"/drive/v3/files/{fileId}");

        var request = new RestRequest() { Method = Method.Delete };
        var accessToken = await GetAccessTokenAsync();

        request.AddHeader("Authorization", "Bearer " + accessToken);

        var response = await client.ExecuteAsync(request);

        return response.IsSuccessful;
    }

    public async Task<GoogleDriveResponseModel> CreateFileAsync(IFormFile file, string folderId)
    {
        var fileName = Guid.NewGuid().ToString().Split("-").First() + "_" + file.FileName;
        var accessToken = await GetAccessTokenAsync();
        var uploadUrl = await GetUploadUrlAsync(accessToken, fileName, folderId);

        if (string.IsNullOrEmpty(uploadUrl))
        {
            return new GoogleDriveResponseModel();
        }

        var client = new RestClient(uploadUrl);
        var request = new RestRequest() { Method = Method.Put };

        request.AddHeader("Authorization", "Bearer " + accessToken);
        request.AddHeader("Content-Type", "application/octet-stream");

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        request.AddFile("file", fileBytes, fileName);

        var response = await client.ExecuteAsync(request);

        return JsonConvert.DeserializeObject<GoogleDriveResponseModel>(response.Content ?? "{}") ?? new GoogleDriveResponseModel();
    }

    public async Task<GoogleDriveWebViewLinkModel> GetPublicLinkAsync(string fieldId)
    {
        var client = new RestClient(_driveOption.Value.DriveApiUrl + $"/drive/v3/files/{fieldId}/permissions");
        var request = new RestRequest()
        {
            Method = Method.Post
        };
        var accessToken = await GetAccessTokenAsync();

        request.AddHeader("Authorization", "Bearer " + accessToken);
        request.AddHeader("Content-Type", "application/json");

        // Set the request body to grant permissions to anyone with the link
        var requestBody = new
        {
            role = "reader",
            type = "anyone"
        };
        request.AddJsonBody(requestBody);

        var response = await client.ExecuteAsync(request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            client = new RestClient(_driveOption.Value.DriveApiUrl + $"/drive/v3/files/{fieldId}");
            request = new RestRequest()
            {
                Method = Method.Get
            };
            request.AddHeader("Authorization", "Bearer " + accessToken);
            request.AddParameter("fields", "webViewLink");

            response = await client.ExecuteAsync(request);
            var result = JsonConvert.DeserializeObject<GoogleDriveWebViewLinkModel>(response.Content ?? "{}");

            return result ?? new GoogleDriveWebViewLinkModel();
        }
        else
        {
            return new GoogleDriveWebViewLinkModel();
        }
    }

    public async Task<bool> RevokeShareLinkAsync(string fieldId)
    {
        var client = new RestClient(_driveOption.Value.DriveApiUrl + $"/drive/v3/files/{fieldId}/permissions");
        var request = new RestRequest()
        {
            Method = Method.Get
        };
        var accessToken = await GetAccessTokenAsync();

        request.AddHeader("Authorization", "Bearer " + accessToken);

        var response = await client.ExecuteAsync(request);
        var permissionsModel = JsonConvert.DeserializeObject<GoogleDrivePermissionsModel>(response.Content ?? "{}");

        if (permissionsModel is not { Permissions: { } }) return true;
        var permissions = permissionsModel.Permissions;

        // Find the permission with type "anyone" and delete it
        var permissionToDelete = permissions.FirstOrDefault(p => p.Type == "anyone");

        if (permissionToDelete == null) return true;

        client = new RestClient(_driveOption.Value.DriveApiUrl +
                                $"/drive/v3/files/{fieldId}/permissions/{permissionToDelete.Id}");
        request = new RestRequest()
        {
            Method = Method.Delete
        };
        request.AddHeader("Authorization", "Bearer " + accessToken);

        response = await client.ExecuteAsync(request);

        return true;
    }

    private async Task<string> GetUploadUrlAsync(string accessToken, string fileName, string folderId)
    {
        var client = new RestClient(_driveOption.Value.DriveApiUrl + "/upload/drive/v3/files?uploadType=resumable");
        var request = new RestRequest() { Method = Method.Post };

        request.AddHeader("Authorization", "Bearer " + accessToken);
        request.AddHeader("Content-Type", "application/json; charset=UTF-8");

        request.AddJsonBody(new
        {
            name = fileName,
            description = $"{DateTime.UtcNow.Ticks} {folderId}",
            parents = new[] { folderId }
        });

        var response = await client.ExecuteAsync(request);

        if (!response.IsSuccessful || response.Headers == null)
        {
            return "";
        }

        var uploadUrl = response.Headers.FirstOrDefault(s => s.Name == "Location")?.Value?.ToString();

        return uploadUrl ?? string.Empty;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var client = new RestClient(_driveOption.Value.Oauth2Url + "/token");
        var request = new RestRequest() { Method = Method.Post };

        request.AddParameter("client_id", _driveOption.Value.ClientID);
        request.AddParameter("client_secret", _driveOption.Value.ClientSecret);
        request.AddParameter("refresh_token", _driveOption.Value.RefreshToken);
        request.AddParameter("grant_type", _driveOption.Value.GrandType);

        var response = await client.ExecuteAsync(request);

        var jObject = JObject.Parse(response.Content ?? string.Empty);

        return jObject.ContainsKey("access_token") ? (string)jObject["access_token"]! : "";
    }
}