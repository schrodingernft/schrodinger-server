using System.Threading.Tasks;

namespace SchrodingerServer.Ipfs;

public interface IIpfsAppService
{
    public Task<string> TestIpfs();

    public Task<string> Upload(string content, string name);
}