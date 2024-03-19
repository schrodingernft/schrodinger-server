using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using SchrodingerServer.Common;
using SchrodingerServer.Config;
using SchrodingerServer.Symbol.Index;
using SchrodingerServer.Symbol.Provider;
using SchrodingerServer.Token;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Symbol;


public interface IXgrPriceService
{
    Task SaveXgrDayPriceAsync(bool isGen0);
}


public class XgrPriceService : IXgrPriceService,ISingletonDependency
{
    private readonly ISchrodingerSymbolProvider _schrodingerSymbolProvider;
    private readonly ISymbolPriceGraphProvider _symbolPriceGraphProvider;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly IConfigAppService _configAppService;
    private readonly UniswapV3Provider _uniswapV3Provider;
    private readonly ISymbolDayPriceProvider _symbolDayPriceProvider;
    private readonly ILogger<XgrPriceService> _logger;
    private const int QueryOnceLimit = 100;
    private readonly string _dateTimeFormat = "yyyyMMdd";
    
    public XgrPriceService( ISchrodingerSymbolProvider schrodingerSymbolProvider,
        ISymbolPriceGraphProvider symbolPriceGraphProvider,
        ITokenPriceProvider tokenPriceProvider,
        IConfigAppService configAppService,
        UniswapV3Provider uniswapV3Provider,
        ISymbolDayPriceProvider symbolDayPriceProvider,
        ILogger<XgrPriceService> logger)
    {
        _schrodingerSymbolProvider = schrodingerSymbolProvider;
        _symbolPriceGraphProvider = symbolPriceGraphProvider;
        _tokenPriceProvider = tokenPriceProvider;
        _configAppService = configAppService;
        _uniswapV3Provider = uniswapV3Provider;
        _symbolDayPriceProvider = symbolDayPriceProvider;
        _logger = logger;
    }

    public async Task SaveXgrDayPriceAsync(bool isGen0)
    {
        var skipCount = 0;
        var date = getUTCDay();
        var dateStr = date.AddDays(-1).ToString(_dateTimeFormat);
        while (true)
        {
            var schrodingerSymbolList =
                await _schrodingerSymbolProvider.GetSchrodingerSymbolList(skipCount, QueryOnceLimit);
            if (schrodingerSymbolList.IsNullOrEmpty()) break;
            skipCount += QueryOnceLimit;
            List<SymbolDayPriceIndex> symbolDayPriceIndexList = new List<SymbolDayPriceIndex>();
            foreach (var item in schrodingerSymbolList)
            {
                var price = await GetSymbolPrice(item.Symbol,date.ToUtcSeconds(),isGen0);
                if (price > 0)
                {
                    var symbolDayPriceIndex = new SymbolDayPriceIndex()
                    {
                        Id = $"{item.Symbol}-{dateStr}",
                        Symbol = item.Symbol,
                        Price = price,
                        Date = dateStr
                    };
                    symbolDayPriceIndexList.Add(symbolDayPriceIndex);
                }
            }
            if (symbolDayPriceIndexList.Count > 0)
            {
                await _symbolDayPriceProvider.SaveSymbolDayPriceIndex(symbolDayPriceIndexList);
            }
            _logger.LogInformation("SaveXgrDayPriceAsync date:{date} isGen0:{isGen0} count:{count}", dateStr,isGen0,symbolDayPriceIndexList.Count);
        }
       
    }
    
    private DateTime getUTCDay()
    {
        DateTime nowUtc = DateTime.UtcNow;
        return new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    private async Task<decimal> GetSymbolPrice(string symbol,long date,bool isGen0)
    {
        var getMyNftListingsDto = new GetNFTListingsDto()
        {
            ChainId = _configAppService.GetConfig()["curChain"],
            Symbol = symbol,
            SkipCount = 0,
            MaxResultCount = 1
        };
        decimal usdPrice = 0;
        try
        {
            bool isGen0Symbol  = GetIsGen0FromSymbol(symbol);
            if (isGen0 && isGen0Symbol)
            {
                var tokenResponse  = await _uniswapV3Provider.GetLatestUSDPriceAsync(date);
                if (tokenResponse != null )
                {
                    usdPrice = Convert.ToDecimal(tokenResponse.PriceUSD);
                }
            }else if(!isGen0 && !isGen0Symbol)
            {
                var listingDto = await _symbolPriceGraphProvider.GetNFTListingsAsync(getMyNftListingsDto);
                if (listingDto != null && listingDto.TotalCount > 0)
                {
                    var tokenPrice = listingDto.Items[0].Prices;
                    var symbolUsdPrice = await _tokenPriceProvider.GetPriceByCacheAsync(listingDto.Items[0].PurchaseToken.Symbol);
                    usdPrice = tokenPrice* symbolUsdPrice;
                }
            }
            return  usdPrice;
        }catch (Exception e)
        {
            _logger.LogError(e, "GetSymbolPrice error symbol:{symbol} date {date}", symbol,date);
        }
        return 0;
    }
    
    public static bool GetIsGen0FromSymbol(string symbol)
    {
        try
        {
            return Convert.ToInt32(symbol.Split(CommonConstant.Separator)[1]) == 1;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public class GetNFTListingsDto
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int SkipCount { get; set; }
    
    public int MaxResultCount { get; set; }
}



