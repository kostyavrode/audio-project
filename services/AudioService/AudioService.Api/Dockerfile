FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["shared/Common.Monitoring/Common.Monitoring.csproj", "shared/Common.Monitoring/"]
COPY ["services/AudioService/AudioService.Api/AudioService.Api.csproj", "services/AudioService/AudioService.Api/"]
COPY ["services/AudioService/AudioService.Application/AudioService.Application.csproj", "services/AudioService/AudioService.Application/"]
COPY ["services/AudioService/AudioService.Domain/AudioService.Domain.csproj", "services/AudioService/AudioService.Domain/"]
COPY ["services/AudioService/AudioService.Infrastructure/AudioService.Infrastructure.csproj", "services/AudioService/AudioService.Infrastructure/"]
RUN dotnet restore "services/AudioService/AudioService.Api/AudioService.Api.csproj"
COPY ["shared/Common.Monitoring/", "shared/Common.Monitoring/"]
COPY ["services/AudioService/", "services/AudioService/"]
WORKDIR "/src/services/AudioService/AudioService.Api"
RUN dotnet build "AudioService.Api.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/services/AudioService/AudioService.Api"
RUN dotnet publish "AudioService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AudioService.Api.dll"]
