using Homeowner360.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Homeowner360.Api.Tests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                var testSettings =
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] =
                            "Homeowner360-Test-Jwt-Key-Only-For-Automated-Tests-2026",
                        ["Jwt:Issuer"] =
                            "Homeowner360.Api",
                        ["Jwt:Audience"] =
                            "Homeowner360.Client",
                        ["Jwt:ExpirationMinutes"] =
                            "60"
                    };

                config.AddInMemoryCollection(testSettings);
            });

        builder.ConfigureServices(services =>
        {
            var descriptor =
                services.SingleOrDefault(
                    service => service.ServiceType ==
                        typeof(DbContextOptions<HomeownerDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<HomeownerDbContext>(
                options =>
                {
                    options.UseInMemoryDatabase(
                        "Homeowner360IntegrationTests");
                });
        });
    }
}