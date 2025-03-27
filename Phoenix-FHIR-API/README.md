# Phoenix FHIR API

A comprehensive FHIR R4 transformation layer for healthcare data, enabling real-time mapping between legacy healthcare systems and FHIR resources.

## Project Overview

This project provides a complete FHIR R4 transformation layer that:

1. Maps legacy healthcare data to standard FHIR R4 resources using the Firely SDK
2. Supports real-time transformation of data without using mock data
3. Enables CRUD operations on FHIR resources
4. Validates FHIR resources against the standard using both Firely SDK validation and secondary verification
5. Serves as a foundation for modern healthcare applications

## Core Focus

**Primary Focus**: Build out all FHIR R4 models using the Firely SDK (Hl7.Fhir.R4) with:
- Comprehensive mapping between SQL database fields and FHIR resources
- Incremental implementation approach (one resource type at a time)
- Thorough validation against FHIR standards
- No mock data - all mappings connect to real data sources

**Secondary Focus**: Implement additional verification mechanisms to ensure:
- Compliance with US Core Implementation Guide
- Proper handling of extensions and complex data types
- Consistent representation across different FHIR resources

## Architecture

The project follows a clean architecture with clear separation of concerns:

- **Controllers**: API endpoints for FHIR resources
- **Services**: Business logic and data retrieval from real sources
- **Mappers**: Transform legacy data to FHIR resources with field-level mapping
- **Validators**: Ensure FHIR compliance through multiple validation methods
- **Models**: Data structures for both legacy and FHIR resources

## FHIR Resources Implementation Plan

This API will implement the following FHIR resources in an incremental approach:

1. **Patient** (Initial Implementation)
   - Maps PatientID → Patient.id
   - Maps First/Last → Patient.name
   - Maps Gender → Patient.gender
   - Maps BirthDate → Patient.birthDate
   - Maps Address fields → Patient.address
   - Maps Phone/Email → Patient.telecom

2. **Future Resources** (In order of implementation):
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

Each resource will be thoroughly tested and validated before moving to the next one.

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
- `POST /api/fhir/Patient` - Create a new patient
- `PUT /api/fhir/Patient/{id}` - Update a patient
- `DELETE /api/fhir/Patient/{id}` - Delete a patient
- `GET /api/fhir/Patient/{id}/$everything` - Get a patient bundle with all related resources

As more resources are implemented, additional endpoints will be added.

## Development Roadmap

1. **Implement core FHIR resource mappers incrementally**
   - Focus on one resource type at a time
   - Ensure proper field-level mapping from SQL to FHIR
   - Validate each resource against FHIR standards

2. **Add comprehensive validation**
   - Primary validation using Firely SDK
   - Secondary verification for additional compliance checks
   - US Core profile validation where applicable

3. **Implement CRUD operations for all resources**
   - Connect to real data sources (no mock data)
   - Ensure proper error handling and validation

4. **Add search capabilities and additional FHIR operations**
   - Implement FHIR search parameters
   - Support operations like $everything

5. **Implement security and performance optimizations**
   - Add authentication and authorization
   - Optimize for performance and scalability

## License

This project is licensed under the MIT License - see the LICENSE file for details.
