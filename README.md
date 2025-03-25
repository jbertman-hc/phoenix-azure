# Phoenix-Azure Application

## Overview
Phoenix-Azure is a comprehensive healthcare application that connects to a remote API at `apiserviceswin20250318.azurewebsites.net/api/` to display patient information, medical records, and provides a Repository Explorer tool for interacting with various data repositories.

## Project Structure
- **Phoenix-AzureAPI**: ASP.NET Core API that serves as the backend
- **Phoenix-AzureClient**: HTML/JavaScript client application
- **DataAccess**: Data access layer with repository implementations

## Features

### Patient Management
- View a list of all patients with search and sort capabilities
- View detailed patient information including demographics and contact details
- Access patient medical records and history

### Repository Explorer
- Browse available data repositories and their methods
- View repository schemas and sample data
- Execute repository methods with parameters and view results
- Gain visibility into the data model and capabilities of the system

## Architecture
The application follows a three-tier architecture:
- **Presentation Layer**: HTML/JavaScript client and API controllers
- **Business Logic Layer**: Service classes and domain models
- **Data Access Layer**: Repository interfaces with SQL and FHIR implementations

The architecture implements a flexible data source strategy that allows switching between SQL and FHIR data sources:
- Repository interfaces define common data access methods
- Concrete implementations handle specific data source interactions
- A factory pattern creates the appropriate repository based on configuration

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- Modern web browser (Chrome, Firefox, Edge, etc.)
- Access to the remote API at `apiserviceswin20250318.azurewebsites.net/api/`

### Running the Application
1. Start the API server:
   ```
   cd Phoenix-AzureAPI
   dotnet run
   ```
   The API will be available at http://localhost:5300

2. Start the client server:
   ```
   cd Phoenix-AzureClient
   python -m http.server 8080
   ```
   The client will be available at http://localhost:8080

## API Endpoints

### Patient Controller
- `GET /api/Patient`: Get a list of all patients
- `GET /api/Patient/{id}`: Get details for a specific patient
- `GET /api/Patient/{id}/MedicalRecords`: Get medical records for a specific patient

### Repository Explorer Controller
- `GET /api/RepositoryExplorer/available`: Get a list of available repositories
- `GET /api/RepositoryExplorer/explore/{repositoryName}`: Explore a specific repository
- `GET /api/RepositoryExplorer/explore/{repositoryName}/{methodName}`: Execute a method on a repository

## Data Sources
The application can connect to different data sources:
- **Remote API**: Connects to `apiserviceswin20250318.azurewebsites.net/api/` for patient data
- **SQL Database**: Uses repository implementations in the DataAccess layer
- **FHIR Server**: Can connect to FHIR endpoints for standardized healthcare data

## Development
- The API uses ASP.NET Core with .NET 9.0
- The client uses vanilla JavaScript with Bootstrap 5
- Repository pattern is used for data access abstraction
- CORS is configured to allow cross-origin requests between client and API

## Future Enhancements
- Authentication and authorization
- Patient data editing capabilities
- Advanced filtering and reporting
- Integration with additional healthcare systems

# Phoenix-Azure

A modern healthcare application that demonstrates integration between a .NET Core API and a JavaScript client, showcasing repository pattern implementation and flexible data source strategy.

## Project Overview

Phoenix-Azure is a healthcare application that implements a three-tier architecture:

1. **Presentation Layer**: JavaScript client with HTML/CSS frontend
2. **Business Logic Layer**: .NET Core API service (port 5300)
3. **Data Access Layer**: Abstracted repositories that can connect to either SQL or FHIR data sources

The application provides a Repository Explorer that allows users to browse and interact with various repositories in the system.

## Key Features

- **Repository Explorer**: Browse and interact with all available repositories
- **Flexible Data Source Strategy**: Switch between SQL and FHIR data sources
- **Patient Portal**: Access patient data from remote API endpoints
- **Modern UI**: Clean, responsive interface built with Bootstrap

## Architecture

The Phoenix project implements a flexible data source strategy that allows switching between SQL and FHIR data sources:

1. Repository interfaces define common data access methods
2. Concrete implementations (SqlPatientRepository and FhirPatientRepository) handle specific data source interactions
3. A factory pattern (RepositoryFactory) creates the appropriate repository based on configuration
4. Environment variables control which data source is used (DataSourceSettings__Source)

## Project Structure

- **Phoenix-AzureAPI/**: .NET Core API service
  - **Controllers/**: API endpoints for repository exploration and patient data
  - **Models/**: Data models for patients and repositories
  - **Services/**: Business logic services
  
- **Phoenix-AzureClient/**: JavaScript client application
  - HTML/CSS/JavaScript frontend
  - Repository explorer interface
  - Patient data visualization

## Getting Started

### Prerequisites

- .NET Core SDK 9.0 or later
- Node.js (for development tools)
- Web browser

### Running the Application

1. Start the API server:
   ```
   cd Phoenix-AzureAPI
   dotnet run
   ```

2. Start the client (using a simple HTTP server):
   ```
   cd Phoenix-AzureClient
   python3 -m http.server 8080
   ```

3. Open your browser and navigate to:
   ```
   http://localhost:8080
   ```

## API Endpoints

- `GET /api/RepositoryExplorer/available`: Get all available repositories
- `GET /api/RepositoryExplorer/{repositoryName}`: Get details for a specific repository
- `GET /api/RepositoryExplorer/{repositoryName}/methods`: Get methods for a specific repository
- `GET /api/RepositoryExplorer/explore/{repositoryName}/{methodName}`: Execute a method on a repository

## Configuration

The API service can be configured to use different data sources:

- Set `DataSourceSettings__Source` to `"SQL"` or `"FHIR"` to switch between data sources
- The FHIR server URL is configured to `http://phoenix-fhir:8080/fhir` by default

## License

This project is licensed under the MIT License - see the LICENSE file for details.