# Phoenix-Azure API

## Overview
The Phoenix-Azure API provides backend services for the Phoenix-Azure application. It connects to remote healthcare data sources and exposes endpoints for retrieving patient information, medical records, and exploring repository data.

## Features
- **Patient Data**: Retrieve patient demographics and medical records
- **Repository Explorer**: Explore available data repositories and execute repository methods
- **Flexible Data Source Strategy**: Supports both SQL and FHIR data sources

## Architecture
The API follows a three-tier architecture:
- **Presentation Layer**: REST API controllers
- **Business Logic Layer**: Service classes and domain models
- **Data Access Layer**: Repository interfaces with SQL and FHIR implementations

## API Endpoints

### Patient Controller
- `GET /api/Patient`: Get a list of all patients
- `GET /api/Patient/{id}`: Get details for a specific patient
- `GET /api/Patient/{id}/MedicalRecords`: Get medical records for a specific patient

### Repository Explorer Controller
- `GET /api/RepositoryExplorer/available`: Get a list of available repositories
- `GET /api/RepositoryExplorer/explore/{repositoryName}`: Explore a specific repository
- `GET /api/RepositoryExplorer/explore/{repositoryName}/{methodName}`: Execute a method on a repository

## Data Source Configuration
The API can be configured to use either SQL or FHIR as the data source:
- **SQL**: Uses traditional database connections with Dapper for data access
- **FHIR**: Connects to a FHIR server for healthcare data in a standardized format

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- Access to a SQL database or FHIR server

### Running the API
1. Navigate to the Phoenix-AzureAPI directory
2. Run `dotnet build` to build the project
3. Run `dotnet run` to start the API server
4. The API will be available at http://localhost:5300

## Configuration
The API can be configured through the `appsettings.json` file:
- `ConnectionStrings`: Database connection strings
- `DataSourceSettings`: Configure which data source to use (SQL or FHIR)
- `FhirSettings`: FHIR server configuration

## Development
- The API uses ASP.NET Core with .NET 9.0
- Controllers follow RESTful design principles
- Repository pattern is used for data access abstraction
