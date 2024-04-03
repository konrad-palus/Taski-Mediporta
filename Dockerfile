FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TaskApi-Mediporta.csproj", "./"]
RUN dotnet restore "TaskApi-Mediporta.csproj"
COPY . .
RUN dotnet build "TaskApi-Mediporta.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TaskApi-Mediporta.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskApi-Mediporta.dll"]