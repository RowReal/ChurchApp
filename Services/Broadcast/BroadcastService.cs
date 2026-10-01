using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class BroadcastService
    {
        private readonly AppDbContext _context;

        public BroadcastService(AppDbContext context)
        {
            _context = context;
        }
        private static string GetCanonicalAudienceType(
    string? value)
        {
            var normalized =
                NormalizeAudienceType(value);

            return normalized switch
            {
                "allworkers" =>
                    "AllWorkers",

                "allleaders" =>
                    "AllLeaders",

                "selecteddirectorates" =>
                    "SelectedDirectorates",

                "owndirectorate" =>
                    "OwnDirectorate",

                _ => string.Empty
            };
        }
        // =========================================================
        // SENDER ACCESS
        // =========================================================

        public async Task<BroadcastSenderAccess>
            GetSenderAccessAsync(int workerId)
        {
            var result = new BroadcastSenderAccess();

            var worker = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == workerId &&
                    x.IsActive);

            if (worker == null)
                return result;

            var role = Normalize(worker.Role);

            // -----------------------------------------------------
            // 1. PSO
            // -----------------------------------------------------
            if (role == "pastor in charge")
            {
                result.CanSend = true;
                result.SenderType = "PSO";
                result.SenderDisplayLabel = "PSO";

                result.CanSendToAllWorkers = true;
                result.CanSendToAllLeaders = true;
                result.CanSelectDirectorates = true;

                result.AllowedDirectorateIds =
                    await GetAllActiveDirectorateIdsAsync();

                return result;
            }

            // -----------------------------------------------------
            // 2. HEAD OF MEAT
            //
            // This deliberately checks the configured HEAD only.
            // Assistant Head of MEAT does NOT receive this
            // church-wide broadcast authority.
            // -----------------------------------------------------
            var meatDirectorate = await _context.Directorates
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.IsActive &&
                    x.HeadWorkerId == workerId &&
                    (
                        x.Code.ToUpper() == "MEAT" ||
                        x.Name.ToUpper() == "MEAT" ||
                        x.Name.ToUpper().Contains("MEAT")
                    ));

            if (meatDirectorate != null)
            {
                result.CanSend = true;
                result.SenderType = "MEAT";
                result.SenderDisplayLabel = "MEAT";

                result.CanSendToAllWorkers = true;
                result.CanSendToAllLeaders = true;
                result.CanSelectDirectorates = true;

                result.AllowedDirectorateIds =
                    await GetAllActiveDirectorateIdsAsync();

                return result;
            }

            // -----------------------------------------------------
            // 3. CLUSTER HEAD
            //
            // Based ONLY on configured SupervisoryCluster.HeadWorkerId.
            // -----------------------------------------------------
            var clusterDirectorateIds =
                await _context.SupervisoryClusters
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.HeadWorkerId == workerId)
                    .SelectMany(x => x.Directorates)
                    .Select(x => x.DirectorateId)
                    .Distinct()
                    .ToListAsync();

            if (clusterDirectorateIds.Count > 0)
            {
                result.CanSend = true;
                result.SenderType = "ClusterHead";
                result.SenderDisplayLabel = "Asst Pastor";

                result.CanSendToAllWorkers = false;
                result.CanSendToAllLeaders = false;
                result.CanSelectDirectorates = true;

                result.AllowedDirectorateIds =
                    clusterDirectorateIds;

                return result;
            }

            // -----------------------------------------------------
            // 4. HEAD OF DIRECTORATE
            //
            // Only the configured HeadWorkerId receives broadcast
            // authority. Assistant Head is a Leader/recipient but
            // is NOT a broadcast sender.
            // -----------------------------------------------------
            var headedDirectorateIds =
                await _context.Directorates
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.HeadWorkerId == workerId)
                    .Select(x => x.Id)
                    .ToListAsync();

            if (headedDirectorateIds.Count > 0)
            {
                result.CanSend = true;
                result.SenderType = "DirectorateHead";
                result.SenderDisplayLabel = "Directorate Head";

                result.CanSendToAllWorkers = false;
                result.CanSendToAllLeaders = false;

                // Head of Directorate cannot choose another
                // Directorate. The UI will use OwnDirectorate.
                result.CanSelectDirectorates = false;

                result.AllowedDirectorateIds =
                    headedDirectorateIds;

                return result;
            }

            return result;
        }


        // =========================================================
        // AVAILABLE DIRECTORATES FOR THE SENDER
        // =========================================================

        public async Task<List<BroadcastDirectorateOption>>
            GetAvailableDirectoratesAsync(int senderWorkerId)
        {
            var access =
                await GetSenderAccessAsync(senderWorkerId);

            if (!access.CanSend ||
                access.AllowedDirectorateIds.Count == 0)
            {
                return new List<BroadcastDirectorateOption>();
            }

            return await _context.Directorates
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    access.AllowedDirectorateIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new BroadcastDirectorateOption
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();
        }


        // =========================================================
        // RESOLVE RECIPIENTS
        //
        // IMPORTANT:
        // PSO is ALWAYS removed from the final recipient list.
        // =========================================================

        public async Task<List<int>> ResolveRecipientIdsAsync(
            int senderWorkerId,
            string audienceType,
            IEnumerable<int>? selectedDirectorateIds = null)
        {
            var access =
                await GetSenderAccessAsync(senderWorkerId);

            if (!access.CanSend)
            {
                throw new InvalidOperationException(
                    "You are not authorised to send broadcasts.");
            }

            var audience =
                NormalizeAudienceType(audienceType);

            List<int> recipientIds;

            switch (audience)
            {
                case "allworkers":

                    if (!access.CanSendToAllWorkers)
                    {
                        throw new InvalidOperationException(
                            "You are not authorised to broadcast to all workers.");
                    }

                    recipientIds = await _context.Workers
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                        .Select(x => x.Id)
                        .ToListAsync();

                    break;


                case "allleaders":

                    if (!access.CanSendToAllLeaders)
                    {
                        throw new InvalidOperationException(
                            "You are not authorised to broadcast to all leaders.");
                    }

                    recipientIds =
                        await GetAllLeaderIdsAsync();

                    break;


                case "selecteddirectorates":

                    if (!access.CanSelectDirectorates)
                    {
                        throw new InvalidOperationException(
                            "You are not authorised to select Directorates.");
                    }

                    var selectedIds =
                        selectedDirectorateIds?
                            .Distinct()
                            .ToList()
                        ?? new List<int>();

                    if (selectedIds.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Please select at least one Directorate.");
                    }

                    // Security check:
                    // Every selected Directorate must belong to
                    // the sender's permitted scope.
                    if (selectedIds.Any(id =>
                        !access.AllowedDirectorateIds.Contains(id)))
                    {
                        throw new InvalidOperationException(
                            "One or more selected Directorates are outside your authorised scope.");
                    }

                    recipientIds = await _context.Workers
                        .AsNoTracking()
                        .Where(x =>
                            x.IsActive &&
                            x.DirectorateId.HasValue &&
                            selectedIds.Contains(
                                x.DirectorateId.Value))
                        .Select(x => x.Id)
                        .ToListAsync();

                    break;


                case "owndirectorate":

                    if (access.SenderType != "DirectorateHead")
                    {
                        throw new InvalidOperationException(
                            "Own Directorate broadcasting is restricted to Heads of Directorate.");
                    }

                    if (access.AllowedDirectorateIds.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "No Directorate is assigned to this sender.");
                    }

                    recipientIds = await _context.Workers
                        .AsNoTracking()
                        .Where(x =>
                            x.IsActive &&
                            x.DirectorateId.HasValue &&
                            access.AllowedDirectorateIds.Contains(
                                x.DirectorateId.Value))
                        .Select(x => x.Id)
                        .ToListAsync();

                    break;


                default:
                    throw new InvalidOperationException(
                        "Invalid broadcast audience.");
            }


            // =====================================================
            // ABSOLUTE RULE:
            // NO ONE SENDS A BROADCAST TO PSO.
            // =====================================================

            var psoIds = await _context.Workers
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Role != null &&
                    x.Role.ToLower() == "pastor in charge")
                .Select(x => x.Id)
                .ToListAsync();

            recipientIds = recipientIds
                .Except(psoIds)
                .Distinct()
                .ToList();

            return recipientIds;
        }


        // =========================================================
        // ALL LEADERS
        //
        // Agreed definition:
        // - Head of Cluster
        // - Head of Directorate
        // - Asst/Assistant Head of Directorate
        // - Head of Service
        // - Asst/Assistant Head of Service
        //
        // Head of Department is deliberately NOT included.
        // PSO is deliberately NOT included.
        // =========================================================

        public async Task<List<int>> GetAllLeaderIdsAsync()
        {
            var leaderIds = new HashSet<int>();

            // =========================================================
            // A. CLUSTER HEADS
            // Based on actual Supervisory Cluster configuration.
            // =========================================================
            var clusterHeadIds =
                await _context.SupervisoryClusters
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.HeadWorkerId.HasValue)
                    .Select(x => x.HeadWorkerId!.Value)
                    .Distinct()
                    .ToListAsync();

            leaderIds.UnionWith(clusterHeadIds);


            // =========================================================
            // B. CONFIGURED DIRECTORATE LEADERSHIP
            // Keep the organisational configuration as a valid source.
            // =========================================================
            var directorateLeadership =
                await _context.Directorates
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .Select(x => new
                    {
                        x.HeadWorkerId,
                        x.AssistantHeadWorkerId
                    })
                    .ToListAsync();

            foreach (var item in directorateLeadership)
            {
                if (item.HeadWorkerId.HasValue)
                    leaderIds.Add(item.HeadWorkerId.Value);

                if (item.AssistantHeadWorkerId.HasValue)
                    leaderIds.Add(item.AssistantHeadWorkerId.Value);
            }


            // =========================================================
            // C. ROLE-BASED LEADERS
            //
            // This ensures leaders recorded in Workers.Role are also
            // recognised even where the Directorate configuration has
            // not yet been populated.
            //
            // Included:
            // - Head of Directorate
            // - Asst Head of Directorate
            // - Assistant Head of Directorate
            // - Head of Service
            // - Asst Head of Service
            // - Assistant Head of Service
            //
            // Head of Department is deliberately NOT included.
            // =========================================================
            var roleBasedLeaderIds =
                await _context.Workers
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.Role != null &&
                        (
                            x.Role.ToLower().Trim() ==
                                "head of directorate" ||

                            x.Role.ToLower().Trim() ==
                                "asst head of directorate" ||

                            x.Role.ToLower().Trim() ==
                                "assistant head of directorate" ||

                            x.Role.ToLower().Trim() ==
                                "head of service" ||

                            x.Role.ToLower().Trim() ==
                                "asst head of service" ||

                            x.Role.ToLower().Trim() ==
                                "assistant head of service"
                        ))
                    .Select(x => x.Id)
                    .ToListAsync();

            leaderIds.UnionWith(roleBasedLeaderIds);


            // =========================================================
            // D. KEEP ACTIVE WORKERS ONLY
            // =========================================================
            var activeLeaderIds =
                await _context.Workers
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        leaderIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync();


            // =========================================================
            // E. PSO CAN NEVER BE A BROADCAST RECIPIENT
            // =========================================================
            var psoIds =
                await _context.Workers
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.Role != null &&
                        x.Role.ToLower().Trim() ==
                            "pastor in charge")
                    .Select(x => x.Id)
                    .ToListAsync();

            return activeLeaderIds
                .Except(psoIds)
                .Distinct()
                .ToList();
        }


        // =========================================================
        // HELPERS
        // =========================================================

        private async Task<List<int>>
            GetAllActiveDirectorateIdsAsync()
        {
            return await _context.Directorates
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();
        }


        private static string Normalize(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        }


        private static string NormalizeAudienceType(
            string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
        // =========================================================
        // SEND BROADCAST
        // =========================================================

        public async Task<SendBroadcastResult> SendBroadcastAsync(
            SendBroadcastRequest request)
        {
            if (request == null)
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message = "Invalid broadcast request."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message = "Broadcast subject is required."
                };
            }

            if (string.IsNullOrWhiteSpace(request.MessageBody))
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message = "Broadcast message is required."
                };
            }

            var sender = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SenderWorkerId &&
                    x.IsActive);

            if (sender == null)
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message = "The broadcast sender could not be found."
                };
            }

            var access =
                await GetSenderAccessAsync(request.SenderWorkerId);

            if (!access.CanSend)
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message =
                        "You are not authorised to send broadcasts."
                };
            }

            var audienceType =
                GetCanonicalAudienceType(request.AudienceType);

            if (string.IsNullOrWhiteSpace(audienceType))
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message = "Please select a valid audience."
                };
            }

            // -----------------------------------------------------
            // Resolve recipients using the security rules already
            // implemented in Stage 2A.
            // -----------------------------------------------------

            List<int> recipientIds;

            try
            {
                recipientIds = await ResolveRecipientIdsAsync(
                    request.SenderWorkerId,
                    audienceType,
                    request.SelectedDirectorateIds);
            }
            catch (InvalidOperationException ex)
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }

            if (recipientIds.Count == 0)
            {
                return new SendBroadcastResult
                {
                    Success = false,
                    Message =
                        "No eligible recipients were found for this broadcast."
                };
            }


            // -----------------------------------------------------
            // Validate and prepare Directorate selections.
            // -----------------------------------------------------

            var audienceDirectorateIds = new List<int>();

            if (audienceType == "SelectedDirectorates")
            {
                audienceDirectorateIds =
                    request.SelectedDirectorateIds
                        .Distinct()
                        .ToList();
            }
            else if (audienceType == "OwnDirectorate")
            {
                audienceDirectorateIds =
                    access.AllowedDirectorateIds
                        .Distinct()
                        .ToList();
            }


            // -----------------------------------------------------
            // Create message.
            // -----------------------------------------------------

            var now = DateTime.UtcNow;

            var broadcast = new BroadcastMessage
            {
                Subject = request.Subject.Trim(),
                MessageBody = request.MessageBody.Trim(),

                SenderType = access.SenderType,
                SenderDisplayLabel =
                    access.SenderDisplayLabel,

                AudienceType = audienceType,

                SentByWorkerId =
                    request.SenderWorkerId,

                CreatedAt = now,
                SentAt = now,

                ExpiresAt = request.ExpiresAt,

                IsSent = true,
                IsActive = true
            };


            // -----------------------------------------------------
            // Create the per-worker inbox records.
            //
            // Every recipient starts unread.
            // -----------------------------------------------------

            foreach (var workerId in recipientIds.Distinct())
            {
                broadcast.Recipients.Add(
                    new BroadcastRecipient
                    {
                        WorkerId = workerId,

                        IsRead = false,
                        FirstReadAt = null,

                        EmailNotificationSent = false,
                        EmailNotificationSentAt = null,
                        EmailNotificationError = null,

                        CreatedAt = now
                    });
            }


            // -----------------------------------------------------
            // Record selected Directorate audience.
            // This gives us an audit trail of what the sender chose.
            // -----------------------------------------------------

            foreach (var directorateId
                     in audienceDirectorateIds.Distinct())
            {
                broadcast.AudienceDirectorates.Add(
                    new BroadcastAudienceDirectorate
                    {
                        DirectorateId = directorateId
                    });
            }


            // -----------------------------------------------------
            // Transaction:
            // message + recipients + audience must save together.
            // -----------------------------------------------------

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.BroadcastMessages.Add(broadcast);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new SendBroadcastResult
                {
                    Success = true,

                    BroadcastMessageId =
                        broadcast.Id,

                    RecipientCount =
                        broadcast.Recipients.Count,

                    Message =
                        $"Broadcast sent successfully to {broadcast.Recipients.Count} recipient(s)."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        // =========================================================
        // MY MESSAGES
        // =========================================================

        public async Task<int> GetUnreadCountAsync(int workerId)
        {
            return await _context.BroadcastRecipients
                .AsNoTracking()
                .CountAsync(x =>
                    x.WorkerId == workerId &&
                    !x.IsRead &&
                    x.BroadcastMessage != null &&
                    x.BroadcastMessage.IsSent &&
                    x.BroadcastMessage.IsActive &&
                    (
                        !x.BroadcastMessage.ExpiresAt.HasValue ||
                        x.BroadcastMessage.ExpiresAt.Value >
                            DateTime.UtcNow
                    ));
        }


        // =========================================================
        // MY MESSAGES INBOX
        // =========================================================

        public async Task<List<MyMessageListItem>>
            GetMyMessagesAsync(int workerId)
        {
            var now = DateTime.UtcNow;

            return await _context.BroadcastRecipients
                .AsNoTracking()
                .Where(x =>
                    x.WorkerId == workerId &&
                    x.BroadcastMessage != null &&
                    x.BroadcastMessage.IsSent &&
                    x.BroadcastMessage.IsActive &&
                    (
                        !x.BroadcastMessage.ExpiresAt.HasValue ||
                        x.BroadcastMessage.ExpiresAt.Value > now
                    ))
                .OrderBy(x => x.IsRead)
                .ThenByDescending(x =>
                    x.BroadcastMessage!.SentAt)
                .Select(x => new MyMessageListItem
                {
                    BroadcastMessageId =
                        x.BroadcastMessageId,

                    Subject =
                        x.BroadcastMessage!.Subject,

                    SenderDisplayLabel =
                        x.BroadcastMessage.SenderDisplayLabel,

                    SentAt =
                        x.BroadcastMessage.SentAt
                            ?? x.BroadcastMessage.CreatedAt,

                    IsRead = x.IsRead,

                    FirstReadAt = x.FirstReadAt
                })
                .ToListAsync();
        }


        // =========================================================
        // OPEN MESSAGE
        //
        // Security:
        // The worker can retrieve the message ONLY if that worker
        // actually has a BroadcastRecipient record for it.
        //
        // Opening the message for the first time marks it as read.
        // FirstReadAt is never overwritten on subsequent opens.
        // =========================================================

        public async Task<MyMessageDetails?> OpenMyMessageAsync(
            int workerId,
            int broadcastMessageId)
        {
            var recipient =
                await _context.BroadcastRecipients
                    .Include(x => x.BroadcastMessage)
                    .FirstOrDefaultAsync(x =>
                        x.WorkerId == workerId &&
                        x.BroadcastMessageId ==
                            broadcastMessageId);

            if (recipient == null ||
                recipient.BroadcastMessage == null)
            {
                return null;
            }

            var message = recipient.BroadcastMessage;

            if (!message.IsSent ||
                !message.IsActive)
            {
                return null;
            }

            if (message.ExpiresAt.HasValue &&
                message.ExpiresAt.Value <= DateTime.UtcNow)
            {
                return null;
            }


            // -----------------------------------------------------
            // First opening only.
            // -----------------------------------------------------

            if (!recipient.IsRead)
            {
                recipient.IsRead = true;

                if (!recipient.FirstReadAt.HasValue)
                {
                    recipient.FirstReadAt =
                        DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }


            return new MyMessageDetails
            {
                BroadcastMessageId =
                    message.Id,

                Subject =
                    message.Subject,

                MessageBody =
                    message.MessageBody,

                SenderDisplayLabel =
                    message.SenderDisplayLabel,

                SentAt =
                    message.SentAt ??
                    message.CreatedAt,

                IsRead =
                    recipient.IsRead,

                FirstReadAt =
                    recipient.FirstReadAt
            };
        }
        // =========================================================
        // PREVIEW BROADCAST RECIPIENTS
        //
        // Used by the New Broadcast page before sending.
        // The same recipient-resolution rules used for sending
        // are used here, so the preview count matches the
        // eligible audience.
        // =========================================================

        public async Task<BroadcastRecipientPreview>
            PreviewRecipientsAsync(
                int senderWorkerId,
                string audienceType,
                IEnumerable<int>? selectedDirectorateIds = null)
        {
            var result = new BroadcastRecipientPreview();

            try
            {
                var access =
                    await GetSenderAccessAsync(senderWorkerId);

                if (!access.CanSend)
                {
                    result.Message =
                        "You are not authorised to send broadcasts.";

                    return result;
                }

                var canonicalAudience =
                    GetCanonicalAudienceType(audienceType);

                if (string.IsNullOrWhiteSpace(canonicalAudience))
                {
                    result.Message =
                        "Please select a valid audience.";

                    return result;
                }

                var recipientIds =
                    await ResolveRecipientIdsAsync(
                        senderWorkerId,
                        canonicalAudience,
                        selectedDirectorateIds);

                result.Success = true;
                result.RecipientCount =
                    recipientIds.Distinct().Count();

                result.Message =
                    result.RecipientCount == 1
                        ? "1 eligible recipient"
                        : $"{result.RecipientCount} eligible recipients";

                return result;
            }
            catch (InvalidOperationException ex)
            {
                result.Success = false;
                result.RecipientCount = 0;
                result.Message = ex.Message;

                return result;
            }
        }
        // =========================================================
        // SENT BROADCASTS
        //
        // Returns broadcasts created by the logged-in sender,
        // including recipient/read statistics.
        // =========================================================

        public async Task<List<SentBroadcastListItem>>
            GetSentBroadcastsAsync(int senderWorkerId)
        {
            var access =
                await GetSenderAccessAsync(senderWorkerId);

            if (!access.CanSend)
            {
                return new List<SentBroadcastListItem>();
            }

            return await _context.BroadcastMessages
                .AsNoTracking()
                .Where(x =>
                    x.SentByWorkerId == senderWorkerId &&
                    x.IsSent &&
                    x.IsActive)
                .OrderByDescending(x =>
                    x.SentAt ?? x.CreatedAt)
                .Select(x => new SentBroadcastListItem
                {
                    BroadcastMessageId = x.Id,

                    Subject = x.Subject,

                    AudienceType = x.AudienceType,

                    SentAt =
                        x.SentAt ?? x.CreatedAt,

                    RecipientCount =
                        x.Recipients.Count(),

                    ReadCount =
                        x.Recipients.Count(r => r.IsRead),

                    UnreadCount =
                        x.Recipients.Count(r => !r.IsRead),

                    DirectorateNames =
                        x.AudienceDirectorates
                            .Where(a =>
                                a.Directorate != null)
                            .Select(a =>
                                a.Directorate!.Name)
                            .OrderBy(name => name)
                            .ToList()
                })
                .ToListAsync();
        }
        // =========================================================
        // GET SENT BROADCAST DETAILS
        // Only the original sender can retrieve the broadcast.
        // =========================================================

        public async Task<SentBroadcastDetails?>
            GetBroadcastDetailsAsync(
                int senderWorkerId,
                int broadcastMessageId)
        {
            var access =
                await GetSenderAccessAsync(senderWorkerId);

            if (!access.CanSend)
                return null;

            return await _context.BroadcastMessages
                .AsNoTracking()
                .Where(x =>
                    x.Id == broadcastMessageId &&
                    x.SentByWorkerId == senderWorkerId &&
                    x.IsSent &&
                    x.IsActive)
                .Select(x => new SentBroadcastDetails
                {
                    BroadcastMessageId = x.Id,

                    Subject = x.Subject,

                    MessageBody = x.MessageBody,

                    AudienceType = x.AudienceType,

                    SenderDisplayLabel =
                        x.SenderDisplayLabel,

                    SentAt =
                        x.SentAt ?? x.CreatedAt,

                    RecipientCount =
                        x.Recipients.Count(),

                    ReadCount =
                        x.Recipients.Count(r => r.IsRead),

                    UnreadCount =
                        x.Recipients.Count(r => !r.IsRead),

                    SelectedDirectorateIds =
                        x.AudienceDirectorates
                            .Select(a => a.DirectorateId)
                            .ToList(),

                    DirectorateNames =
                        x.AudienceDirectorates
                            .Where(a => a.Directorate != null)
                            .Select(a => a.Directorate!.Name)
                            .OrderBy(name => name)
                            .ToList()
                })
                .FirstOrDefaultAsync();
        }
        // =========================================================
        // UPDATE SENT BROADCAST
        //
        // Rules:
        // - Only original sender can edit.
        // - Audience is revalidated against sender's current scope.
        // - New recipients are added unread.
        // - Removed recipients lose the message.
        // - If subject/message changes, retained recipients are
        //   reset to unread because the content has changed.
        // - If only audience changes, retained recipients keep
        //   their existing read status.
        // =========================================================

        public async Task<UpdateBroadcastResult>
            UpdateBroadcastAsync(
                UpdateBroadcastRequest request)
        {
            if (request == null)
            {
                return new UpdateBroadcastResult
                {
                    Message = "Invalid update request."
                };
            }

            var subject =
                request.Subject?.Trim()
                ?? string.Empty;

            var messageBody =
                request.MessageBody?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(subject))
            {
                return new UpdateBroadcastResult
                {
                    Message = "Subject is required."
                };
            }

            if (string.IsNullOrWhiteSpace(messageBody))
            {
                return new UpdateBroadcastResult
                {
                    Message = "Message is required."
                };
            }

            if (subject.Length > 200)
            {
                return new UpdateBroadcastResult
                {
                    Message =
                        "Subject cannot exceed 200 characters."
                };
            }

            var access =
                await GetSenderAccessAsync(
                    request.SenderWorkerId);

            if (!access.CanSend)
            {
                return new UpdateBroadcastResult
                {
                    Message =
                        "You are not authorised to edit broadcasts."
                };
            }

            var broadcast =
                await _context.BroadcastMessages
                    .Include(x => x.Recipients)
                    .Include(x => x.AudienceDirectorates)
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                            request.BroadcastMessageId &&
                        x.SentByWorkerId ==
                            request.SenderWorkerId &&
                        x.IsSent &&
                        x.IsActive);

            if (broadcast == null)
            {
                return new UpdateBroadcastResult
                {
                    Message =
                        "Broadcast was not found or you do not have permission to edit it."
                };
            }

            var canonicalAudience =
                GetCanonicalAudienceType(
                    request.AudienceType);

            if (string.IsNullOrWhiteSpace(
                canonicalAudience))
            {
                return new UpdateBroadcastResult
                {
                    Message =
                        "Please select a valid audience."
                };
            }

            List<int> newRecipientIds;

            try
            {
                newRecipientIds =
                    (await ResolveRecipientIdsAsync(
                        request.SenderWorkerId,
                        canonicalAudience,
                        request.SelectedDirectorateIds))
                    .Distinct()
                    .ToList();
            }
            catch (InvalidOperationException ex)
            {
                return new UpdateBroadcastResult
                {
                    Message = ex.Message
                };
            }

            if (newRecipientIds.Count == 0)
            {
                return new UpdateBroadcastResult
                {
                    Message =
                        "There are no eligible recipients for the selected audience."
                };
            }

            var contentChanged =
                !string.Equals(
                    broadcast.Subject,
                    subject,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    broadcast.MessageBody,
                    messageBody,
                    StringComparison.Ordinal);

            var existingRecipientIds =
                broadcast.Recipients
                    .Select(x => x.WorkerId)
                    .ToHashSet();

            var newRecipientIdSet =
                newRecipientIds.ToHashSet();

            // Remove workers no longer in the audience.
            var recipientsToRemove =
                broadcast.Recipients
                    .Where(x =>
                        !newRecipientIdSet.Contains(
                            x.WorkerId))
                    .ToList();

            if (recipientsToRemove.Count > 0)
            {
                _context.BroadcastRecipients
                    .RemoveRange(recipientsToRemove);
            }

            // Existing recipients:
            // reset to unread only when content changed.
            if (contentChanged)
            {
                foreach (var recipient
                    in broadcast.Recipients
                        .Where(x =>
                            newRecipientIdSet.Contains(
                                x.WorkerId)))
                {
                    recipient.IsRead = false;
                    recipient.FirstReadAt = null;
                }
            }

            // Add newly eligible recipients.
            foreach (var workerId in newRecipientIds)
            {
                if (existingRecipientIds.Contains(
                    workerId))
                {
                    continue;
                }

                broadcast.Recipients.Add(
                    new BroadcastRecipient
                    {
                        WorkerId = workerId,

                        IsRead = false,

                        FirstReadAt = null,

                        EmailNotificationSent = false,

                        EmailNotificationSentAt = null,

                        EmailNotificationError = null,

                        CreatedAt = DateTime.UtcNow
                    });
            }

            // Replace stored Directorate audience.
            if (broadcast.AudienceDirectorates.Count > 0)
            {
                _context.BroadcastAudienceDirectorates
                    .RemoveRange(
                        broadcast.AudienceDirectorates);
            }

            if (canonicalAudience ==
                    "SelectedDirectorates")
            {
                foreach (var directorateId
                    in request.SelectedDirectorateIds
                        .Distinct())
                {
                    broadcast.AudienceDirectorates.Add(
                        new BroadcastAudienceDirectorate
                        {
                            DirectorateId =
                                directorateId
                        });
                }
            }
            else if (canonicalAudience ==
                     "OwnDirectorate")
            {
                // Resolve the sender's permitted Directorate
                // through the existing access service.
                var directorates =
                    await GetAvailableDirectoratesAsync(
                        request.SenderWorkerId);

                foreach (var directorate
                    in directorates)
                {
                    broadcast.AudienceDirectorates.Add(
                        new BroadcastAudienceDirectorate
                        {
                            DirectorateId =
                                directorate.Id
                        });
                }
            }

            broadcast.Subject =
                subject;

            broadcast.MessageBody =
                messageBody;

            broadcast.AudienceType =
                canonicalAudience;

            await _context.SaveChangesAsync();

            return new UpdateBroadcastResult
            {
                Success = true,

                RecipientCount =
                    newRecipientIds.Count,

                ContentChanged =
                    contentChanged,

                Message =
                    contentChanged
                        ? "Broadcast updated. Recipients have been marked unread so they can review the revised message."
                        : "Broadcast audience updated successfully."
            };
        }
        // =========================================================
        // REMOVE SENT BROADCAST
        //
        // Soft delete:
        // IsActive = false.
        //
        // Existing inbox queries already require IsActive,
        // therefore recipients will no longer see the message.
        // The database record remains available for audit/history.
        // =========================================================

        public async Task<bool>
            RemoveBroadcastAsync(
                int senderWorkerId,
                int broadcastMessageId)
        {
            var broadcast =
                await _context.BroadcastMessages
                    .FirstOrDefaultAsync(x =>
                        x.Id == broadcastMessageId &&
                        x.SentByWorkerId == senderWorkerId &&
                        x.IsSent &&
                        x.IsActive);

            if (broadcast == null)
                return false;

            var access =
                await GetSenderAccessAsync(
                    senderWorkerId);

            if (!access.CanSend)
                return false;

            broadcast.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
        }
    }


    // =============================================================
    // SUPPORTING DTOs
    // =============================================================

    public class SentBroadcastDetails
    {
        public int BroadcastMessageId { get; set; }

        public string Subject { get; set; }
            = string.Empty;

        public string MessageBody { get; set; }
            = string.Empty;

        public string AudienceType { get; set; }
            = string.Empty;

        public string SenderDisplayLabel { get; set; }
            = string.Empty;

        public DateTime SentAt { get; set; }

        public int RecipientCount { get; set; }

        public int ReadCount { get; set; }

        public int UnreadCount { get; set; }

        public List<int> SelectedDirectorateIds
        {
            get;
            set;
        } = new();

        public List<string> DirectorateNames
        {
            get;
            set;
        } = new();
    }


    public class UpdateBroadcastRequest
    {
        public int BroadcastMessageId { get; set; }

        public int SenderWorkerId { get; set; }

        public string Subject { get; set; }
            = string.Empty;

        public string MessageBody { get; set; }
            = string.Empty;

        public string AudienceType { get; set; }
            = string.Empty;

        public List<int> SelectedDirectorateIds
        {
            get;
            set;
        } = new();
    }


    public class UpdateBroadcastResult
    {
        public bool Success { get; set; }

        public int RecipientCount { get; set; }

        public bool ContentChanged { get; set; }

        public string Message { get; set; }
            = string.Empty;
    }

    public class SentBroadcastListItem
    {
        public int BroadcastMessageId { get; set; }

        public string Subject { get; set; }
            = string.Empty;

        public string AudienceType { get; set; }
            = string.Empty;

        public DateTime SentAt { get; set; }

        public int RecipientCount { get; set; }

        public int ReadCount { get; set; }

        public int UnreadCount { get; set; }

        public List<string> DirectorateNames { get; set; }
            = new();
    }
    public class BroadcastRecipientPreview
    {
        public bool Success { get; set; }

        public int RecipientCount { get; set; }

        public string Message { get; set; }
            = string.Empty;
    }
    public class BroadcastSenderAccess
    {
        public bool CanSend { get; set; }

        public string SenderType { get; set; }
            = string.Empty;

        public string SenderDisplayLabel { get; set; }
            = string.Empty;

        public bool CanSendToAllWorkers { get; set; }

        public bool CanSendToAllLeaders { get; set; }

        public bool CanSelectDirectorates { get; set; }

        public List<int> AllowedDirectorateIds { get; set; }
            = new();
    }


    public class BroadcastDirectorateOption
    {
        public int Id { get; set; }

        public string Name { get; set; }
            = string.Empty;
    }
}
public class SendBroadcastRequest
{
    public int SenderWorkerId { get; set; }

    public string Subject { get; set; }
        = string.Empty;

    public string MessageBody { get; set; }
        = string.Empty;

    // AllWorkers, AllLeaders,
    // SelectedDirectorates, OwnDirectorate
    public string AudienceType { get; set; }
        = string.Empty;

    public List<int> SelectedDirectorateIds { get; set; }
        = new();

    public DateTime? ExpiresAt { get; set; }
}


public class SendBroadcastResult
{
    public bool Success { get; set; }

    public int BroadcastMessageId { get; set; }

    public int RecipientCount { get; set; }

    public string Message { get; set; }
        = string.Empty;
}
public class MyMessageListItem
{
    public int BroadcastMessageId { get; set; }

    public string Subject { get; set; }
        = string.Empty;

    public string SenderDisplayLabel { get; set; }
        = string.Empty;

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public DateTime? FirstReadAt { get; set; }
}


public class MyMessageDetails
{
    public int BroadcastMessageId { get; set; }

    public string Subject { get; set; }
        = string.Empty;

    public string MessageBody { get; set; }
        = string.Empty;

    public string SenderDisplayLabel { get; set; }
        = string.Empty;

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public DateTime? FirstReadAt { get; set; }
}