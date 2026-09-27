FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["GameStore.Api/GameStore.Api.csproj", "GameStore.Api/"]
RUN dotnet restore "GameStore.Api/GameStore.Api.csproj"

COPY GameStore.Api/ GameStore.Api/
WORKDIR /src/GameStore.Api
RUN dotnet publish "GameStore.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GameStore.Api.dll"]