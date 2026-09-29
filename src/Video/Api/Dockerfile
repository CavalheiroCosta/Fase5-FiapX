FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Video/Domain/Video.Domain.csproj", "src/Video/Domain/"]
COPY ["src/Video/Application/Video.Application.csproj", "src/Video/Application/"]
COPY ["src/Video/Infra/Video.Infra.csproj", "src/Video/Infra/"]
COPY ["src/Video/Api/Video.Api.csproj", "src/Video/Api/"]
RUN dotnet restore "./src/Video/Api/Video.Api.csproj"
COPY . .
WORKDIR "/src/src/Video/Api"
RUN dotnet build "./Video.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Video.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Video.Api.dll"]
