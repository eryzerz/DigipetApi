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

### API Documentation

#### Auth

- Login

  - POST /api/auth/login

- Register

  - POST /api/auth/register

- Refresh Token

  - POST /api/auth/refresh-token

- Logout
  - POST /api/auth/logout

#### Pets

- Get all pets:

  - GET /api/pet

- Get a pet by id:

  - GET /api/pet/{id}

- Get all adopted pets by user:

  - GET /api/pet

- Get all available pets to adopt:

  - GET /api/pet/available

- Adopt a pet:

  - PATCH /api/pet/{id}/adopt

- Return a pet, will decrease the pet's happiness and mood:

  - PATCH /api/pet/{id}/return

- Interact with a pet; you can feed, play, train, groom, and adventure with a pet. Each interaction will have a different effect on the pet's attributes:

  - PATCH /api/pet/{id}/interact

- Create a schedule to feed a pet:
  - POST /api/pet/{id}/schedule-feeding

**Note:** The attributes of the adopted pets will decrease over time, and the pets will need to be fed, played with, groomed, trained, and/or adventured with to keep their attributes up.

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

3. To be able to use the API, you will need to register a user and get the JWT token. You can do this by using the Register endpoint and then using the Login endpoint to get the JWT token.

4. You can use the JWT token to authorize your requests to the API. Find the "Authorize" button in the swagger UI and enter the JWT token in the "Value" field. **Do not forget to add the "Bearer " prefix to the token**.

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
4. Install coverlet.console globally on your machine:
   ```bash
   dotnet tool install --global coverlet.console
   ```
5. Generate a coverage report:
   ```bash
   coverlet bin/Debug/net8.0/DigipetApi.Tests.dll --target "dotnet" --targetargs "test --no-build" --format lcov --output ./lcov.info
   ```
