FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["shared/Common.Monitoring/Common.Monitoring.csproj", "shared/Common.Monitoring/"]
COPY ["services/GroupsService/GroupsService.Api/GroupsService.Api.csproj", "services/GroupsService/GroupsService.Api/"]
COPY ["services/GroupsService/GroupsService.Application/GroupsService.Application.csproj", "services/GroupsService/GroupsService.Application/"]
COPY ["services/GroupsService/GroupsService.Domain/GroupsService.Domain.csproj", "services/GroupsService/GroupsService.Domain/"]
COPY ["services/GroupsService/GroupsService.Infrastructure/GroupsService.Infrastructure.csproj", "services/GroupsService/GroupsService.Infrastructure/"]
RUN dotnet restore "services/GroupsService/GroupsService.Api/GroupsService.Api.csproj"
COPY ["shared/Common.Monitoring/", "shared/Common.Monitoring/"]
COPY ["services/GroupsService/", "services/GroupsService/"]
WORKDIR "/src/services/GroupsService/GroupsService.Api"
RUN dotnet build "GroupsService.Api.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/services/GroupsService/GroupsService.Api"
RUN dotnet publish "GroupsService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GroupsService.Api.dll"]
