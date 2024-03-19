using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SchrodingerServer.Adopts.dispatcher;
using SchrodingerServer.Dtos.TraitsDto;
using SchrodingerServer.Options;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace SchrodingerServer.ImageGen;

public class ImageProviderTest : SchrodingerServerDomainTestBase
{
    private AutoMaticImageProvider _autoMaticImageProvider { get; set; }

    public ImageProviderTest(ITestOutputHelper output) : base(output)
    {
        var monitor = Mock.Of<IOptionsMonitor<TraitsOptions>>(x => x.CurrentValue == new TraitsOptions()
        {
            AutoMaticImageGenerateUrl = "http://fs.iis.pub:13888/sdapi/v1/txt2img"
        });
        var stableDiffusionOption = Mock.Of<IOptionsMonitor<StableDiffusionOption>>(x => x.CurrentValue == new StableDiffusionOption());
        _autoMaticImageProvider = new AutoMaticImageProvider(NullLogger<ImageProvider>.Instance, null, null, monitor, stableDiffusionOption);
    }

    [Fact]
    public async void TestGenAutoMatic()
    {
        var path = "./";
        var adoptId = "1234";
        var gImage = new GenerateImage()
        {
            newAttributes = new List<Trait>() { new() { traitType = "Background", value = "Fantasy Forest" } },
            baseImage = new BaseImage()
            {
                attributes = new List<Trait>() { new() { traitType = "Clothes", value = "Doraemon" } },
            }
        };
        var prompt = _autoMaticImageProvider.GetPrompt(gImage);
        prompt.ShouldNotBeNull();
        prompt.ShouldNotBeEmpty();
        var res = await _autoMaticImageProvider.QueryImageInfoByAiAsync(adoptId, gImage);
        res.ShouldNotBeNull();
        res.images.Count.ShouldBe(2);
        foreach (var img in res.images)
        {
            img.ShouldNotBeNull();
            img.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async void TestProviderType()
    {
        ProviderType.AutoMatic.ToString().ShouldBe("AutoMatic");
        ProviderType.Default.ToString().ShouldBe("Default");
    }
}