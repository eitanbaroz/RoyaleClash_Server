# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# HTTP port
EXPOSE 3000

# TCP game/server port
EXPOSE 5070

# Make ASP.NET listen on port 3000
ENV ASPNETCORE_URLS=http://+:3000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "eitan.dll"]