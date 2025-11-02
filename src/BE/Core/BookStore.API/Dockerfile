FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy tất cả các project đúng vị trí
COPY ["Core/BookStore.API/BookStore.API.csproj", "Core/BookStore.API/"]
COPY ["Core/BookStore.Application/BookStore.Application.csproj", "Core/BookStore.Application/"]
COPY ["Core/BookStore.Domain/BookStore.Domain.csproj", "Core/BookStore.Domain/"]
COPY ["Core/BookStore.Infrastructure/BookStore.Infrastructure.csproj", "Core/BookStore.Infrastructure/"]
COPY ["BookStore.Shared/BookStore.Shared.csproj", "BookStore.Shared/"]

# restore
RUN dotnet restore "Core/BookStore.API/BookStore.API.csproj"

# copy toàn bộ source
COPY . .


WORKDIR "/src/Core/BookStore.API"

RUN dotnet build "BookStore.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookStore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookStore.API.dll"]
