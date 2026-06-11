FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 3000
EXPOSE 5070

ENV ASPNETCORE_URLS=http://+:3000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RoyaleClash.dll"]