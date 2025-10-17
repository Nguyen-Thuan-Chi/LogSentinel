using System.Threading;
using System.Threading.Tasks;

namespace LogSentinel.BUS.Interfaces
{
    public interface IEventImporter
    {
        Task StartStreamingAsync(CancellationToken cancellationToken);
        Task ImportBatchAsync(string directoryPath);
    }
}
