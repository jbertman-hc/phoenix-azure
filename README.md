# Phoenix-Azure Application

## Overview
Phoenix-Azure is a healthcare application that connects to a remote API at `apiserviceswin20250318.azurewebsites.net/api/` to display patient information and medical records. The application includes FHIR integration, allowing it to transform SQL data into FHIR-compliant resources for healthcare interoperability.

## Project Structure
- **Phoenix-AzureAPI**: ASP.NET Core API that serves as the backend
- **Phoenix-AzureClient**: HTML/JavaScript client application
- **DataAccess**: Data access layer with repository implementations

## Features

### Patient Management
- View a list of all patients with search and sort capabilities
- View detailed patient information including demographics and contact details
- Access patient medical records and history

### FHIR Integration
- Transform SQL database data into FHIR-compliant resources
- Access standardized FHIR endpoints for healthcare interoperability
- Explore FHIR resources using the FHIR Explorer interface
- View capability statements and patient resources in FHIR format

## Data Sources

**IMPORTANT NOTE**: The application connects to the local API server at `http://localhost:5300/api`, which acts as a proxy to the Azure API at `apiserviceswin20250318.azurewebsites.net/api` for all data, including FHIR resources. This ensures that you're working with real patient data from the Azure SQL database while avoiding CORS issues.

The FHIR integration demonstrates how the application can transform data from an Azure SQL database into FHIR-compliant resources, following a flexible data source strategy that allows switching between SQL and FHIR data sources.

## Architecture

The Phoenix project implements a flexible data source strategy:

1. **Three-tier architecture**:
   - **Presentation Layer**: HTML/JavaScript client
   - **Business Logic Layer**: Local API server proxying to Azure API service
   - **Data Access Layer**: Abstracted repositories that can connect to either SQL or FHIR

2. **FHIR Integration Components**:
   - **FhirController**: Exposes FHIR-compliant RESTful endpoints
   - **PatientFhirMapper**: Maps between domain Patient model and FHIR resources
   - **FhirService**: Handles serialization, deserialization, and validation of FHIR resources

## Application Interfaces

The Phoenix-Azure application provides two main interfaces for accessing patient data:

### 1. SQL Explorer (index.html)
- Traditional healthcare application interface
- Displays patient data in a user-friendly tabular format directly from SQL
- Focuses on clinical workflow and patient management
- Uses the regular API endpoint (`/api/Patient`)
- Optimized for healthcare professionals and administrative staff

### 2. FHIR Explorer (fhir-explorer.html)
- Technical interface for exploring FHIR resources
- Displays patient data in FHIR-compliant JSON format
- Focuses on interoperability and standards compliance
- Uses the FHIR API endpoint (`/api/fhir`)
- Designed for developers and integration specialists

## Quick Start Guide

### Prerequisites
- Modern web browser (Chrome, Firefox, Edge, etc.)
- .NET 6.0 SDK (for running the API server)
- Python 3.x (for running the client server)
- Internet connection to access the Azure API

### Running the Application

#### Step 1: Start the API Server
```bash
cd Phoenix-AzureAPI
dotnet run
```

The API will be available at `http://localhost:5300`.

#### Step 2: Start the Client Server
```bash
cd Phoenix-AzureClient
python3 -m http.server 8080
```

The client will be available at `http://localhost:8080`.

#### Step 3: Access the Application
1. Open the SQL Explorer at `http://localhost:8080/index.html`
2. Open the FHIR Explorer at `http://localhost:8080/fhir-explorer.html`
3. Click the "Capability Statement" button to see the FHIR metadata
4. Click the "Patient" button to explore patient resources:
   - Leave the ID field empty and click "Fetch Resource" to get all patients
   - Enter a specific ID (e.g., "1036") and click "Fetch Resource" to get a single patient

## FHIR Integration Demo

To see the FHIR integration in action:

1. Start the API server as described above
2. Open the FHIR Explorer at `http://localhost:8080/fhir-explorer.html`
3. Click the "Capability Statement" button to see the FHIR metadata
4. Click the "Patient" button to explore patient resources:
   - Leave the ID field empty and click "Fetch Resource" to get all patients
   - Enter a specific ID (e.g., "1036") and click "Fetch Resource" to get a single patient

## Implementation Details

The FHIR Explorer is configured to connect directly to the local API server at `http://localhost:5300/api/fhir`, which acts as a proxy to the Azure API.

## Detailed Documentation

For more detailed information about the FHIR integration, please refer to:
- [FHIR Integration Guide](/Phoenix-AzureAPI/Documentation/FHIR-Integration-Guide.md)

## Current Status
- **Index Page**: Fully functional with working search functionality that filters in real-time
- **FHIR Integration**: Fully functional with working endpoints and client-side explorer

## API Endpoints

### FHIR Endpoints
- `GET /api/fhir/metadata` - Get FHIR capability statement
- `GET /api/fhir/Patient` - Get all patients as FHIR resources
- `GET /api/fhir/Patient/{id}` - Get a specific patient as a FHIR resource

### Patient Endpoints
- `GET /api/Patient` - Get all patients
- `GET /api/Patient/{id}` - Get patient by ID

## Troubleshooting
- If you encounter CORS issues, this is because the Azure API might have CORS restrictions
- If patient data doesn't load, check the network tab in your browser's developer tools
- If the client server fails to start, ensure port 8080 is not in use by another application

## Future Enhancements
- Authentication and authorization
- Additional FHIR resource types (Observation, Condition, etc.)
- Enhanced search capabilities for FHIR resources
- Improved error handling and user feedback