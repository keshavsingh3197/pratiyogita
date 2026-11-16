using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

/// <summary>
/// Reads active live-stream quiz sessions from <c>AdminDb.live_quiz_sessions</c>, records
/// zero-log verified viewer entries directly into <c>AdminDb.live_quiz_entries</c>,
/// and serves the permanent Hall of Winners timeline from <c>AdminDb.live_quiz_winners</c>
/// (with local <c>PratiyogitaDb</c> / in-memory fallback when <c>AdminDb</c> is unconfigured).
/// </summary>
public sealed class QuizWinnersService
{
    private const string SessionsCollection = "live_quiz_sessions";
    private const string EntriesCollection = "live_quiz_entries";
    private const string WinnersCollection = "live_quiz_winners";

    private readonly IMongoCollection<BsonDocument>? _centralSessions;
    private readonly IMongoCollection<BsonDocument>? _centralEntries;
    private readonly IMongoCollection<BsonDocument>? _centralWinners;
    private readonly IMongoCollection<QuizWinnerRecord>? _localWinners;
    private readonly ILogger<QuizWinnersService> _logger;
    private readonly TimeProvider _clock;

    // In-memory store enables clean zero-Mongo testing & single-instance dev rehearsal.
    private readonly ConcurrentDictionary<string, BsonDocument> _memSessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, BsonDocument> _memEntries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, QuizWinnerRecord> _memWinners = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _indexGate = new(1, 1);
    private volatile bool _indexesEnsured;

    public QuizWinnersService(
        MongoDbService db,
        IConfiguration configuration,
        ILogger<QuizWinnersService> logger,
        TimeProvider? clock = null)
    {
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
        _localWinners = db.GetCollection<QuizWinnerRecord>(WinnersCollection);

        var enabled = !string.Equals(configuration["CentralMongo:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
        if (!enabled) return;

        var connString = FirstNonEmpty(
            configuration["CentralMongo:ConnectionString"],
            Environment.GetEnvironmentVariable("CentralMongo__ConnectionString"),
            configuration["Mongo:ConnectionString"],
            Environment.GetEnvironmentVariable("Mongo__ConnectionString"));
        var dbName = FirstNonEmpty(
            configuration["CentralMongo:DatabaseName"],
            configuration["CentralMongo:Database"],
            Environment.GetEnvironmentVariable("CentralMongo__DatabaseName")) ?? "AdminDb";

        if (string.IsNullOrWhiteSpace(connString)) return;

        try
        {
            var settings = MongoClientSettings.FromConnectionString(connString);
            settings.ConnectTimeout = TimeSpan.FromSeconds(4);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(4);
            settings.SocketTimeout = TimeSpan.FromSeconds(5);
            var centralDb = new MongoClient(settings).GetDatabase(dbName);
            _centralSessions = centralDb.GetCollection<BsonDocument>(SessionsCollection);
            _centralEntries = centralDb.GetCollection<BsonDocument>(EntriesCollection);
            _centralWinners = centralDb.GetCollection<BsonDocument>(WinnersCollection);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QuizWinnersService could not initialize AdminDb collections; falling back to local store.");
        }
    }

    /// <summary>Constructor for pure unit tests with no Mongo dependency.</summary>
    public QuizWinnersService(ILogger<QuizWinnersService> logger, TimeProvider? clock = null)
    {
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        if (_localWinners is not null)
        {
            try
            {
                await _localWinners.Indexes.CreateOneAsync(
                    new CreateIndexModel<QuizWinnerRecord>(
                        Builders<QuizWinnerRecord>.IndexKeys.Descending(x => x.SelectedAtUtc),
                        new CreateIndexOptions { Name = "ix_local_quiz_winners_selected" }),
                    cancellationToken: ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not create local quiz_winners index.");
            }
        }

        if (_indexesEnsured || _centralEntries is null || _centralWinners is null) return;
        await _indexGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_indexesEnsured) return;
            await _centralEntries.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("SessionId").Ascending("ParticipantKey"),
                    new CreateIndexOptions { Name = "ux_live_quiz_entries_session_participant", Unique = true }),
                cancellationToken: ct).ConfigureAwait(false);
            await _centralWinners.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Descending("SelectedAtUtc"),
                    new CreateIndexOptions { Name = "ix_live_quiz_winners_selected" }),
                cancellationToken: ct).ConfigureAwait(false);
            _indexesEnsured = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Non-fatal: failed ensuring central live quiz indexes.");
        }
        finally
        {
            _indexGate.Release();
        }
    }

    public void SeedInMemorySession(BsonDocument sessionDoc)
    {
        var id = Str(sessionDoc, "_id") ?? Guid.NewGuid().ToString("n");
        _memSessions[id] = sessionDoc;
    }

    public async Task<ActiveQuizDto?> GetActiveQuizAsync(CancellationToken ct = default)
    {
        BsonDocument? doc = null;
        if (_centralSessions is not null)
        {
            try
            {
                var recent = await _centralSessions
                    .Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAtUtc"))
                    .Limit(15)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                doc = recent.FirstOrDefault(s => string.Equals(Str(s, "Status"), "Open", StringComparison.OrdinalIgnoreCase))
                    ?? recent.FirstOrDefault(s =>
                        (s.TryGetValue("CreatedAtUtc", out var dt) && dt.IsValidDateTime ? dt.ToUniversalTime() : DateTime.MinValue)
                        >= _clock.GetUtcNow().UtcDateTime.AddHours(-12));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed reading AdminDb.live_quiz_sessions.");
            }
        }

        doc ??= _memSessions.Values
            .OrderByDescending(s => s.TryGetValue("CreatedAtUtc", out var dt) && dt.IsValidDateTime ? dt.ToUniversalTime() : DateTime.MinValue)
            .FirstOrDefault();

        if (doc is null) return null;

        var sessionId = Str(doc, "_id") ?? string.Empty;
        var count = await CountEntriesAsync(sessionId, ct).ConfigureAwait(false);
        return MapActiveQuiz(doc, count);
    }

    public async Task<SubmitQuizAnswerResponse> SubmitVerifiedAnswerAsync(
        string sessionId,
        VerifiedYouTubeChannel channel,
        string optionKey,
        CancellationToken ct = default)
    {
        var normalizedOption = NormalizeOptionKey(optionKey)
            ?? throw new ZeroLogVerifyException("option-invalid", "Choose option A, B, C, or D.");

        var session = await LoadSessionDocAsync(sessionId.Trim(), ct).ConfigureAwait(false)
            ?? throw new ZeroLogVerifyException("quiz-not-found", "Live quiz round not found.", 404);

        var status = Str(session, "Status") ?? "Open";
        if (!string.Equals(status, "Open", StringComparison.OrdinalIgnoreCase))
            throw new ZeroLogVerifyException("quiz-closed", "This live quiz round is already closed.", 409);

        var options = ParseOptions(session);
        if (!options.Any(o => string.Equals(o.Key, normalizedOption, StringComparison.OrdinalIgnoreCase)))
            throw new ZeroLogVerifyException("option-invalid", "Selected option does not exist on this quiz round.");

        var correctOption = Str(session, "CorrectOptionKey") ?? string.Empty;
        var participantKey = "yt:" + channel.ChannelId.Trim();
        var docId = $"{sessionId.Trim()}:{participantKey}";
        var now = _clock.GetUtcNow().UtcDateTime;

        var existing = await FindEntryByIdAsync(docId, ct).ConfigureAwait(false);
        if (existing is not null)
            throw new ZeroLogVerifyException("already-answered", "Your verified YouTube channel has already submitted an answer for this round.", 409);

        var entryId = "ent_" + Guid.NewGuid().ToString("n")[..12];
        var entryDoc = new BsonDocument
        {
            ["_id"] = docId,
            ["SessionId"] = sessionId.Trim(),
            ["ParticipantKey"] = participantKey,
            ["Source"] = "pratiyogita_web",
            ["YouTubeChannelId"] = channel.ChannelId.Trim(),
            ["ChannelTitle"] = channel.ChannelTitle,
            ["ChannelHandle"] = channel.ChannelHandle ?? string.Empty,
            ["ThumbnailUrl"] = channel.ThumbnailUrl ?? string.Empty,
            ["AnswerOption"] = normalizedOption,
            ["RawMessage"] = "Pratiyogita Verified Web Entry",
            ["IsCorrect"] = string.Equals(normalizedOption, correctOption, StringComparison.OrdinalIgnoreCase),
            ["SubmittedAtUtc"] = now,
        };

        _memEntries[docId] = entryDoc;
        if (_centralEntries is not null)
        {
            await EnsureIndexesAsync(ct).ConfigureAwait(false);
            await _centralEntries.InsertOneAsync(entryDoc, cancellationToken: ct).ConfigureAwait(false);
        }

        return new SubmitQuizAnswerResponse(
            entryId,
            sessionId.Trim(),
            channel.ChannelId.Trim(),
            channel.ChannelTitle,
            channel.ChannelHandle,
            normalizedOption,
            now);
    }

    public async Task<IReadOnlyList<QuizWinnerRecord>> ListWinnersAsync(
        string? channelFilter = null,
        string? query = null,
        int limit = 50,
        CancellationToken ct = default)
    {
        var clamped = Math.Clamp(limit, 1, 100);
        var items = new Dictionary<string, QuizWinnerRecord>(StringComparer.Ordinal);

        if (_centralWinners is not null)
        {
            try
            {
                await EnsureIndexesAsync(ct).ConfigureAwait(false);
                var centralDocs = await _centralWinners
                    .Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(Builders<BsonDocument>.Sort.Descending("SelectedAtUtc"))
                    .Limit(clamped)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var d in centralDocs)
                {
                    var mapped = MapWinner(d);
                    items[mapped.Id] = mapped;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed reading central live_quiz_winners; merging local store.");
            }
        }

        if (_localWinners is not null)
        {
            try
            {
                var locals = await _localWinners
                    .Find(_ => true)
                    .SortByDescending(x => x.SelectedAtUtc)
                    .Limit(clamped)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                foreach (var w in locals)
                    items.TryAdd(w.Id, w);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed reading local quiz_winners.");
            }
        }

        foreach (var w in _memWinners.Values)
            items.TryAdd(w.Id, w);

        IEnumerable<QuizWinnerRecord> seq = items.Values;
        if (!string.IsNullOrWhiteSpace(channelFilter))
        {
            var cf = channelFilter.Trim().TrimStart('@');
            seq = seq.Where(w =>
                (!string.IsNullOrWhiteSpace(w.StreamChannelHandle) && w.StreamChannelHandle.Contains(cf, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(w.WinnerChannelHandle) && w.WinnerChannelHandle.Contains(cf, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            seq = seq.Where(w =>
                w.WinnerChannelTitle.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(w.WinnerChannelHandle) && w.WinnerChannelHandle.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                w.StreamTitle.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                w.Question.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(w.QuestionHi) && w.QuestionHi.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        return seq.OrderByDescending(w => w.SelectedAtUtc).Take(clamped).ToList();
    }

    public async Task<QuizWinnerRecord> RecordWinnerManualAsync(
        AnnounceQuizWinnerRequest req,
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var winner = new QuizWinnerRecord
        {
            Id = "win_" + Guid.NewGuid().ToString("n")[..12],
            SessionId = "manual_" + Guid.NewGuid().ToString("n")[..8],
            StreamTitle = req.StreamTitle.Trim(),
            StreamChannelHandle = NormalizeHandle(req.StreamChannelHandle) ?? "@keshavsinghworld",
            BroadcastId = string.IsNullOrWhiteSpace(req.BroadcastId) ? null : req.BroadcastId.Trim(),
            StreamDateUtc = now,
            Question = req.Question.Trim(),
            QuestionHi = string.IsNullOrWhiteSpace(req.QuestionHi) ? null : req.QuestionHi.Trim(),
            CorrectOptionKey = NormalizeOptionKey(req.CorrectOptionKey) ?? req.CorrectOptionKey.Trim(),
            CorrectOptionText = req.CorrectOptionText.Trim(),
            WinnerChannelId = req.WinnerChannelId.Trim(),
            WinnerChannelTitle = req.WinnerChannelTitle.Trim(),
            WinnerChannelHandle = NormalizeHandle(req.WinnerChannelHandle),
            WinnerThumbnailUrl = string.IsNullOrWhiteSpace(req.WinnerThumbnailUrl) ? null : req.WinnerThumbnailUrl.Trim(),
            WinnerSource = "admin_manual",
            PrizeTitle = string.IsNullOrWhiteSpace(req.PrizeTitle) ? "Pratiyogita Verified Winner Badge" : req.PrizeTitle.Trim(),
            TotalEntrants = Math.Max(1, req.TotalEntrants),
            EligibleEntrants = Math.Max(1, req.EligibleEntrants),
            DrawAlgorithm = "CSPRNG (RandomNumberGenerator.GetInt32)",
            SelectedAtUtc = now,
            VerifiedByYouTubeReadonly = true,
        };

        _memWinners[winner.Id] = winner;

        if (_centralWinners is not null)
        {
            try
            {
                var doc = ToWinnerBson(winner);
                await _centralWinners.ReplaceOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", winner.Id),
                    doc,
                    new ReplaceOptions { IsUpsert = true },
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Non-fatal: could not write manual winner to AdminDb.live_quiz_winners.");
            }
        }

        if (_localWinners is not null)
        {
            await _localWinners.ReplaceOneAsync(
                x => x.Id == winner.Id,
                winner,
                new ReplaceOptions { IsUpsert = true },
                ct).ConfigureAwait(false);
        }

        return winner;
    }

    private async Task<BsonDocument?> LoadSessionDocAsync(string sessionId, CancellationToken ct)
    {
        if (_centralSessions is not null)
        {
            var doc = await _centralSessions
                .Find(Builders<BsonDocument>.Filter.Eq("_id", sessionId))
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (doc is not null) return doc;
        }
        return _memSessions.TryGetValue(sessionId, out var mem) ? mem : null;
    }

    private async Task<BsonDocument?> FindEntryByIdAsync(string docId, CancellationToken ct)
    {
        if (_centralEntries is not null)
        {
            var doc = await _centralEntries
                .Find(Builders<BsonDocument>.Filter.Eq("_id", docId))
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (doc is not null) return doc;
        }
        return _memEntries.TryGetValue(docId, out var mem) ? mem : null;
    }

    private async Task<int> CountEntriesAsync(string sessionId, CancellationToken ct)
    {
        if (_centralEntries is not null)
        {
            try
            {
                return (int)await _centralEntries
                    .CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("SessionId", sessionId), cancellationToken: ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed counting live_quiz_entries.");
            }
        }

        return _memEntries.Values.Count(e => string.Equals(Str(e, "SessionId"), sessionId, StringComparison.Ordinal));
    }

    private static ActiveQuizDto MapActiveQuiz(BsonDocument doc, int totalEntrants)
    {
        var status = Str(doc, "Status") ?? "Open";
        var options = ParseOptions(doc);
        ActiveQuizWinnerDto? winner = null;
        if (doc.TryGetValue("Winner", out var wVal) && wVal.IsBsonDocument)
        {
            var w = wVal.AsBsonDocument;
            winner = new ActiveQuizWinnerDto(
                Str(w, "WinnerId") ?? string.Empty,
                Str(w, "WinnerChannelId") ?? string.Empty,
                Str(w, "WinnerChannelTitle") ?? string.Empty,
                Str(w, "WinnerChannelHandle"),
                Str(w, "WinnerThumbnailUrl"),
                Str(w, "Source") ?? "youtube_chat",
                Str(w, "PrizeTitle") ?? string.Empty,
                w.TryGetValue("TotalEntrants", out var te) && te.IsInt32 ? te.AsInt32 : 0,
                w.TryGetValue("EligibleEntrants", out var ee) && ee.IsInt32 ? ee.AsInt32 : 0,
                Str(w, "DrawAlgorithm") ?? "CSPRNG",
                w.TryGetValue("SelectedAtUtc", out var sa) && sa.IsValidDateTime ? sa.ToUniversalTime() : DateTime.UtcNow);
        }

        return new ActiveQuizDto(
            Str(doc, "_id") ?? string.Empty,
            Str(doc, "StreamTitle") ?? "Keshav Network Live Quiz",
            Str(doc, "StreamChannelHandle") ?? "@keshavsinghworld",
            Str(doc, "BroadcastId"),
            Str(doc, "Question") ?? string.Empty,
            Str(doc, "QuestionHi"),
            options,
            RevealedCorrectOptionKey: string.Equals(status, "Open", StringComparison.OrdinalIgnoreCase)
                ? null
                : Str(doc, "CorrectOptionKey"),
            Str(doc, "PrizeTitle") ?? string.Empty,
            Str(doc, "Language") ?? "hi",
            status,
            totalEntrants,
            doc.TryGetValue("CreatedAtUtc", out var c) && c.IsValidDateTime ? c.ToUniversalTime() : DateTime.UtcNow,
            doc.TryGetValue("ClosedAtUtc", out var cl) && cl.IsValidDateTime ? cl.ToUniversalTime() : null,
            doc.TryGetValue("DrawnAtUtc", out var dr) && dr.IsValidDateTime ? dr.ToUniversalTime() : null,
            winner);
    }

    private static List<QuizOptionDto> ParseOptions(BsonDocument doc)
    {
        var list = new List<QuizOptionDto>();
        if (doc.TryGetValue("Options", out var val) && val.IsBsonArray)
        {
            foreach (var o in val.AsBsonArray.OfType<BsonDocument>())
            {
                list.Add(new QuizOptionDto(
                    Str(o, "Key") ?? "A",
                    Str(o, "Text") ?? string.Empty,
                    Str(o, "TextHi")));
            }
        }
        return list;
    }

    private static QuizWinnerRecord MapWinner(BsonDocument d) => new()
    {
        Id = Str(d, "_id") ?? string.Empty,
        SessionId = Str(d, "SessionId") ?? string.Empty,
        StreamTitle = Str(d, "StreamTitle") ?? string.Empty,
        BroadcastId = Str(d, "BroadcastId"),
        StreamChannelHandle = Str(d, "StreamChannelHandle") ?? "@keshavsinghworld",
        StreamDateUtc = d.TryGetValue("StreamDateUtc", out var sd) && sd.IsValidDateTime ? sd.ToUniversalTime() : DateTime.UtcNow,
        Question = Str(d, "Question") ?? string.Empty,
        QuestionHi = Str(d, "QuestionHi"),
        CorrectOptionKey = Str(d, "CorrectOptionKey") ?? string.Empty,
        CorrectOptionText = Str(d, "CorrectOptionText") ?? string.Empty,
        WinnerChannelId = Str(d, "WinnerChannelId") ?? string.Empty,
        WinnerChannelTitle = Str(d, "WinnerChannelTitle") ?? string.Empty,
        WinnerChannelHandle = Str(d, "WinnerChannelHandle"),
        WinnerThumbnailUrl = Str(d, "WinnerThumbnailUrl"),
        WinnerSource = Str(d, "WinnerSource") ?? "youtube_chat",
        PrizeTitle = Str(d, "PrizeTitle") ?? string.Empty,
        TotalEntrants = d.TryGetValue("TotalEntrants", out var te) && te.IsInt32 ? te.AsInt32 : 0,
        EligibleEntrants = d.TryGetValue("EligibleEntrants", out var ee) && ee.IsInt32 ? ee.AsInt32 : 0,
        DrawAlgorithm = Str(d, "DrawAlgorithm") ?? "CSPRNG (RandomNumberGenerator.GetInt32)",
        SelectedAtUtc = d.TryGetValue("SelectedAtUtc", out var sa) && sa.IsValidDateTime ? sa.ToUniversalTime() : DateTime.UtcNow,
        VerifiedByYouTubeReadonly = !d.TryGetValue("VerifiedByYouTubeReadonly", out var v) || !v.IsBoolean || v.AsBoolean,
    };

    private static BsonDocument ToWinnerBson(QuizWinnerRecord w) => new()
    {
        ["_id"] = w.Id,
        ["SessionId"] = w.SessionId,
        ["StreamTitle"] = w.StreamTitle,
        ["BroadcastId"] = w.BroadcastId is null ? BsonNull.Value : w.BroadcastId,
        ["StreamChannelHandle"] = w.StreamChannelHandle,
        ["StreamDateUtc"] = w.StreamDateUtc,
        ["Question"] = w.Question,
        ["QuestionHi"] = w.QuestionHi is null ? BsonNull.Value : w.QuestionHi,
        ["CorrectOptionKey"] = w.CorrectOptionKey,
        ["CorrectOptionText"] = w.CorrectOptionText,
        ["WinnerChannelId"] = w.WinnerChannelId,
        ["WinnerChannelTitle"] = w.WinnerChannelTitle,
        ["WinnerChannelHandle"] = w.WinnerChannelHandle is null ? BsonNull.Value : w.WinnerChannelHandle,
        ["WinnerThumbnailUrl"] = w.WinnerThumbnailUrl is null ? BsonNull.Value : w.WinnerThumbnailUrl,
        ["WinnerSource"] = w.WinnerSource,
        ["PrizeTitle"] = w.PrizeTitle,
        ["TotalEntrants"] = w.TotalEntrants,
        ["EligibleEntrants"] = w.EligibleEntrants,
        ["DrawAlgorithm"] = w.DrawAlgorithm,
        ["SelectedAtUtc"] = w.SelectedAtUtc,
        ["VerifiedByYouTubeReadonly"] = w.VerifiedByYouTubeReadonly,
    };

    private static string? NormalizeOptionKey(string? raw)
    {
        var s = raw?.Trim().ToUpperInvariant();
        return s is "A" or "B" or "C" or "D" ? s : null;
    }

    private static string? NormalizeHandle(string? handle)
    {
        var h = handle?.Trim();
        if (string.IsNullOrWhiteSpace(h)) return null;
        return h.StartsWith('@') ? h : "@" + h;
    }

    private static string? Str(BsonDocument doc, string name) =>
        doc.TryGetValue(name, out var v) && v.IsString && !string.IsNullOrWhiteSpace(v.AsString) ? v.AsString : null;

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();
}
