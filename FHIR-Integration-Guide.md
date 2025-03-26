# FHIR Integration Guide for Phoenix-Azure

## Overview

This document provides a comprehensive guide to how Phoenix-Azure connects to the AmazingCharts API and transforms data into FHIR resources. It also explains the validation process for ensuring FHIR compliance.

## Architecture

The Phoenix-Azure application follows a proxy architecture:

1. **Client-Side Application**: The web interface that users interact with
2. **Local API Server**: Acts as a proxy to handle CORS issues and transform data
3. **Azure API**: The source of patient data at `https://apiserviceswin20250318.azurewebsites.net/api`

## Data Flow

### Patient Data Retrieval

1. **Client Request**: The client (browser) requests patient data from the local API server
2. **Proxy Request**: The local API server forwards the request to the Azure API
3. **Data Transformation**: The local API server transforms the response into FHIR resources
4. **Client Response**: The transformed data is returned to the client

### Endpoints Used

- **Addendum Endpoint**: `/api/addendum/{id}` - Used to retrieve patient data
  - This endpoint is used because it reliably returns patient information
  - We extract patient details from the `patID` and `patientName` fields

## FHIR Resource Generation

Phoenix-Azure transforms AmazingCharts data into standard FHIR resources:

1. **Patient Resources**: Basic demographic information
2. **Bundle Resources**: Collections of related resources
3. **OperationOutcome Resources**: Results of validation operations

## FHIR Validation Process

### Client-Side Validation Request

```javascript
// Example of client-side validation request
const response = await fetch(`${FHIR_API_BASE_URL}/$validate`, {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        resourceToValidate: fhirResource
    })
});
```

### Server-Side Validation Process

1. **Request Parsing**: The server extracts the FHIR resource from the request
   - Supports both direct resources and resources wrapped in a `resourceToValidate` property
2. **Resource Validation**: The resource is validated against FHIR profiles using Firely SDK
3. **Result Generation**: An OperationOutcome resource is created with validation results
4. **Response**: The validation results are returned to the client

```csharp
// Server-side validation endpoint
[HttpPost("$validate")]
[Produces("application/fhir+json")]
[Consumes("application/json", "application/fhir+json")]
public IActionResult ValidateResource([FromBody] JObject request)
{
    // Extract resource from request (handles both formats)
    JToken resourceToken = null;
    if (request.TryGetValue("resourceToValidate", out resourceToken))
    {
        // Resource is in the resourceToValidate property
    }
    else
    {
        // Resource is the entire request
        resourceToken = request;
    }
    
    // Validate the resource using Firely SDK
    var validationResult = _fhirService.Validate(resource);
    
    // Return the validation outcome
    return Content(json, "application/fhir+json");
}
```

## Error Handling

The application implements robust error handling:

1. **API Connection Errors**: Graceful handling of connection issues to the Azure API
2. **Resource Not Found**: Proper feedback when resources aren't available
3. **Validation Errors**: Clear communication of validation failures

## Best Practices

1. **Use Known Endpoints**: Only use documented endpoints from the Azure API
2. **Handle Both Formats**: Support both direct resources and wrapped resources
3. **Provide Clear Feedback**: Always return meaningful error messages
4. **Avoid Assumptions**: Don't assume the existence of undocumented endpoints

## Troubleshooting

Common issues and their solutions:

1. **500 Error from Patient Bundle**: Check that the PatientDataService is using the correct endpoint
2. **Validation Failures**: Ensure the resource structure matches FHIR specifications
3. **CORS Issues**: Verify that the local API server is properly configured to handle CORS

## References

- [FHIR Documentation](https://www.hl7.org/fhir/)
- [Firely .NET SDK](https://fire.ly/products/firely-net-sdk/)
- [AmazingCharts API Documentation](https://apiserviceswin20250318.azurewebsites.net/swagger/index.html)
