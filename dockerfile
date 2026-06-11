# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 3000
EXPOSE 5070

ENV ASPNETCORE_URLS=http://+:3000

COPY --from=build /app/publish .

# IMPORTANT: replace with your real dll name
ENTRYPOINT ["dotnet", "RoyaleClash.dll"]