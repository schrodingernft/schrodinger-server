using System;

namespace SchrodingerServer.Common;

public static class CommonConstant
{

    public const string EmptyString = "";
    public const string UnderLind = "_";
    public const string Comma = ",";

    public const string Success = "Success";
    
    public const string ELF = "ELF";
    public const string MainChainId = "AELF";
    public const string TDVVChainId = "tDVV";
    public const string TDVWChainId = "tDVW";
    public const string Separator = "-";
    
    public const string ZealyClientName = "Zealy";
    public const string ZealyApiKeyName = "x-api-key";
    public const string GetReviewsUri = "public/communities/projectschrodinger/reviews1";
    public const string GetUserUri = "public/communities/projectschrodinger/users";
    
    public static DateTimeOffset DefaultAbsoluteExpiration = DateTime.Parse("2099-01-01 12:00:00");
}