FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RSGM.Api.csproj", "./"]
RUN dotnet restore "RSGM.Api.csproj"

COPY . .

RUN dotnet publish "RSGM.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "dotnet RSGM.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]