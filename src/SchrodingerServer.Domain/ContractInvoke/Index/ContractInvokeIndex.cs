using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using SchrodingerServer.Common;
using SchrodingerServer.Entities;

namespace SchrodingerServer.ContractInvoke.Index;

public class ContractInvokeIndex : SchrodingerEntity<Guid>, IIndexBuild
{
    [Keyword] public string ChainId { get; set; }

    // uniq id of bizData
    [Keyword] public string BizId { get; set; }

    // bizTypeName
    [Keyword] public string BizType { get; set; }

    // which contract to invoke
    [Keyword] public string ContractName { get; set; }

    // which contract method to invoke
    [Keyword] public string ContractMethod { get; set; }

    // account of invoker
    [Keyword] public string Sender { get; set; }

    [Keyword] public string Param { get; set; }

    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string Status { get; set; }

    public TransactionStatus TransactionStatus { get; set; }

    [Keyword] public string TransactionResult { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}