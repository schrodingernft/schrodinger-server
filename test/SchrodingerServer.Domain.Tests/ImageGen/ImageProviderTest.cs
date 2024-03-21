using System;
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
    private DefaultImageProvider _defaultImageProvider { get; set; }

    public ImageProviderTest(ITestOutputHelper output) : base(output)
    {
        var monitor = Mock.Of<IOptionsMonitor<TraitsOptions>>(x => x.CurrentValue == new TraitsOptions()
        {
            ImageGenerateUrl = "http://35.202.61.156/image/generate",
            AutoMaticImageGenerateUrl = "http://fs.iis.pub:13888/sdapi/v1/txt2img"
        });
        var stableDiffusionOption = Mock.Of<IOptionsMonitor<StableDiffusionOption>>(x => x.CurrentValue == new StableDiffusionOption());
        _autoMaticImageProvider = new AutoMaticImageProvider(NullLogger<ImageProvider>.Instance, null, null, monitor, stableDiffusionOption);
        _defaultImageProvider = new DefaultImageProvider(NullLogger<ImageProvider>.Instance, null, monitor, null);
    }

    [Fact]
    public async void TestGenAutoMatic()
    {
        var path = "./";
        var adoptId = "1234";
        var gImage = new GenerateImage()
        {
            newAttributes = new List<Trait>()
            {
                new() { traitType = "Background", value = "Parallel Dimension Gateway" },
                new()
                {
                    traitType = "Breed",
                    value = "Exotic Shorthair"
                },
                new()
                {
                    traitType = "Pet",
                    value = "Baby Snail"
                },
            },
            baseImage = new BaseImage()
            {
                attributes = new List<Trait>() { new() { traitType = "Clothes", value = "Velvet Top" } },
            }
        };
        var prompt = _autoMaticImageProvider.GetPrompt(gImage);
        prompt.ShouldNotBeNull();
        prompt.ShouldNotBeEmpty();
        var start = DateTime.Now;
        var res = await _autoMaticImageProvider.QueryImageInfoByAiAsync(adoptId, gImage);
        var cost = (DateTime.Now - start).TotalMilliseconds;
        res.ShouldNotBeNull();
        res.images.Count.ShouldBe(2);
        foreach (var img in res.images)
        {
            img.ShouldNotBeNull();
            img.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async void TestGenSG()
    {
        var path = "./";
        var adoptId = Guid.NewGuid().ToString("N");
        var gImage = new GenerateImage()
        {
            newAttributes = new List<Trait>()
            {
                new() { traitType = "Background", value = "Parallel Dimension Gateway" },
                new()
                {
                    traitType = "Breed",
                    value = "Exotic Shorthair"
                },
                new()
                {
                    traitType = "Pet",
                    value = "Baby Snail"
                },
            },
            baseImage = new BaseImage()
            {
                attributes = new List<Trait>() { new() { traitType = "Clothes", value = "Velvet Top" } },
            }
        };
        var start = DateTime.Now;
        var res = await _defaultImageProvider.RequestGenerateImage(adoptId, new GenerateOpenAIImage()
        {
            newAttributes = gImage.newAttributes, baseImage = gImage.baseImage,
            numImages = 2
        });
        var cost = (DateTime.Now - start).TotalMilliseconds;
        res.ShouldBe(adoptId);
        var images = new List<string>();
        do
        {
            images = await _defaultImageProvider.QueryImages(adoptId);
        } while (images == null || images.Count == 0);

        foreach (var img in images)
        {
            img.ShouldNotBeNull();
            img.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public async void TestProviderType()
    {
        string.IsNullOrEmpty(null).ShouldBeTrue();
        ProviderType.AutoMatic.ToString().ShouldBe("AutoMatic");
        ProviderType.Default.ToString().ShouldBe("Default");
    }
}