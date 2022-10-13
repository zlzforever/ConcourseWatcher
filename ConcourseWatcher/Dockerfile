FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
COPY fly /usr/bin/fly

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ConcourseWatcher/ConcourseWatcher.csproj", "ConcourseWatcher/"]
RUN dotnet restore "ConcourseWatcher/ConcourseWatcher.csproj"
COPY . .
WORKDIR "/src/ConcourseWatcher"
RUN dotnet build "ConcourseWatcher.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConcourseWatcher.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConcourseWatcher.dll"]
