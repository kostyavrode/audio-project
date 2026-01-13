FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AudioService.Api/AudioService.Api.csproj", "AudioService.Api/"]
COPY ["AudioService.Application/AudioService.Application.csproj", "AudioService.Application/"]
COPY ["AudioService.Domain/AudioService.Domain.csproj", "AudioService.Domain/"]
COPY ["AudioService.Infrastructure/AudioService.Infrastructure.csproj", "AudioService.Infrastructure/"]
RUN dotnet restore "AudioService.Api/AudioService.Api.csproj"
COPY . .
WORKDIR "/src/AudioService.Api"
RUN dotnet build "AudioService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AudioService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AudioService.Api.dll"]
