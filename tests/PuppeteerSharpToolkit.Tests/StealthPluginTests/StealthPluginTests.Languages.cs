using System.Text.Json;

using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public partial class StealthPluginTests {
    [Theory]
    [InlineData(false, "")] // empty will default to "en-US"
    [InlineData(false, "fr-FR")]
    [InlineData(true, "")] // empty will default to "en-US"
    [InlineData(true, "fr-FR")]
    public async Task Languages_Plugin_Test(bool secondNavigation, string language) {
        var pluginManager = new PluginManager();
        if (language.Length is 0) {
            pluginManager.Register(new LanguagesPlugin());
        } else {
            pluginManager.Register(new LanguagesPlugin("fr-FR"));
        }

        await using var browser = await pluginManager.LaunchAsync();
        var context = await browser.CreateBrowserContextAsync();
        await using var page = await context.NewPageAsync();

        await page.GoToAsync("https://google.com");
        await Test(page, language);

        if (secondNavigation) {
            await page.ReloadAsync();
            await Test(page, language);
        }

        static async Task Test(IPage page, string containedLanguage) {
            var fingerPrint = await page.GetFingerPrint();

            var text = fingerPrint.GetRawText(); // for debug

            var languagesJson = fingerPrint.GetProperty("languages").GetRawText();

            var languages = JsonSerializer.Deserialize<string[]>(languagesJson);

            Assert.NotNull(languages);

            Assert.Contains(containedLanguage, languages);
        }
    }
}
