FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["TemporalDashboard.Web/TemporalDashboard.Web.csproj", "TemporalDashboard.Web/"]
COPY ["TemporalDashboard.WorkflowDiagramming/TemporalDashboard.WorkflowDiagramming.csproj", "TemporalDashboard.WorkflowDiagramming/"]
RUN dotnet restore "TemporalDashboard.Web/TemporalDashboard.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/TemporalDashboard.Web"
RUN dotnet build "TemporalDashboard.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TemporalDashboard.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TemporalDashboard.Web.dll"]
