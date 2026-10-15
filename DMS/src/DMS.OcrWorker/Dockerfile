# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copying and restoring Project Files
COPY ["src/DMS.OcrWorker/DMS.OcrWorker.csproj", "src/DMS.OcrWorker/"]
COPY ["src/DMS.BLL/DMS.BLL.csproj", "src/DMS.BLL/"]
COPY ["src/DMS.Domain/DMS.Domain.csproj", "src/DMS.Domain/"]
RUN dotnet restore "src/DMS.OcrWorker/DMS.OcrWorker.csproj"

# Copy and publish the source code
COPY . .
WORKDIR "/src/src/DMS.OcrWorker"
RUN dotnet publish "DMS.OcrWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime-Stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DMS.OcrWorker.dll"]