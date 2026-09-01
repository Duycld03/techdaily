FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/TechDaily.Domain/TechDaily.Domain.csproj", "src/TechDaily.Domain/"]
COPY ["src/TechDaily.Application/TechDaily.Application.csproj", "src/TechDaily.Application/"]
COPY ["src/TechDaily.Infrastructure/TechDaily.Infrastructure.csproj", "src/TechDaily.Infrastructure/"]
COPY ["src/TechDaily.Api/TechDaily.Api.csproj", "src/TechDaily.Api/"]

RUN dotnet restore "src/TechDaily.Api/TechDaily.Api.csproj"

COPY src/ src/
WORKDIR "/src/src/TechDaily.Api"
RUN dotnet publish "TechDaily.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "TechDaily.Api.dll"]
