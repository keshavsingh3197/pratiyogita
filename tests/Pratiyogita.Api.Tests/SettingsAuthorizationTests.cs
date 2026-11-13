using System.Reflection;
using KeshavSingh.Auth;
using KeshavSingh.Core;
using Microsoft.AspNetCore.Authorization;
using Pratiyogita.Api.Controllers;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Xunit;

namespace Pratiyogita.Api.Tests;

public class SettingsAuthorizationTests
{
    [Fact]
    public void SettingsController_MutatingEndpoints_RequireAdminRole()
    {
        var adminMethods = new[]
        {
            nameof(SettingsController.UpdatePayments),
            nameof(SettingsController.UploadQrImage),
            nameof(SettingsController.DeleteQrImage),
            nameof(SettingsController.UpdateBranding),
        };

        foreach (var methodName in adminMethods)
        {
            var method = typeof(SettingsController).GetMethod(methodName);
            Assert.NotNull(method);
            var attr = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
            Assert.Equal(Roles.Admin, attr!.Roles);
        }
    }

    [Fact]
    public void SettingsController_ReadEndpoints_AllowAnonymous()
    {
        var publicMethods = new[]
        {
            nameof(SettingsController.GetPayments),
            nameof(SettingsController.GetQrImage),
            nameof(SettingsController.GetBranding),
        };

        foreach (var methodName in publicMethods)
        {
            var method = typeof(SettingsController).GetMethod(methodName);
            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        }
    }

    [Fact]
    public void PlatformSettings_DefaultBrandingAndEcosystemToggle_ArePopulated()
    {
        var defaults = new PlatformSettings();
        Assert.Equal("Pratiyogita", defaults.SiteName);
        Assert.True(defaults.ShowEcosystemBar);
        Assert.False(string.IsNullOrWhiteSpace(defaults.HeroTitle));
        Assert.False(string.IsNullOrWhiteSpace(defaults.CentralConfigUrl));

        var dto = new BrandingSettingsDto(
            defaults.SiteName,
            defaults.Tagline,
            defaults.HeroBadge,
            defaults.HeroTitle,
            defaults.HeroSubtitle,
            defaults.CentralConfigUrl,
            defaults.ShowEcosystemBar,
            defaults.LastUpdatedAt);
        Assert.Equal("Pratiyogita", dto.SiteName);
        Assert.True(dto.ShowEcosystemBar);
    }
}
