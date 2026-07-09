# ===== Build Stage =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for better Docker layer caching.
COPY ["OVCMOVE.slnx", "./"]
COPY ["src/OVCMOVE.Api/OVCMOVE.Api.csproj", "src/OVCMOVE.Api/"]
COPY ["src/OVCMOVE.Application/OVCMOVE.Application.csproj", "src/OVCMOVE.Application/"]
COPY ["src/OVCMOVE.Infrastructure/OVCMOVE.Infrastructure.csproj", "src/OVCMOVE.Infrastructure/"]
COPY ["src/OVCMOVE.Domain/OVCMOVE.Domain.csproj", "src/OVCMOVE.Domain/"]
COPY ["plugin/OVCMOVE.2026.Plugin/OVCMOVE.2026.Plugin.csproj", "plugin/OVCMOVE.2026.Plugin/"]
COPY ["test/OVCMOVE.Test.Application/OVCMOVE.Test.Application.csproj", "test/OVCMOVE.Test.Application/"]

RUN dotnet restore "OVCMOVE.slnx"

# Copy toàn bộ source
COPY . .
WORKDIR "/src/src/OVCMOVE.Api"
RUN dotnet build "OVCMOVE.Api.csproj" -c Release -o /app/build

# ===== Publish Stage =====
FROM build AS publish
RUN dotnet publish "OVCMOVE.Api.csproj" -c Release -o /app/publish

# ===== Runtime Stage =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

# Copy published files
COPY --from=publish /app/publish .

# Entrypoint
ENTRYPOINT ["dotnet", "OVCMOVE.Api.dll"]
