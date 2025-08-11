using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public partial class StealthPluginTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Vendor_Plugin_Test(bool subsequentNavigation) {
        var pluginManager = new PluginManager();
        pluginManager.Register(new VendorPlugin());

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
            var vendor = await page.EvaluateExpressionAsync<string>("navigator.vendor");
            Assert.Equal("Google Inc.", vendor);
        }
    }
}
