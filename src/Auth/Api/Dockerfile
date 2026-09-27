FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Auth/Domain/Auth.Domain.csproj", "src/Auth/Domain/"]
COPY ["src/Auth/Application/Auth.Application.csproj", "src/Auth/Application/"]
COPY ["src/Auth/Infra/Auth.Infra.csproj", "src/Auth/Infra/"]
COPY ["src/Auth/Api/Auth.Api.csproj", "src/Auth/Api/"]
RUN dotnet restore "./src/Auth/Api/Auth.Api.csproj"
COPY . .
WORKDIR "/src/src/Auth/Api"
RUN dotnet build "./Auth.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Auth.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
