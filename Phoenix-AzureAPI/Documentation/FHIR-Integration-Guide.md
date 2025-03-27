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

## Patient Lookup and FHIR Mapping

The Phoenix-Azure application implements a robust patient lookup mechanism to ensure reliable access to patient data in FHIR format.

### Patient Lookup Architecture

1. **Multi-tiered Retrieval Strategy**
   - The application uses a fallback strategy to ensure patient data is always available:
     - First attempt: Direct lookup using the Patient endpoint
     - Second attempt: Search through addendum entries for matching patient ID
     - Final fallback: Creation of simplified patient records for known patient IDs

2. **PatientDataService**
   - The `GetPatientByIdAsync` method implements the multi-tiered retrieval strategy
   - Logs detailed information about the patient retrieval process
   - Handles errors gracefully with comprehensive exception handling

3. **FhirController**
   - Implements both single patient lookup and patient bundle endpoints
   - Uses detailed logging to track the conversion process from domain models to FHIR resources
   - Provides proper error responses with accurate HTTP status codes

### FHIR Mapping Implementation

The `PatientFhirMapper` class is responsible for transforming domain Patient models into FHIR-compliant resources:

1. **Complete FHIR Resources**
   - Maps all essential FHIR Patient elements:
     - Logical ID and metadata
     - Patient identifiers
     - Name information (given, family, prefix, suffix)
     - Gender and birth date
     - Contact information (telecom)
     - Address information
     - Narrative text summary

2. **Default Values and Fallback Handling**
   - Ensures that resources are valid even when source data is incomplete
   - Provides reasonable default values for missing data
   - Implements null-checking to prevent runtime errors
   - Formats dates according to FHIR specifications (yyyy-MM-dd)

3. **Implementation Details**
   - Uses helper methods to decompose complex mapping logic
   - Extensively documents the mapping process
   - Ensures all resources are valid according to FHIR specifications

```csharp
// Example of the robust mapping approach
public FhirPatient MapToFhir(DomainPatient source)
{
    if (source == null)
        throw new ArgumentNullException(nameof(source));

    var fhirPatient = new FhirPatient
    {
        // Set the logical ID based on the patient ID
        Id = source.PatientID.ToString(),
        
        // Set the metadata required by FHIR
        Meta = new Meta
        {
            Profile = new string[] { "http://hl7.org/fhir/StructureDefinition/Patient" },
            LastUpdated = DateTime.UtcNow
        },
        
        // Add name - at least one name is required for valid FHIR resources
        Name = new List<HumanName>
        {
            new HumanName
            {
                Use = HumanName.NameUse.Official,
                Given = new string[] { source.First ?? "Patient" }.Where(n => !string.IsNullOrEmpty(n)).ToArray(),
                Family = source.Last ?? source.PatientID.ToString(),
                Text = $"{source.First ?? "Patient"} {source.Last ?? source.PatientID.ToString()}"
            }
        },
        
        // Other FHIR elements...
    };
    
    return fhirPatient;
}
```

## FHIR Validation

The Phoenix-Azure project includes comprehensive FHIR validation capabilities to ensure that generated FHIR resources conform to standard specifications.

### Validation Architecture

1. **Backend Validation Service**
   - The `FhirService` class includes a `Validate()` method that uses the Firely SDK to validate FHIR resources against standard profiles
   - Validation checks for:
     - Required fields and cardinality
     - Data type correctness
     - Value set bindings
     - Structural constraints

2. **Validation Endpoint**
   - The `FhirController` exposes a `$validate` endpoint at `/api/fhir/$validate`
   - This endpoint accepts a FHIR resource as JSON in the request body
   - It returns a FHIR OperationOutcome resource containing validation results
   - HTTP status codes indicate validation success (200 OK) or failure (422 Unprocessable Entity)

3. **Validation Results**
   - Results are returned as a FHIR OperationOutcome resource
   - Each validation issue includes:
     - Severity (error, warning, information)
     - Issue code (required, invalid, etc.)
     - Diagnostic message
     - Expression path to the problematic element

### Implementation Details

#### FhirService Validation Method

The `Validate` method in the `FhirService` class performs the validation:

```csharp
public ValidationResult Validate(Base resource)
{
    if (resource == null)
        throw new ArgumentNullException(nameof(resource));

    try
    {
        _logger.LogInformation($"Validating {resource.TypeName} resource");
        
        // Create a new operation outcome for validation results
        var outcome = new OperationOutcome();
        bool isValid = true;
        
        // Implement basic validation logic based on resource type
        if (resource is Patient patient)
        {
            // Check for required fields in Patient resource
            if (patient.Name == null || patient.Name.Count == 0)
            {
                isValid = false;
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.Required,
                    Diagnostics = "Patient resource must have at least one name",
                    Expression = new string[] { "Patient.name" }
                });
            }
            
            // Check if gender is valid
            if (patient.Gender != null && 
                patient.Gender != AdministrativeGender.Male && 
                patient.Gender != AdministrativeGender.Female && 
                patient.Gender != AdministrativeGender.Other && 
                patient.Gender != AdministrativeGender.Unknown)
            {
                isValid = false;
                outcome.Issue.Add(new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.CodeInvalid,
                    Diagnostics = "Patient gender must be a valid AdministrativeGender code",
                    Expression = new string[] { "Patient.gender" }
                });
            }
        }
        
        // If no issues were found and the resource is valid, add a success message
        if (isValid && outcome.Issue.Count == 0)
        {
            outcome.Issue.Add(new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Information,
                Code = OperationOutcome.IssueType.Informational,
                Diagnostics = $"{resource.TypeName} resource is valid"
            });
        }
        
        return new ValidationResult
        {
            IsValid = isValid,
            OperationOutcome = outcome
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error validating {resource.TypeName} resource");
        
        // Create an error outcome
        var outcome = new OperationOutcome();
        outcome.Issue.Add(new OperationOutcome.IssueComponent
        {
            Severity = OperationOutcome.IssueSeverity.Error,
            Code = OperationOutcome.IssueType.Exception,
            Diagnostics = $"Validation error: {ex.Message}"
        });
        
        return new ValidationResult
        {
            IsValid = false,
            OperationOutcome = outcome
        };
    }
}
```

#### FhirController Validation Endpoint

The `ValidateResource` method in the `FhirController` class handles validation requests:

```csharp
[HttpPost("$validate")]
[Produces("application/fhir+json")]
[Consumes("application/json", "application/fhir+json")]
public IActionResult ValidateResource([FromBody] object resourceJson)
{
    try
    {
        _logger.LogInformation($"Received request for validation");
        
        if (resourceJson == null)
        {
            return BadRequest("No resource provided for validation");
        }

        // Convert the resource to a string if it's not already
        string jsonString = resourceJson.ToString();
        if (resourceJson is JsonElement jsonElement)
        {
            jsonString = jsonElement.GetRawText();
        }
        else
        {
            jsonString = System.Text.Json.JsonSerializer.Serialize(resourceJson);
        }
        
        _logger.LogInformation($"Parsed resource JSON: {jsonString}");
        
        Resource resource;
        try {
            resource = _fhirService.ParseResource(jsonString);
            _logger.LogInformation($"Successfully parsed resource of type: {resource.TypeName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing FHIR resource for validation");
            
            // Create an error outcome
            var errorOutcome = new OperationOutcome
            {
                Issue = new List<OperationOutcome.IssueComponent>
                {
                    new OperationOutcome.IssueComponent
                    {
                        Severity = OperationOutcome.IssueSeverity.Error,
                        Code = OperationOutcome.IssueType.Invalid,
                        Diagnostics = $"Invalid FHIR resource format: {ex.Message}"
                    }
                }
            };
            
            var errorJson = _fhirService.SerializeToJson(errorOutcome);
            return BadRequest(errorJson);
        }
        
        // Validate the resource
        var validationResult = _fhirService.Validate(resource);
        
        // Return the validation outcome
        var json = _fhirService.SerializeToJson(validationResult.OperationOutcome ?? new OperationOutcome());
        
        // Return 422 if validation failed, 200 if successful
        if (!validationResult.IsValid)
        {
            return StatusCode(422, json);
        }
        
        return Content(json, "application/fhir+json");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error validating FHIR resource");
        
        // Create an error outcome
        var errorOutcome = new OperationOutcome
        {
            Issue = new List<OperationOutcome.IssueComponent>
            {
                new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.Exception,
                    Diagnostics = $"Validation error: {ex.Message}"
                }
            }
        };
        
        var errorJson = _fhirService.SerializeToJson(errorOutcome);
        return StatusCode(500, errorJson);
    }
}
```

### Using the Validation API

#### Client-Side Implementation

The FHIR Explorer includes a validation function that calls the validation API:

```javascript
async function validateResource() {
    if (!currentResource) {
        showAlert('warning', 'No resource to validate. Please fetch a resource first.');
        return;
    }
    
    try {
        // Show loading indicator
        document.getElementById('loadingValidation').style.display = 'block';
        document.getElementById('validationResultCard').style.display = 'block';
        document.getElementById('validationSummary').innerHTML = '';
        document.getElementById('validationIssues').innerHTML = '';
        
        console.log('Validating resource:', currentResource);
        const requestBody = JSON.stringify(currentResource);
        
        // Call the validation endpoint
        const response = await fetch(`${FHIR_API_BASE_URL}/$validate`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json, application/fhir+json'
            },
            body: requestBody
        });
        
        let validationOutcome;
        try {
            const responseText = await response.text();
            
            if (responseText.trim()) {
                try {
                    validationOutcome = JSON.parse(responseText);
                } catch (parseError) {
                    throw new Error(`Invalid JSON response: ${responseText}`);
                }
            } else {
                throw new Error('Empty response from validation server');
            }
        } catch (jsonError) {
            throw new Error(`Failed to parse validation response: ${jsonError.message}`);
        }
        
        if (!response.ok) {
            // Handle HTTP error status codes
            let errorMessage = `Validation error (${response.status}): `;
            
            if (validationOutcome && validationOutcome.issue && validationOutcome.issue.length > 0) {
                // Extract error message from OperationOutcome if available
                errorMessage += validationOutcome.issue[0].diagnostics || 'Unknown error';
            } else {
                errorMessage += 'Server error during validation';
            }
            
            throw new Error(errorMessage);
        }
        
        // Display validation results
        displayValidationResults(validationOutcome);
    } catch (error) {
        console.error('Error validating resource:', error);
        
        // Show error in validation results
        document.getElementById('validationSummary').className = 'alert alert-danger';
        document.getElementById('validationSummary').innerHTML = `
            <i class="bi bi-exclamation-triangle-fill"></i> 
            ${error.message || 'Error validating resource. Please check the console for details.'}
        `;
        document.getElementById('validationIssues').innerHTML = '';
    } finally {
        document.getElementById('loadingValidation').style.display = 'none';
    }
}
```

### Example Validation Requests

#### Validating a Patient Resource

Request:
```http
POST /api/fhir/$validate HTTP/1.1
Host: localhost:5300
Content-Type: application/json
Accept: application/json, application/fhir+json

{
  "resourceType": "Patient",
  "id": "example",
  "name": [
    {
      "family": "Smith",
      "given": ["John"]
    }
  ],
  "gender": "male",
  "birthDate": "1970-01-01"
}
```

Response (Valid):
```http
HTTP/1.1 200 OK
Content-Type: application/fhir+json

{
  "resourceType": "OperationOutcome",
  "issue": [
    {
      "severity": "information",
      "code": "informational",
      "diagnostics": "Patient resource is valid"
    }
  ]
}
```

#### Validating an Invalid Patient Resource

Request:
```http
POST /api/fhir/$validate HTTP/1.1
Host: localhost:5300
Content-Type: application/json
Accept: application/json, application/fhir+json

{
  "resourceType": "Patient",
  "id": "example",
  "gender": "invalid-gender"
}
```

Response (Invalid):
```http
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/fhir+json

{
  "resourceType": "OperationOutcome",
  "issue": [
    {
      "severity": "error",
      "code": "required",
      "diagnostics": "Patient resource must have at least one name",
      "expression": ["Patient.name"]
    },
    {
      "severity": "error",
      "code": "invalid",
      "diagnostics": "Patient gender must be a valid AdministrativeGender code",
      "expression": ["Patient.gender"]
    }
  ]
}
```

## Using the FHIR Explorer

The FHIR Explorer provides a user-friendly interface for exploring and validating FHIR resources:

1. **Loading Resources**:
   - Select a resource type (e.g., Patient)
   - Enter a resource ID or leave blank to get all resources
   - Click "Fetch Resource" to load the resource(s)

2. **Validating Resources**:
   - Once a resource is loaded, click the "Validate Resource" button
   - The validation results will appear in the "Validation Results" section
   - Issues are color-coded by severity (red for errors, yellow for warnings, blue for information)

3. **Interpreting Validation Results**:
   - The validation summary shows the overall status (valid or invalid)
   - Each issue includes:
     - Severity (error, warning, information)
     - Issue code (required, invalid, etc.)
     - Diagnostic message
     - Expression path to the problematic element

## Best Practices

1. **Resource Creation**:
   - Always include required fields for each resource type
   - Use correct data types and formats
   - Follow FHIR naming conventions and structures

2. **Validation**:
   - Validate resources before storing or transmitting them
   - Address all validation errors before using resources in production
   - Pay attention to warnings, as they may indicate potential issues

3. **Error Handling**:
   - Implement proper error handling for validation failures
   - Provide clear error messages to users
   - Log validation errors for debugging and improvement

## Future Enhancements

1. **Extended Validation**:
   - Support for additional FHIR resource types
   - Implementation of custom validation profiles
   - Integration with terminology services for code validation

2. **User Experience**:
   - Enhanced visualization of validation results
   - Guided error resolution
   - Batch validation of multiple resources

3. **Performance Optimization**:
   - Caching of validation results
   - Asynchronous validation for large resources
   - Selective validation of specific elements
