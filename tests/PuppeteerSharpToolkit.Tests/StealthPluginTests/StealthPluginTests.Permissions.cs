using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public partial class StealthPluginTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Permissions_Plugin_ShouldBe_DeniedInHttpSite(bool subsequentNavigation) {
        var pluginManager = new PluginManager();
        pluginManager.Register(new PermissionsPlugin());

        await using var browser = await pluginManager.LaunchAsync();
        var context = await browser.CreateBrowserContextAsync();
        await using var page = await context.NewPageAsync();

        await page.GoToAsync("http://info.cern.ch/");
        await Test(page);

        if (subsequentNavigation) {
            await page.ReloadAsync();
            await Test(page);
        }

        static async Task Test(IPage page) {
            var finger = await page.GetFingerPrint();
            var s = finger.ToString(); // for debug

            Assert.Equal("prompt", finger.GetProperty("permissions").GetProperty("state").GetString());
            Assert.Equal("default", finger.GetProperty("permissions").GetProperty("permission").GetString());
        }
    }
}
