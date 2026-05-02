# ─── Base runtime ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# ─── Build ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Core/BookStore.API/BookStore.API.csproj",                       "Core/BookStore.API/"]
COPY ["Core/BookStore.Shared/BookStore.Shared.csproj",                 "Core/BookStore.Shared/"]
COPY ["Core/BookStore.Application/BookStore.Application.csproj",       "Core/BookStore.Application/"]
COPY ["Core/BookStore.Domain/BookStore.Domain.csproj",                 "Core/BookStore.Domain/"]
COPY ["Core/BookStore.Infrastructure/BookStore.Infrastructure.csproj", "Core/BookStore.Infrastructure/"]

RUN dotnet restore "Core/BookStore.API/BookStore.API.csproj"

COPY . .

RUN dotnet build "Core/BookStore.API/BookStore.API.csproj" -c Release --no-restore
RUN dotnet publish "Core/BookStore.API/BookStore.API.csproj" \
    -c Release -o /app/publish --no-build /p:UseAppHost=false

# ─── Migrator — dùng dotnet-ef để add/update migrations ────────────
# ENTRYPOINT là "dotnet-ef", docker-compose override CMD để add hoặc update
FROM build AS migrator
RUN dotnet tool install --global dotnet-ef --version 8.0.*
ENV PATH="${PATH}:/root/.dotnet/tools"
WORKDIR /src
ENTRYPOINT ["dotnet-ef"]
CMD ["database", "update", \
    "--project",         "Core/BookStore.Infrastructure/BookStore.Infrastructure.csproj", \
    "--startup-project", "Core/BookStore.API/BookStore.API.csproj", \
    "--configuration",   "Release", \
    "--no-build"]

# ─── Final API image ───────────────────────────────────────────────
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookStore.API.dll"]
