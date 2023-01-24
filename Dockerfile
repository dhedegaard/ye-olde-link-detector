FROM mcr.microsoft.com/dotnet/sdk:7.0
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Install dotnet ef tooling.
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

ENV TOKEN=set_me
ENV CONNECTION_STRING=set_me

CMD dotnet out/ye-olde-link-detector.dll
