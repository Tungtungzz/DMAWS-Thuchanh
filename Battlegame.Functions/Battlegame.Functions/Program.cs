// Program.cs
using Battlegame.Functions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

var host = Host.CreateDefaultBuilder(args)
    // <- Đây là method extension cung cấp bởi package
    // Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Lấy connection string từ cấu hình (local.settings.json / env)
        var conn = context.Configuration.GetConnectionString("BattlegameDb")
                   ?? "Server=(localdb)\\MSSQLLocalDB;Database=BATTLEGAME;Trusted_Connection=True;";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(conn));

        // nếu bạn muốn register thêm services, repository, v.v. => add ở đây
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();
