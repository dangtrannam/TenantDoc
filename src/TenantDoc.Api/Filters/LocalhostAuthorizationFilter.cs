using Hangfire.Dashboard;

namespace TenantDoc.Api.Filters;

public class LocalhostAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var remoteAddress = httpContext.Connection.RemoteIpAddress;
        
        // Allow localhost connections (127.0.0.1, ::1, and loopback)
        if (remoteAddress != null)
        {
            return remoteAddress.ToString() == "127.0.0.1" 
                || remoteAddress.ToString() == "::1" 
                || System.Net.IPAddress.IsLoopback(remoteAddress);
        }
        
        return false;
    }
}
