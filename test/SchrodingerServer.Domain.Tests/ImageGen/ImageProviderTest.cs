using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SchrodingerServer.Adopts.dispatcher;
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
        _autoMaticImageProvider = new AutoMaticImageProvider(NullLogger<ImageProvider>.Instance, null, null, null, monitor);
    }

    [Fact]
    public async void TestGenAutoMatic()
    {
        var path = "./";
        var adoptId = "1234";
        var prompt =
            "A cute cat with two hands raised, ((pixel art)), cat add an Alien touch, (wearing Yoshi costume:0.1), Underwater Abyssal Reef background, Wearing a Mushroom Hat, (Has Fire-shaped eyes:1.2), (mouth Biting a Dagger:1.2), Wearing a Sunflower chain, (The cat is accompanied by a Baby Flame cat:1.6), Ditto, Black Bean Pad paw, High-Waiste Shorts";
        var res = await _autoMaticImageProvider.QueryImageInfoByAiAsync(adoptId, prompt);
        res.ShouldNotBeNull();
        res.images.Count.ShouldBe(2);
        foreach (var img in res.images)
        {
            img.ShouldNotBeNull();
            img.ShouldNotBeEmpty();
        }
    }
}