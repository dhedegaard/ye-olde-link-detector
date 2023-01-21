FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY --from=build-env /app/out .

VOLUME [ "/app/data" ]
CMD [ "sh", "-c", "dotnet tool install --global dotnet-ef && dotnet ef database update && dotnet ye-olde-link-detector.dll"]
