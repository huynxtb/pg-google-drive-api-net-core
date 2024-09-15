using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GoogleDriveAPI.Models;
using GoogleDriveAPI.Service;
using Microsoft.Extensions.Options;

namespace GoogleDriveAPI.Controllers;

public class GoogleDriveController : Controller
{
    private readonly IGoogleDriveService _googleDriveService;
    private readonly IOptions<GoogleDriveApiOptionModel> _driveOption;

    public GoogleDriveController(IGoogleDriveService googleDriveService,
        IOptions<GoogleDriveApiOptionModel> driveOption)
    {
        _googleDriveService = googleDriveService;
        _driveOption = driveOption;
    }

    public async Task<IActionResult> FileManagement()
    {
        var data = await _googleDriveService.GetListFileAsync();

        return View(data);
    }

    public async Task<IActionResult> FolderManagement()
    {
        var data = await _googleDriveService.GetListFolderAsync();

        return View(data);
    }

    public async Task<IActionResult> GetFileByFolderId(string folderId)
    {
        var data = await _googleDriveService.GetListFileByFolderIdAsync(folderId);

        return View("FileManagement", data);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string fieldId)
    {
        var data = await _googleDriveService.DeleteAsync(fieldId);

        return Json(data);
    }

    public async Task<IActionResult> UploadFile()
    {
        var data = await _googleDriveService.GetListFolderAsync();

        return View(data);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitUploadFile(IFormFile fileUpload, string folderId)
    {
        var data = await _googleDriveService.CreateFileAsync(fileUpload, folderId);

        return RedirectToAction("FileManagement");
    }

    public IActionResult CreateFolder()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SubmitCreateFolder(string folderName)
    {
        var folders = await _googleDriveService.GetListFolderAsync();
        var folderNames = folders.Files.Select(s => s.Name).ToList();

        if (folderNames.Contains(folderName))
        {
            return Redirect($"/GoogleDrive/CreateFolder?folderName={folderName}");
        }

        var data = await _googleDriveService.CreateFolderAsync(folderName);

        return RedirectToAction("FolderManagement");
    }

    [HttpGet]
    public async Task<IActionResult> GetShareLink(string fieldId)
    {
        var link = await _googleDriveService.GetPublicLinkAsync(fieldId);
        
        return View("ShareLink", link);
    }
    
    [HttpGet]
    public async Task<IActionResult> RevokeShareLink(string fieldId, string redirectUri)
    {
        var result = await _googleDriveService.RevokeShareLinkAsync(fieldId);

        return Redirect(redirectUri);
    }
    
    [HttpGet]
    public async Task<IActionResult> Download(string fieldId, string fileName)
    {
        // Set the chunk size (in bytes)
        const int chunkSize = 100 * 1024 * 1024; // 100 MB

        // Create a new HttpClient instance
        var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization",
            "Bearer " + await _googleDriveService.GetAccessTokenAsync());

        // Get the file size
        var response = await httpClient.GetAsync(
            _driveOption.Value.DriveApiUrl + $"/drive/v3/files/{fieldId}?alt=media",
            HttpCompletionOption.ResponseHeadersRead);

        var fileSize = response.Content.Headers.ContentLength ?? -1;

        // Get the file extension
        var contentType = response.Content.Headers
            .FirstOrDefault(s => s.Key == "Content-Type")
            .Value
            .First()
            .ToString()
            .Split("/")[1];

        // Set the response headers
        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        Response.ContentType = "application/octet-stream";
        Response.ContentLength = fileSize;

        // Download the file in chunks
        long position = 0;
        var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[chunkSize];
        var bytesRead = 0;
        while (position < fileSize && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            if (bytesRead != buffer.Length)
            {
                // If we didn't read a full chunk, create a new buffer with the correct size
                buffer = new byte[bytesRead];
            }

            // Write the chunk to the response stream
            await Response.Body.WriteAsync(buffer.AsMemory(0, bytesRead));

            position += bytesRead;
        }

        return new EmptyResult();
    }
}