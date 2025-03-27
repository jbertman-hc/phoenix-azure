// FHIR Visualizer Application
document.addEventListener('DOMContentLoaded', function() {
    // Configuration
    let config = {
        serverUrl: 'http://localhost:5300/api',
        currentResourceType: 'Patient',
        selectedResourceId: null,
        backendInfo: {
            apiUrl: 'https://apiserviceswin20250318.azurewebsites.net/api',
            dataSource: 'Azure SQL Database',
            dataSourceDescription: 'Legacy healthcare data from AmazingCharts SQL database'
        },
        currentBundle: null
    };

    // DOM elements
    const elements = {
        resourceList: document.getElementById('resource-list'),
        resourceDetails: document.getElementById('resource-details'),
        refreshBtn: document.getElementById('refresh-btn'),
        viewBundleBtn: document.getElementById('view-bundle-btn'),
        bundleModal: document.getElementById('bundle-modal'),
        bundleContent: document.getElementById('bundle-content'),
        bundleValidateBtn: document.getElementById('bundle-validate-btn'),
        validationResults: document.getElementById('validation-results'),
        validationLoading: document.getElementById('validation-loading')
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
        
        try {
            const response = await fetch(`${config.serverUrl}/${resourceType}`);
            if (!response.ok) {
                throw new Error(`Failed to fetch resources: ${response.status}`);
            }
            
            const data = await response.json();
            
            // Store the full bundle for later viewing
            config.currentBundle = data;
            
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
            alert('No bundle available to validate');
            return;
        }
        
        // Show validation card and loading state
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
                    
                    let severityClass = 'validation-info';
                    if (severity === 'error') {
                        severityClass = 'validation-error';
                    } else if (severity === 'warning') {
                        severityClass = 'validation-warning';
                    }
                    
                    return `
                        <div class="validation-issue ${severityClass}">
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
                        ${config.currentResourceType} Bundle is valid. No issues found.
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

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const outcome = await response.json();
            return outcome;
        } catch (error) {
            console.error('Error validating bundle:', error);
            throw error;
        }
    }
});
