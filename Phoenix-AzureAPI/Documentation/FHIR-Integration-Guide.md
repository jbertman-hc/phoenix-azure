# Phoenix-Azure FHIR Integration Guide

## Overview

This document provides a comprehensive guide to the FHIR integration in the Phoenix-Azure project. The integration allows the system to expose patient data from an Azure SQL database as FHIR-compliant resources, enabling interoperability with other healthcare systems.

## Architecture

The Phoenix project implements a flexible data source strategy that allows switching between SQL and FHIR data sources:

1. **Three-tier architecture**:
   - **Presentation Layer**: HTML/JavaScript client
   - **Business Logic Layer**: Azure API service
   - **Data Access Layer**: Abstracted repositories that can connect to either SQL or FHIR

2. **Key architectural components**:
   - Repository interfaces define common data access methods
   - Concrete implementations (SqlPatientRepository and FhirPatientRepository) handle specific data source interactions
   - A factory pattern (RepositoryFactory) creates the appropriate repository based on configuration
   - Environment variables control which data source is used (DataSourceSettings__Source)

3. **FHIR Integration Components**:
   - **FhirController**: Exposes FHIR-compliant RESTful endpoints
   - **PatientFhirMapper**: Maps between domain Patient model and FHIR Patient resources
   - **FhirService**: Handles serialization, deserialization, and validation of FHIR resources

## Data Flow

1. **Data Source (Azure SQL Database)**
   - Patient data is stored in Azure SQL tables with fields like `PatientID`, `First`, `Last`, etc.
   - The `Patient` model in `Models/Patient.cs` represents this SQL data structure

2. **Data Access Layer**
   - `PatientDataService` retrieves data from the Azure SQL database via the Azure API
   - It makes API calls to various repositories (Demographics, MedicalRecords, etc.)
   - This service aggregates comprehensive patient data from multiple sources

3. **Mapping Layer (SQL → FHIR)**
   - `PatientFhirMapper` transforms the SQL database model to FHIR resources
   - It maps fields like:
     - `PatientID` → FHIR `Patient.id`
     - `First/Last` → FHIR `Patient.name`
     - `BirthDate` → FHIR `Patient.birthDate`
     - `Gender` → FHIR `AdministrativeGender`
     - `PatientAddress/City/State/Zip` → FHIR `Patient.address`
     - `Phone/Email` → FHIR `Patient.telecom`

4. **FHIR Service Layer**
   - `FhirService` handles serialization, deserialization, and validation of FHIR resources
   - Ensures the mapped resources conform to the FHIR specification

5. **API Layer (RESTful Endpoints)**
   - `FhirController` exposes standardized FHIR endpoints:
     - `GET /api/fhir/Patient/{id}` - Get a single patient
     - `GET /api/fhir/Patient` - Get all patients
     - `GET /api/fhir/metadata` - Get capability statement

6. **Response Format**
   - All responses are formatted as standard FHIR JSON (`application/fhir+json`)
   - This makes the API compatible with any FHIR-compliant system

## Running the POC

### Prerequisites

- Python 3 (for running the client application)
- Modern web browser (Chrome, Firefox, Edge, etc.)
- Internet connection to access the Azure API

### Step 1: Clone the Repository

```bash
git clone https://github.com/your-org/phoenix-azure.git
cd phoenix-azure
```

### Step 2: Run the Client Application

```bash
# Navigate to the client project directory
cd Phoenix-AzureClient

# Start a simple HTTP server to serve the client files
python3 -m http.server 8080
```

The client application will be available at `http://localhost:8080`.

### Step 3: Access the FHIR Explorer

Open your browser and navigate to:
```
http://localhost:8080/fhir-explorer.html
```

This will open the FHIR Explorer interface where you can:
1. View the FHIR Capability Statement by clicking the "Capability Statement" button
2. Explore Patient resources by clicking the "Patient" button
3. View individual patients by entering a patient ID (the default is "1001") and clicking "Fetch Resource"
4. View all patients by leaving the ID field empty and clicking "Fetch Resource"
5. See a list of available patient IDs in the sidebar

The FHIR Explorer automatically loads all patients when the page is first opened, providing an immediate view of the data.

### Step 4: Explore Other Client Features

The client application includes the following features:
- `http://localhost:8080/index.html` - SQL Explorer (direct SQL data access)
- `http://localhost:8080/fhir-explorer.html` - FHIR Explorer (FHIR-compliant resources)

## Implementation Details

### API Connection via Local Proxy

The FHIR Explorer is configured to connect to a local API server that acts as a proxy to the Azure API endpoint:

```javascript
// Configuration in fhir-explorer.js
const API_BASE_URL = 'http://localhost:5300/api/fhir';
const DEFAULT_PATIENT_ID = '1001';
```

This architecture resolves CORS issues while ensuring that the FHIR Explorer works with actual SQL data:

1. The client (FHIR Explorer) makes requests to the local API server
2. The local API server handles these requests and proxies them to the Azure API
3. The local API server retrieves real patient data from the Azure SQL database
4. The data is mapped to FHIR resources using the PatientFhirMapper
5. The FHIR-compliant responses are returned to the client

### Default Patient ID and Empty Search Handling

The FHIR Explorer is configured with a default patient ID of 1001:

```javascript
// Set default patient ID when the page loads
if (resourceIdInput) {
    resourceIdInput.value = DEFAULT_PATIENT_ID;
}

// Reset to default ID when switching back to Patient resource type
resourceTypeSelect.addEventListener('change', () => {
    selectedResourceType = resourceTypeSelect.value;
    
    // Clear resource ID if not Patient
    if (selectedResourceType !== 'Patient') {
        resourceIdInput.value = '';
    } else {
        resourceIdInput.value = DEFAULT_PATIENT_ID;
    }
});
```

The Explorer also supports searching for all patients when no ID is provided:

```javascript
async function fetchResource(resourceType, resourceId = '') {
    // ...
    let url = `${API_BASE_URL}/${resourceType}`;
    if (resourceId) {
        url += `/${resourceId}`;
    } else {
        // Fetch all resources of the specified type when no ID is provided
    }
    // ...
}
```

### FHIR Explorer vs. Index Page

The Phoenix-Azure application includes two main interfaces for viewing patient data:

1. **Index Page** (`index.html`):
   - Provides a traditional healthcare application interface
   - Displays patient data in a user-friendly tabular format
   - Focuses on clinical workflow and patient management
   - Uses the regular API endpoint (`/api/Patient`)
   - Returns data in a simple JSON format optimized for UI rendering

2. **FHIR Explorer** (`fhir-explorer.html`):
   - Provides a technical interface for exploring FHIR resources
   - Displays patient data in FHIR-compliant JSON format
   - Focuses on interoperability and standards compliance
   - Uses a hybrid approach:
     - Fetches data from the regular API endpoint (`/api/Patient`)
     - Transforms the data into FHIR-compliant format for display
     - Supports capability statements and other FHIR-specific features

#### Data Flow Comparison

**Index Page Data Flow:**
```
Client Request → API Server (/api/Patient) → PatientDataService → Azure API → SQL Database → Simple JSON Response → UI Rendering
```

**FHIR Explorer Data Flow:**
```
Client Request → API Server (/api/Patient) → PatientDataService → Azure API → SQL Database → Simple JSON Response → Client-side FHIR Transformation → FHIR JSON Display
```

This hybrid approach ensures that the FHIR Explorer works reliably while still providing the FHIR-compliant view that is essential for healthcare interoperability standards.

#### Implementation Details

The FHIR Explorer uses a client-side transformation function to convert regular patient data into FHIR format:

```javascript
function convertPatientToFhir(patient) {
    return {
        resourceType: "Patient",
        id: patient.patientID.toString(),
        meta: {
            profile: ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
        },
        name: [{
            use: "official",
            family: patient.last || "",
            given: [patient.first || ""]
        }],
        gender: patient.gender ? patient.gender.toLowerCase() : "unknown",
        birthDate: patient.birthDate || "",
        address: [{
            line: [patient.patientAddress || ""],
            city: patient.city || "",
            state: patient.state || "",
            postalCode: patient.zip || ""
        }],
        telecom: [
            {
                system: "phone",
                value: patient.phone || "",
                use: "home"
            },
            {
                system: "email",
                value: patient.email || "",
                use: "home"
            }
        ]
    };
}
```

This transformation ensures that the data displayed in the FHIR Explorer adheres to the FHIR standard, even though it's fetched from the regular API endpoint.

### PatientDataService

The `PatientDataService` retrieves patient data from the Azure API:

```csharp
public async Task<Patient> GetPatientByIdAsync(int id)
{
    var client = _httpClientFactory.CreateClient();
    var url = $"{_apiBaseUrl}/Patient/{id}";
    
    var response = await client.GetAsync(url);
    
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var patient = JsonSerializer.Deserialize<Patient>(content, options);
        return patient;
    }
    
    throw new Exception($"Failed to retrieve patient with ID {id}");
}
```

### FhirController

The `FhirController` uses the `PatientDataService` to retrieve patient data and then maps it to FHIR resources:

```csharp
[HttpGet("Patient/{id}")]
[Produces("application/fhir+json")]
public async Task<IActionResult> GetPatient(int id)
{
    // Get the patient from the database
    var patient = await GetPatientFromDatabase(id);
    
    // Map to FHIR Patient
    var fhirPatient = _patientMapper.MapToFhir(patient);
    
    // Serialize to JSON
    var json = _fhirService.SerializeToJson(fhirPatient);
    
    return Content(json, "application/fhir+json");
}
```

## Testing the API Directly

You can also test the FHIR endpoints directly using curl:

```bash
# Get capability statement
curl -X GET "https://apiserviceswin20250318.azurewebsites.net/api/fhir/metadata" -H "accept: application/fhir+json"

# Get all patients
curl -X GET "https://apiserviceswin20250318.azurewebsites.net/api/fhir/Patient" -H "accept: application/fhir+json"

# Get patient by ID
curl -X GET "https://apiserviceswin20250318.azurewebsites.net/api/fhir/Patient/1036" -H "accept: application/fhir+json"
```

## Troubleshooting

### Common Issues

1. **CORS issues**:
   - If you encounter CORS errors, this is because the Azure API might have CORS restrictions.
   - You may need to use a CORS proxy or browser extension to bypass these restrictions.

2. **Patient data not appearing in FHIR format**:
   - Verify that the PatientFhirMapper is correctly mapping between domain model and FHIR resources.
   - Check the network tab in your browser's developer tools to ensure the API requests are successful.

3. **FHIR validation errors**:
   - Ensure that the mapped FHIR resources conform to the FHIR specification.
   - Check the FhirService.Validate method for any validation issues.

4. **Network connectivity issues**:
   - Ensure you have a stable internet connection to access the Azure API.
   - Check if the Azure API endpoint is accessible from your network.

## References

- [HL7 FHIR Documentation](https://www.hl7.org/fhir/)
- [Firely .NET SDK Documentation](https://docs.fire.ly/projects/Firely-NET-SDK/en/latest/)
- [Azure API for FHIR Documentation](https://docs.microsoft.com/en-us/azure/healthcare-apis/fhir/)
