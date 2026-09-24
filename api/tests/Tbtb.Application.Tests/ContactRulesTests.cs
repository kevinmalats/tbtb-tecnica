using Tbtb.Application.Validation;
using Xunit;

namespace Tbtb.Application.Tests;

public sealed class ContactRulesTests
{
    [Theory]
    [InlineData("LLAMADA")]
    [InlineData("WHATSAPP")]
    [InlineData("CORREO")]
    public void CA02_AllowsContractChannels(string value) => Assert.True(ContactRules.IsChannel(value));

    [Theory]
    [InlineData("SMS")]
    [InlineData("")]
    [InlineData(null)]
    public void CA02_RejectsUnknownChannels(string? value) => Assert.False(ContactRules.IsChannel(value));

    [Theory]
    [InlineData("2026-09-24T10:00:00Z")]
    [InlineData("2026-09-24T05:00:00.123-05:00")]
    public void CA02_AcceptsOffsetWithAtMostMilliseconds(string value) => Assert.True(ContactRules.TryInstant(value, out _));

    [Theory]
    [InlineData("2026-09-24T10:00:00")]
    [InlineData("2026-09-24T10:00:00.1234Z")]
    [InlineData("invalid")]
    public void CA02_RejectsInvalidInstantShape(string value) => Assert.False(ContactRules.TryInstant(value, out _));

    [Fact]
    public void CA03_RequiresEightByteRowVersion()
    {
        Assert.True(ContactRules.TryVersion(Convert.ToBase64String(new byte[8]), out _));
        Assert.False(ContactRules.TryVersion(Convert.ToBase64String(new byte[7]), out _));
        Assert.False(ContactRules.TryVersion("not-base64", out _));
    }
}
