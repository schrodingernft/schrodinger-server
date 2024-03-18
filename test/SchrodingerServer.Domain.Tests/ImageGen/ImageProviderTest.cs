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
            AutoMaticImageGenerateUrl = "http://192.168.11.40:3008/traits-to-image"
        });
        var stableDiffusionOption = Mock.Of<IOptionsMonitor<StableDiffusionOption>>(x => x.CurrentValue == new StableDiffusionOption());
        _autoMaticImageProvider = new AutoMaticImageProvider(NullLogger<ImageProvider>.Instance, null, null, monitor, stableDiffusionOption);
    }

    [Fact]
    public async void TestGenAutoMatic()
    {
        var path = "./";
        var adoptId = "1234";
        var prompt =
            "A cute cat with two hands raised, ((pixel art)), cat add an Alien touch, (wearing Yoshi costume:0.1), Underwater Abyssal Reef background, Wearing a Mushroom Hat, (Has Fire-shaped eyes:1.2), (mouth Biting a Dagger:1.2), Wearing a Sunflower chain, (The cat is accompanied by a Baby Flame cat:1.6), Ditto, Black Bean Pad paw, High-Waiste Shorts";
        var gImage = new GenerateImage()
        {
            newAttributes = new List<Trait>() { new() { traitType = "Background", value = "Fantasy Forest" } },
            baseImage = new BaseImage()
            {
                attributes = new List<Trait>() { new() { traitType = "Clothes", value = "Doraemon" } },
            }
        };

        var res = await _autoMaticImageProvider.QueryImageInfoByAiAsync(adoptId, gImage);
        res.ShouldNotBeNull();
        res.images.Count.ShouldBe(2);
        foreach (var img in res.images)
        {
            img.ShouldNotBeNull();
            img.ShouldNotBeEmpty();
        }
    }
}