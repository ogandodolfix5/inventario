# Usa la imagen oficial de .NET
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["InventarioMvc/InventarioMvc.csproj", "InventarioMvc/"]
RUN dotnet restore "InventarioMvc/InventarioMvc.csproj"
COPY . .
WORKDIR "/src/InventarioMvc"
RUN dotnet build "InventarioMvc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventarioMvc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventarioMvc.dll"]
