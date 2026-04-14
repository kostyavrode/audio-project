FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["shared/Common.Monitoring/Common.Monitoring.csproj", "shared/Common.Monitoring/"]
COPY ["services/NotificationService/NotificationService.Api/NotificationService.Api.csproj", "services/NotificationService/NotificationService.Api/"]
COPY ["services/NotificationService/NotificationService.Application/NotificationService.Application.csproj", "services/NotificationService/NotificationService.Application/"]
COPY ["services/NotificationService/NotificationService.Domain/NotificationService.Domain.csproj", "services/NotificationService/NotificationService.Domain/"]
COPY ["services/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "services/NotificationService/NotificationService.Infrastructure/"]
RUN dotnet restore "services/NotificationService/NotificationService.Api/NotificationService.Api.csproj"
COPY ["shared/Common.Monitoring/", "shared/Common.Monitoring/"]
COPY ["services/NotificationService/", "services/NotificationService/"]
WORKDIR "/src/services/NotificationService/NotificationService.Api"
RUN dotnet build "NotificationService.Api.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/services/NotificationService/NotificationService.Api"
RUN dotnet publish "NotificationService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
