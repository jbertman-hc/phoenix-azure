// Configuration
const API_BASE_URL = 'http://localhost:5300/api';
const FHIR_API_BASE_URL = `${API_BASE_URL}/fhir`;
const DEFAULT_PATIENT_ID = '1001';

// Global variables
const resourceTypes = [
    'Bundle:Patient List',
    'Patient', 
    'Capability Statement'
];
let selectedResourceType = 'Bundle:Patient List'; // Default to Patient Bundle
let currentResource = null; // Store the current resource for validation

// DOM Elements
let resourceTypeSelect;
let resourceIdInput;
let fetchResourceBtn;
let validateResourceBtn;
let validateResourceButtonTop;
let copyJsonBtn;
let resourceJson;
let resourceHeaderElement;
let resourceContainer;
let loadingResource;
let resourceError;
let noResource;
let alertContainer;
let validationResults;
let patientIdsList;
let validationResultCard;
let loadingValidation;
let validationSummary;
let validationIssues;

// Handle page load
document.addEventListener('DOMContentLoaded', function() {
    // Initialize UI elements
    initializeUI();
    
    // Load default resource if specified in URL
    const urlParams = new URLSearchParams(window.location.search);
    const resourceTypeParam = urlParams.get('resourceType');
    const resourceIdParam = urlParams.get('resourceId');
    const patientIdParam = urlParams.get('patientId');
    
    if (resourceTypeParam) {
        // Set the resource type select
        if (resourceTypeSelect) {
            const matchingOption = Array.from(resourceTypeSelect.options).find(option => 
                option.value === resourceTypeParam
            );
            
            if (matchingOption) {
                resourceTypeSelect.value = resourceTypeParam;
                console.log(`Set resource type from URL: ${resourceTypeParam}`);
                
                // If we have a resource ID, set that too
                if (resourceIdParam && resourceIdInput) {
                    resourceIdInput.value = resourceIdParam;
                    console.log(`Set resource ID from URL: ${resourceIdParam}`);
                } else if (patientIdParam && resourceIdInput && resourceTypeParam === 'Patient') {
                    resourceIdInput.value = patientIdParam;
                    console.log(`Set patient ID from URL: ${patientIdParam}`);
                }
                
                // Fetch the resource
                if (fetchResourceBtn) {
                    console.log('Auto-fetching resource from URL parameters');
                    fetchResourceBtn.click();
                }
            }
        }
    } else {
        // Always auto-load the Patient Bundle on page load
        console.log('Auto-loading Patient Bundle');
        fetchResource('Bundle:Patient List', '');
    }
});

// Initialize UI elements
function initializeUI() {
    // Get DOM elements
    resourceTypeSelect = document.getElementById('resourceType');
    resourceIdInput = document.getElementById('resourceId');
    fetchResourceBtn = document.getElementById('fetchResource');
    validateResourceBtn = document.getElementById('validateResource');
    validateResourceButtonTop = document.getElementById('validateResourceButton');
    copyJsonBtn = document.getElementById('copyJson');
    resourceJson = document.getElementById('resourceJson');
    resourceHeaderElement = document.getElementById('resourceHeader');
    resourceContainer = document.getElementById('resourceContainer');
    loadingResource = document.getElementById('loadingResource');
    resourceError = document.getElementById('resourceError');
    noResource = document.getElementById('noResource');
    alertContainer = document.getElementById('alertContainer');
    validationResults = document.getElementById('validationResults');
    patientIdsList = document.getElementById('patientIdsList');
    validationResultCard = document.getElementById('validationResultCard');
    loadingValidation = document.getElementById('loadingValidation');
    validationSummary = document.getElementById('validationSummary');
    validationIssues = document.getElementById('validationIssues');
    
    console.log('DOM content loaded, initializing FHIR Explorer');
    initializeResourceTypeSelect();
    
    // Try to fetch available patient IDs
    try {
        fetchAvailablePatientIds();
    } catch (error) {
        console.error('Error fetching patient IDs:', error);
    }
    
    // Populate resource type select
    if (resourceTypeSelect) {
        resourceTypes.forEach(type => {
            const option = document.createElement('option');
            option.value = type;
            option.textContent = type;
            resourceTypeSelect.appendChild(option);
        });
        
        // Set default resource type
        resourceTypeSelect.value = selectedResourceType;
        
        // Add event listener for resource type change
        resourceTypeSelect.addEventListener('change', function() {
            selectedResourceType = this.value;
            
            // Show/hide resource ID input based on resource type
            if (resourceIdInput) {
                const resourceIdContainer = document.getElementById('resourceIdContainer');
                if (selectedResourceType === 'Bundle:Patient List' || selectedResourceType === 'Capability Statement') {
                    if (resourceIdContainer) resourceIdContainer.style.display = 'none';
                } else {
                    if (resourceIdContainer) resourceIdContainer.style.display = 'block';
                }
            }
        });
        
        // Trigger change event to set initial visibility
        resourceTypeSelect.dispatchEvent(new Event('change'));
    }
    
    // Add event listeners
    if (fetchResourceBtn) {
        fetchResourceBtn.addEventListener('click', function() {
            const resourceType = resourceTypeSelect ? resourceTypeSelect.value : selectedResourceType;
            const resourceId = resourceIdInput ? resourceIdInput.value : '';
            fetchResource(resourceType, resourceId);
        });
    }
    
    if (validateResourceBtn) {
        validateResourceBtn.addEventListener('click', validateCurrentResource);
    }
    
    if (validateResourceButtonTop) {
        validateResourceButtonTop.addEventListener('click', validateCurrentResource);
    }
    
    if (copyJsonBtn) {
        copyJsonBtn.addEventListener('click', function() {
            if (resourceJson && resourceJson.textContent) {
                navigator.clipboard.writeText(resourceJson.textContent)
                    .then(() => showAlert('success', 'JSON copied to clipboard!'))
                    .catch(err => showAlert('danger', `Error copying to clipboard: ${err}`));
            }
        });
    }
}

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

// Function to fetch a FHIR resource
async function fetchResource(resourceType, resourceId) {
    try {
        showLoadingResource(true, `Loading ${resourceType}...`);
        showResourceContainer(false);
        showResourceError(false);
        showNoResource(false);
        showValidationResults(false);
        
        let url = '';
        
        if (resourceType === 'Bundle:Patient List') {
            // Use the FHIR Patient endpoint to get all patients as a Bundle
            url = `${FHIR_API_BASE_URL}/Patient`;
            console.log('Fetching Patient Bundle from:', url);
        } else if (resourceType === 'Patient') {
            // Use the FHIR Patient endpoint with ID
            url = `${FHIR_API_BASE_URL}/Patient/${resourceId}`;
            console.log('Fetching Patient from:', url);
        } else if (resourceType === 'Capability Statement') {
            // Use the metadata endpoint for capability statement
            url = `${FHIR_API_BASE_URL}/metadata`;
            console.log('Fetching Capability Statement from:', url);
        } else {
            // For other FHIR resources
            url = `${FHIR_API_BASE_URL}/${resourceType}${resourceId ? `/${resourceId}` : ''}`;
            console.log('Fetching resource from:', url);
        }
        
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Accept': 'application/json, application/fhir+json'
            }
        });
        
        console.log('Response status:', response.status, 'Content-Type:', response.headers.get('Content-Type'));
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error(`API error (${response.status}):`, errorText);
            showResourceError(true, `Error: ${response.status} ${response.statusText}`);
            throw new Error(`API error: ${response.status}`);
        }
        
        // Parse the response
        const responseText = await response.text();
        try {
            currentResource = JSON.parse(responseText);
            console.log('Resource loaded:', currentResource);
            
            // If this is a patient list but not in a Bundle format, convert it to a Bundle
            if (resourceType === 'Bundle:Patient List' && currentResource) {
                // Check if it's already a Bundle or needs conversion
                if (!currentResource.resourceType) {
                    // Convert the patient list to a FHIR Bundle
                    const patientResources = Array.isArray(currentResource) ? 
                        currentResource.map(patient => {
                            // Convert regular Patient model to FHIR Patient resource
                            return {
                                resourceType: 'Patient',
                                id: patient.patientID?.toString() || patient.PatientID?.toString() || '1',
                                name: [{
                                    given: [patient.firstName || patient.FirstName || 'Unknown'],
                                    family: patient.lastName || patient.LastName || 'Unknown'
                                }],
                                birthDate: patient.birthDate || patient.BirthDate || null
                            };
                        }) : [];
                    
                    // Create a proper FHIR Bundle
                    currentResource = {
                        resourceType: 'Bundle',
                        type: 'searchset',
                        total: patientResources.length,
                        entry: patientResources.map(resource => ({
                            resource: resource,
                            fullUrl: `Patient/${resource.id}`
                        }))
                    };
                    console.log('Converted patient list to Bundle:', currentResource);
                }
            }
            
            // Update the UI with the resource
            displayResource(currentResource, resourceType);
            
            // Enable the validate button
            if (validateResourceBtn) {
                validateResourceBtn.disabled = false;
            }
            
            return currentResource;
        } catch (jsonError) {
            console.error('Error parsing JSON:', jsonError);
            showResourceError(true, 'Invalid JSON response from API');
            throw new Error('Invalid JSON response');
        }
    } catch (error) {
        console.error('Error fetching resource:', error);
        showAlert(`Error fetching resource: ${error}`, 'danger');
        showLoadingResource(false);
        return null;
    }
}

// Validate the current FHIR resource
async function validateCurrentResource() {
    if (!currentResource) {
        showAlert('No resource to validate. Please fetch a resource first.', 'warning');
        return;
    }
    
    try {
        showLoadingResource(true, "Validating FHIR resource against standard profiles...");
        showValidationResults(false);
        
        console.log('Validating resource:', currentResource.resourceType);
        
        // Prepare the request
        const url = `${FHIR_API_BASE_URL}/$validate`;
        console.log('Sending validation request to:', url);
        
        // Make sure the resource has a resourceType property
        if (!currentResource.resourceType) {
            console.error('Resource is missing resourceType property');
            showAlert('Error: Resource is missing resourceType property', 'danger');
            return;
        }
        
        console.log(`Validating ${currentResource.resourceType} resource`);
        
        // Send the resource directly without wrapping it
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/fhir+json'
            },
            body: JSON.stringify(currentResource)
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error(`Validation API error (${response.status}): ${errorText}`);
            showAlert(`Error validating resource: ${errorText || response.statusText}`, 'danger');
            return;
        }
        
        // Parse the validation results
        const validationResults = await response.json();
        console.log('Validation results:', validationResults);
        
        // Display the validation results
        displayValidationResults(validationResults);
        
        // Show a summary alert based on validation outcome
        if (validationResults.issue && validationResults.issue.length > 0) {
            // Count issues by severity
            const errorCount = validationResults.issue.filter(i => i.severity === 'error').length;
            const warningCount = validationResults.issue.filter(i => i.severity === 'warning').length;
            const infoCount = validationResults.issue.filter(i => i.severity === 'information').length;
            
            if (errorCount > 0) {
                showAlert(`Validation found ${errorCount} error(s), ${warningCount} warning(s), and ${infoCount} info message(s).`, 'danger');
            } else if (warningCount > 0) {
                showAlert(`Validation successful with ${warningCount} warning(s) and ${infoCount} info message(s).`, 'warning');
            } else {
                showAlert('Validation successful with minor notes. See details below.', 'info');
            }
        } else {
            showAlert('Validation successful! The resource is valid according to FHIR standards.', 'success');
        }
    } catch (error) {
        console.error('Error validating resource:', error);
        showAlert(`Error validating resource: ${error.message}`, 'danger');
    } finally {
        showLoadingResource(false);
    }
}

// Display validation results
function displayValidationResults(operationOutcome) {
    if (!operationOutcome) {
        console.error('No validation results to display');
        return;
    }
    
    console.log('Displaying validation results:', operationOutcome);
    
    // Get the validation results container
    const resultsContainer = document.getElementById('validationResultsContent');
    if (!resultsContainer) {
        console.error('Validation results container not found');
        return;
    }
    
    // Clear previous results
    resultsContainer.innerHTML = '';
    
    // Check if we have issues to display
    if (operationOutcome.issue && operationOutcome.issue.length > 0) {
        // Create a summary
        const totalIssues = operationOutcome.issue.length;
        const errorCount = operationOutcome.issue.filter(i => i.severity === 'error').length;
        const warningCount = operationOutcome.issue.filter(i => i.severity === 'warning').length;
        const infoCount = operationOutcome.issue.filter(i => i.severity === 'information').length;
        
        let summaryClass = 'alert-success';
        if (errorCount > 0) {
            summaryClass = 'alert-danger';
        } else if (warningCount > 0) {
            summaryClass = 'alert-warning';
        } else if (infoCount > 0) {
            summaryClass = 'alert-info';
        }
        
        // Add summary
        const summary = document.createElement('div');
        summary.className = `alert ${summaryClass} mb-3`;
        summary.innerHTML = `
            <h6 class="mb-0">Validation Summary</h6>
            <p class="mb-0">
                Found ${totalIssues} issue(s): ${errorCount} error(s), ${warningCount} warning(s), ${infoCount} information message(s)
            </p>
        `;
        resultsContainer.appendChild(summary);
        
        // Create a list for the issues
        const issuesList = document.createElement('div');
        issuesList.className = 'validation-issues';
        
        // Add each issue
        operationOutcome.issue.forEach((issue, index) => {
            const issueDiv = document.createElement('div');
            
            // Set class based on severity
            let severityClass = '';
            switch (issue.severity) {
                case 'error':
                    severityClass = 'validation-error';
                    break;
                case 'warning':
                    severityClass = 'validation-warning';
                    break;
                case 'information':
                    severityClass = 'validation-information';
                    break;
                default:
                    severityClass = 'validation-information';
            }
            
            issueDiv.className = `validation-issue ${severityClass}`;
            
            // Format the issue details
            let issueContent = `
                <div class="d-flex justify-content-between">
                    <strong>${issue.severity.toUpperCase()}</strong>
                    <span class="text-muted small">Code: ${issue.code || 'N/A'}</span>
                </div>
            `;
            
            // Add diagnostics if available
            if (issue.diagnostics) {
                issueContent += `<p class="mb-0">${issue.diagnostics}</p>`;
            }
            
            // Add location if available
            if (issue.location && issue.location.length > 0) {
                issueContent += `<p class="small text-muted mb-0">Location: ${issue.location.join(', ')}</p>`;
            }
            
            issueDiv.innerHTML = issueContent;
            issuesList.appendChild(issueDiv);
        });
        
        resultsContainer.appendChild(issuesList);
    } else {
        // No issues found - valid resource
        const validMessage = document.createElement('div');
        validMessage.className = 'alert alert-success';
        validMessage.innerHTML = `
            <h6 class="mb-0"><i class="bi bi-check-circle-fill"></i> Resource is Valid</h6>
            <p class="mb-0">No issues were found with this FHIR resource.</p>
        `;
        resultsContainer.appendChild(validMessage);
    }
    
    // Show the validation results section
    showValidationResults(true);
}

// Show/hide validation results
function showValidationResults(show) {
    const validationResults = document.getElementById('validationResults');
    if (validationResults) {
        validationResults.style.display = show ? 'block' : 'none';
    }
}

// Show resource error
function showResourceError(show, message = '') {
    const resourceError = document.getElementById('resourceError');
    const errorMessage = document.getElementById('errorMessage');
    
    if (resourceError) {
        resourceError.style.display = show ? 'block' : 'none';
        
        if (errorMessage && message) {
            errorMessage.textContent = message;
        }
    }
}

// Show alert message
function showAlert(message, type = 'info') {
    const alertContainer = document.getElementById('alertContainer');
    if (!alertContainer) return;
    
    // Create alert element
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show`;
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    // Add to container
    alertContainer.appendChild(alert);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        if (alert.parentNode === alertContainer) {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }
    }, 5000);
}

// Display a FHIR resource in the UI
function displayResource(resource, resourceType) {
    if (!resource) {
        showNoResource(true);
        return;
    }
    
    // Update resource header
    if (resourceHeaderElement) {
        let headerText = resource.resourceType || resourceType;
        if (resource.id) {
            headerText += ` (ID: ${resource.id})`;
        }
        resourceHeaderElement.textContent = headerText;
    }
    
    // Format and display the JSON
    if (resourceJson) {
        resourceJson.textContent = JSON.stringify(resource, null, 2);
        
        // Apply syntax highlighting if available
        if (window.hljs) {
            window.hljs.highlightElement(resourceJson);
        }
    }
    
    // If this is a Bundle with patients, show the patient list
    if (resource.resourceType === 'Bundle' && resource.entry && patientIdsList) {
        patientIdsList.innerHTML = '';
        
        const patients = resource.entry.filter(entry => 
            entry.resource && entry.resource.resourceType === 'Patient'
        ).map(entry => entry.resource);
        
        if (patients.length === 0) {
            patientIdsList.innerHTML = '<li class="list-group-item">No patients found in bundle</li>';
        } else {
            patients.forEach(patient => {
                const listItem = document.createElement('li');
                listItem.className = 'list-group-item d-flex justify-content-between align-items-center';
                
                const idSpan = document.createElement('span');
                const patientId = patient.id;
                
                // Get patient name
                let patientName = 'Unknown';
                if (patient.name && patient.name.length > 0) {
                    const name = patient.name[0];
                    const given = name.given ? name.given.join(' ') : '';
                    const family = name.family || '';
                    patientName = `${given} ${family}`.trim();
                }
                
                idSpan.textContent = `${patientName} (ID: ${patientId})`;
                
                const viewButton = document.createElement('button');
                viewButton.className = 'btn btn-sm btn-primary';
                viewButton.textContent = 'View Details';
                viewButton.addEventListener('click', () => {
                    if (resourceTypeSelect) resourceTypeSelect.value = 'Patient';
                    if (resourceIdInput) resourceIdInput.value = patientId;
                    fetchResource('Patient', patientId);
                });
                
                listItem.appendChild(idSpan);
                listItem.appendChild(viewButton);
                patientIdsList.appendChild(listItem);
            });
        }
    }
    
    // Show the resource container
    showResourceContainer(true);
    showLoadingResource(false);
}

// UI Functions
function showLoadingResource(show, message = '') {
    if (loadingResource) {
        loadingResource.style.display = show ? 'block' : 'none';
        if (show && message) {
            loadingResource.innerHTML = `<i class="bi bi-spinner bi-spin"></i> ${message}`;
        } else {
            loadingResource.innerHTML = '';
        }
    }
}

function showResourceContainer(show) {
    if (resourceContainer) {
        resourceContainer.style.display = show ? 'block' : 'none';
    }
    
    // Also show/hide the copy button
    if (copyJsonBtn) {
        copyJsonBtn.style.display = show ? 'block' : 'none';
    }
    
    // Hide loading indicator when showing the resource
    if (show && loadingResource) {
        loadingResource.style.display = 'none';
    }
}

function showNoResource(show) {
    if (noResource) {
        noResource.style.display = show ? 'block' : 'none';
    }
}

function copyToClipboard(text) {
    try {
        // Create a temporary textarea element
        const textarea = document.createElement('textarea');
        textarea.value = text;
        document.body.appendChild(textarea);
        
        // Select and copy the text
        textarea.select();
        document.execCommand('copy');
        
        // Remove the temporary element
        document.body.removeChild(textarea);
        
        // Show success message
        showAlert('JSON copied to clipboard', 'success');
    } catch (error) {
        console.error('Error copying to clipboard:', error);
        showAlert('Failed to copy to clipboard: ' + error.message, 'danger');
    }
}
