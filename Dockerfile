FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
WORKDIR /app

# Install dotnet ef tooling.
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy everything
COPY . ./
# Build and publish a release
RUN dotnet publish YeOldeLinkDetector.Bot -c Release -o out

ENV TOKEN=set_me \
  CONNECTION_STRING=set_me

CMD ["dotnet", "out/YeOldeLinkDetector.Bot.dll"]
