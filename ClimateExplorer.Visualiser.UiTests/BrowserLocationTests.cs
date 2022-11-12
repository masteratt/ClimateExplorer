using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

namespace ClimateExplorer.Visualiser.UiTests
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class BrowserLocationTests
    {
        IPlaywright _playwright;
        IBrowser _browser;
        IPage _page;

        string _baseApiUrl = "http://localhost:54836";
        string _baseSiteUrl = "http://localhost:5298";

        [SetUp]
        public async Task Setup()
        {
            _playwright = await Playwright.CreateAsync();
            var chromium = _playwright.Chromium;
            _browser = await chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, });
            var context = await _browser.NewContextAsync( new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1440, Height = 845} });
            _page = await context.NewPageAsync();           
        }

        [TearDown]
        public async Task TearDown()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        [Test]
        public async Task GpToAllLocationsAndGetAScreenshot()
        {
            var request = await _playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = _baseApiUrl,
            });

            var response = await request.GetAsync("location");


            Assert.True(response.Ok);
            var jsonResponse = await response.JsonAsync();

            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
            };

            var locations = new List<Location>();
            foreach (var locationObj in jsonResponse?.EnumerateArray())
            {
                var location = locationObj.Deserialize<Location>(options);
                locations.Add(location);
            }

            var di = new DirectoryInfo("Assets\\Locations");
            if (!di.Exists)
            {
                di.Create();
            }

            var locationsTemplate = System.IO.File.ReadAllText("locations-template.markdown");

            locationsTemplate = locationsTemplate.Replace("NumberOfLocations", locations.Count.ToString());

            var markdown = new List<string>();

            var firstLocation = true;

            foreach (var location in locations)
            {
                await _page.GotoAsync($"{_baseSiteUrl}/location/{location.Id}");
                Thread.Sleep(firstLocation ? 5000 : 3000);
                await _page.ScreenshotAsync(new()
                {
                    Path = $"Assets\\Locations\\{location.Name}.png",
                });

                markdown.Add($"| [{location.Name}]({_baseSiteUrl}/location/{location.Id}) | ![{location.Name}]({{{{site.url}}}}/blog/assets/locations/{location.Name}.png){{: width=\"400\" }} | \r\n");
                firstLocation = false;
            }

            markdown.ForEach(x => locationsTemplate += x);

            System.IO.File.WriteAllText("locations.markdown", locationsTemplate);
        }

    }
}