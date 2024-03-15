using System;
using SchrodingerServer.Common;
using Volo.Abp.EventBus;

namespace SchrodingerServer.ContractInvoke.Eto;

[EventName("ContractInvokeEto")]
public class ContractInvokeEto
{
    public string Id { get; set; }

    public string ChainId { get; set; }

    public string BizId { get; set; }

    public string BizType { get; set; }

    public string ContractName { get; set; }

    public string ContractMethod { get; set; }

    public string Sender { get; set; }

    public string Param { get; set; }

    public string TransactionId { get; set; }
    
    public string Status { get; set; }

    public TransactionStatus TransactionStatus { get; set; }

    public string TransactionResult { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}