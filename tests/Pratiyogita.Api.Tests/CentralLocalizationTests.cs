using System.Reflection;
using KeshavSingh.Core;
using Microsoft.AspNetCore.Authorization;
using Pratiyogita.Api.Controllers;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;
using Xunit;

namespace Pratiyogita.Api.Tests;

public sealed class CentralLocalizationTests
{
    [Fact]
    public void ResolvePolicy_UsesAdminPolicy_WhenSiteOverrideDisabled()
    {
        var central = new EffectiveLocalizationPolicyDto(
            SiteKey: "pratiyogita",
            DefaultLocale: "hi",
            EnabledLocales: ["hi", "en"],
            AllowVisitorLanguageSwitch: true,
            ShowLanguagePicker: true,
            ForceLocale: false,
            ControlledBy: "admin",
            OverrideCentralLocalization: false);

        var local = new SiteLocalizationConfig
        {
            OverrideCentralLocalization = false,
            DefaultLocale = "en",
            AllowVisitorLanguageSwitch = false,
        };

        var resolved = CentralLocalizationService.ResolvePolicy(central, local);

        Assert.Equal("hi", resolved.DefaultLocale);
        Assert.Equal("admin", resolved.ControlledBy);
        Assert.True(resolved.AllowVisitorLanguageSwitch);
        Assert.False(resolved.OverrideCentralLocalization);
    }

    [Fact]
    public void ResolvePolicy_AppliesLocalSiteOverride_WhenEnabled()
    {
        var central = new EffectiveLocalizationPolicyDto(
            SiteKey: "pratiyogita",
            DefaultLocale: "en",
            EnabledLocales: ["en", "hi"],
            AllowVisitorLanguageSwitch: true,
            ShowLanguagePicker: true,
            ForceLocale: false,
            ControlledBy: "admin",
            OverrideCentralLocalization: false);

        var local = new SiteLocalizationConfig
        {
            OverrideCentralLocalization = true,
            DefaultLocale = "hi",
            EnabledLocales = ["en"],
            AllowVisitorLanguageSwitch = false,
            ShowLanguagePicker = false,
            ForceLocale = true,
        };

        var resolved = CentralLocalizationService.ResolvePolicy(central, local);

        Assert.Equal("hi", resolved.DefaultLocale);
        Assert.Contains("hi", resolved.EnabledLocales);
        Assert.Equal("site-override", resolved.ControlledBy);
        Assert.False(resolved.AllowVisitorLanguageSwitch);
        Assert.False(resolved.ShowLanguagePicker);
        Assert.True(resolved.ForceLocale);
    }

    [Fact]
    public void I18nController_UpdateEndpointRequiresAdminRole()
    {
        var update = typeof(I18nController).GetMethod(nameof(I18nController.UpdateSiteLocalization));
        Assert.NotNull(update);
        var auth = update!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(auth);
        Assert.Equal(Roles.Admin, auth!.Roles);
    }
}
