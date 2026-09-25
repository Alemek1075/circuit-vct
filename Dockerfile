FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Circuit/Circuit.csproj src/Circuit/
RUN dotnet restore src/Circuit/Circuit.csproj
COPY src/Circuit/ src/Circuit/
RUN dotnet publish src/Circuit/Circuit.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
VOLUME /app/data
ENTRYPOINT ["dotnet", "Circuit.dll"]
