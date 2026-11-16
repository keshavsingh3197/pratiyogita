using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

[BsonIgnoreExtraElements]
public sealed class SiteLocalizationConfig
{
    public const string DocumentId = "localization";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    public bool OverrideCentralLocalization { get; set; } = false;
    public string DefaultLocale { get; set; } = "en";
    public List<string> EnabledLocales { get; set; } = ["en", "hi"];
    public bool AllowVisitorLanguageSwitch { get; set; } = true;
    public bool ShowLanguagePicker { get; set; } = true;
    public bool ForceLocale { get; set; } = false;
    public Dictionary<string, Dictionary<string, string>> LocalOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? LastUpdatedByUserId { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
