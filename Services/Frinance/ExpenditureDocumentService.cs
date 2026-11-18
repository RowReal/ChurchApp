using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services;

public sealed class ExpenditureDocumentService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    private readonly string root;
    private const long MaxFileBytes = 10 * 1024 * 1024;
    private static readonly Dictionary<string,string> Types = new(StringComparer.OrdinalIgnoreCase)
    { [".pdf"] = "application/pdf", [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg" };

    public ExpenditureDocumentService(AppDbContext db, AuthService auth, IConfiguration config)
    {
        this.db = db; this.auth = auth;
        root = config["Finance:ExpenditureDocumentsPath"] ?? "/var/data/finance/expenditure";
    }

    public async Task AddAsync(int expenditureId, IBrowserFile file)
    {
        if (!auth.IsAuthenticated || !auth.IsInRole(AuthService.Roles.ChurchAdmin) || auth.CurrentWorker == null)
            throw new UnauthorizedAccessException("Only Church Admin can attach expenditure documents.");
        var record = await db.Set<ExpenditureRecord>().SingleOrDefaultAsync(x => x.Id == expenditureId)
            ?? throw new InvalidOperationException("Expenditure not found.");
        if (record.SubmittedByWorkerId != auth.CurrentWorker.WorkerId || record.Status != ExpenditureStatus.PendingAuditor)
            throw new InvalidOperationException("Documents may be added only by the submitting Admin while awaiting Auditor review.");
        var originalName = Path.GetFileName(file.Name);
        var extension = Path.GetExtension(originalName);
        if (!Types.TryGetValue(extension, out var expectedType) || file.Size <= 0 || file.Size > MaxFileBytes)
            throw new InvalidOperationException("Upload a PDF, PNG or JPG file up to 10 MB.");
        // Browser MIME is untrusted; use allowlisted extension plus signature check.
        Directory.CreateDirectory(root);
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(root, storedName);
        try
        {
            await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var source = file.OpenReadStream(MaxFileBytes))
                await source.CopyToAsync(output);
            var signature = new byte[8];
            await using (var check = File.OpenRead(fullPath))
            {
                var read = await check.ReadAsync(signature);
                var valid = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? read >= 5 && signature.AsSpan(0,5).SequenceEqual("%PDF-"u8)
                    : extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                        ? read >= 8 && signature.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})
                        : read >= 3 && signature[0] == 255 && signature[1] == 216 && signature[2] == 255;
                if (!valid) throw new InvalidOperationException("The file content does not match its permitted type.");
            }
            db.Set<ExpenditureAttachment>().Add(new ExpenditureAttachment {
                ExpenditureRecordId = record.Id, OriginalFileName = originalName[..Math.Min(originalName.Length,255)],
                StoredFileName = storedName, ContentType = expectedType, SizeBytes = file.Size });
            await db.SaveChangesAsync();
        }
        catch { if (File.Exists(fullPath)) File.Delete(fullPath); throw; }
    }
}
