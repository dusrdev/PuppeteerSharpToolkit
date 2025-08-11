using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public class UserAgentPluginTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UserAgent_Plugin_Test(bool secondNavigation) {
        var pluginManager = new PluginManager();
        pluginManager.Register(new UserAgentPlugin());

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
            var finger = await page.GetFingerPrint();
            Assert.DoesNotContain("HeadlessChrome", finger.GetProperty("userAgent").GetString());
        }
    }
}