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

### Comprehensive Patient View
- Access complete patient information aggregated from multiple repositories
- View data in a structured, tabular format
- Explore the repository map to understand data sources

## Data Sources

**IMPORTANT NOTE**: Currently, only the front index patient list retrieves real data from the Azure API. All other features (Repository Explorer, comprehensive patient view, medical records, etc.) use mock data due to CORS limitations with the Azure API. This is a temporary solution until the CORS issues are resolved.

## Current Status
- **Index Page**: Fully functional with working search functionality that filters in real-time
- **Repository Explorer**: UI is complete, but backend integration is still in progress
- **Comprehensive Patient View**: UI is complete with consistent styling, but data aggregation is still in development

## Quick Start Guide

### Prerequisites
- .NET 9.0 SDK or later
- Modern web browser (Chrome, Firefox, Edge, etc.)
- Python 3.x (for running the client server)

### Running the Application

#### Step 1: Start the API Server
```bash
cd Phoenix-AzureAPI
dotnet run
```
The API will be available at http://localhost:5300

#### Step 2: Start the Client Server
```bash
cd Phoenix-AzureClient
python -m http.server 8081
```
The client will be available at http://localhost:8081

#### Step 3: Access the Application
Open your browser and navigate to http://localhost:8081

### Testing the Application
1. **Patient List**: On the index page, use the search box to filter patients in real-time
2. **Repository Explorer**: Navigate to the Repository Explorer page to view available repositories
3. **Comprehensive Patient View**: Enter a patient ID (e.g., 1036) to view aggregated patient data

## Architecture
The application follows a three-tier architecture:
- **Presentation Layer**: HTML/JavaScript client and API controllers
- **Business Logic Layer**: Service classes and domain models
- **Data Access Layer**: Repository interfaces with SQL and FHIR implementations

The architecture implements a flexible data source strategy that allows switching between SQL and FHIR data sources:
- Repository interfaces define common data access methods
- Concrete implementations handle specific data source interactions
- A factory pattern creates the appropriate repository based on configuration

## API Endpoints

### Patient Controller
- `GET /api/Patient`: Get a list of all patients
- `GET /api/Patient/{id}`: Get details for a specific patient
- `GET /api/Patient/{id}/MedicalRecords`: Get medical records for a specific patient

### Repository Explorer Controller
- `GET /api/RepositoryExplorer/available`: Get a list of available repositories
- `GET /api/RepositoryExplorer/explore/{repositoryName}`: Explore a specific repository
- `GET /api/RepositoryExplorer/explore/{repositoryName}/{methodName}`: Execute a method on a repository

### Patient Data Controller
- `GET /api/PatientData/comprehensive/{patientId}`: Get comprehensive patient data from all repositories
- `GET /api/PatientData/repositories-map`: Get a map of all repositories needed for patient data

## Development
- The API uses ASP.NET Core with .NET 9.0
- The client uses vanilla JavaScript with Bootstrap 5
- Repository pattern is used for data access abstraction
- CORS is configured to allow cross-origin requests between client and API

## Troubleshooting
- If you encounter CORS issues, ensure the API server is running and has CORS properly configured
- If patient data doesn't load, check the API connection in the browser console
- If the client server fails to start, ensure port 8081 is not in use by another application

## Future Enhancements
- Authentication and authorization
- Patient data editing capabilities
- Advanced filtering and reporting
- Integration with additional healthcare systems