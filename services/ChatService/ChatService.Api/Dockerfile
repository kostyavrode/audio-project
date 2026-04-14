FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["shared/Common.Monitoring/Common.Monitoring.csproj", "shared/Common.Monitoring/"]
COPY ["services/ChatService/ChatService.Api/ChatService.Api.csproj", "services/ChatService/ChatService.Api/"]
COPY ["services/ChatService/ChatService.Application/ChatService.Application.csproj", "services/ChatService/ChatService.Application/"]
COPY ["services/ChatService/ChatService.Domain/ChatService.Domain.csproj", "services/ChatService/ChatService.Domain/"]
COPY ["services/ChatService/ChatService.Infrastructure/ChatService.Infrastructure.csproj", "services/ChatService/ChatService.Infrastructure/"]
RUN dotnet restore "services/ChatService/ChatService.Api/ChatService.Api.csproj"
COPY ["shared/Common.Monitoring/", "shared/Common.Monitoring/"]
COPY ["services/ChatService/", "services/ChatService/"]
WORKDIR "/src/services/ChatService/ChatService.Api"
RUN dotnet build "ChatService.Api.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/services/ChatService/ChatService.Api"
RUN dotnet publish "ChatService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatService.Api.dll"]
