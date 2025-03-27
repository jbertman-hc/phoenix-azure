# Phoenix Healthcare Interoperability Platform

## Project Overview

The Phoenix Healthcare Interoperability Platform is a comprehensive solution designed to transform legacy healthcare data into FHIR-compliant resources, enabling seamless integration with modern healthcare systems. This repository contains multiple components that work together to retrieve, transform, and present healthcare data in standardized formats.

## Repository Structure

The repository consists of several key components:

### 1. Phoenix-FHIR-API

A standards-compliant FHIR R4 API that serves as the transformation layer between legacy data sources and FHIR-compatible systems.

**Key Features:**
- Retrieves real patient data from the Azure backend using multiple strategies
- Transforms legacy data models into standard FHIR R4 resources
- Implements comprehensive mapping between SQL database fields and FHIR resources
- Provides FHIR-compliant RESTful endpoints
- Validates resources against FHIR standards
- Uses the Firely SDK (Hl7.Fhir.R4) for FHIR implementation

**Implementation Details:**
- **Controllers**: Handle incoming requests and return appropriate FHIR resources
  - `PatientController`: Manages patient-related requests and transformations
  - `FhirController`: Provides general FHIR functionality and operations
- **Services**: Contain business logic and data access
  - `LegacyApiService`: Connects to the Azure backend to retrieve patient data
- **Mappers**: Transform legacy data models to FHIR resources
  - `PatientFhirMapper`: Converts demographic data to FHIR Patient resources
- **Validators**: Ensure FHIR compliance
  - `FhirResourceValidator`: Validates resources against FHIR specifications

### 2. FHIR-Visualizer (Main Client)

A modern, flexible web-based client application specifically designed to visualize and interact with FHIR resources.

**Key Features:**
- Displays multiple FHIR resource types in a user-friendly interface
- Provides both formatted human-readable and raw JSON views of resources
- Validates resources against FHIR standards
- Explores server metadata and capability statements
- Configurable to connect to different FHIR servers

**Implementation Details:**
- Built with HTML, CSS, and JavaScript
- Responsive design that works on desktop and mobile devices
- No dependencies on server-side rendering
- Communicates directly with the Phoenix-FHIR-API

### 3. DataAccess

A data access layer that provides repository implementations for connecting to various data sources.

**Key Features:**
- Implements the repository pattern for data access abstraction
- Supports both SQL and FHIR data sources
- Provides a consistent interface for data retrieval and manipulation

### 4. Phoenix-AzureAPI (Deprecated)

> **Note:** This component is deprecated in favor of the Phoenix-FHIR-API, which provides a more standards-compliant implementation.

An ASP.NET Core API that serves as the backend for the Phoenix-Azure application, connecting to the remote Azure API.

### 5. Phoenix-AzureClient (Deprecated)

> **Note:** This component is deprecated in favor of the FHIR-Visualizer, which provides a more comprehensive and flexible interface for FHIR resources.

A web-based client application that provides a user interface for interacting with the Phoenix-FHIR-API.

## Architecture and Data Flow

The Phoenix Healthcare Interoperability Platform implements a multi-tiered architecture:

### 1. Data Access Tier
- **Legacy API Integration**: Connects to existing healthcare endpoints at `apiserviceswin20250318.azurewebsites.net/api`
- **Repository Pattern**: Abstracts data access through repository interfaces
- **Data Retrieval Services**: Specialized services for fetching specific types of healthcare data

### 2. Transformation Tier
- **Mappers**: Dedicated components that transform legacy models to FHIR resources
- **Field-Level Transformations**: Explicit mapping between source and target fields
- **Extension Handling**: Proper management of FHIR extensions for non-standard data

### 3. Validation Tier
- **FHIR Validators**: Ensures all resources comply with FHIR R4 specifications
- **Business Rule Validation**: Additional validation for domain-specific rules
- **Error Handling**: Comprehensive error reporting and management

### 4. API Tier
- **RESTful Endpoints**: FHIR-compliant API endpoints
- **Resource Controllers**: Dedicated controllers for each FHIR resource type
- **Operation Support**: Implementation of standard FHIR operations

### 5. Presentation Tier
- **UI Components**: Modern web interface consuming FHIR resources
- **FHIR Client**: Communicates with the API tier using standard FHIR interactions

### Data Flow Process

1. **Request Initiation**: User interaction with the FHIR-Visualizer triggers a request
2. **API Handling**: The Phoenix-FHIR-API receives the request
3. **Data Retrieval**: The API retrieves data from the Azure backend using the LegacyApiService
4. **Transformation**: The retrieved data is transformed into FHIR resources using appropriate mappers
5. **Validation**: The FHIR resources are validated against FHIR standards
6. **Response**: The validated FHIR resources are returned to the client
7. **Presentation**: The FHIR-Visualizer displays the FHIR resources to the user

## Implementation Approach

The Phoenix Healthcare Interoperability Platform follows these key principles:

1. **Standards Compliance**: All FHIR resources are fully compliant with FHIR R4 specifications
2. **Real Data Sources**: The platform uses only real data sources, avoiding mock data
3. **Incremental Implementation**: Resources are implemented one type at a time, starting with Patient
4. **Comprehensive Mapping**: Each field in the legacy data is explicitly mapped to its FHIR equivalent
5. **Thorough Validation**: All resources are validated against FHIR standards before being returned

## Current Implementation Status

The platform currently supports the following FHIR resources:

1. **Patient**: Complete implementation with mapping from demographic data
   - Includes proper identifiers, names, gender, birth dates, addresses, and telecom information
   - Complies with US Core profile requirements

Future development will focus on implementing additional FHIR resources, including:
- Practitioner
- Organization
- Location
- Observation
- Condition
- Procedure
- MedicationRequest

## Getting Started

### Prerequisites
- .NET 6.0 or later
- Python 3.x (for running the FHIR-Visualizer)

### Running the Application
1. Start the Phoenix-FHIR-API:
   ```
   cd Phoenix-FHIR-API
   dotnet run
   ```

2. Start the FHIR-Visualizer:
   ```
   cd FHIR-Visualizer
   python -m http.server 8095
   ```

3. Access the FHIR-Visualizer at `http://localhost:8095`

## Conclusion

The Phoenix Healthcare Interoperability Platform provides a robust solution for transforming legacy healthcare data into FHIR-compliant resources. By following FHIR standards and best practices, the platform enables seamless integration with modern healthcare systems while maintaining compatibility with existing data sources.
