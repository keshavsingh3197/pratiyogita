using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

public sealed record QuizOptionDto(string Key, string Text, string? TextHi = null);

public sealed record VerifiedYouTubeChannel(
    string ChannelId,
    string ChannelTitle,
    string? ChannelHandle,
    string? ThumbnailUrl,
    DateTime VerifiedAtUtc);

public sealed record ActiveQuizWinnerDto(
    string WinnerId,
    string WinnerChannelId,
    string WinnerChannelTitle,
    string? WinnerChannelHandle,
    string? WinnerThumbnailUrl,
    string Source,
    string PrizeTitle,
    int TotalEntrants,
    int EligibleEntrants,
    string DrawAlgorithm,
    DateTime SelectedAtUtc);

public sealed record ActiveQuizDto(
    string Id,
    string StreamTitle,
    string StreamChannelHandle,
    string? BroadcastId,
    string Question,
    string? QuestionHi,
    IReadOnlyList<QuizOptionDto> Options,
    string? RevealedCorrectOptionKey,
    string PrizeTitle,
    string Language,
    string Status,
    int TotalEntrants,
    DateTime CreatedAtUtc,
    DateTime? ClosedAtUtc,
    DateTime? DrawnAtUtc,
    ActiveQuizWinnerDto? Winner);

[BsonIgnoreExtraElements]
public sealed class QuizWinnerRecord
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string StreamTitle { get; set; } = string.Empty;
    public string? BroadcastId { get; set; }
    public string StreamChannelHandle { get; set; } = "@keshavsinghworld";
    public DateTime StreamDateUtc { get; set; } = DateTime.UtcNow;
    public string Question { get; set; } = string.Empty;
    public string? QuestionHi { get; set; }
    public string CorrectOptionKey { get; set; } = string.Empty;
    public string CorrectOptionText { get; set; } = string.Empty;
    public string WinnerChannelId { get; set; } = string.Empty;
    public string WinnerChannelTitle { get; set; } = string.Empty;
    public string? WinnerChannelHandle { get; set; }
    public string? WinnerThumbnailUrl { get; set; }
    public string WinnerSource { get; set; } = "youtube_chat";
    public string PrizeTitle { get; set; } = string.Empty;
    public int TotalEntrants { get; set; }
    public int EligibleEntrants { get; set; }
    public string DrawAlgorithm { get; set; } = "CSPRNG (RandomNumberGenerator.GetInt32)";
    public DateTime SelectedAtUtc { get; set; } = DateTime.UtcNow;
    public bool VerifiedByYouTubeReadonly { get; set; } = true;
}

public sealed record ActiveQuizStatusResponse(
    bool YouTubeVerifyConfigured,
    ActiveQuizDto? ActiveQuiz);

public sealed record YouTubeVerifyAuthUrlResponse(
    string AuthorizationUrl,
    string Scope,
    string ZeroLogGuarantee);

public sealed record YouTubeVerifyCompleteRequest(
    string Code,
    string State,
    string RedirectUri);

public sealed record YouTubeVerifyCompleteResponse(
    VerifiedYouTubeChannel Channel,
    string VerificationToken,
    DateTime ExpiresAtUtc,
    string ZeroLogGuarantee);

public sealed record SubmitQuizAnswerRequest(
    string SessionId,
    string VerificationToken,
    string OptionKey);

public sealed record SubmitQuizAnswerResponse(
    string EntryId,
    string SessionId,
    string ChannelId,
    string ChannelTitle,
    string? ChannelHandle,
    string AnswerOption,
    DateTime SubmittedAtUtc);

public sealed record AnnounceQuizWinnerRequest(
    string StreamTitle,
    string? StreamChannelHandle,
    string? BroadcastId,
    string Question,
    string? QuestionHi,
    string CorrectOptionKey,
    string CorrectOptionText,
    string WinnerChannelId,
    string WinnerChannelTitle,
    string? WinnerChannelHandle,
    string? WinnerThumbnailUrl,
    string? PrizeTitle,
    int TotalEntrants = 1,
    int EligibleEntrants = 1);
