# Phoenix FHIR API

A comprehensive FHIR transformation layer for healthcare data, enabling real-time mapping between legacy healthcare systems and FHIR resources.

## Project Overview

This project provides a complete FHIR transformation layer that:

1. Maps legacy healthcare data to standard FHIR resources
2. Supports real-time transformation of data
3. Enables CRUD operations on FHIR resources
4. Validates FHIR resources against the standard
5. Serves as a foundation for modern healthcare applications

## Architecture

The project follows a clean architecture with clear separation of concerns:

- **Controllers**: API endpoints for FHIR resources
- **Services**: Business logic and data retrieval
- **Mappers**: Transform legacy data to FHIR resources
- **Validators**: Ensure FHIR compliance
- **Models**: Data structures for both legacy and FHIR resources

## FHIR Resources

This API supports the following FHIR resources:

- Patient
- Practitioner
- Organization
- Location
- AllergyIntolerance
- Condition
- MedicationStatement
- DocumentReference
- Observation
- Appointment
- PractitionerRole
- Bundle

## Getting Started

### Prerequisites

- .NET 6.0 or higher
- Access to legacy healthcare API endpoints

### Installation

1. Clone the repository
2. Configure the connection to your legacy healthcare API in `appsettings.json`
3. Run the application

```bash
dotnet restore
dotnet run
```

## Usage

The API provides RESTful endpoints for all supported FHIR resources:

- `GET /api/fhir/Patient/{id}` - Get a patient by ID
- `GET /api/fhir/Practitioner/{id}` - Get a practitioner by ID
- `GET /api/fhir/Organization/{id}` - Get an organization by ID
- `GET /api/fhir/Bundle/Patient/{id}` - Get a patient bundle with all related resources

All endpoints support standard FHIR operations and return valid FHIR resources.

## Development Roadmap

1. Implement core FHIR resource mappers
2. Add validation for all FHIR resources
3. Implement CRUD operations
4. Add search capabilities
5. Implement FHIR operations (e.g., $everything)
6. Add authentication and authorization
7. Implement subscription capabilities

## License

This project is licensed under the MIT License - see the LICENSE file for details.
