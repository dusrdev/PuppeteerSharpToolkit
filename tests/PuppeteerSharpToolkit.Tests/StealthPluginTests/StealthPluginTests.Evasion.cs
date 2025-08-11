using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public partial class StealthPluginTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Evasion_Plugin_HasMimeTypes(bool subsequentNavigation) {
        var pluginManager = new PluginManager();
        pluginManager.Register(new EvasionPlugin());

        await using var browser = await pluginManager.LaunchAsync();
        var context = await browser.CreateBrowserContextAsync();
        await using var page = await context.NewPageAsync();

        await page.GoToAsync("https://google.com");
        await Test(page);

        if (subsequentNavigation) {
            await page.ReloadAsync();
            await Test(page);
        }

        static async Task Test(IPage page) {
            var fingerPrint = await page.GetFingerPrint();

            var text = fingerPrint.GetRawText(); // for debug

            Assert.Equal(5, fingerPrint.GetProperty("plugins").GetArrayLength());
            Assert.Equal(2, fingerPrint.GetProperty("mimeTypes").GetArrayLength());
        }
    }
}
