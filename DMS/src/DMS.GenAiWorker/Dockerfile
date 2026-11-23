FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/DMS.GenAiWorker/DMS.GenAiWorker.csproj", "src/DMS.GenAiWorker/"]
COPY ["src/DMS.API/DMS.API.csproj", "src/DMS.API/"]
COPY ["src/DMS.BLL/DMS.BLL.csproj", "src/DMS.BLL/"]
COPY ["src/DMS.Domain/DMS.Domain.csproj", "src/DMS.Domain/"]
RUN dotnet restore "src/DMS.GenAiWorker/DMS.GenAiWorker.csproj"

COPY . .
WORKDIR "/src/src/DMS.GenAiWorker"
RUN dotnet publish "DMS.GenAiWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DMS.GenAiWorker.dll"]