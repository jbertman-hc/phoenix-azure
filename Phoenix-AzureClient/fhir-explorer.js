// Configuration
const API_BASE_URL = 'http://localhost:5300/api';
const FHIR_API_BASE_URL = 'http://localhost:5300/api/fhir';
const DEFAULT_PATIENT_ID = '1001';

// Global variables
let resourceTypes = ['Patient', 'Practitioner', 'Organization', 'Encounter', 'Observation'];
let selectedResourceType = 'Patient';

// DOM Elements
const resourceTypeSelect = document.getElementById('resourceType');
const resourceIdInput = document.getElementById('resourceId');
const fetchResourceBtn = document.getElementById('fetchResource');
const loadingResource = document.getElementById('loadingResource');
const noResource = document.getElementById('noResource');
const resourceError = document.getElementById('resourceError');
const resourceErrorMessage = document.getElementById('resourceErrorMessage');
const resourceContainer = document.getElementById('resourceContainer');
const resourceJson = document.getElementById('resourceJson');
const copyJsonBtn = document.getElementById('copyJson');
const alertContainer = document.getElementById('alertContainer');
const patientIdsList = document.getElementById('patientIdsList');

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
            return;
        }
        
        if (!response.ok) {
            throw new Error(`API error: ${response.status}`);
        }
        
        const responseText = await response.text();
        console.log('Raw API response received');
        
        try {
            // Parse the JSON
            const json = JSON.parse(responseText);
            
            // If we're using the regular API, convert to FHIR-like format for display
            let displayJson;
            if (useRegularApi) {
                if (Array.isArray(json)) {
                    // Convert array of patients to FHIR Bundle
                    displayJson = {
                        resourceType: "Bundle",
                        type: "searchset",
                        total: json.length,
                        entry: json.map(patient => ({
                            resource: convertPatientToFhir(patient)
                        }))
                    };
                } else {
                    // Convert single patient to FHIR Patient
                    displayJson = convertPatientToFhir(json);
                }
            } else {
                displayJson = json;
            }
            
            // Format the JSON for display
            const formattedJson = JSON.stringify(displayJson, null, 2);
            
            // Display the JSON
            resourceJson.textContent = formattedJson;
            showResourceContainer(true);
            copyJsonBtn.style.display = 'block';
            
            // Show success message
            if (resourceId) {
                showAlert('success', `Successfully fetched ${resourceType} resource with ID ${resourceId}`);
            } else {
                showAlert('success', `Successfully fetched all ${resourceType} resources`);
            }
        } catch (jsonError) {
            console.error('Error parsing JSON:', jsonError);
            throw new Error('Invalid JSON response from API');
        }
    } catch (error) {
        console.error('Error fetching FHIR resource:', error);
        showResourceError(true, `Failed to fetch resource: ${error.message}`);
    } finally {
        showLoadingResource(false);
    }
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
