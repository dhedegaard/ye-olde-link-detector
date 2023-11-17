FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine
WORKDIR /app

# Install dotnet ef tooling.
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

ENV TOKEN=set_me
ENV CONNECTION_STRING=set_me

CMD dotnet out/YeOldeLinkDetector.Bot.dll
