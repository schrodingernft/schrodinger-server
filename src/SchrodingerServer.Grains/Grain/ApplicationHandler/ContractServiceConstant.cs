namespace SchrodingerServer.Grains.Grain.ApplicationHandler;

public static class MethodName
{
    public const string Transfer = "Transfer";
    public const string GetBalance = "GetBalance";
}

public static class TransactionState
{
    public const string Mined = "MINED";
    public const string Pending = "PENDING";
}