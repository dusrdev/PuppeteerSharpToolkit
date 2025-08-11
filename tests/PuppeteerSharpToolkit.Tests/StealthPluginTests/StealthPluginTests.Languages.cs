using System.Text.Json;

using PuppeteerSharpToolkit.Plugins;

namespace PuppeteerSharpToolkit.Tests.StealthPluginTests;

public partial class StealthPluginTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Languages_Plugin_Test(bool subsequentNavigation, bool isFrench) {
        var pluginManager = new PluginManager();
        if (!isFrench) {
            pluginManager.Register(new LanguagesPlugin());
        } else {
            pluginManager.Register(new LanguagesPlugin("fr-FR"));
        }

        string language = isFrench switch {
            true => "fr-FR",
            _ => "en-US"
        };

        await using var browser = await pluginManager.LaunchAsync();
        var context = await browser.CreateBrowserContextAsync();
        await using var page = await context.NewPageAsync();

        await page.GoToAsync("https://google.com");
        await Test(page, language);

        if (subsequentNavigation) {
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
