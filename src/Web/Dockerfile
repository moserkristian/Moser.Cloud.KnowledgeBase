FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Orchestration/ServiceDefaults/ServiceDefaults.csproj", "src/Orchestration/ServiceDefaults/"]
COPY ["src/Modules/Ingestion/Ingestion.Application/Ingestion.Application.csproj", "src/Modules/Ingestion/Ingestion.Application/"]
COPY ["src/Modules/Ingestion/Ingestion.Infrastructure/Ingestion.Infrastructure.csproj", "src/Modules/Ingestion/Ingestion.Infrastructure/"]
COPY ["src/Modules/Assistant/Assistant.Domain/Assistant.Domain.csproj", "src/Modules/Assistant/Assistant.Domain/"]
COPY ["src/Modules/Assistant/Assistant.Application/Assistant.Application.csproj", "src/Modules/Assistant/Assistant.Application/"]
COPY ["src/Modules/Assistant/Assistant.Infrastructure/Assistant.Infrastructure.csproj", "src/Modules/Assistant/Assistant.Infrastructure/"]
COPY ["src/Web/Web.csproj", "src/Web/"]

RUN dotnet restore "src/Web/Web.csproj"

COPY . .
WORKDIR "/src/src/Web"
RUN dotnet build "Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.dll"]
