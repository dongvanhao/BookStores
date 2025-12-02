# ------------------ Base runtime image ------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# ------------------ Build image ------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Core/BookStore.API/BookStore.API.csproj", "Core/BookStore.API/"]
COPY ["BookStore.Shared/BookStore.Shared.csproj", "BookStore.Shared/"]
COPY ["Core/BookStore.Application/BookStore.Application.csproj", "Core/BookStore.Application/"]
COPY ["Core/BookStore.Domain/BookStore.Domain.csproj", "Core/BookStore.Domain/"]
COPY ["Core/BookStore.Infrastructure/BookStore.Infrastructure.csproj", "Core/BookStore.Infrastructure/"]

RUN dotnet restore "Core/BookStore.API/BookStore.API.csproj"

COPY . .
WORKDIR "/src/Core/BookStore.API"
RUN dotnet publish "BookStore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ------------------ Final runtime image ------------------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookStore.API.dll"]
