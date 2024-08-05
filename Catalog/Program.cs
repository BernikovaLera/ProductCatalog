using System.Text.Json.Serialization;
using Catalog.Data;
using Catalog.Rabbit;
using Catalog.Web;
using EasyNetQ;
using Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using WorkerPrice;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("DefaultConnection")));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddSwaggerGen();

builder.Services.AddTransient<Cache>();

builder.Services.AddSession();
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = "localhost";
    options.InstanceName = "local";
});

// Add Quartz services
builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
builder.Services.AddSingleton<IJobFactory, SingletonJobFactory>();
builder.Services.AddHostedService<QuartzHostedService>();
builder.Services.AddTransient<QuartzApp>();

// Add Rabbit services
builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();

// Добавление EasyNetQ
builder.Services.AddSingleton<IBus>(RabbitHutch.CreateBus("host=localhost"));
builder.Services.AddTransient<Worker>();

var app = builder.Build();



AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
