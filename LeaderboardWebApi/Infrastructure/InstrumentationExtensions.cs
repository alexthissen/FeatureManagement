﻿using HealthChecks.UI.Client;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LeaderboardWebApi.Infrastructure
{
    public static class InstrumentationExtensions
    {
        public static void MapInstrumentation(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapHealthChecks("/health",
                    new HealthCheckOptions()
                    {
                        Predicate = _ => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    });
                app.MapHealthChecksUI();
            }

            // Readiness and liveliness endpoints
            app.MapHealthChecks("/health/ready",
                new HealthCheckOptions()
                {
                    Predicate = reg => reg.Tags.Contains("ready"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .RequireHost($"*:{app.Configuration["ManagementPort"]}");
            app.MapHealthChecks("/health/lively",
                new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .RequireHost($"*:{app.Configuration["ManagementPort"]}");
        }

        public static void AddInstrumentation(this WebApplicationBuilder builder)
        {
            string key = builder.Configuration["ApplicationInsights:InstrumentationKey"];

            IHealthChecksBuilder health = builder.Services.AddHealthChecks();
            health.AddApplicationInsightsPublisher(key);
            builder.Services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(10);
            });
            builder.Services
                .AddHealthChecksUI(setup =>
                {
                    setup.AddHealthCheckEndpoint("Leaderboard Web API Health checks", "http://localhost/health");
                    setup.SetEvaluationTimeInSeconds(5);
                })
                .AddInMemoryStorage();

            builder.Services.AddSingleton<ITelemetryInitializer, ServiceNameInitializer>();
            builder.Services.AddApplicationInsightsTelemetry(options =>
            {
                options.DeveloperMode = builder.Environment.IsDevelopment();
                options.InstrumentationKey = key;
            });
        }
    }
}
