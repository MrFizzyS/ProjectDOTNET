using System.Diagnostics;

namespace Platform.ServiceDefaults;

public static class BusinessTelemetry
{
    public static readonly ActivitySource Source = new("Platform.Business");
}