FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG SERVICE_NAME=People
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/Orchestration/ServiceDefaults/ServiceDefaults.csproj", "src/Orchestration/ServiceDefaults/"]
COPY ["src/Microservices/People/People.Domain/People.Domain.csproj", "src/Microservices/People/People.Domain/"]
COPY ["src/Microservices/People/People.Application/People.Application.csproj", "src/Microservices/People/People.Application/"]
COPY ["src/Microservices/People/People.Infrastructure/People.Infrastructure.csproj", "src/Microservices/People/People.Infrastructure/"]
COPY ["src/Microservices/People/People.API/People.API.csproj", "src/Microservices/People/People.API/"]

RUN dotnet restore "src/Microservices/People/People.API/People.API.csproj"

COPY . .
WORKDIR "/src/src/Microservices/People/People.API"
RUN dotnet build "People.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "People.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "People.API.dll"]
