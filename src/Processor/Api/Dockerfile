FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Processor/Domain/Processor.Domain.csproj", "src/Processor/Domain/"]
COPY ["src/Processor/Application/Processor.Application.csproj", "src/Processor/Application/"]
COPY ["src/Processor/Infra/Processor.Infra.csproj", "src/Processor/Infra/"]
COPY ["src/Processor/Api/Processor.Api.csproj", "src/Processor/Api/"]
RUN dotnet restore "./src/Processor/Api/Processor.Api.csproj"
COPY . .
WORKDIR "/src/src/Processor/Api"
RUN dotnet build "./Processor.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Processor.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Processor.Api.dll"]
