using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Application.Tools.Interfaces;
using Application.Tools.Seeders;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;

namespace Application.Tools
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a valid argument");
                return;
            }

            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();

            var databaseManager = scope.ServiceProvider.GetRequiredService<DatabaseManager>();

            switch (args[0])
            {
                case "fresh":
                    await databaseManager.Reset();
                    break;

                case "seed":
                    await databaseManager.Reset();

                    var token = new CancellationToken();

                    foreach (var seeder in scope.ServiceProvider.GetRequiredService<IEnumerable<ISeeder>>())
                    {
                        await seeder.Run(token);
                    }
                    break;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config => config.AddJsonFile("appsettings.json", true, true))
                .ConfigureServices((_, services) =>
                {
                    services.AddInfrastructure(_.Configuration);

                    services.AddScoped<DatabaseManager>();

                    services.AddScoped<UsersSeeder>();
                    services.AddScoped<ArticlesSeeder>();

                    services.AddScoped<IEnumerable<ISeeder>>(options => new List<ISeeder>
                    {
                        options.GetRequiredService<UsersSeeder>(),
                        options.GetRequiredService<ArticlesSeeder>()
                    });
                });
    }
}
