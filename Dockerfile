# ===============================
# BUILD
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia tudo
COPY . .

# Restaura dependências
RUN dotnet restore

# Publica a API
RUN dotnet publish VoxFundamentos.Api/VoxFundamentos.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ===============================
# RUNTIME
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

# Porta usada pelo Render
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "VoxFundamentos.Api.dll"]
