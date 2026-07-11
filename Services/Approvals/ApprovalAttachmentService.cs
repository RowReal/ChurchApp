using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalAttachmentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private const long MaximumFileSize = 10 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".jpg",
            ".jpeg",
            ".png"
        };

        public ApprovalAttachmentService(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task SaveAttachmentsAsync(
            int requestId,
            int uploadedByWorkerId,
            IReadOnlyList<IBrowserFile> files)
        {
            if (files == null || files.Count == 0)
                return;

            var requestExists = await _context.ApprovalRequests
                .AnyAsync(x => x.Id == requestId);

            if (!requestExists)
                throw new Exception("Approval request not found.");

            var webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var relativeFolder = Path.Combine(
                "uploads",
                "approval-requests",
                requestId.ToString());

            var physicalFolder = Path.Combine(
                webRootPath,
                relativeFolder);

            Directory.CreateDirectory(physicalFolder);

            foreach (var file in files)
            {
                ValidateFile(file);

                var extension = Path.GetExtension(file.Name)
                    .ToLowerInvariant();

                var storedFileName =
                    $"{Guid.NewGuid():N}{extension}";

                var physicalPath = Path.Combine(
                    physicalFolder,
                    storedFileName);

                await using (var fileStream = new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write))
                {
                    await file
                        .OpenReadStream(MaximumFileSize)
                        .CopyToAsync(fileStream);
                }

                var relativeFilePath = Path.Combine(
                        relativeFolder,
                        storedFileName)
                    .Replace("\\", "/");

                var attachment = new ApprovalRequestAttachment
                {
                    ApprovalRequestId = requestId,
                    FileName = Path.GetFileName(file.Name),
                    FilePath = relativeFilePath,
                    FileType = file.ContentType,
                    FileSize = file.Size,
                    UploadedByWorkerId = uploadedByWorkerId,
                    UploadedAt = DateTime.Now
                };

                _context.ApprovalRequestAttachments.Add(attachment);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ApprovalRequestAttachment>>
            GetRequestAttachmentsAsync(int requestId)
        {
            return await _context.ApprovalRequestAttachments
                .Include(x => x.UploadedByWorker)
                .Where(x => x.ApprovalRequestId == requestId)
                .OrderBy(x => x.UploadedAt)
                .ToListAsync();
        }

        public async Task DeleteAttachmentAsync(
            int attachmentId,
            int requestedByWorkerId)
        {
            var attachment = await _context.ApprovalRequestAttachments
                .Include(x => x.ApprovalRequest)
                .FirstOrDefaultAsync(x => x.Id == attachmentId);

            if (attachment == null)
                return;

            var isInitiator =
                attachment.ApprovalRequest?.RequestedByWorkerId ==
                requestedByWorkerId;

            var isUploader =
                attachment.UploadedByWorkerId ==
                requestedByWorkerId;

            if (!isInitiator && !isUploader)
            {
                throw new Exception(
                    "You are not permitted to delete this attachment.");
            }

            var webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot");
            }

            var physicalPath = Path.Combine(
                webRootPath,
                attachment.FilePath.Replace(
                    "/",
                    Path.DirectorySeparatorChar.ToString()));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            _context.ApprovalRequestAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }

        private static void ValidateFile(IBrowserFile file)
        {
            if (file.Size <= 0)
            {
                throw new Exception(
                    $"{file.Name} is empty and cannot be uploaded.");
            }

            if (file.Size > MaximumFileSize)
            {
                throw new Exception(
                    $"{file.Name} exceeds the 10 MB file limit.");
            }

            var extension = Path.GetExtension(file.Name)
                .ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                throw new Exception(
                    $"{file.Name} is not an allowed file type.");
            }
        }
    }
}