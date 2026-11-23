using ChurchApp.Data;
using ChurchApp.Models.Event;
using ChurchApp.Services;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event;

public sealed class EventFunService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    public EventFunService(AppDbContext db, AuthService auth)
    { this.db = db; this.auth = auth; }
    private string WorkerId => auth.IsAuthenticated && !string.IsNullOrWhiteSpace(auth.CurrentWorker?.WorkerId)
        ? auth.CurrentWorker.WorkerId : throw new UnauthorizedAccessException("Please sign in.");
    private void Admin()
    {
        _ = WorkerId;
        if (!auth.CanAccessAdminPanel()) throw new UnauthorizedAccessException("Administrator access required.");
    }
    public async Task<List<ChurchEvent>> EventsAsync()
    {
        Admin(); 
        return await db.ChurchEvents.AsNoTracking().OrderByDescending(x=>x.StartsAtUtc).ToListAsync();
    }
    public async Task<List<EventFunGame>> GamesAsync(bool admin = false)
    {
        if (admin) Admin(); else _ = WorkerId;
        
        var query = db.EventFunGames.AsNoTracking();
        if (!admin) query = query.Where(x => x.IsPublished);
        return await query.OrderByDescending(x=>x.Id).ToListAsync();
    }
    public async Task<EventFunGame> GameAsync(int id, bool admin = false)
    {
        if (admin) Admin(); else _ = WorkerId;
        
        var game = await db.EventFunGames.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);
        if (game is null || (!admin && !game.IsPublished)) throw new InvalidOperationException("Game unavailable.");
        return game;
    }
    public async Task<List<EventFunQuestion>> QuestionsAsync(int id, bool admin = false)
    {
        // Correct answers are confidential until the MC reveals them.
        // Never expose the question bank through participant-facing service methods.
        Admin();
        
        return await db.EventFunQuestions.AsNoTracking().Where(x=>x.EventFunGameId==id).OrderBy(x=>x.Position).ToListAsync();
    }
    public async Task SaveGameAsync(EventFunGame input)
    {
        Admin();
        if (input.Kind is not ("FastestFinger" or "BibleQuiz")) throw new InvalidOperationException("Invalid game type.");
        if (string.IsNullOrWhiteSpace(input.Title) || input.ClosesAtUtc<=input.OpensAtUtc)
            throw new InvalidOperationException("Enter a title and valid opening/closing times.");
        if (input.Kind=="FastestFinger" && string.IsNullOrWhiteSpace(input.Phrase))
            throw new InvalidOperationException("Enter the challenge phrase.");
        
        if (!await db.ChurchEvents.AnyAsync(x=>x.Id==input.ChurchEventId)) throw new InvalidOperationException("Select an event.");
        if (input.Id==0) db.EventFunGames.Add(input);
        else
        {
            var current=await db.EventFunGames.FindAsync(input.Id) ?? throw new InvalidOperationException("Game not found.");
            current.Title=input.Title.Trim(); current.ChurchEventId=input.ChurchEventId;
            current.Phrase=input.Phrase.Trim(); current.OpensAtUtc=input.OpensAtUtc;
            current.ClosesAtUtc=input.ClosesAtUtc; current.IsPublished=input.IsPublished;
            // Game type and MC state cannot be changed via general configuration.
        }
        await db.SaveChangesAsync();
    }
    public async Task SaveQuestionsAsync(int gameId, IReadOnlyList<EventFunQuestion> questions)
    {
        Admin();
        if (questions.Count!=4 || questions.Select(x=>x.Position).Distinct().Count()!=4 ||
            !questions.Select(x=>x.Position).OrderBy(x=>x).SequenceEqual(new[]{1,2,3,4}))
            throw new InvalidOperationException("Exactly four numbered questions are required.");
        if (questions.Any(q=>new[]{q.Text,q.OptionA,q.OptionB,q.OptionC,q.OptionD}.Any(string.IsNullOrWhiteSpace) ||
            q.CorrectOption is not ("A" or "B" or "C" or "D")))
            throw new InvalidOperationException("Complete each question, four choices and correct answer.");
        
        if (!await db.EventFunGames.AnyAsync(x=>x.Id==gameId && x.Kind=="BibleQuiz")) throw new InvalidOperationException("Quiz not found.");
        if (await db.EventFunEntries.AnyAsync(x=>x.EventFunGameId==gameId))
            throw new InvalidOperationException("Questions cannot change after submissions have started.");
        var existing=await db.EventFunQuestions.Where(x=>x.EventFunGameId==gameId).ToListAsync();
        // Update existing positions in place to avoid unique-index collisions.
        foreach(var q in questions)
        {
            var target=existing.FirstOrDefault(x=>x.Position==q.Position);
            if(target==null) { target=new EventFunQuestion{EventFunGameId=gameId,Position=q.Position}; db.EventFunQuestions.Add(target); }
            target.Text=q.Text.Trim();target.OptionA=q.OptionA.Trim();target.OptionB=q.OptionB.Trim();
            target.OptionC=q.OptionC.Trim();target.OptionD=q.OptionD.Trim();
            target.CorrectOption=q.CorrectOption;target.BibleReference=q.BibleReference?.Trim();
        }
        await db.SaveChangesAsync();
    }
    public async Task SetQuizControlAsync(int gameId,string action)
    {
        Admin(); 
        var game=await db.EventFunGames.FindAsync(gameId) ?? throw new InvalidOperationException("Quiz not found.");
        if(game.Kind!="BibleQuiz") throw new InvalidOperationException("Not a quiz.");
        switch(action)
        {
            case "open":
                if(await db.EventFunQuestions.CountAsync(x=>x.EventFunGameId==gameId)!=4)
                    throw new InvalidOperationException("Save all four questions first.");
                game.QuizSubmissionsOpen=true;game.RevealAnswers=false;break;
            case "close":game.QuizSubmissionsOpen=false;break;
            case "reveal":game.QuizSubmissionsOpen=false;game.RevealAnswers=true;break;
            case "hide":game.RevealAnswers=false;break;
            case "leaderboard":game.ShowLeaderboard=!game.ShowLeaderboard;break;
            default:throw new InvalidOperationException("Unknown control.");
        }
        await db.SaveChangesAsync();
    }
    public async Task<EventFunEntry?> MyEntryAsync(int gameId)
    {
        var id=WorkerId;
        return await db.EventFunEntries.AsNoTracking().FirstOrDefaultAsync(x=>x.EventFunGameId==gameId && x.WorkerId==id);
    }
    public async Task<EventFunEntry> SubmitPhraseAsync(int gameId,string phrase)
    {
        var id=WorkerId;
        
        // SQLite permits only one writer at a time. Begin the transaction before reading the sequence;
        // the unique index additionally prevents duplicate numbers across concurrent requests.
        for(int retry=0;retry<5;retry++)
        {
            try
            {
                await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var game=await db.EventFunGames.FirstOrDefaultAsync(x=>x.Id==gameId && x.IsPublished && x.Kind=="FastestFinger")
                    ?? throw new InvalidOperationException("Challenge unavailable.");
                var now=DateTime.UtcNow;
                if(now<game.OpensAtUtc || now>=game.ClosesAtUtc) throw new InvalidOperationException("Challenge is not open.");
                if(!string.Equals(Normalise(phrase),Normalise(game.Phrase),StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The phrase is incorrect. Please try again.");
                var existing=await db.EventFunEntries.FirstOrDefaultAsync(x=>x.EventFunGameId==gameId && x.WorkerId==id);
                if(existing!=null) return existing;
                var max=await db.EventFunEntries.Where(x=>x.EventFunGameId==gameId).MaxAsync(x=>(int?)x.EntryNumber) ?? 0;
                var entry=new EventFunEntry{EventFunGameId=gameId,WorkerId=id,EntryNumber=max+1,SubmittedAtUtc=now};
                db.EventFunEntries.Add(entry);await db.SaveChangesAsync();await tx.CommitAsync();return entry;
            }
            catch(DbUpdateException) when(retry<4) { db.ChangeTracker.Clear();await Task.Delay(50*(retry+1)); }
            catch(Microsoft.Data.Sqlite.SqliteException e) when(e.SqliteErrorCode is 5 or 6 && retry<4)
                {db.ChangeTracker.Clear();await Task.Delay(50*(retry+1));}
        }
        throw new InvalidOperationException("High demand. Please try again.");
    }
    private static string Normalise(string? text)=>string.Join(" ",(text??"").Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries));
    public async Task<EventFunEntry> SubmitQuizAsync(int gameId,string answers)
    {
        var id=WorkerId;
        if(answers.Length!=4 || answers.Any(c=>c is not ('A' or 'B' or 'C' or 'D')))
            throw new InvalidOperationException("Select A, B, C or D for all four questions.");
        
        var game=await db.EventFunGames.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==gameId && x.Kind=="BibleQuiz" && x.IsPublished)
            ?? throw new InvalidOperationException("Quiz unavailable.");
        var now=DateTime.UtcNow;
        if(!game.QuizSubmissionsOpen || now<game.OpensAtUtc || now>=game.ClosesAtUtc)
            throw new InvalidOperationException("Quiz submissions are closed.");
        var existing=await db.EventFunEntries.AsNoTracking().FirstOrDefaultAsync(x=>x.EventFunGameId==gameId && x.WorkerId==id);
        if(existing!=null) return existing;
        var questions=await db.EventFunQuestions.AsNoTracking().Where(x=>x.EventFunGameId==gameId).OrderBy(x=>x.Position).ToListAsync();
        if(questions.Count!=4) throw new InvalidOperationException("Quiz is not configured.");
        var score=Enumerable.Range(0,4).Count(i=>questions[i].CorrectOption==answers[i].ToString())*25;
        var entry=new EventFunEntry{EventFunGameId=gameId,WorkerId=id,Answers=answers,Score=score,SubmittedAtUtc=now};
        db.EventFunEntries.Add(entry);
        try {await db.SaveChangesAsync();return entry;}
        catch(DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.EventFunEntries.AsNoTracking().FirstAsync(x=>x.EventFunGameId==gameId && x.WorkerId==id);
        }
    }
    public async Task<List<EventFunEntry>> ResultsAsync(int gameId)
    {
        Admin();
        var kind=await db.EventFunGames.Where(x=>x.Id==gameId).Select(x=>x.Kind).FirstAsync();
        var query=db.EventFunEntries.AsNoTracking().Where(x=>x.EventFunGameId==gameId);
        return kind=="BibleQuiz"
            ? await query.OrderByDescending(x=>x.Score).ThenBy(x=>x.SubmittedAtUtc).ToListAsync()
            : await query.OrderBy(x=>x.EntryNumber).ToListAsync();
    }
}
