using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public partial class StealthPluginTests {
    [Theory]
    [InlineData(false, "navigator.webdriver")]
    [InlineData(false, "navigator.javaEnabled()")]
    [InlineData(true, "navigator.webdriver")]
    [InlineData(true, "navigator.javaEnabled()")]
    public async Task WebDriver_Plugin_Test(bool secondNavigation, string expression) {
        var pluginManager = new PluginManager();
        pluginManager.Register(new WebDriverPlugin());

        await using var browser = await pluginManager.LaunchAsync();
        var context = await browser.CreateBrowserContextAsync();
        await using var page = await context.NewPageAsync();

        await page.GoToAsync("https://google.com");
        await Test(page, expression);

        if (secondNavigation) {
            await page.ReloadAsync();
            await Test(page, expression);
        }

        static async Task Test(IPage page, string expression) {
            var data = await page.EvaluateExpressionAsync<bool>(expression);
            Assert.False(data);
        }
    }
}
