FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY smartscheduler.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "smartscheduler.dll"]
