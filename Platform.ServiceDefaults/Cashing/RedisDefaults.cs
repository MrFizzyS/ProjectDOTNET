using StackExchange.Redis;

namespace Platform.ServiceDefaults.Caching;

public static class RedisDefaults
{
    public static void FailFast(ConfigurationOptions options)
    {
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 1000;
        options.SyncTimeout = 1000;
        options.AsyncTimeout = 1000;
        options.BacklogPolicy = BacklogPolicy.FailFast;
    }
}