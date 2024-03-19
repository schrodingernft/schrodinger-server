using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using Nest;
using SchrodingerServer.EntityEventHandler.Core.IndexHandler;
using SchrodingerServer.EntityEventHandler.Core.Options;
using Shouldly;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace SchrodingerServer.Limiter;

public class IRateDistributeLimiterTest : SchrodingerServerDomainTestBase
{
    private IRateDistributeLimiter _rateDistributeLimiter { get; set; }

    public IRateDistributeLimiterTest(ITestOutputHelper output) : base(output)
    {
        var monitor = Mock.Of<IOptionsMonitor<RateLimitOptions>>(x => x.CurrentValue == new RateLimitOptions()
        {
            RedisRateLimitOptions = new List<RateLimitOption>()
            {
                new()
                {
                    Name = "test",
                    TokenLimit = 1,
                    TokensPerPeriod = 1,
                    ReplenishmentPeriod = 1
                }
            }
        });
        _rateDistributeLimiter = new RateDistributeLimiter(GetService<IConnectionMultiplexer>(), monitor);
    }


    [Fact]
    public async void Test1()
    {
        var limiter = _rateDistributeLimiter.GetRateLimiterInstance("test");
        limiter.ShouldNotBeNull();
        var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.AcquireAsync(2));
        Assert.Equal("permitCount", ex.ParamName);
    }
}