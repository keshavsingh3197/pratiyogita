using Microsoft.Extensions.Logging.Abstractions;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;
using Xunit;

namespace Pratiyogita.Api.Tests;

public sealed class CentralAdSenseIntegrationTests
{
    [Fact]
    public void SanitizeAdConfig_PreservesAdsEnabled_WhenCentralPublisherIdExists()
    {
        const string centralPubId = "ca-pub-1234567890123456";
        var dto = new AdMonetizationDto(
            AdsEnabled: true,
            AdSenseClientId: centralPubId,
            HeaderAdSlotId: "1112223334",
            InFeedAdSlotId: "5556667778",
            SidebarAdSlotId: "",
            FooterAdSlotId: "9998887776",
            AdLabel: "Sponsored Ad",
            SponsorBannerEnabled: true,
            SponsorBadge: "Top",
            SponsorTitle: "Study Kit",
            SponsorDescription: "Prep materials",
            SponsorCtaText: "Open",
            SponsorUrl: "https://example.com/kit");

        var stored = PlatformSettingsService.SanitizeAdConfig(dto, centralPubId);

        Assert.True(stored.AdsEnabled);
        // Should not duplicate central publisher ID into local Pratiyogita database.
        Assert.Equal(string.Empty, stored.AdSenseClientId);
        Assert.Equal("5556667778", stored.InFeedAdSlotId);
        Assert.Equal("9998887776", stored.FooterAdSlotId);

        var hydrated = PlatformSettingsService.ToAdDto(stored, centralPubId);
        Assert.True(hydrated.AdsEnabled);
        Assert.Equal(centralPubId, hydrated.AdSenseClientId);
    }

    [Fact]
    public async Task CentralAdminConfigReader_CachesAndReturnsAdSenseClientId()
    {
        var calls = 0;
        var reader = new CentralAdminConfigReader(_ =>
        {
            calls++;
            return Task.FromResult<CentralPratiyogitaConfigSnapshot?>(
                new CentralPratiyogitaConfigSnapshot("  ca-pub-7777888899990000  "));
        }, NullLogger<CentralAdminConfigReader>.Instance);

        var first = await reader.GetSnapshotAsync();
        var second = await reader.GetSnapshotAsync();

        Assert.Equal(1, calls);
        Assert.Equal("  ca-pub-7777888899990000  ", first.AdSenseClientId);
        Assert.Equal(first.AdSenseClientId, second.AdSenseClientId);

        reader.InvalidateCache();
        await reader.GetSnapshotAsync();
        Assert.Equal(2, calls);
    }
}
