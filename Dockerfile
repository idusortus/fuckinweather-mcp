FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/FuknWeather.Api/FuknWeather.Api.csproj", "src/FuknWeather.Api/"]
RUN dotnet restore "src/FuknWeather.Api/FuknWeather.Api.csproj"
COPY . .
WORKDIR "/src/src/FuknWeather.Api"
RUN dotnet build "FuknWeather.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FuknWeather.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FuknWeather.Api.dll"]
