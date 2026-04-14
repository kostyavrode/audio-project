FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["shared/Common.Monitoring/Common.Monitoring.csproj", "shared/Common.Monitoring/"]
COPY ["services/AuthService/AuthService.Api/AuthService.Api.csproj", "services/AuthService/AuthService.Api/"]
COPY ["services/AuthService/AuthService.Application/AuthService.Application.csproj", "services/AuthService/AuthService.Application/"]
COPY ["services/AuthService/AuthService.Domain/AuthService.Domain.csproj", "services/AuthService/AuthService.Domain/"]
COPY ["services/AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "services/AuthService/AuthService.Infrastructure/"]
RUN dotnet restore "services/AuthService/AuthService.Api/AuthService.Api.csproj"
COPY ["shared/Common.Monitoring/", "shared/Common.Monitoring/"]
COPY ["services/AuthService/", "services/AuthService/"]
WORKDIR "/src/services/AuthService/AuthService.Api"
RUN dotnet build "AuthService.Api.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/services/AuthService/AuthService.Api"
RUN dotnet publish "AuthService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
