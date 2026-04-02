FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/DatoSchemaSync/DatoSchemaSync.csproj", "src/DatoSchemaSync/"]
RUN dotnet restore "src/DatoSchemaSync/DatoSchemaSync.csproj"
COPY . .
WORKDIR "/src/src/DatoSchemaSync"
RUN dotnet build "DatoSchemaSync.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DatoSchemaSync.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DatoSchemaSync.dll"]
