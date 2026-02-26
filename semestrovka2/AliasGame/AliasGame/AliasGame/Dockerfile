
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY AliasGame.sln .
COPY Shared/AliasGame.Shared.csproj Shared/
COPY Server/AliasGame.Server.csproj Server/


RUN dotnet restore Server/AliasGame.Server.csproj


COPY Shared/ Shared/
COPY Server/ Server/


RUN dotnet publish Server/AliasGame.Server.csproj -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .


EXPOSE 7777


ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "AliasGame.Server.dll"]
