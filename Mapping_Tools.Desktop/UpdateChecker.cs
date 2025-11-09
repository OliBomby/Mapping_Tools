using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop;

public class UpdateChecker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        // TODO: Implement update checking logic
    }
}
