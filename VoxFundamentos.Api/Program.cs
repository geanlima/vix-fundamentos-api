using VoxFundamentos.Api.Filters;
using VoxFundamentos.Application;
using VoxFundamentos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ArgumentExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddMemoryCache();
// Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// CORS (ANTES do MapControllers)
app.UseCors("web");

app.MapControllers();

app.Run();
