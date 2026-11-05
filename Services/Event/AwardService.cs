using ChurchApp.Data;
using ChurchApp.Models;
using ChurchApp.Models.Event;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ChurchApp.Services.Event;

public sealed class AwardService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    private readonly PrivilegeService privileges;
    public const string ResultsPrivilege = "Access-Award-Results";
    public const string SetupPrivilege = "Manage-Awards";
    public AwardService(AppDbContext db, AuthService auth, PrivilegeService privileges)
    { this.db = db; this.auth = auth; this.privileges = privileges; }

    private Worker Current => auth.CurrentWorker is { IsActive: true } w ? w : throw new UnauthorizedAccessException("Please log in as an active worker.");
    private bool Admin => auth.IsInRole(AuthService.Roles.ChurchAdmin, AuthService.Roles.PastorInCharge);
    public async Task<bool> CanManageAsync() => Admin || await privileges.HasPrivilegeAsync(Current.Id, SetupPrivilege);
    public async Task<bool> CanSeeResultsAsync() => Admin || await privileges.HasPrivilegeAsync(Current.Id, ResultsPrivilege);
    private async Task RequireManageAsync() { _ = Current; if (!await CanManageAsync()) throw new UnauthorizedAccessException("Award setup access denied."); }
    private async Task RequireResultsAsync() { _ = Current; if (!await CanSeeResultsAsync()) throw new UnauthorizedAccessException("Award results access denied."); }

    public async Task<List<AwardProgramme>> ResultsProgrammesAsync()
    { await RequireResultsAsync(); return await db.Set<AwardProgramme>().AsNoTracking().OrderByDescending(x => x.Year).ToListAsync(); }
    public async Task<List<AwardProgramme>> ProgrammesAsync() { await RequireManageAsync(); return await db.Set<AwardProgramme>().AsNoTracking().OrderByDescending(x => x.Year).ToListAsync(); }
    public async Task<AwardProgramme> SaveProgrammeAsync(AwardProgramme p)
    {
        await RequireManageAsync();
        if (string.IsNullOrWhiteSpace(p.Name) || p.Year < 2000 || p.Year > 2200) throw new InvalidOperationException("Enter a valid programme and year.");
        if (p.Id == 0) db.Set<AwardProgramme>().Add(p);
        else { var row = await db.Set<AwardProgramme>().FindAsync(p.Id) ?? throw new InvalidOperationException("Programme not found."); row.Name = p.Name.Trim(); row.Year = p.Year; row.IsActive = p.IsActive; }
        await db.SaveChangesAsync(); return p;
    }
    public async Task<List<AwardCategory>> SetupAwardsAsync(int programmeId)
    { await RequireManageAsync(); return await db.Set<AwardCategory>().AsNoTracking().Where(x => x.AwardProgrammeId == programmeId).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(); }
    public async Task SaveAwardAsync(AwardCategory input)
    {
        await RequireManageAsync();
        if (!await db.Set<AwardProgramme>().AnyAsync(x => x.Id == input.AwardProgrammeId)) throw new InvalidOperationException("Programme not found.");
        if (string.IsNullOrWhiteSpace(input.Name) || input.ClosesAtUtc <= input.OpensAtUtc) throw new InvalidOperationException("Provide an award name and a valid nomination window.");
        if (!new[] { "AllWorkers", "Leaders", "PastorInCharge" }.Contains(input.NominationAudience) ||
            !new[] { "Worker", "Directorate" }.Contains(input.NomineeType) || !new[] { "Any", "Male", "Female" }.Contains(input.GenderFilter))
            throw new InvalidOperationException("Invalid award configuration.");
        if (input.MaxNominationsPerWorker < 1 || input.MaxNominationsPerWorker > 20) throw new InvalidOperationException("Maximum nominations must be between 1 and 20.");
        if (!new[] { "Any", "OneMaleOneFemale" }.Contains(input.SelectionRule)) throw new InvalidOperationException("Invalid selection rule.");
        if (input.SelectionRule == "OneMaleOneFemale" && (input.NomineeType != "Worker" || input.MaxNominationsPerWorker != 2 || input.GenderFilter != "Any")) throw new InvalidOperationException("Male/female pairing requires two worker nominations with gender Any.");
        if (input.NomineeType == "Directorate" && input.MaxNominationsPerWorker != 1) throw new InvalidOperationException("Directorate award must allow exactly one nomination.");
        if (input.RestrictNomineesToOwnDirectorate && (input.NomineeType != "Worker" || input.NominationAudience != "Leaders"))
            throw new InvalidOperationException("Own-directorate worker nominations require Leader audience.");
        if (input.NomineeType == "Directorate" && input.NominationAudience != "Leaders")
            throw new InvalidOperationException("Directorate nominations must be restricted to Leaders (Heads of Directorates).");
        if (input.NomineeType == "Directorate" &&
            (input.SelectionRule != "Any" || input.GenderFilter != "Any" || input.RestrictNomineesToOwnDirectorate))
        {
            throw new InvalidOperationException(
                "Directorate awards require the Other Directorates scope without gender restrictions.");
        }

        input.RestrictNomineesToOwnDirectorate = input.NominationAudience == "Leaders" && input.NomineeType == "Worker" && input.RestrictNomineesToOwnDirectorate;
        if (input.Id == 0) db.Set<AwardCategory>().Add(input);
        else
        {
            var row = await db.Set<AwardCategory>().FindAsync(input.Id) ?? throw new InvalidOperationException("Award not found.");
            if (row.AwardProgrammeId != input.AwardProgrammeId) throw new InvalidOperationException("Programme cannot be changed.");
            row.Name = input.Name.Trim(); row.Description = input.Description; row.NominationAudience = input.NominationAudience;
            row.NomineeType = input.NomineeType; row.GenderFilter = input.GenderFilter; row.NominationAllowed = input.NominationAllowed; row.RestrictNomineesToOwnDirectorate = input.RestrictNomineesToOwnDirectorate;
            row.MaxNominationsPerWorker = input.MaxNominationsPerWorker; row.SelectionRule = input.SelectionRule;
            row.IsActive = input.IsActive; row.OpensAtUtc = input.OpensAtUtc; row.ClosesAtUtc = input.ClosesAtUtc; row.SortOrder = input.SortOrder;
        }
        await db.SaveChangesAsync();
    }

    private static bool Eligible(Worker w, AwardCategory a)
    {
        return a.NominationAudience switch
        {
            "AllWorkers" => true,
            "Leaders" => w.Role == AuthService.Roles.HeadOfDirectorate && w.DirectorateId.HasValue,
            "PastorInCharge" => w.Role == AuthService.Roles.PastorInCharge,
            _ => false
        };
    }
    public async Task<List<AwardCategory>> MyOpenAwardsAsync()
    {
        var w = Current; var now = DateTime.UtcNow;
        var activeProgrammeIds = await db.Set<AwardProgramme>().AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToListAsync();
        var awards = await db.Set<AwardCategory>().AsNoTracking().Where(x => activeProgrammeIds.Contains(x.AwardProgrammeId) && x.IsActive && x.NominationAllowed && x.OpensAtUtc <= now && x.ClosesAtUtc > now).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        return awards.Where(x => Eligible(w, x)).ToList();
    }
    public async Task<Worker?> MySelectedWorkerAsync(int awardId, int workerId)
    {
        var w = Current; var award = await OpenEligibleAwardAsync(awardId, w);
        if (award.NomineeType != "Worker") return null;
        return await db.Workers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workerId && x.IsActive && x.Id != w.Id &&
            (award.GenderFilter == "Any" || x.Sex == award.GenderFilter) &&
            (!award.RestrictNomineesToOwnDirectorate || (w.DirectorateId.HasValue && (x.DirectorateId == w.DirectorateId || x.SecondaryDirectorateId == w.DirectorateId))));
    }
    public async Task<List<Worker>> SearchWorkersAsync(int awardId, string search)
    {
        var currentWorker = Current;
        var award = await OpenEligibleAwardAsync(awardId, currentWorker);

        if (award.NomineeType != "Worker")
        {
            return new List<Worker>();
        }

        search = (search ?? string.Empty).Trim();

        // Church-wide is the default: all active workers except the nominator.
        var query = db.Workers
            .AsNoTracking()
            .Where(worker => worker.IsActive && worker.Id != currentWorker.Id);

        if (award.RestrictNomineesToOwnDirectorate)
        {
            // This flag is permitted only for Head-of-Directorate worker awards.
            // Include workers assigned to the directorate as primary or secondary.
            var directorateId = currentWorker.DirectorateId!.Value;

            query = query.Where(worker =>
                worker.DirectorateId == directorateId ||
                worker.SecondaryDirectorateId == directorateId);
        }

        if (award.GenderFilter != "Any")
        {
            query = query.Where(worker => worker.Sex == award.GenderFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(worker =>
                worker.FirstName.Contains(search) ||
                worker.LastName.Contains(search) ||
                worker.WorkerId.Contains(search));
        }

        // Limit initial dropdown size; users can search for more names.
        return await query
            .OrderBy(worker => worker.FirstName)
            .ThenBy(worker => worker.LastName)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<Directorate>> OtherDirectoratesAsync(int awardId)
    {
        var w = Current; var award = await OpenEligibleAwardAsync(awardId, w);
        if (award.NomineeType != "Directorate" || w.Role != AuthService.Roles.HeadOfDirectorate || !w.DirectorateId.HasValue)
            throw new UnauthorizedAccessException("Only a Head of Directorate with an assigned directorate may nominate another directorate.");
        return await db.Directorates.AsNoTracking().Where(x => x.Id != w.DirectorateId.Value).OrderBy(x => x.Name).ToListAsync();
    }
    private async Task<AwardCategory> OpenEligibleAwardAsync(int awardId, Worker w)
    {
        var now = DateTime.UtcNow;
        var award = await db.Set<AwardCategory>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == awardId && x.IsActive && x.NominationAllowed && x.OpensAtUtc <= now && x.ClosesAtUtc > now)
            ?? throw new InvalidOperationException("Nomination for this award is not open.");
        if (!await db.Set<AwardProgramme>().AnyAsync(x => x.Id == award.AwardProgrammeId && x.IsActive) || !Eligible(w, award))
            throw new UnauthorizedAccessException("You cannot nominate for this award.");
        return award;
    }
    public async Task<List<AwardNomination>> MyNominationsAsync()
    { var w = Current; return await db.Set<AwardNomination>().AsNoTracking().Where(x => x.NominatorWorkerId == w.Id).ToListAsync(); }
    public sealed record NominationInput(int AwardId, int SlotNumber, int? WorkerId, int? DirectorateId, bool NoNomination, string Reason);
    public async Task SubmitAsync(List<NominationInput> inputs)
    {
        var w = Current;
        if (inputs.GroupBy(x => (x.AwardId, x.SlotNumber)).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Duplicate nomination slots.");
        var validated = new List<(NominationInput Input, AwardCategory Award, Worker? Nominee)>();
        foreach (var input in inputs)
        {
            var award = await OpenEligibleAwardAsync(input.AwardId, w);
            if (input.SlotNumber < 1 || input.SlotNumber > award.MaxNominationsPerWorker)
                throw new InvalidOperationException("Nomination slot exceeds the award maximum.");
            Worker? nominee = null;
            if (input.NoNomination)
            {
                if (input.WorkerId.HasValue || input.DirectorateId.HasValue)
                    throw new InvalidOperationException("No nomination must not have a nominee.");
            }
            else if (award.NomineeType == "Worker")
            {
                if (!input.WorkerId.HasValue || input.DirectorateId.HasValue || string.IsNullOrWhiteSpace(input.Reason))
                    throw new InvalidOperationException("Select a worker and provide justification for each nomination.");
                nominee = await db.Workers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.WorkerId.Value && x.IsActive)
                    ?? throw new InvalidOperationException("Nominee not found or inactive.");
                if (nominee.Id == w.Id) throw new InvalidOperationException("Self-nomination is not allowed.");
                if (award.GenderFilter != "Any" && !string.Equals(nominee.Sex, award.GenderFilter, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Nominee does not match the award gender.");
                if (award.RestrictNomineesToOwnDirectorate && (!w.DirectorateId.HasValue ||
                    (nominee.DirectorateId != w.DirectorateId && nominee.SecondaryDirectorateId != w.DirectorateId)))
                    throw new InvalidOperationException("Leader nominees must belong to your directorate.");
            }
            else
            {
                if (w.Role != AuthService.Roles.HeadOfDirectorate || !w.DirectorateId.HasValue)
                    throw new UnauthorizedAccessException("Head of Directorate role required.");
                if (!input.DirectorateId.HasValue || input.WorkerId.HasValue || string.IsNullOrWhiteSpace(input.Reason))
                    throw new InvalidOperationException("Choose a directorate and provide justification.");
                if (input.DirectorateId == w.DirectorateId) throw new InvalidOperationException("You cannot nominate your own directorate.");
                if (!await db.Directorates.AnyAsync(x => x.Id == input.DirectorateId.Value))
                    throw new InvalidOperationException("Directorate not found.");
            }
            if ((input.Reason ?? "").Length > 2000) throw new InvalidOperationException("Justification is too long.");
            validated.Add((input, award, nominee));
        }
        // Check the complete resulting set, including previously saved slots not in this request.
        foreach (var group in validated.GroupBy(x => x.Award.Id))
        {
            var award = group.First().Award;
            var existing = await db.Set<AwardNomination>().AsNoTracking()
                .Where(x => x.AwardCategoryId == award.Id && x.NominatorWorkerId == w.Id)
                .ToListAsync();
            var combined = existing.ToDictionary(x => x.SlotNumber, x => (x.NomineeWorkerId, x.NomineeDirectorateId, x.NoNomination));
            foreach (var item in group)
                combined[item.Input.SlotNumber] = (item.Input.NoNomination ? null : item.Input.WorkerId,
                    item.Input.NoNomination ? null : item.Input.DirectorateId, item.Input.NoNomination);
            if (combined.Count > award.MaxNominationsPerWorker || combined.Keys.Any(k => k < 1 || k > award.MaxNominationsPerWorker))
                throw new InvalidOperationException("Too many nominations for this award.");
            var workerIds = combined.Values.Where(x => !x.NoNomination && x.NomineeWorkerId.HasValue)
                .Select(x => x.NomineeWorkerId!.Value).ToList();
            if (workerIds.Count != workerIds.Distinct().Count())
                throw new InvalidOperationException("You cannot nominate the same worker twice for an award.");
            var directorateIds = combined.Values.Where(x => !x.NoNomination && x.NomineeDirectorateId.HasValue)
                .Select(x => x.NomineeDirectorateId!.Value).ToList();
            if (directorateIds.Count != directorateIds.Distinct().Count())
                throw new InvalidOperationException("You cannot nominate the same directorate twice.");
            if (award.SelectionRule == "OneMaleOneFemale" && workerIds.Count > 0)
            {
                var sexes = await db.Workers.AsNoTracking().Where(x => workerIds.Contains(x.Id))
                    .Select(x => x.Sex).ToListAsync();
                if (sexes.Any(x => !string.Equals(x, "Male", StringComparison.OrdinalIgnoreCase) &&
                                   !string.Equals(x, "Female", StringComparison.OrdinalIgnoreCase)) ||
                    sexes.Count(x => string.Equals(x, "Male", StringComparison.OrdinalIgnoreCase)) > 1 ||
                    sexes.Count(x => string.Equals(x, "Female", StringComparison.OrdinalIgnoreCase)) > 1)
                    throw new InvalidOperationException("This award permits one male and one female nominee only.");
            }
        }
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var (input, _, _) in validated)
        {
            var row = await db.Set<AwardNomination>().FirstOrDefaultAsync(x =>
                x.AwardCategoryId == input.AwardId && x.NominatorWorkerId == w.Id && x.SlotNumber == input.SlotNumber);
            if (row == null)
            {
                row = new AwardNomination { AwardCategoryId = input.AwardId, NominatorWorkerId = w.Id, SlotNumber = input.SlotNumber };
                db.Set<AwardNomination>().Add(row);
            }
            row.NoNomination = input.NoNomination;
            row.NomineeWorkerId = input.NoNomination ? null : input.WorkerId;
            row.NomineeDirectorateId = input.NoNomination ? null : input.DirectorateId;
            row.Justification = input.NoNomination ? "" : (input.Reason ?? "").Trim();
            row.IsSubmitted = true; row.UpdatedAtUtc = DateTime.UtcNow; row.SubmittedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    public sealed record NominationEvidence(
        int NominationId,
        string NominatorName,
        string NominatorDirectorate,
        string Justification,
        DateTime SubmittedAtUtc);

    public sealed record ResultNominee(
        int NomineeId,
        string Name,
        string Gender,
        int Count,
        int Rank,
        List<NominationEvidence> Entries);

    public sealed record DirectorateResultGroup(
        int DirectorateId,
        string DirectorateName,
        List<ResultNominee> Nominees);

    public sealed record AwardResultDetail(
        int AwardId,
        string AwardName,
        string Description,
        string NomineeType,
        string SelectionRule,
        string GenderFilter,
        int TotalNominations,
        List<ResultNominee> Nominees,
        bool IsOwnDirectorateAward,
        List<DirectorateResultGroup> DirectorateGroups);

    public async Task<List<AwardResultDetail>> ResultsDashboardAsync(int programmeId)
    {
        await RequireResultsAsync();

        var awards = await db.Set<AwardCategory>()
            .AsNoTracking()
            .Where(x => x.AwardProgrammeId == programmeId && x.NominationAllowed)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var awardIds = awards.Select(x => x.Id).ToList();
        var votes = await db.Set<AwardNomination>()
            .AsNoTracking()
            .Where(x => awardIds.Contains(x.AwardCategoryId) &&
                        x.IsSubmitted && !x.NoNomination)
            .ToListAsync();

        var workerIds = votes
            .Where(x => x.NomineeWorkerId.HasValue)
            .Select(x => x.NomineeWorkerId!.Value)
            .Concat(votes.Select(x => x.NominatorWorkerId))
            .Distinct()
            .ToList();

        var workers = await db.Workers.AsNoTracking()
            .Where(x => workerIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.FirstName,
                x.LastName,
                x.Sex,
                x.DirectorateId
            })
            .ToDictionaryAsync(x => x.Id);

        var directorates = await db.Directorates.AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        string WorkerName(int id) => workers.TryGetValue(id, out var worker)
            ? (worker.FirstName + " " + worker.LastName).Trim()
            : "Unknown worker";

        string DirectorateName(int? id) => id.HasValue
            ? directorates.GetValueOrDefault(id.Value, "Unknown directorate")
            : "Unassigned directorate";

        List<ResultNominee> BuildRanking(List<AwardNomination> nominations, bool isDirectorate)
        {
            var grouped = nominations
                .Where(x => isDirectorate ? x.NomineeDirectorateId.HasValue : x.NomineeWorkerId.HasValue)
                .GroupBy(x => isDirectorate ? x.NomineeDirectorateId!.Value : x.NomineeWorkerId!.Value)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => isDirectorate ? DirectorateName(x.Key) : WorkerName(x.Key))
                .ToList();

            var nominees = new List<ResultNominee>();
            var rank = 0;
            foreach (var group in grouped)
            {
                rank++;
                var name = isDirectorate ? DirectorateName(group.Key) : WorkerName(group.Key);
                var gender = !isDirectorate && workers.TryGetValue(group.Key, out var worker)
                    ? worker.Sex ?? ""
                    : "";

                var evidence = group
                    .OrderByDescending(x => x.SubmittedAtUtc)
                    .Select(x =>
                    {
                        var nominator = workers.GetValueOrDefault(x.NominatorWorkerId);
                        return new NominationEvidence(
                            x.Id,
                            WorkerName(x.NominatorWorkerId),
                            DirectorateName(nominator?.DirectorateId),
                            x.Justification ?? "",
x.SubmittedAtUtc ?? x.UpdatedAtUtc);
                    })
                    .ToList();

                nominees.Add(new ResultNominee(group.Key, name, gender, group.Count(), rank, evidence));
            }

            return nominees;
        }

        var result = new List<AwardResultDetail>();
        foreach (var award in awards)
        {
            var awardVotes = votes.Where(x => x.AwardCategoryId == award.Id).ToList();
            var isOwnDirectorate = award.NomineeType == "Worker" &&
                                   award.NominationAudience == "Leaders" &&
                                   award.RestrictNomineesToOwnDirectorate;

            var groups = new List<DirectorateResultGroup>();
            if (isOwnDirectorate)
            {
                // Group by the nominating Head's directorate, not the nominee's
                // primary directorate (which may differ for secondary assignments).
                groups = awardVotes
                    .GroupBy(x => workers.TryGetValue(x.NominatorWorkerId, out var head)
                        ? head.DirectorateId
                        : null)
                    .OrderBy(x => DirectorateName(x.Key))
                    .Select(x => new DirectorateResultGroup(
                        x.Key ?? 0,
                        DirectorateName(x.Key),
                        BuildRanking(x.ToList(), false)))
                    .ToList();
            }

            result.Add(new AwardResultDetail(
                award.Id,
                award.Name,
                award.Description,
                award.NomineeType,
                award.SelectionRule,
                award.GenderFilter,
                awardVotes.Count,
                BuildRanking(awardVotes, award.NomineeType == "Directorate"),
                isOwnDirectorate,
                groups));
        }

        return result;
    }

    public sealed record Ranking(int AwardId, string AwardName, string Nominee, int Count, int Rank);
    public async Task<List<Ranking>> RankingsAsync(int programmeId)
    {
        await RequireResultsAsync();
        var awards = await db.Set<AwardCategory>().AsNoTracking().Where(x => x.AwardProgrammeId == programmeId && x.NominationAllowed).ToListAsync();
        var ids = awards.Select(x => x.Id).ToList();
        var votes = await db.Set<AwardNomination>().AsNoTracking().Where(x => ids.Contains(x.AwardCategoryId) && x.IsSubmitted && !x.NoNomination).ToListAsync();
        var workerIds = votes.Where(x => x.NomineeWorkerId.HasValue).Select(x => x.NomineeWorkerId!.Value).Distinct().ToList();
        var directorateIds = votes.Where(x => x.NomineeDirectorateId.HasValue).Select(x => x.NomineeDirectorateId!.Value).Distinct().ToList();
        var workers = await db.Workers.AsNoTracking().Where(x => workerIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName);
        var directorates = await db.Directorates.AsNoTracking().Where(x => directorateIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        var result = new List<Ranking>();
        foreach (var award in awards)
        {
            var grouped = votes.Where(x => x.AwardCategoryId == award.Id)
                .GroupBy(x => x.NomineeWorkerId.HasValue ? "W" + x.NomineeWorkerId : "D" + x.NomineeDirectorateId)
                .Select(g => new { Key = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ThenBy(x => x.Key).ToList();
            var rank = 0; var previous = -1; var position = 0;
            foreach (var g in grouped)
            {
                position++; if (g.Count != previous) rank = position; previous = g.Count;
                if (rank > 3) break;
                var numericId = int.Parse(g.Key.Substring(1));
                var name = g.Key[0] == 'W' ? workers.GetValueOrDefault(numericId, "Unknown worker") : directorates.GetValueOrDefault(numericId, "Unknown directorate");
                result.Add(new Ranking(award.Id, award.Name, name, g.Count, rank));
            }
        }
        return result;
    }
}









