FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GroupsService.Api/GroupsService.Api.csproj", "GroupsService.Api/"]
COPY ["GroupsService.Application/GroupsService.Application.csproj", "GroupsService.Application/"]
COPY ["GroupsService.Domain/GroupsService.Domain.csproj", "GroupsService.Domain/"]
COPY ["GroupsService.Infrastructure/GroupsService.Infrastructure.csproj", "GroupsService.Infrastructure/"]
RUN dotnet restore "GroupsService.Api/GroupsService.Api.csproj"
COPY . .
WORKDIR "/src/GroupsService.Api"
RUN dotnet build "GroupsService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GroupsService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GroupsService.Api.dll"]
