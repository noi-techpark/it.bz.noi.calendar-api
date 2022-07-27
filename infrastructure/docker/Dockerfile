FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY it.bz.noi.calendar-api/it.bz.noi.calendar-api.csproj it.bz.noi.calendar-api/
RUN dotnet restore it.bz.noi.calendar-api/it.bz.noi.calendar-api.csproj
COPY . .
WORKDIR /src/it.bz.noi.calendar-api
RUN dotnet build it.bz.noi.calendar-api.csproj -c Release -o /app

FROM build AS test
WORKDIR /src
RUN dotnet test it.bz.noi.calendar-api.sln

FROM build AS publish
RUN dotnet publish it.bz.noi.calendar-api.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "it.bz.noi.calendar-api.dll"]
