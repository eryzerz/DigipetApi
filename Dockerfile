# Use the official .NET SDK image as a base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Api/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY Api/. ./
RUN dotnet publish DigipetApi.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Install OpenSSL
RUN apt-get update && apt-get install -y openssl

# Generate self-signed certificate
RUN openssl req -x509 -newkey rsa:4096 -keyout /app/cert.key -out /app/cert.crt -days 365 -nodes -subj "/CN=localhost"
RUN openssl pkcs12 -export -out /app/cert.pfx -inkey /app/cert.key -in /app/cert.crt -passout pass:YourSecurePassword

# Set permissions
RUN chmod 644 /app/cert.pfx

# Expose the ports the app runs on
EXPOSE 80
EXPOSE 443

# Set the entry point for the application using the HTTPS profile
ENTRYPOINT ["dotnet", "DigipetApi.dll"]