# Digital Pet Management API

This is a REST API for a Digital Pet Management application, providing a comprehensive backend for managing virtual pets.

## Core Features

- JWT Authentication
- Data Caching using Redis
- Background processing

## Functionality

With this API, users can:

- Adopt virtual pets
- Interact with their pets
- Create and manage feeding schedules for pets

## Getting Started

### Prerequisites

- Docker
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Running the Project

To run the project, use Docker Compose:

1. Run the project using Docker Compose:

   ```bash
   docker-compose up --build
   ```

2. Once all containers are running, access the API documentation at https://localhost:8081/swagger/index.html

### Unit Test

1. Navigate to the Tests folder:

   ```bash
   cd ./Tests
   ```

2. Build the Tests project:
   ```bash
   dotnet build DigipetApi.Tests.csproj
   ```
3. Run the test:
   ```bash
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./lcov.info -v n
   ```
4. Generate a coverage report:
   ```bash
   coverlet bin/Debug/net8.0/DigipetApi.Tests.dll --target "dotnet" --targetargs "test --no-build" --format lcov --output ./lcov.info
   ```
