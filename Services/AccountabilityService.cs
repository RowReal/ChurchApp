// Services/AccountabilityService.cs
using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    // Move these classes outside the service class for better organization
    public class CaseStatistics
    {
        public int TotalCases { get; set; }
        public int OpenCases { get; set; }
        public int InProgressCases { get; set; }
        public int EscalatedCases { get; set; }
        public int ResolvedCases { get; set; }
        public int OverdueCases { get; set; }
        public int CreatedByMe { get; set; }
        public int AssignedToMe { get; set; }
        public int MyCases { get; set; }
    }

    public class GrowthHubCounts
    {
        public int Total { get; set; }
        public int AssignedToMe { get; set; }
        public int MyFeedback { get; set; }
        public int Overdue { get; set; }
    }

    public class AccountabilityService
    {
        private readonly AppDbContext _context;
        private readonly WorkerService _workerService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AccountabilityService> _logger;

        public AccountabilityService(
            AppDbContext context,
            WorkerService workerService,
            IWebHostEnvironment environment,
            ILogger<AccountabilityService> logger)
        {
            _context = context;
            _workerService = workerService;
            _environment = environment;
            _logger = logger;
        }

        // Get hierarchy level from role
        public int GetHierarchyLevelFromRole(string role)
        {
            if (string.IsNullOrEmpty(role)) return 1;

            role = role.ToLowerInvariant();

            if (role.Contains("pastor in charge") || role.Contains("senior pastor") || role.Contains("head pastor"))
                return 4;
            if (role.Contains("head of service") || role.Contains("service head") || role.Contains("service leader"))
                return 3;
            if (role.Contains("head of directorate") || role.Contains("directorate head") || role.Contains("directorate leader"))
                return 2;
            if (role.Contains("director") || role.Contains("leader") || role.Contains("coordinator"))
                return 2;

            return 1;
        }

        // Get or create worker hierarchy record
        public async Task<WorkerHierarchy> GetOrCreateWorkerHierarchyAsync(int workerId)
        {
            var hierarchy = await _context.WorkerHierarchies
                .Include(h => h.Worker)
                .Include(h => h.ReportsToWorker)
                .Include(h => h.Directorate)
                .FirstOrDefaultAsync(h => h.WorkerId == workerId && h.IsActive);

            if (hierarchy == null)
            {
                var worker = await _context.Workers
                    .Include(w => w.Directorate)
                    .Include(w => w.Supervisor)
                    .FirstOrDefaultAsync(w => w.Id == workerId);

                if (worker == null)
                    throw new Exception($"Worker with ID {workerId} not found");

                hierarchy = new WorkerHierarchy
                {
                    WorkerId = workerId,
                    HierarchyLevel = GetHierarchyLevelFromRole(worker.Role),
                    DirectorateId = worker.DirectorateId,
                    ReportsToWorkerId = worker.SupervisorId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.WorkerHierarchies.Add(hierarchy);
                await _context.SaveChangesAsync();
            }

            return hierarchy;
        }

        // Get cases visible to a worker based on hierarchy
        public async Task<List<AccountabilityCase>> GetVisibleCasesAsync(int workerId)
        {
            var workerHierarchy = await GetOrCreateWorkerHierarchyAsync(workerId);
            var workerLevel = workerHierarchy.HierarchyLevel;

            var query = _context.AccountabilityCases
                .Include(c => c.Worker)
                .Include(c => c.CreatedByWorker)
                .Include(c => c.AssignedToWorker)
                .Include(c => c.Messages)
                .Where(c => c.IsActive);

            List<AccountabilityCase> cases;

            switch (workerLevel)
            {
                case 4: // Pastor sees all
                    cases = await query.ToListAsync();
                    break;

                case 3: // Head of Service
                    var serviceDirectorates = await GetServiceDirectoratesAsync(workerId);
                    cases = await query
                        .Where(c => (c.EscalationLevel >= 3 && !c.IsConfidential) ||
                                   c.WorkerId == workerId ||
                                   c.CreatedByWorkerId == workerId
                                   )
                                   //(c.Worker.SupervisorId == workerId))
                        .ToListAsync();
                    break;

                case 2: // Head of Directorate
                    var directorateWorkers = await GetDirectorateWorkersAsync(workerHierarchy.DirectorateId ?? 0);
                    cases = await query
                        .Where(c => /*c.EscalationLevel == 1 ||*/
                        (!c.IsConfidential &&
                                   (c.WorkerId == workerId ||
                                   directorateWorkers.Contains(c.WorkerId) ))
                                    || c.CreatedByWorkerId == workerId)
                        //(c.Worker.SupervisorId == workerId))
                        .ToListAsync();
                    break;

                default: // Regular worker (level 1)
                    cases = await query
                        .Where(c => c.WorkerId == workerId ||
                                   (c.EscalationLevel == 1 && c.AssignedToWorkerId == workerId))
                        .ToListAsync();
                    break;
            }

            return cases;
        }

        // Create new accountability case
        public async Task<AccountabilityCase> CreateCaseAsync(
            string title,
            string description,
            int workerId,
            int createdById,
            string priority = "Medium",
            string category = "General",
            DateTime? occurrenceDate = null,
            DateTime? dueDate = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var creatorHierarchy = await GetOrCreateWorkerHierarchyAsync(createdById);

                var caseObj = new AccountabilityCase
                {
                    Title = title,
                    Description = description,
                    WorkerId = workerId,
                    CreatedByWorkerId = createdById,
                    Priority = priority,
                    Category = category,
                    OccurrenceDate = occurrenceDate ?? DateTime.Today,
                    DueDate = dueDate,
                    EscalationLevel = creatorHierarchy.HierarchyLevel,
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true
                };

                // Auto-assign based on hierarchy
                if (creatorHierarchy.HierarchyLevel == 2)
                {
                    caseObj.AssignedToWorkerId = workerId;
                }
                else if (creatorHierarchy.HierarchyLevel == 1)
                {
                    var headOfDirectorate = await GetHeadOfDirectorateAsync(workerId);
                    caseObj.AssignedToWorkerId = headOfDirectorate;
                }

                _context.AccountabilityCases.Add(caseObj);
                await _context.SaveChangesAsync();

                var action = new CaseAction
                {
                    CaseId = caseObj.Id,
                    PerformedByWorkerId = createdById,
                    ActionType = "Create",
                    Description = $"Case created: {title}",
                    ActionDate = DateTime.UtcNow
                };

                _context.CaseActions.Add(action);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return caseObj;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating accountability case");
                throw;
            }
        }

        // Add initial message to case
        public async Task<AccountabilityMessage> AddInitialMessageAsync(
            int caseId,
            int senderId,
            string content,
            string messageType = "Question")
        {
            var caseObj = await _context.AccountabilityCases
                .Include(c => c.Worker)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseObj == null)
                throw new Exception($"Case with ID {caseId} not found");

            var senderHierarchy = await GetOrCreateWorkerHierarchyAsync(senderId);
            var subjectHierarchy = await GetOrCreateWorkerHierarchyAsync(caseObj.WorkerId);

            string visibleToLevels = DetermineInitialVisibility(senderHierarchy.HierarchyLevel, subjectHierarchy.HierarchyLevel);

            var message = new AccountabilityMessage
            {
                CaseId = caseId,
                SenderWorkerId = senderId,
                MessageType = messageType,
                Content = content,
                VisibleToLevels = visibleToLevels,
                SentDate = DateTime.UtcNow,
                IsRead = false
            };

            _context.AccountabilityMessages.Add(message);

            caseObj.LastUpdated = DateTime.UtcNow;
            caseObj.Status = "InProgress";

            await _context.SaveChangesAsync();
            return message;
        }

        // Send response message with proper visibility
        public async Task<AccountabilityMessage> SendResponseAsync(
            int caseId,
            int senderId,
            string content,
            string messageType,
            string visibleTo,
            int? replyToMessageId = null,
            byte[]? fileData = null,
            string? fileName = null)
        {
            var caseObj = await _context.AccountabilityCases
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseObj == null)
                throw new Exception($"Case with ID {caseId} not found");

            var message = new AccountabilityMessage
            {
                CaseId = caseId,
                SenderWorkerId = senderId,
                MessageType = messageType,
                Content = content,
                VisibleToLevels = visibleTo,
                ReplyToMessageId = replyToMessageId,
                SentDate = DateTime.UtcNow,
                IsRead = false
            };

            // Handle attachment
            if (fileData != null && fileData.Length > 0 && !string.IsNullOrEmpty(fileName))
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "accountability");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var safeFileName = Path.GetFileName(fileName);
                var fileExtension = Path.GetExtension(safeFileName);
                var uniqueFileName = $"{caseId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, uniqueFileName);

                await System.IO.File.WriteAllBytesAsync(filePath, fileData);

                message.AttachmentPath = uniqueFileName;
                message.AttachmentName = safeFileName;
            }

            _context.AccountabilityMessages.Add(message);

            caseObj.LastUpdated = DateTime.UtcNow;

            if (messageType == "Resolution")
                caseObj.Status = "Resolved";
            else if (messageType == "Warning")
                caseObj.Status = "Escalated";

            await _context.SaveChangesAsync();

            var action = new CaseAction
            {
                CaseId = caseId,
                PerformedByWorkerId = senderId,
                ActionType = "Message",
                Description = $"{messageType} sent",
                ActionDate = DateTime.UtcNow
            };

            _context.CaseActions.Add(action);
            await _context.SaveChangesAsync();

            return message;
        }

        // Escalate case to next level
        public async Task EscalateCaseAsync(int caseId, int escalatedBy, string reason)
        {
            var caseObj = await _context.AccountabilityCases
                .Include(c => c.Worker)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseObj == null)
                throw new Exception($"Case with ID {caseId} not found");

            if (caseObj.EscalationLevel >= 4)
                throw new Exception("Case is already at the highest escalation level");

            var escalatedByHierarchy = await GetOrCreateWorkerHierarchyAsync(escalatedBy);

            if (escalatedByHierarchy.HierarchyLevel <= caseObj.EscalationLevel)
                throw new Exception("You do not have authority to escalate this case");

            caseObj.EscalationLevel++;
            caseObj.Status = "Escalated";
            caseObj.LastUpdated = DateTime.UtcNow;

            caseObj.AssignedToWorkerId = await GetWorkerAtLevelAsync(caseObj.EscalationLevel, caseObj.WorkerId);

            await SendResponseAsync(
                caseId,
                escalatedBy,
                $"Case escalated to level {caseObj.EscalationLevel}. Reason: {reason}",
                "Escalation",
                GetVisibilityForEscalation(caseObj.EscalationLevel)
            );

            var action = new CaseAction
            {
                CaseId = caseId,
                PerformedByWorkerId = escalatedBy,
                ActionType = "Escalate",
                Description = $"Escalated to level {caseObj.EscalationLevel}. Reason: {reason}",
                ActionDate = DateTime.UtcNow
            };

            _context.CaseActions.Add(action);
            await _context.SaveChangesAsync();
        }

        // Get messages visible to current worker for a case
        public async Task<List<AccountabilityMessage>> GetVisibleMessagesAsync(int caseId, int workerId)
        {
            var workerHierarchy = await GetOrCreateWorkerHierarchyAsync(workerId);
            var workerLevel = workerHierarchy.HierarchyLevel;

            var caseObj = await _context.AccountabilityCases
                .Include(c => c.Worker)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (caseObj == null) return new List<AccountabilityMessage>();

            var allMessages = await _context.AccountabilityMessages
                .Include(m => m.SenderWorker)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.CaseId == caseId)
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            var visibleMessages = new List<AccountabilityMessage>();

            foreach (var message in allMessages)
            {
                if (IsMessageVisible(message, workerId, workerLevel, caseObj))
                {
                    visibleMessages.Add(message);

                    if (!message.IsRead && message.SenderWorkerId != workerId)
                    {
                        message.IsRead = true;
                        message.ReadDate = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return visibleMessages;
        }

        // Get case statistics for dashboard
        public async Task<CaseStatistics> GetStatisticsAsync(int workerId)
        {
            var visibleCases = await GetVisibleCasesAsync(workerId);

            return new CaseStatistics
            {
                TotalCases = visibleCases.Count,
                OpenCases = visibleCases.Count(c => c.Status == "Open"),
                InProgressCases = visibleCases.Count(c => c.Status == "InProgress"),
                EscalatedCases = visibleCases.Count(c => c.Status == "Escalated"),
                ResolvedCases = visibleCases.Count(c => c.Status == "Resolved"),
                OverdueCases = visibleCases.Count(c => c.DueDate.HasValue && c.DueDate < DateTime.Today && c.Status != "Resolved"),
                CreatedByMe = visibleCases.Count(c => c.CreatedByWorkerId == workerId),
                AssignedToMe = visibleCases.Count(c => c.AssignedToWorkerId == workerId),
                MyCases = visibleCases.Count(c => c.WorkerId == workerId)
            };
        }

        // Get growth hub counts for sidebar
        public async Task<GrowthHubCounts> GetGrowthHubCountsAsync(int workerId)
        {
            var visibleCases = await GetVisibleCasesAsync(workerId);
            var assignedCases = visibleCases.Where(c => c.AssignedToWorkerId == workerId).ToList();

            return new GrowthHubCounts
            {
                Total = visibleCases.Count(c => c.Status != "Resolved" && c.Status != "Closed"),
                AssignedToMe = assignedCases.Count(c => c.Status != "Resolved" && c.Status != "Closed"),
                MyFeedback = visibleCases.Count(c =>
                    (c.WorkerId == workerId || c.CreatedByWorkerId == workerId) &&
                    c.Status != "Resolved" && c.Status != "Closed"),
                Overdue = visibleCases.Count(c => c.IsOverdue)
            };
        }

        // Check if user can escalate a case
        public async Task<bool> CanEscalateCaseAsync(int caseId, int workerId)
        {
            var caseObj = await GetCaseByIdAsync(caseId);
            if (caseObj == null) return false;

            var workerHierarchy = await GetOrCreateWorkerHierarchyAsync(workerId);
            var workerLevel = workerHierarchy.HierarchyLevel;

            if (caseObj.Status == "Resolved" || caseObj.Status == "Closed")
                return false;

            if (caseObj.EscalationLevel >= 4)
                return false;

            // Worker (level 1) can escalate to Head of Service (level 3)
            if (workerLevel == 1 && caseObj.EscalationLevel == 1)
                return true;

            // Head of Directorate (level 2) can escalate to Head of Service (level 3)
            if (workerLevel == 2 && caseObj.EscalationLevel <= 2)
                return true;

            // Head of Service (level 3) can escalate to Pastor (level 4)
            if (workerLevel == 3 && caseObj.EscalationLevel <= 3)
                return true;

            // Pastor (level 4) cannot escalate further
            if (workerLevel == 4)
                return false;

            return caseObj.WorkerId == workerId ||
                   caseObj.CreatedByWorkerId == workerId ||
                   caseObj.AssignedToWorkerId == workerId ||
                   workerLevel > caseObj.EscalationLevel;
        }

        // Get next escalation level name
        public async Task<string> GetNextEscalationLevelAsync(int caseId, int workerId)
        {
            var caseObj = await GetCaseByIdAsync(caseId);
            if (caseObj == null) return "Unknown";

            var workerHierarchy = await GetOrCreateWorkerHierarchyAsync(workerId);
            var workerLevel = workerHierarchy.HierarchyLevel;

            return workerLevel switch
            {
                1 => "Head of Service",
                2 => "Head of Service",
                3 => "Pastor in Charge",
                _ => "Unknown"
            };
        }

        // Helper methods
        private bool IsMessageVisible(AccountabilityMessage message, int workerId, int workerLevel, AccountabilityCase caseObj)
        {
            if (message.SenderWorkerId == workerId) return true;

            if (caseObj.WorkerId == workerId)
            {
                var visibleLevels = message.VisibleToLevels?.Split(',')
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(int.Parse)
                    .ToList() ?? new List<int>();

                if (visibleLevels.Contains(1) && !message.IsConfidential)
                    return true;
            }

            var messageVisibleLevels = message.VisibleToLevels?.Split(',')
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToList() ?? new List<int>();

            if (messageVisibleLevels.Count == 0)
                return false;

            if (!message.IsConfidential && workerLevel > messageVisibleLevels.Min())
                return true;

            return messageVisibleLevels.Contains(workerLevel);
        }

        private string DetermineInitialVisibility(int senderLevel, int subjectLevel)
        {
            var levels = new List<int> { senderLevel };
            if (subjectLevel < senderLevel)
                levels.Add(subjectLevel);

            return string.Join(",", levels.Distinct().OrderBy(l => l));
        }

        private string GetVisibilityForEscalation(int escalationLevel)
        {
            return escalationLevel switch
            {
                1 => "1,2",
                2 => "1,2,3",
                3 => "1,2,3,4",
                _ => "1,2,3,4"
            };
        }

        private async Task<List<int>> GetDirectorateWorkersAsync(int directorateId)
        {
            return await _context.Workers
                .Where(w => w.DirectorateId == directorateId && w.IsActive)
                .Select(w => w.Id)
                .ToListAsync();
        }

        private async Task<List<int>> GetServiceDirectoratesAsync(int headOfServiceId)
        {
            var headOfService = await _context.Workers
                .FirstOrDefaultAsync(w => w.Id == headOfServiceId);

            if (headOfService == null) return new List<int>();

            return await _context.Directorates
                .Where(d => d.IsActive)
                .Select(d => d.Id)
                .ToListAsync();
        }

        private async Task<int?> GetHeadOfDirectorateAsync(int workerId)
        {
            var worker = await _context.Workers
                .Include(w => w.Directorate)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker?.DirectorateId == null) return null;

            var head = await _context.Workers
                .FirstOrDefaultAsync(w =>
                    w.DirectorateId == worker.DirectorateId &&
                    w.Role.Contains("Head of Directorate", StringComparison.OrdinalIgnoreCase) &&
                    w.IsActive);

            return head?.Id;
        }

        private async Task<int?> GetWorkerAtLevelAsync(int level, int subjectWorkerId)
        {
            var subjectWorker = await _context.Workers
                .Include(w => w.Directorate)
                .FirstOrDefaultAsync(w => w.Id == subjectWorkerId);

            if (subjectWorker == null) return null;

            switch (level)
            {
                case 1:
                    return subjectWorkerId;
                case 2:
                    return await GetHeadOfDirectorateAsync(subjectWorkerId);
                case 3:
                    var headOfServiceHierarchy = await _context.WorkerHierarchies
                        .Include(wh => wh.Worker)
                        .FirstOrDefaultAsync(wh =>
                            wh.HierarchyLevel == 3 &&
                            wh.IsActive);
                    return headOfServiceHierarchy?.WorkerId;
                case 4:
                    var pastorHierarchy = await _context.WorkerHierarchies
                        .Include(wh => wh.Worker)
                        .FirstOrDefaultAsync(wh =>
                            wh.HierarchyLevel == 4 &&
                            wh.IsActive);
                    return pastorHierarchy?.WorkerId;
                default:
                    return null;
            }
        }

        // Additional methods (keep these as they were)
        public async Task UpdateCaseConfidentialityAsync(int caseId, bool isConfidential)
        {
            var caseObj = await _context.AccountabilityCases.FindAsync(caseId);
            if (caseObj != null)
            {
                caseObj.IsConfidential = isConfidential;
                caseObj.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<CaseAction>> GetCaseActionsAsync(int caseId)
        {
            return await _context.CaseActions
                .Include(a => a.PerformedByWorker)
                .Where(a => a.CaseId == caseId)
                .OrderByDescending(a => a.ActionDate)
                .ToListAsync();
        }

        public async Task<bool> CanWorkerViewCaseAsync(int caseId, int workerId)
        {
            var visibleCases = await GetVisibleCasesAsync(workerId);
            return visibleCases.Any(c => c.Id == caseId);
        }

        public async Task<AccountabilityCase?> GetCaseByIdAsync(int caseId)
        {
            return await _context.AccountabilityCases
                .Include(c => c.Worker)
                .Include(c => c.CreatedByWorker)
                .Include(c => c.AssignedToWorker)
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == caseId && c.IsActive);
        }

        public async Task UpdateCaseStatusAsync(int caseId, string status)
        {
            var caseObj = await _context.AccountabilityCases.FindAsync(caseId);
            if (caseObj != null)
            {
                caseObj.Status = status;
                caseObj.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<AccountabilityCase?> GetCaseForEditAsync(int caseId, int workerId)
        {
            var caseObj = await GetCaseByIdAsync(caseId);
            if (caseObj == null) return null;

            var workerHierarchy = await GetOrCreateWorkerHierarchyAsync(workerId);

            if (caseObj.CreatedByWorkerId == workerId)
                return caseObj;

            if (await IsSupervisorOfWorkerAsync(workerId, caseObj.WorkerId))
                return caseObj;

            var subjectHierarchy = await GetOrCreateWorkerHierarchyAsync(caseObj.WorkerId);
            if (workerHierarchy.HierarchyLevel > subjectHierarchy.HierarchyLevel)
                return caseObj;

            return null;
        }

        private async Task<bool> IsSupervisorOfWorkerAsync(int supervisorId, int workerId)
        {
            var worker = await _context.Workers
                .Include(w => w.Supervisor)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            return worker?.SupervisorId == supervisorId;
        }
    }
}
