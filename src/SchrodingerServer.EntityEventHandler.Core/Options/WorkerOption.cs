using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace SchrodingerServer.EntityEventHandler.Core.Options;

public class WorkerOptions
{
    public const string DefaultCron = "0 0/3 * * * ?";

    public string[] ChainIds { get; set; }
    
    public string BizDate { get; set; }

    public Dictionary<string, Worker> Workers { get; set; } = new Dictionary<string, Worker>();

    public string GetWorkerBizDate(string workerName)
    {
        var workerBizDate = Workers.TryGetValue(workerName, out var worker) ? worker.BizDate : null;

        return workerBizDate.IsNullOrEmpty() ? BizDate : workerBizDate;
    }
}


public class Worker
{
    public int Minutes { get; set; } = 10;
    public string Cron { get; set; } = WorkerOptions.DefaultCron;
    public string BizDate { get; set; }
}