using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using SchrodingerServer.Symbol.Index;
using Volo.Abp.DependencyInjection;

namespace SchrodingerServer.Symbol.Provider;


public interface ISymbolDayPriceProvider
{
  Task SaveSymbolDayPriceIndex(List<SymbolDayPriceIndex> symbolDayPriceIndex);

}
public class SymbolDayPriceProvider : ISymbolDayPriceProvider, ISingletonDependency
{
    private readonly INESTRepository<SymbolDayPriceIndex, string> _symbolDayPriceIndexRepository;


    public  SymbolDayPriceProvider(INESTRepository<SymbolDayPriceIndex, string> symbolDayPriceIndexRepository)
    {
        _symbolDayPriceIndexRepository = symbolDayPriceIndexRepository;
    }

    public async Task SaveSymbolDayPriceIndex(List<SymbolDayPriceIndex> symbolDayPriceIndex)
    {
         await _symbolDayPriceIndexRepository.BulkAddOrUpdateAsync(symbolDayPriceIndex);
    }
}