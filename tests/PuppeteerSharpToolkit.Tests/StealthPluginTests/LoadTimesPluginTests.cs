using System.Text.Json;

using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public class LoadTimesPluginTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadTimes_Plugin_Test(bool secondNavigation) {
        var pluginManager = new PluginManager();
        pluginManager.Register(new LoadTimesPlugin());

        await using var browser = await pluginManager.LaunchAsync();
        var context = await browser.CreateBrowserContextAsync();
        await using var page = await context.NewPageAsync();

        await page.GoToAsync("https://google.com");
        await Test(page);

        if (secondNavigation) {
            await page.ReloadAsync();
            await Test(page);
        }

        static async Task Test(IPage page) {
            var loadTimes = await page.EvaluateFunctionAsync<JsonElement>("() => window.chrome.loadTimes()");

            Assert.True(loadTimes.TryGetProperty("connectionInfo", out var prop));
            Assert.Equal(JsonValueKind.String, prop.ValueKind);

            Assert.True(loadTimes.TryGetProperty("npnNegotiatedProtocol", out prop));
            Assert.Equal(JsonValueKind.String, prop.ValueKind);

            Assert.True(loadTimes.TryGetProperty("wasAlternateProtocolAvailable", out prop));
            Assert.True(prop.ValueKind is JsonValueKind.True or JsonValueKind.False);

            Assert.True(loadTimes.TryGetProperty("wasFetchedViaSpdy", out prop));
            Assert.True(prop.ValueKind is JsonValueKind.True or JsonValueKind.False);

            Assert.True(loadTimes.TryGetProperty("wasNpnNegotiated", out prop));
            Assert.True(prop.ValueKind is JsonValueKind.True or JsonValueKind.False);

            Assert.True(loadTimes.TryGetProperty("firstPaintAfterLoadTime", out prop));
            Assert.Equal(JsonValueKind.Number, prop.ValueKind);

            Assert.True(loadTimes.TryGetProperty("requestTime", out prop));
            Assert.Equal(JsonValueKind.Number, prop.ValueKind);

            Assert.True(loadTimes.TryGetProperty("startLoadTime", out prop));
            Assert.Equal(JsonValueKind.Number, prop.ValueKind);

            Assert.True(loadTimes.TryGetProperty("commitLoadTime", out prop));
            Assert.Equal(JsonValueKind.Number, prop.ValueKind);

            Assert.True(loadTimes.TryGetProperty("finishDocumentLoadTime", out prop));
            Assert.Equal(JsonValueKind.Number, prop.ValueKind);

            Assert.True(loadTimes.TryGetProperty("firstPaintTime", out prop));
            Assert.Equal(JsonValueKind.Number, prop.ValueKind);
        }
    }
}
