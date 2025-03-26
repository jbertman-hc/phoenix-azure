# Phoenix-Azure Application

## Overview
Phoenix-Azure is a comprehensive healthcare application that connects to a remote API at `apiserviceswin20250318.azurewebsites.net/api/` to display patient information, medical records, and provides a Repository Explorer tool for interacting with various data repositories. The application now includes FHIR integration, allowing it to transform SQL data into FHIR-compliant resources for healthcare interoperability.

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

### FHIR Integration (New)
- Transform SQL database data into FHIR-compliant resources
- Access standardized FHIR endpoints for healthcare interoperability
- Explore FHIR resources using the FHIR Explorer interface
- View capability statements and patient resources in FHIR format

## Data Sources

**IMPORTANT NOTE**: The application connects directly to the Azure API at `apiserviceswin20250318.azurewebsites.net/api` for all data, including FHIR resources. This ensures that you're working with real patient data from the Azure SQL database.

The FHIR integration demonstrates how the application can transform data from an Azure SQL database into FHIR-compliant resources, following a flexible data source strategy that allows switching between SQL and FHIR data sources.

## Architecture

The Phoenix project implements a flexible data source strategy:

1. **Three-tier architecture**:
   - **Presentation Layer**: HTML/JavaScript client
   - **Business Logic Layer**: Azure API service
   - **Data Access Layer**: Abstracted repositories that can connect to either SQL or FHIR

2. **FHIR Integration Components**:
   - **FhirController**: Exposes FHIR-compliant RESTful endpoints
   - **PatientFhirMapper**: Maps between domain Patient model and FHIR resources
   - **FhirService**: Handles serialization, deserialization, and validation of FHIR resources

## Quick Start Guide

### Prerequisites
- Modern web browser (Chrome, Firefox, Edge, etc.)
- Python 3.x (for running the client server)
- Internet connection to access the Azure API

### Running the Application

#### Step 1: Start the Client Server
```bash
cd Phoenix-AzureClient
python3 -m http.server 8080
```

The client will be available at `http://localhost:8080`.

#### Step 2: Access the Application
- Main Patient Portal: `http://localhost:8080/index.html`
- Repository Explorer: `http://localhost:8080/repository-explorer.html`
- Comprehensive Patient View: `http://localhost:8080/patient-comprehensive.html`
- FHIR Explorer: `http://localhost:8080/fhir-explorer.html`

## FHIR Integration Demo

To see the FHIR integration in action:

1. Start the client server as described above
2. Open the FHIR Explorer at `http://localhost:8080/fhir-explorer.html`
3. Click the "Capability Statement" button to see the FHIR metadata
4. Click the "Patient" button to explore patient resources:
   - Leave the ID field empty and click "Fetch Resource" to get all patients
   - Enter a specific ID (e.g., "1036") and click "Fetch Resource" to get a single patient

## Implementation Details

The FHIR Explorer is configured to connect directly to the Azure API endpoint:

```javascript
// Configuration in fhir-explorer.js
const API_BASE_URL = 'https://apiserviceswin20250318.azurewebsites.net/api/fhir';
```

This allows the client to retrieve real patient data from the Azure SQL database and transform it into FHIR resources without needing to run a local API server.

## Detailed Documentation

For more detailed information about the FHIR integration, please refer to:
- [FHIR Integration Guide](/Phoenix-AzureAPI/Documentation/FHIR-Integration-Guide.md)

## Current Status
- **Index Page**: Fully functional with working search functionality that filters in real-time
- **Repository Explorer**: UI is complete, but backend integration is still in progress
- **Comprehensive Patient View**: UI is complete with consistent styling, but data aggregation is still in development
- **FHIR Integration**: Fully functional with working endpoints and client-side explorer

## API Endpoints

### FHIR Endpoints
- `GET /api/fhir/metadata` - Get FHIR capability statement
- `GET /api/fhir/Patient` - Get all patients as FHIR resources
- `GET /api/fhir/Patient/{id}` - Get a specific patient as a FHIR resource

### Patient Endpoints
- `GET /api/Patient` - Get all patients
- `GET /api/Patient/{id}` - Get patient by ID

### Repository Explorer Endpoints
- `GET /api/RepositoryExplorer/repositories` - Get all available repositories
- `GET /api/RepositoryExplorer/explore/{repository}/{method}` - Execute a repository method

## Troubleshooting
- If you encounter CORS issues, this is because the Azure API might have CORS restrictions
- If patient data doesn't load, check the network tab in your browser's developer tools
- If the client server fails to start, ensure port 8080 is not in use by another application

## Future Enhancements
- Authentication and authorization
- Additional FHIR resource types (Observation, Condition, etc.)
- Enhanced search capabilities for FHIR resources
- Improved error handling and user feedback