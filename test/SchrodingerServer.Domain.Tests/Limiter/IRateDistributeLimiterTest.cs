using System;
using System.Collections.Generic;
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
        _rateDistributeLimiter = new RateDistributeLimiter(GetService<IConnectionMultiplexer>(), new RateLimitOptions()
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