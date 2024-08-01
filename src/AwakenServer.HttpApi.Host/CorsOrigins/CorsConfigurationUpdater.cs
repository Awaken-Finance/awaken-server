using System;
using System.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AwakenServer;

public class CorsConfigurationUpdater
{
    private readonly IOptionsMonitor<AppOptions> _corsOptionsMonitor;
    private readonly IServiceProvider _serviceProvider;

    public CorsConfigurationUpdater(IOptionsMonitor<AppOptions> corsOptionsMonitor, IServiceProvider serviceProvider)
    {
        _corsOptionsMonitor = corsOptionsMonitor;
        _serviceProvider = serviceProvider;
        _corsOptionsMonitor.OnChange((newOptions, _) =>
        {
            var corsOrigins = newOptions.CorsOrigins
                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.RemovePostFix("/"))
                .ToArray();
        
            using (var scope = _serviceProvider.CreateScope())
            {
                var corsOptions = scope.ServiceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;

                corsOptions.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithOrigins(corsOrigins)
                        .WithAbpExposedHeaders()
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            }
        
            Log.Information("CORS options updated: " + string.Join(", ", corsOrigins));
        });
    }

}