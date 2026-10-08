FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["src/DeepPace.Domain/DeepPace.Domain.csproj", "src/DeepPace.Domain/"]
COPY ["src/DeepPace.Application/DeepPace.Application.csproj", "src/DeepPace.Application/"]
COPY ["src/DeepPace.Infrastructure/DeepPace.Infrastructure.csproj", "src/DeepPace.Infrastructure/"]
COPY ["src/DeepPace.Api/DeepPace.Api.csproj", "src/DeepPace.Api/"]

RUN dotnet restore "src/DeepPace.Api/DeepPace.Api.csproj"

COPY src/ src/
WORKDIR "/src/src/DeepPace.Api"
RUN dotnet publish "DeepPace.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "DeepPace.Api.dll"]
