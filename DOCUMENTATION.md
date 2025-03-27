# Phoenix FHIR API Project Documentation

## Project Overview

The Phoenix FHIR API project is focused on creating a standards-compliant FHIR R4 API that maps legacy healthcare data from Azure to FHIR resources. The project includes:

1. **Phoenix-FHIR-API**: A .NET Core API that retrieves data from Azure and maps it to FHIR resources
2. **FHIR-Visualizer**: A web-based tool for viewing and validating FHIR resources

## Current Status

### Completed Features

1. **Patient Resource Implementation**:
   - Successfully mapped legacy patient data to FHIR Patient resources
   - Implemented proper identifiers, names, gender, birth dates, addresses, and telecom information
   - Ensured US Core profile compliance

2. **FHIR Visualizer**:
   - Created a web-based tool for viewing FHIR resources
   - Added raw JSON view for resource details
   - Implemented resource and bundle validation
   - Added loading indicators for better UX
   - Integrated third-party HAPI FHIR validation as a fallback

3. **Validation**:
   - Implemented local validation for individual resources
   - Added bundle validation functionality
   - Integrated HAPI FHIR Validator as a fallback for redundant validation
   - Ensured proper error handling and user feedback

### Technical Implementation

1. **Data Retrieval Strategy**:
   - First tries the PatientIndex endpoint to get patient IDs
   - Fetches demographics for each patient ID
   - Falls back to addendum records if needed

2. **FHIR Compliance**:
   - Uses Firely SDK for FHIR R4 implementation
   - Follows US Core Implementation Guide
   - Validates resources against FHIR standards using both local and third-party validators

## Next Steps

Following our incremental implementation approach, we will continue to implement additional FHIR resource types in this order:

1. **Practitioner Resources**:
   - Map provider data to FHIR Practitioner resources
   - Implement proper identifiers, names, and qualifications
   - Ensure US Core profile compliance

2. **Organization Resources**:
   - Map facility/practice data to FHIR Organization resources
   - Implement proper identifiers, names, and contact information
   - Create relationships between practitioners and organizations

3. **Location Resources**:
   - Map location data to FHIR Location resources
   - Implement proper identifiers, names, and address information
   - Link locations to organizations

4. **Observation Resources**:
   - Map clinical data to FHIR Observation resources
   - Implement proper codes, values, and references
   - Ensure proper linking to patients

## Project Focus

Throughout implementation, we will maintain our focus on:

1. **Using Real Data Sources**: No mock data, only real-time mappings from existing data
2. **FHIR R4 Compliance**: Following FHIR standards and US Core profiles
3. **Incremental Implementation**: One resource type at a time, thoroughly tested
4. **Validation**: Comprehensive validation against FHIR standards using multiple validation mechanisms

## Technical Details

- **API URL**: http://localhost:5301/api
- **Visualizer URL**: http://localhost:8095
- **Backend Data Source**: Azure SQL Database (https://apiserviceswin20250318.azurewebsites.net/api)
- **Third-Party Validator**: HAPI FHIR Validator (https://hapi.fhir.org/baseR4/$validate)

## Recent Changes

- Added third-party HAPI FHIR validation as a fallback for redundant validation
- Fixed validation functionality in the FHIR Visualizer
- Improved error handling for validation requests
- Updated UI to show raw JSON for resource details
- Added loading indicators for better user experience
- Changed API port from 5300 to 5301 to avoid conflicts
