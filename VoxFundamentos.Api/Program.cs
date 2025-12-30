using VoxFundamentos.Application;
using VoxFundamentos.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
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

app.UseHttpsRedirection();

// CORS (ANTES do MapControllers)
app.UseCors("web");

app.MapControllers();

app.Run();
