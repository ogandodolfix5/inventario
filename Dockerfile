# Etapa base: runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080

# Etapa build: SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copiar archivo de proyecto y restaurar dependencias
COPY ["InventarioMvc/InventarioMvc.csproj", "InventarioMvc/"]
RUN dotnet restore "InventarioMvc/InventarioMvc.csproj"

# Copiar todo y compilar
COPY . .
WORKDIR "/src/InventarioMvc"
RUN dotnet build "InventarioMvc.csproj" -c Release -o /app/build

# Etapa publish: generar archivos finales
FROM build AS publish
RUN dotnet publish "InventarioMvc.csproj" -c Release -o /app/publish

# Etapa final: imagen lista para producción
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render asigna el puerto en la variable PORT
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_HOST_PATH=/usr/share/dotnet/dotnet

ENTRYPOINT ["dotnet", "InventarioMvc.dll"]
