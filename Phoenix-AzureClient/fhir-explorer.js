// Configuration
const API_BASE_URL = 'http://localhost:5300/api';
const FHIR_API_BASE_URL = 'http://localhost:5300/api/fhir';
const DEFAULT_PATIENT_ID = '1001';

// Global variables
let resourceTypes = ['Patient', 'Practitioner', 'Organization', 'Encounter', 'Observation'];
let selectedResourceType = 'Patient';
let currentResource = null; // Store the current resource for validation

// DOM Elements
const resourceTypeSelect = document.getElementById('resourceType');
const resourceIdInput = document.getElementById('resourceId');
const fetchResourceBtn = document.getElementById('fetchResource');
const validateResourceBtn = document.getElementById('validateResource');
const loadingResource = document.getElementById('loadingResource');
const noResource = document.getElementById('noResource');
const resourceError = document.getElementById('resourceError');
const resourceErrorMessage = document.getElementById('resourceErrorMessage');
const resourceContainer = document.getElementById('resourceContainer');
const resourceJson = document.getElementById('resourceJson');
const copyJsonBtn = document.getElementById('copyJson');
const alertContainer = document.getElementById('alertContainer');
const patientIdsList = document.getElementById('patientIdsList');
const validationResultCard = document.getElementById('validationResultCard');
const loadingValidation = document.getElementById('loadingValidation');
const validationSummary = document.getElementById('validationSummary');
const validationIssues = document.getElementById('validationIssues');

// Event Listeners
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM content loaded, initializing FHIR Explorer');
    initializeResourceTypeSelect();
    
    // Check for patient ID in URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    const patientIdParam = urlParams.get('patientId');
    
    // Set patient ID from URL parameter or use default
    if (resourceIdInput) {
        if (patientIdParam) {
            resourceIdInput.value = patientIdParam;
            // Set resource type to Patient
            if (resourceTypeSelect) {
                resourceTypeSelect.value = 'Patient';
            }
        } else {
            resourceIdInput.value = DEFAULT_PATIENT_ID;
        }
    }
    
    fetchAvailablePatientIds();
    
    // If patient ID was provided in URL, fetch that specific patient
    if (patientIdParam) {
        fetchResource('Patient', patientIdParam);
    } else {
        // Otherwise fetch all patients as before
        fetchResource('Patient', '');
    }
    
    if (fetchResourceBtn) {
        fetchResourceBtn.addEventListener('click', () => {
            const resourceType = resourceTypeSelect.value;
            const resourceId = resourceIdInput.value.trim();
            fetchResource(resourceType, resourceId);
        });
    } else {
        console.error('Fetch resource button not found');
    }
    
    if (validateResourceBtn) {
        validateResourceBtn.addEventListener('click', () => {
            validateResource();
        });
    } else {
        console.error('Validate resource button not found');
    }
    
    if (copyJsonBtn) {
        copyJsonBtn.addEventListener('click', () => {
            copyToClipboard(resourceJson.textContent);
        });
    } else {
        console.error('Copy JSON button not found');
    }
    
    if (resourceTypeSelect) {
        resourceTypeSelect.addEventListener('change', () => {
            selectedResourceType = resourceTypeSelect.value;
            console.log('Selected resource type:', selectedResourceType);
            
            // Clear resource ID if not Patient
            if (selectedResourceType !== 'Patient') {
                resourceIdInput.value = '';
            } else {
                resourceIdInput.value = DEFAULT_PATIENT_ID;
            }
        });
    } else {
        console.error('Resource type select not found');
    }
});

// Initialize resource type select
function initializeResourceTypeSelect() {
    if (!resourceTypeSelect) {
        console.error('Resource type select element not found');
        return;
    }
    
    resourceTypeSelect.innerHTML = '';
    resourceTypes.forEach(type => {
        const option = document.createElement('option');
        option.value = type;
        option.textContent = type;
        resourceTypeSelect.appendChild(option);
    });
    
    resourceTypeSelect.value = selectedResourceType;
}

// Fetch available patient IDs
async function fetchAvailablePatientIds() {
    try {
        console.log('Fetching available patient IDs');
        
        const response = await fetch(`${API_BASE_URL}/Patient`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });
        
        if (!response.ok) {
            throw new Error(`API error: ${response.status}`);
        }
        
        const patients = await response.json();
        console.log('Available patient IDs:', patients);
        
        // Display patient IDs in the UI
        if (patientIdsList) {
            patientIdsList.innerHTML = '';
            
            if (patients.length === 0) {
                patientIdsList.innerHTML = '<li class="list-group-item">No patient IDs available</li>';
            } else {
                patients.forEach(patient => {
                    const listItem = document.createElement('li');
                    listItem.className = 'list-group-item d-flex justify-content-between align-items-center';
                    
                    const idSpan = document.createElement('span');
                    idSpan.textContent = `Patient ID: ${patient.patientID} - ${patient.fullName}`;
                    
                    const useButton = document.createElement('button');
                    useButton.className = 'btn btn-sm btn-primary';
                    useButton.textContent = 'Use';
                    useButton.addEventListener('click', () => {
                        resourceIdInput.value = patient.patientID;
                        resourceTypeSelect.value = 'Patient';
                        selectedResourceType = 'Patient';
                    });
                    
                    listItem.appendChild(idSpan);
                    listItem.appendChild(useButton);
                    patientIdsList.appendChild(listItem);
                });
            }
        }
    } catch (error) {
        console.error('Error fetching patient IDs:', error);
        if (patientIdsList) {
            patientIdsList.innerHTML = `<li class="list-group-item text-danger">Error fetching patient IDs: ${error.message}</li>`;
        }
    }
}

// Fetch a FHIR resource from the API
async function fetchResource(resourceType, resourceId = '') {
    try {
        showLoadingResource(true);
        showResourceContainer(false);
        showResourceError(false);
        showNoResource(false); // Hide the "No resource loaded" message immediately when fetching
        showValidationResults(false); // Hide validation results when fetching a new resource
        
        let url;
        let useRegularApi = false;
        
        // For Patient resources, use the regular API that we know works
        if (resourceType === 'Patient') {
            url = `${API_BASE_URL}/${resourceType}`;
            useRegularApi = true;
            if (resourceId) {
                url += `/${resourceId}`;
            }
        } else {
            // For other resource types, try the FHIR API
            url = `${FHIR_API_BASE_URL}/${resourceType}`;
            if (resourceId) {
                url += `/${resourceId}`;
            }
        }
        
        console.log(`Fetching resource from: ${url}`);
        
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });
        
        if (response.status === 404) {
            showResourceError(true, `Resource not found: ${resourceType}/${resourceId}`);
            showLoadingResource(false);
            validateResourceBtn.disabled = true;
            currentResource = null;
            return;
        }
        
        if (!response.ok) {
            throw new Error(`API error: ${response.status}`);
        }
        
        let data = await response.json();
        
        // If we used the regular API, convert to FHIR format
        if (useRegularApi) {
            if (Array.isArray(data)) {
                // If it's an array of patients
                data = {
                    resourceType: 'Bundle',
                    type: 'searchset',
                    total: data.length,
                    entry: data.map(patient => ({
                        resource: convertPatientToFhir(patient)
                    }))
                };
            } else {
                // If it's a single patient
                data = convertPatientToFhir(data);
            }
        }
        
        // Store the current resource for validation
        currentResource = data;
        
        // Format the JSON for display
        const formattedJson = JSON.stringify(data, null, 2);
        
        // Display the resource
        resourceJson.textContent = formattedJson;
        showResourceContainer(true);
        showLoadingResource(false);
        copyJsonBtn.style.display = 'block';
        
        // Enable the validate button
        validateResourceBtn.disabled = false;
        
        console.log('Resource fetched successfully:', data);
    } catch (error) {
        console.error('Error fetching resource:', error);
        showResourceError(true, `Error fetching resource: ${error.message}`);
        showLoadingResource(false);
        validateResourceBtn.disabled = true;
        currentResource = null;
    }
}

// Validate the current FHIR resource
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
        
        // Call the validation endpoint
        const response = await fetch(`${FHIR_API_BASE_URL}/$validate`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/fhir+json',
                'Accept': 'application/fhir+json'
            },
            body: JSON.stringify(currentResource)
        });
        
        let validationOutcome;
        try {
            const responseText = await response.text();
            console.log('Raw validation response:', responseText);
            
            if (responseText.trim()) {
                validationOutcome = JSON.parse(responseText);
                console.log('Validation outcome:', validationOutcome);
            } else {
                throw new Error('Empty response from validation server');
            }
        } catch (jsonError) {
            console.error('Error parsing validation response:', jsonError);
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

// Display validation results
function displayValidationResults(outcome) {
    const validationSummary = document.getElementById('validationSummary');
    const validationIssues = document.getElementById('validationIssues');
    
    // Make sure the elements are visible
    validationSummary.style.display = 'block';
    
    if (!outcome || !outcome.issue || outcome.issue.length === 0) {
        // No issues found, show success
        validationSummary.className = 'alert alert-success';
        validationSummary.innerHTML = '<i class="bi bi-check-circle-fill"></i> Resource is valid according to FHIR specifications.';
        validationIssues.innerHTML = '';
        return;
    }
    
    // Count issues by severity
    const issueCount = {
        error: 0,
        warning: 0,
        information: 0,
        note: 0
    };
    
    outcome.issue.forEach(issue => {
        const severity = issue.severity.toLowerCase();
        if (issueCount.hasOwnProperty(severity)) {
            issueCount[severity]++;
        }
    });
    
    // Determine overall validation status
    let validationStatus = 'success';
    let statusMessage = 'Resource is valid according to FHIR specifications.';
    
    if (issueCount.error > 0) {
        validationStatus = 'danger';
        statusMessage = `Resource is invalid. Found ${issueCount.error} error(s).`;
    } else if (issueCount.warning > 0) {
        validationStatus = 'warning';
        statusMessage = `Resource is valid but has ${issueCount.warning} warning(s).`;
    } else if (issueCount.information > 0 || issueCount.note > 0) {
        validationStatus = 'info';
        statusMessage = 'Resource is valid but has informational messages.';
    }
    
    // Display summary
    validationSummary.className = `alert alert-${validationStatus}`;
    validationSummary.innerHTML = `<i class="bi bi-${validationStatus === 'success' ? 'check' : 'info'}-circle-fill"></i> ${statusMessage}`;
    
    // Display detailed issues
    validationIssues.innerHTML = '<div class="list-group mt-3">';
    outcome.issue.forEach(issue => {
        const severity = issue.severity.toLowerCase();
        let severityClass = 'secondary';
        
        if (severity === 'error') severityClass = 'danger';
        else if (severity === 'warning') severityClass = 'warning';
        else if (severity === 'information') severityClass = 'info';
        
        validationIssues.innerHTML += `
            <div class="list-group-item list-group-item-${severityClass}">
                <div class="d-flex w-100 justify-content-between">
                    <h5 class="mb-1">${severity.charAt(0).toUpperCase() + severity.slice(1)}</h5>
                    <small>${issue.code || 'N/A'}</small>
                </div>
                <p class="mb-1">${issue.diagnostics || 'No details provided'}</p>
                ${issue.expression ? `<small>Location: ${issue.expression.join(', ')}</small>` : ''}
            </div>
        `;
    });
    validationIssues.innerHTML += '</div>';
}

// Get Bootstrap class for severity
function getSeverityClass(severity) {
    switch (severity.toLowerCase()) {
        case 'error': return 'danger';
        case 'warning': return 'warning';
        case 'information': return 'info';
        case 'note': return 'secondary';
        default: return 'secondary';
    }
}

// Show validation error
function showValidationError(message) {
    validationSummary.className = 'alert alert-danger';
    validationSummary.innerHTML = `<i class="bi bi-exclamation-triangle-fill"></i> Error validating resource: ${message}`;
    validationSummary.style.display = 'block';
}

// Clear validation results
function clearValidationResults() {
    validationSummary.style.display = 'none';
    validationIssues.innerHTML = '';
}

// Show/hide validation results card
function showValidationResults(show) {
    validationResultCard.style.display = show ? 'block' : 'none';
}

// Show/hide validation loading indicator
function showLoadingValidation(show) {
    loadingValidation.style.display = show ? 'block' : 'none';
}

// Convert a regular Patient object to FHIR format for display
function convertPatientToFhir(patient) {
    return {
        resourceType: "Patient",
        id: patient.patientID.toString(),
        meta: {
            profile: ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]
        },
        text: {
            status: "generated",
            div: `<div xmlns="http://www.w3.org/1999/xhtml">${patient.fullName}</div>`
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

// UI Functions
function showLoadingResource(show) {
    if (loadingResource) {
        loadingResource.style.display = show ? 'block' : 'none';
    }
}

function showResourceContainer(show) {
    if (resourceContainer) {
        resourceContainer.style.display = show ? 'block' : 'none';
    }
}

function showResourceError(show, message = '') {
    if (resourceError) {
        resourceError.style.display = show ? 'block' : 'none';
        if (show && resourceErrorMessage) {
            resourceErrorMessage.textContent = message;
        }
    }
}

function showNoResource(show) {
    if (noResource) {
        noResource.style.display = show ? 'block' : 'none';
    }
}

function showAlert(type, message) {
    if (!alertContainer) {
        console.error('Alert container not found');
        return;
    }
    
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show`;
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    alertContainer.appendChild(alert);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        alert.classList.remove('show');
        setTimeout(() => {
            alertContainer.removeChild(alert);
        }, 150);
    }, 5000);
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text)
        .then(() => {
            showAlert('success', 'JSON copied to clipboard');
        })
        .catch(err => {
            console.error('Error copying to clipboard:', err);
            showAlert('danger', 'Failed to copy to clipboard');
        });
}
