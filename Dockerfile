# Etapa base: runtime con .NET 9
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Etapa build: SDK para compilar en .NET 9
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY ["InventarioMvc.csproj", "./"]
RUN dotnet restore "InventarioMvc.csproj"

# Copiar todo el código y compilar
COPY . .
RUN dotnet build "InventarioMvc.csproj" -c Release -o /app/build

# Etapa publish: generar archivos listos para producción
FROM build AS publish
RUN dotnet publish "InventarioMvc.csproj" -c Release -o /app/publish

# Etapa final: imagen lista para ejecución
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "InventarioMvc.dll"]
