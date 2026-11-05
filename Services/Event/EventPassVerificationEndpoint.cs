using System;
using System.Net;
using System.Text;
using ChurchApp.Data;
using ChurchApp.Models.Event;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services.Event;

public static class EventPassVerificationEndpoint
{
    // Public, read-only page. Do not apply login middleware or a global login redirect here.
    public static void MapEventPassVerification(this WebApplication app)
    {
        app.MapGet("/event/verify/{token}", async (string token, AppDbContext db) =>
        {
            if (token.Length != 64 || !System.Text.RegularExpressions.Regex.IsMatch(token, "^[0-9A-Fa-f]{64}$"))
                return Results.Content(RenderInvalid(), "text/html; charset=utf-8", statusCode: 404);

            var pass = await db.EventCheckIns.AsNoTracking()
                .Include(c => c.ChurchEvent).Include(c => c.EventTable).Include(c => c.EventMenuOption)
                .FirstOrDefaultAsync(c => c.VerificationToken == token.ToUpperInvariant());
            if (pass == null || pass.ChurchEvent.IsCancelled)
                return Results.Content(RenderInvalid(), "text/html; charset=utf-8", statusCode: 404);
            var worker = await db.Workers.AsNoTracking()
                .Where(w => w.WorkerId == pass.WorkerId)
                .Select(w => new { w.FirstName, w.LastName }).FirstOrDefaultAsync();
            var name = worker == null ? "Registered worker" : $"{worker.FirstName} {worker.LastName}";
            return Results.Content(RenderValid(pass, name), "text/html; charset=utf-8");
        }).AllowAnonymous();
    }

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
    private static string Shell(string content) => """
        <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
        <meta name="robots" content="noindex,nofollow,noarchive"><title>BCC ServiceHub | Verify Check-In</title>
        <style>body{margin:0;background:#f1f6f2;color:#183b2b;font:16px system-ui,Segoe UI,sans-serif;display:grid;min-height:100vh;place-items:center;padding:18px;box-sizing:border-box}.card{width:min(440px,100%);background:white;border-radius:24px;overflow:hidden;box-shadow:0 16px 45px #183b2b20}.head{background:linear-gradient(120deg,#0e5636,#218455);color:white;text-align:center;padding:28px 22px}.head h1{font-size:23px;margin:10px 0 5px}.head p{margin:0;opacity:.9}.body{padding:26px;text-align:center}.name{font-size:25px;font-weight:750;margin:15px 0 7px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin:22px 0}.tile{background:#f0f6f2;border-radius:14px;padding:17px 9px}.tile small{display:block;color:#63786c;letter-spacing:1px}.tile strong{font-size:23px}.meal{border:1px solid #dce8df;border-radius:14px;padding:15px}.note{font-size:14px;color:#506c5a;margin-top:20px}.check{font-size:43px}.bad{background:#7e2431}</style></head><body><main class="card">
        """ + content + "</main></body></html>";

    private static string RenderInvalid() => Shell("<div class='head bad'><div class='check'>✕</div><h1>Invalid Check-In Pass</h1><p>This code could not be verified.</p></div><div class='body'>Please ask the attendee to open their pass from BCC ServiceHub.</div>");

    private static string RenderValid(EventCheckIn p, string name) => Shell($"""
        <div class="head"><div class="check">✓</div><h1>VERIFIED CHECK-IN</h1><p>{E(p.ChurchEvent.Name)}</p></div>
        <div class="body"><div class="name">{E(name)}</div><div>Genuine event check-in record</div>
        <div class="grid"><div class="tile"><small>TABLE</small><strong>{E(p.EventTable.TableName)}</strong></div><div class="tile"><small>SEAT</small><strong>{p.SeatNumber}</strong></div></div>
        <div class="meal"><small>SELECTED MENU</small><div><strong>{E(p.EventMenuOption.Name)}</strong></div></div>
        <div class="note">Kindly direct this attendee to their assigned seat.</div></div>
        """);
}
