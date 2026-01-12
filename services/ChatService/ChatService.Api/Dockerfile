FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ChatService.Api/ChatService.Api.csproj", "ChatService.Api/"]
COPY ["ChatService.Application/ChatService.Application.csproj", "ChatService.Application/"]
COPY ["ChatService.Domain/ChatService.Domain.csproj", "ChatService.Domain/"]
COPY ["ChatService.Infrastructure/ChatService.Infrastructure.csproj", "ChatService.Infrastructure/"]
RUN dotnet restore "ChatService.Api/ChatService.Api.csproj"
COPY . .
WORKDIR "/src/ChatService.Api"
RUN dotnet build "ChatService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatService.Api.dll"]
