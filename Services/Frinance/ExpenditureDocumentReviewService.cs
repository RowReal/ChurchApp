using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;
namespace ChurchApp.Services;
public sealed class ExpenditureDocumentReviewService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    private readonly PrivilegeService privileges;
    private readonly string root;
    public ExpenditureDocumentReviewService(AppDbContext db, AuthService auth, PrivilegeService privileges, IConfiguration configuration)
    { this.db = db; this.auth = auth; this.privileges = privileges;
      root = configuration["Finance:ExpenditureDocumentsPath"] ?? "/var/data/finance/expenditure"; }
    public async Task<(string Name, string Mime, byte[] Bytes)> ReadAsync(int attachmentId)
    {
        if (!auth.IsAuthenticated || auth.CurrentWorker == null) throw new UnauthorizedAccessException("Sign in first.");
        var permitted = auth.IsInRole(AuthService.Roles.ChurchAdmin, AuthService.Roles.PastorInCharge) ||
            await privileges.HasPrivilegeAsync(auth.CurrentWorker.Id, ExpenditureApprovalService.AuditorPrivilegeCode);
        if (!permitted) throw new UnauthorizedAccessException("Finance document access is required.");
        var file = await db.Set<ExpenditureAttachment>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId)
            ?? throw new InvalidOperationException("Document not found.");
        // Storage names are generated UUIDs; reject anything not matching the expected format.
        if (!System.Text.RegularExpressions.Regex.IsMatch(file.StoredFileName, "^[a-fA-F0-9]{32}\\.(pdf|png|jpg|jpeg)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Invalid stored document name.");
        var path = Path.Combine(root, file.StoredFileName);
        if (!File.Exists(path)) throw new FileNotFoundException("Document is missing from persistent storage.");
        if (file.SizeBytes > 10 * 1024 * 1024) throw new InvalidOperationException("Document exceeds review size limit.");
        return (file.OriginalFileName, file.ContentType, await File.ReadAllBytesAsync(path));
    }
}
