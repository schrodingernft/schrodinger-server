using System.Threading.Tasks;

namespace SchrodingerServer.Token;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string symbol);
}