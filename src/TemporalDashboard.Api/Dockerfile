FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["TemporalDashboard.Api/TemporalDashboard.Api.csproj", "TemporalDashboard.Api/"]
COPY ["TemporalDashboard.WorkflowDiagramming/TemporalDashboard.WorkflowDiagramming.csproj", "TemporalDashboard.WorkflowDiagramming/"]
RUN dotnet restore "TemporalDashboard.Api/TemporalDashboard.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/TemporalDashboard.Api"
RUN dotnet build "TemporalDashboard.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TemporalDashboard.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create uploads directory
RUN mkdir -p /app/uploads

ENTRYPOINT ["dotnet", "TemporalDashboard.Api.dll"]
