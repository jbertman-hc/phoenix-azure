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
- Validate FHIR resources against standard specifications
- Receive detailed feedback on validation issues with severity indicators
- Automatic validation of FHIR resources upon loading
- User-friendly loading states with detailed progress information

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
- Includes validation capabilities to ensure FHIR compliance

## Quick Start Guide

### Prerequisites
- Modern web browser (Chrome, Firefox, Edge, etc.)
- .NET 6.0 SDK (for running the API server)
- Python 3.x (for running the client server)
- Internet connection to access the Azure API

### Fastest Way to Run the Application

Use the provided start script to launch both the API server and client server in one command:

```bash
./start.sh
```

This script will:
1. Start the API server on port 5300
2. Start the client server on port 8080
3. Open the application in your default browser
4. Display the PIDs of both servers for easy termination

The application will be accessible at:
- SQL Explorer: http://localhost:8080/index.html
- FHIR Explorer: http://localhost:8080/fhir-explorer.html

Press Ctrl+C in the terminal to stop all servers.

### Manual Setup (Alternative)

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

1. Start the application using `./start.sh`
2. Open the FHIR Explorer at `http://localhost:8080/fhir-explorer.html`
3. Click the "Capability Statement" button to see the FHIR metadata
4. Click the "Patient" button to explore patient resources:
   - Leave the ID field empty and click "Fetch Resource" to get all patients
   - Enter a specific ID (e.g., "1036") and click "Fetch Resource" to get a single patient
5. Once a resource is loaded, click the "Validate Resource" button to check its compliance with FHIR standards
6. View the validation results, including any errors or warnings

## FHIR Validation

The Phoenix-Azure application includes robust FHIR validation capabilities:

1. **Validation Features**:
   - Validate any FHIR resource against standard profiles
   - Check for required fields, correct data types, and structural constraints
   - Receive detailed feedback on validation issues

2. **How to Use Validation**:
   - Load a FHIR resource in the FHIR Explorer
   - Click the "Validate Resource" button
   - View validation results in the "Validation Results" section
   - Issues are color-coded by severity (red for errors, yellow for warnings, blue for information)

3. **Validation API Endpoint**:
   - `POST /api/fhir/$validate` - Validate a FHIR resource
   - Request body should contain the FHIR resource as JSON
   - Response is a FHIR OperationOutcome resource with validation results

## FHIR Validation Implementation

The Phoenix-Azure application includes comprehensive FHIR validation capabilities to ensure that generated resources conform to the FHIR standard specifications. This section details how the validation functionality works.

### Validation Architecture

The FHIR validation implementation consists of three main components:

1. **Backend Validation Service**:
   - Uses the Firely SDK (Hl7.Fhir.R4) to validate FHIR resources against standard profiles
   - Implements resource parsing with type-specific handling based on the `resourceType` property
   - Returns validation results as FHIR OperationOutcome resources
   - Ensures proper error handling and detailed feedback

2. **Client-Side Integration**:
   - Provides a "Validate Resource" button in the FHIR Explorer UI
   - Prepares resources for validation by ensuring they have the required properties
   - Automatically fixes common issues like missing `resourceType` properties
   - Integrates with external validators for comprehensive validation

3. **External Validator Integration**:
   - Copies the current FHIR resource to the clipboard
   - Opens the Inferno validator (https://inferno.healthit.gov/validator/) in a new tab
   - Allows pasting the resource for immediate validation
   - Provides access to official FHIR validation tools

### FHIR Resource Structure

The application ensures that all FHIR resources follow the standard structure requirements:

1. **Bundle Resources**:
   - Include a self link for searchsets (required by FHIR spec)
   - Use absolute URLs for `fullUrl` values instead of relative paths
   - Ensure unique `fullUrl` values with version IDs for duplicate resources
   - Include search modes for entries in searchset bundles
   - Add timestamps to indicate when the Bundle was created

2. **Patient Resources**:
   - Include required fields like `resourceType`, `id`, and `name`
   - Format dates according to FHIR specifications
   - Generate narrative text summaries for human readability
   - Map domain model properties to their FHIR equivalents

### Data Flow

The FHIR validation process follows this data flow:

1. The client loads a FHIR resource from the backend API
2. The backend fetches data from the external API as POCOs (Plain Old CLR Objects)
3. These POCOs are mapped to FHIR resources using the PatientFhirMapper
4. The FHIR resources are serialized to JSON and sent back to the client
5. When validation is requested, the resource is prepared for validation
6. The resource is copied to the clipboard and the Inferno validator is opened
7. The user pastes the resource into the validator to check for compliance

### Using the Validation Feature

To validate a FHIR resource:

1. Open the FHIR Explorer at http://localhost:8080/fhir-explorer.html
2. Load a FHIR resource (Patient or Bundle)
3. Click the "Validate Resource" button
4. When the Inferno validator opens in a new tab, paste the resource (Ctrl+V or Cmd+V)
5. Click the "Validate" button on the Inferno site
6. Review the validation results to identify any compliance issues

This validation workflow ensures that all FHIR resources generated by the Phoenix-Azure application conform to the FHIR standard specifications, supporting healthcare interoperability.

### Implementation Notes

- The application uses Firely SDK version 5.11.4 for FHIR R4 (Release 4)
- The validation process handles both individual resources and Bundles
- Resources missing the `resourceType` property are automatically fixed before validation
- The system provides clear feedback on validation errors with severity indicators

## Implementation Details

The FHIR Explorer is configured to connect directly to the local API server at `http://localhost:5300/api/fhir`, which acts as a proxy to the Azure API.

## Detailed Documentation

For more detailed information about the FHIR integration, please refer to:
- [FHIR Integration Guide](/Phoenix-AzureAPI/Documentation/FHIR-Integration-Guide.md)

## Current Status
- **Index Page**: Fully functional with working search functionality that filters in real-time
- **FHIR Integration**: Fully functional with working endpoints and client-side explorer
- **FHIR Validation**: Fully implemented with support for validating Patient resources

## API Endpoints

### FHIR Endpoints
- `GET /api/fhir/metadata` - Get FHIR capability statement
- `GET /api/fhir/Patient` - Get all patients as FHIR resources
- `GET /api/fhir/Patient/{id}` - Get a specific patient as a FHIR resource
- `POST /api/fhir/$validate` - Validate a FHIR resource

### Patient Endpoints
- `GET /api/Patient` - Get all patients
- `GET /api/Patient/{id}` - Get patient by ID

## Troubleshooting
- If you encounter CORS issues, this is because the Azure API might have CORS restrictions
- If patient data doesn't load, check the network tab in your browser's developer tools
- If the client server fails to start, ensure port 8080 is not in use by another application
- If validation fails with a 400 error, ensure your request uses Content-Type: application/json

## Future Enhancements
- Authentication and authorization
- Additional FHIR resource types (Observation, Condition, etc.)
- Enhanced search capabilities for FHIR resources
- Improved error handling and user feedback
- Extended validation profiles for additional FHIR resources