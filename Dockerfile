# Use the official .NET 6 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy the remaining files and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Use the runtime image for running the app
FROM mcr.microsoft.com/dotnet/runtime:6.0

WORKDIR /app

# Copy the build output to the runtime container
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "Service Checker.dll"]