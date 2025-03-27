// FHIR Visualizer Application
document.addEventListener('DOMContentLoaded', function() {
    // Configuration
    let config = {
        serverUrl: 'http://localhost:5301/api',
        currentResourceType: 'Patient',
        selectedResourceId: null,
        selectedResource: null,
        backendInfo: {
            apiUrl: 'https://apiserviceswin20250318.azurewebsites.net/api',
            dataSource: 'Azure SQL Database',
            dataSourceDescription: 'Legacy healthcare data from AmazingCharts SQL database'
        },
        currentBundle: null,
        hapiValidatorUrl: 'https://hapi.fhir.org/baseR4/$validate'
    };

    // DOM elements
    const elements = {
        resourceList: document.getElementById('resource-list'),
        resourceDetails: document.getElementById('resource-details'),
        resourceLoading: document.getElementById('resource-loading'),
        refreshBtn: document.getElementById('refresh-btn'),
        viewBundleBtn: document.getElementById('view-bundle-btn'),
        bundleModal: document.getElementById('bundle-modal'),
        bundleContent: document.getElementById('bundle-content'),
        bundleValidateBtn: document.getElementById('bundle-validate-btn'),
        validationResults: document.getElementById('validation-results'),
        validationLoading: document.getElementById('validation-loading'),
        validateResourceBtn: document.getElementById('validate-resource-btn'),
        validateResourceContainer: document.getElementById('validate-resource-container'),
        resourceValidationResults: document.getElementById('resource-validation-results'),
        externalValidatorBtn: document.getElementById('external-validator-btn')
    };

    // Initialize UI
    initializeUI();
    loadResources(config.currentResourceType);

    // Event Listeners
    elements.refreshBtn.addEventListener('click', () => {
        loadResources(config.currentResourceType);
    });

    elements.viewBundleBtn.addEventListener('click', () => {
        viewFullBundle();
    });

    elements.bundleValidateBtn.addEventListener('click', () => {
        validateBundle();
    });

    document.querySelectorAll('[data-resource-type]').forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const resourceType = this.getAttribute('data-resource-type');
            
            // Update active nav link
            document.querySelectorAll('[data-resource-type]').forEach(l => {
                l.classList.remove('active');
            });
            this.classList.add('active');
            
            // Update current resource type and load resources
            config.currentResourceType = resourceType;
            loadResources(resourceType);
        });
    });

    elements.externalValidatorBtn.addEventListener('click', () => {
        if (!config.selectedResource) {
            alert('Please select a resource first');
            return;
        }
        
        // Open the HAPI FHIR Validator in a new tab
        const validatorUrl = 'https://validator.fhir.org/';
        const validatorWindow = window.open(validatorUrl, '_blank');
        
        // Create a message to guide the user
        const resourceJson = JSON.stringify(config.selectedResource, null, 2);
        alert('The FHIR Validator will open in a new tab.\n\n' +
              'To validate your resource:\n' +
              '1. Select "Validate Resource" from the left menu\n' +
              '2. Paste your resource in the text area\n' +
              '3. Click "Validate"\n\n' +
              'The resource JSON has been copied to your clipboard.');
        
        // Copy the resource JSON to clipboard
        navigator.clipboard.writeText(resourceJson).catch(err => {
            console.error('Failed to copy resource to clipboard:', err);
        });
    });

    // Helper function to safely extract value from FHIR property
    function getValueFromFhirProperty(property) {
        if (!property) return null;
        if (property.value !== undefined) return property.value;
        return property;
    }

    // Functions
    function initializeUI() {
    }

    async function loadResources(resourceType) {
        // Clear previous resources and show loading state
        elements.resourceList.innerHTML = '';
        elements.resourceLoading.classList.remove('d-none');
        
        try {
            const response = await fetch(`${config.serverUrl}/${resourceType}`);
            if (!response.ok) {
                throw new Error(`Failed to fetch resources: ${response.status}`);
            }
            
            const data = await response.json();
            
            // Store the full bundle for later viewing
            config.currentBundle = data;
            
            // Hide loading spinner
            elements.resourceLoading.classList.add('d-none');
            
            if (data.entry && data.entry.length > 0) {
                data.entry.forEach(entry => {
                    const resource = entry.resource;
                    const resourceId = getValueFromFhirProperty(resource.id);
                    
                    // Create a list item for each resource
                    const listItem = document.createElement('li');
                    listItem.className = 'list-group-item resource-item';
                    listItem.dataset.resourceId = resourceId;
                    
                    // Determine display name based on resource type
                    let displayName = `${resourceType} ${resourceId}`;
                    
                    if (resourceType === 'Patient' && resource.name && resource.name.length > 0) {
                        const nameObj = resource.name[0];
                        const family = getValueFromFhirProperty(nameObj.family) || '';
                        const given = nameObj.given ? nameObj.given.map(g => getValueFromFhirProperty(g)).join(' ') : '';
                        displayName = given ? `${given} ${family}` : family || displayName;
                    }
                    
                    listItem.innerHTML = `
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <strong>${displayName}</strong>
                                <div class="small text-muted">ID: ${resourceId}</div>
                            </div>
                            <button class="btn btn-sm btn-outline-primary view-resource-btn">
                                <i class="bi bi-eye"></i>
                            </button>
                        </div>
                    `;
                    
                    elements.resourceList.appendChild(listItem);
                    
                    // Add click event to view resource details
                    listItem.querySelector('.view-resource-btn').addEventListener('click', () => {
                        // Remove active class from all resources
                        document.querySelectorAll('.resource-item').forEach(item => {
                            item.classList.remove('active');
                        });
                        
                        // Add active class to selected resource
                        listItem.classList.add('active');
                        
                        // Set selected resource ID
                        config.selectedResourceId = resourceId;
                        
                        // Find the resource in the bundle
                        const resourceEntry = config.currentBundle.entry.find(entry => 
                            getValueFromFhirProperty(entry.resource.id) === resourceId
                        );
                        
                        if (resourceEntry) {
                            config.selectedResource = resourceEntry.resource;
                            displayResourceDetails(resourceEntry.resource);
                        } else {
                            console.error('Resource not found in bundle');
                        }
                    });
                    
                });
            } else {
                elements.resourceList.innerHTML = `
                    <li class="list-group-item text-center">
                        <p class="mb-0">No ${resourceType} resources found</p>
                    </li>
                `;
            }
        } catch (error) {
            console.error(`Error loading ${resourceType} resources:`, error);
        }
    }

    function displayResourceDetails(resource) {
        // Clear previous details
        elements.resourceDetails.innerHTML = '';
        elements.validateResourceContainer.classList.add('d-none');
        
        if (!resource) {
            elements.resourceDetails.innerHTML = '<div class="alert alert-warning">No resource selected</div>';
            return;
        }
        
        try {
            // Display the raw JSON
            const jsonStr = JSON.stringify(resource, null, 2);
            elements.resourceDetails.innerHTML = `
                <div class="card">
                    <div class="card-header">
                        <h5 class="mb-0">Raw JSON</h5>
                    </div>
                    <div class="card-body">
                        <pre class="json-view">${jsonStr}</pre>
                    </div>
                </div>
            `;
            
            // Show validate button
            elements.validateResourceContainer.classList.remove('d-none');
            
            // Set up validate button click handler
            elements.validateResourceBtn.onclick = () => validateResource(resource);
        } catch (error) {
            console.error('Error displaying resource details:', error);
            elements.resourceDetails.innerHTML = `<div class="alert alert-danger">Error displaying resource: ${error.message}</div>`;
        }
    }

    function viewFullBundle() {
        if (!config.currentBundle) {
            alert('No bundle available to view');
            return;
        }
        
        // Show the bundle in a modal
        elements.bundleContent.innerHTML = `
            <pre class="bundle-json">${JSON.stringify(config.currentBundle, null, 2)}</pre>
        `;
        
        const bundleModal = new bootstrap.Modal(elements.bundleModal);
        bundleModal.show();
    }

    async function validateBundle() {
        if (!config.currentBundle) {
            return;
        }
        
        // Show validation loading state
        elements.validationLoading.classList.remove('d-none');
        elements.validationResults.innerHTML = '';
        
        try {
            const outcome = await validateBundleHelper(config.currentBundle);
            elements.validationLoading.classList.add('d-none');
            
            // Display validation results
            if (outcome.issue && outcome.issue.length > 0) {
                const issuesHtml = outcome.issue.map(issue => {
                    const severity = getValueFromFhirProperty(issue.severity) || 'unknown';
                    const details = getValueFromFhirProperty(issue.details?.text) || 'No details provided';
                    const location = issue.expression ? issue.expression.map(e => getValueFromFhirProperty(e)).join(', ') : 'Unknown location';
                    
                    let severityClass = 'alert-info';
                    if (severity === 'error') {
                        severityClass = 'alert-danger';
                    } else if (severity === 'warning') {
                        severityClass = 'alert-warning';
                    }
                    
                    return `
                        <div class="alert ${severityClass} mb-3">
                            <div class="d-flex justify-content-between">
                                <strong>${severity.toUpperCase()}</strong>
                                <span>${location}</span>
                            </div>
                            <div class="mt-2">${details}</div>
                        </div>
                    `;
                }).join('');
                
                elements.validationResults.innerHTML = `
                    <div class="mb-3">
                        <strong>Resource:</strong> ${config.currentResourceType} Bundle
                    </div>
                    <div class="mb-3">
                        <strong>Issues Found:</strong> ${outcome.issue.length}
                    </div>
                    ${issuesHtml}
                `;
            } else {
                elements.validationResults.innerHTML = `
                    <div class="alert alert-success">
                        <i class="bi bi-check-circle-fill me-2"></i>
                        Bundle is valid. No issues found.
                    </div>
                `;
            }
        } catch (error) {
            console.error('Error validating bundle:', error);
            elements.validationLoading.classList.add('d-none');
            elements.validationResults.innerHTML = `
                <div class="alert alert-danger">
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    Failed to validate bundle: ${error.message}
                </div>
            `;
        }
    }

    async function validateBundleHelper(bundle) {
        try {
            console.log('Validating bundle/resource:', JSON.stringify(bundle, null, 2).substring(0, 200) + '...');
            
            const response = await fetch(`${config.serverUrl}/fhir/$validate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/fhir+json',
                    'Accept': 'application/fhir+json'
                },
                body: JSON.stringify({
                    resourceToValidate: bundle
                })
            });

            console.log('Validation response status:', response.status);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Validation error response:', errorText);
                throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
            }

            const outcome = await response.json();
            console.log('Validation outcome:', JSON.stringify(outcome, null, 2).substring(0, 200) + '...');
            return outcome;
        } catch (error) {
            console.error('Error validating bundle/resource:', error);
            // Fallback to HAPI FHIR Validator
            return validateBundleWithHapi(bundle);
        }
    }

    async function validateBundleWithHapi(bundle) {
        try {
            console.log('Validating bundle/resource with HAPI FHIR Validator:', JSON.stringify(bundle, null, 2).substring(0, 200) + '...');
            
            const response = await fetch(config.hapiValidatorUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/fhir+json',
                    'Accept': 'application/fhir+json'
                },
                body: JSON.stringify(bundle)
            });

            console.log('Validation response status:', response.status);
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Validation error response:', errorText);
                throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
            }

            const outcome = await response.json();
            console.log('Validation outcome:', JSON.stringify(outcome, null, 2).substring(0, 200) + '...');
            return outcome;
        } catch (error) {
            console.error('Error validating bundle/resource with HAPI FHIR Validator:', error);
            // Return a default OperationOutcome with the error
            return {
                resourceType: "OperationOutcome",
                issue: [
                    {
                        severity: "error",
                        code: "exception",
                        details: {
                            text: `Validation failed: ${error.message}`
                        }
                    }
                ]
            };
        }
    }

    async function validateResource(resource) {
        if (!resource) {
            return;
        }
        
        // Show validation loading state
        elements.resourceValidationResults.innerHTML = `
            <div class="text-center py-3">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Validating...</span>
                </div>
                <p class="mt-2">Validating resource...</p>
            </div>
        `;
        
        try {
            const outcome = await validateBundleHelper(resource);
            
            // Display validation results
            if (outcome.issue && outcome.issue.length > 0) {
                const issuesHtml = outcome.issue.map(issue => {
                    const severity = getValueFromFhirProperty(issue.severity) || 'unknown';
                    const details = getValueFromFhirProperty(issue.details?.text) || 'No details provided';
                    const location = issue.expression ? issue.expression.map(e => getValueFromFhirProperty(e)).join(', ') : 'Unknown location';
                    
                    let severityClass = 'alert-info';
                    if (severity === 'error') {
                        severityClass = 'alert-danger';
                    } else if (severity === 'warning') {
                        severityClass = 'alert-warning';
                    }
                    
                    return `
                        <div class="alert ${severityClass} mb-3">
                            <div class="d-flex justify-content-between">
                                <strong>${severity.toUpperCase()}</strong>
                                <span>${location}</span>
                            </div>
                            <div class="mt-2">${details}</div>
                        </div>
                    `;
                }).join('');
                
                elements.resourceValidationResults.innerHTML = `
                    <div class="mb-3">
                        <strong>Resource:</strong> ${resource.resourceType}
                    </div>
                    <div class="mb-3">
                        <strong>Issues Found:</strong> ${outcome.issue.length}
                    </div>
                    ${issuesHtml}
                `;
            } else {
                elements.resourceValidationResults.innerHTML = `
                    <div class="alert alert-success">
                        <i class="bi bi-check-circle-fill me-2"></i>
                        Resource is valid. No issues found.
                    </div>
                `;
            }
        } catch (error) {
            console.error('Error validating resource:', error);
            elements.resourceValidationResults.innerHTML = `
                <div class="alert alert-danger">
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    Failed to validate resource: ${error.message}
                </div>
            `;
        }
    }
});
