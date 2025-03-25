// Constants
const API_BASE_URL = 'http://localhost:5300/api';

// DOM Elements
const patientIdInput = document.getElementById('patientIdInput');
const loadPatientBtn = document.getElementById('loadPatientBtn');
const patientDataLoading = document.getElementById('patientDataLoading');
const patientDataContainer = document.getElementById('patientDataContainer');
const patientDataError = document.getElementById('patientDataError');
const repositoryMapContainer = document.getElementById('repositoryMapContainer');

// Event Listeners
document.addEventListener('DOMContentLoaded', function() {
    // Load repository map on page load
    loadRepositoryMap();
    
    // Add click event for loading patient data
    loadPatientBtn.addEventListener('click', function() {
        const patientId = patientIdInput.value.trim();
        if (patientId) {
            loadComprehensivePatientData(patientId);
        } else {
            showError('Please enter a valid Patient ID');
        }
    });
});

/**
 * Load the repository map that shows all repositories needed for comprehensive patient data
 */
async function loadRepositoryMap() {
    try {
        const response = await fetch(`${API_BASE_URL}/PatientData/repositories-map`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        displayRepositoryMap(data);
    } catch (error) {
        console.error('Error loading repository map:', error);
        repositoryMapContainer.innerHTML = `
            <div class="alert alert-danger">
                <h5>Error Loading Repository Map</h5>
                <p>${error.message}</p>
            </div>
        `;
    }
}

/**
 * Display the repository map in the UI
 * @param {Object} repositoriesMap - Map of repositories and their methods
 */
function displayRepositoryMap(repositoriesMap) {
    let html = '<div class="row">';
    
    // Create a card for each repository
    Object.keys(repositoriesMap).forEach(repoName => {
        const methods = repositoriesMap[repoName];
        
        html += `
            <div class="col-md-6 mb-3">
                <div class="card">
                    <div class="card-header">
                        <h5>${formatRepositoryName(repoName)}</h5>
                    </div>
                    <div class="card-body">
                        <p class="text-muted small">Repository: ${repoName}</p>
                        <h6>Available Methods:</h6>
                        <ul class="list-group">
                            ${methods.map(method => `
                                <li class="list-group-item">
                                    <span class="method-name">${method}</span>
                                    <span class="text-muted small ms-2">${getMethodDescription(method)}</span>
                                </li>
                            `).join('')}
                        </ul>
                    </div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    repositoryMapContainer.innerHTML = html;
}

/**
 * Load comprehensive patient data from all repositories
 * @param {string} patientId - The patient ID to load
 */
async function loadComprehensivePatientData(patientId) {
    // Show loading indicator
    patientDataLoading.classList.remove('d-none');
    patientDataContainer.classList.add('d-none');
    patientDataError.classList.add('d-none');
    
    try {
        const response = await fetch(`${API_BASE_URL}/PatientData/comprehensive/${patientId}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        displayComprehensivePatientData(data);
    } catch (error) {
        console.error('Error loading patient data:', error);
        showError(`Failed to load patient data: ${error.message}`);
    } finally {
        // Hide loading indicator
        patientDataLoading.classList.add('d-none');
    }
}

/**
 * Display comprehensive patient data in the UI
 * @param {Object} patientData - The comprehensive patient data
 */
function displayComprehensivePatientData(patientData) {
    // Start with a success message
    let html = '<div class="alert alert-success mb-4">';
    html += '<h5>Comprehensive Patient Data Loaded Successfully</h5>';
    html += '<p>Below is the aggregated data from all patient-related repositories.</p>';
    html += '</div>';
    
    // Create sections for each data category with proper table display
    if (patientData.Demographics) {
        html += createDataTable('Demographics', patientData.Demographics);
    }
    
    if (patientData.MedicalRecords) {
        html += createDataTable('Medical Records', patientData.MedicalRecords);
    }
    
    if (patientData.Medications) {
        html += createDataTable('Medications', patientData.Medications);
    }
    
    if (patientData.Allergies) {
        html += createDataTable('Allergies', patientData.Allergies);
    }
    
    if (patientData.VitalSigns) {
        html += createDataTable('Vital Signs', patientData.VitalSigns);
    }
    
    if (patientData.Insurance) {
        html += createDataTable('Insurance', patientData.Insurance);
    }
    
    if (patientData.Appointments) {
        html += createDataTable('Appointments', patientData.Appointments);
    }
    
    if (patientData.Billing) {
        html += createDataTable('Billing', patientData.Billing);
    }
    
    if (patientData.LabResults) {
        html += createDataTable('Lab Results', patientData.LabResults);
    }
    
    if (patientData.ImagingResults) {
        html += createDataTable('Imaging Results', patientData.ImagingResults);
    }
    
    // Display the complete JSON for reference (collapsed by default)
    html += `
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <h5>Complete Patient Data (JSON)</h5>
                <button class="btn btn-sm btn-outline-secondary" type="button" data-bs-toggle="collapse" data-bs-target="#jsonData">
                    Show/Hide Raw Data
                </button>
            </div>
            <div class="card-body">
                <div class="collapse" id="jsonData">
                    <pre>${JSON.stringify(patientData, null, 2)}</pre>
                </div>
            </div>
        </div>
    `;
    
    patientDataContainer.innerHTML = html;
    patientDataContainer.classList.remove('d-none');
}

/**
 * Create a data table for displaying a category of patient data
 * @param {string} title - The section title
 * @param {Object} data - The data to display
 * @returns {string} HTML for the data table
 */
function createDataTable(title, data) {
    // Check if data is an object with a 'result' property (from repository explorer)
    let tableData = data;
    if (data.result && typeof data.result === 'string') {
        try {
            // Try to parse the result if it's a JSON string
            tableData = JSON.parse(data.result);
        } catch (e) {
            // If parsing fails, use the original data
            tableData = data;
        }
    }
    
    let html = `
        <div class="card mb-4">
            <div class="card-header">
                <h5>${title}</h5>
            </div>
            <div class="card-body">
    `;
    
    // Handle different data types
    if (Array.isArray(tableData)) {
        // For arrays, create a table with all properties
        if (tableData.length > 0) {
            // Get all unique keys from all objects in the array
            const keys = new Set();
            tableData.forEach(item => {
                if (typeof item === 'object' && item !== null) {
                    Object.keys(item).forEach(key => keys.add(key));
                }
            });
            
            if (keys.size > 0) {
                // Create table with headers for each key
                html += '<div class="table-responsive"><table class="table table-striped table-bordered">';
                html += '<thead><tr>';
                keys.forEach(key => {
                    html += `<th>${formatKey(key)}</th>`;
                });
                html += '</tr></thead><tbody>';
                
                // Add rows for each item
                tableData.forEach(item => {
                    html += '<tr>';
                    keys.forEach(key => {
                        const value = item[key] !== undefined ? item[key] : '';
                        html += `<td>${formatValue(value)}</td>`;
                    });
                    html += '</tr>';
                });
                
                html += '</tbody></table></div>';
            } else {
                // Simple array of primitive values
                html += '<ul class="list-group">';
                tableData.forEach(item => {
                    html += `<li class="list-group-item">${formatValue(item)}</li>`;
                });
                html += '</ul>';
            }
        } else {
            html += '<p class="text-muted">No data available</p>';
        }
    } else if (typeof tableData === 'object' && tableData !== null) {
        // For objects, create a key-value table
        const keys = Object.keys(tableData);
        if (keys.length > 0) {
            html += '<table class="table table-striped">';
            keys.forEach(key => {
                if (key !== 'sampleData') { // Skip sampleData property
                    html += '<tr>';
                    html += `<th style="width: 30%">${formatKey(key)}</th>`;
                    html += `<td>${formatValue(tableData[key])}</td>`;
                    html += '</tr>';
                }
            });
            html += '</table>';
            
            // If there's sample data, display it separately
            if (tableData.sampleData) {
                html += '<h5 class="mt-3">Sample Data</h5>';
                html += createNestedDataDisplay(tableData.sampleData);
            }
        } else {
            html += '<p class="text-muted">No data available</p>';
        }
    } else {
        // For primitive values
        html += `<p>${formatValue(tableData)}</p>`;
    }
    
    html += '</div></div>';
    return html;
}

/**
 * Create a display for nested data objects
 * @param {any} data - The nested data to display
 * @returns {string} HTML for the nested data
 */
function createNestedDataDisplay(data) {
    if (Array.isArray(data)) {
        if (data.length === 0) {
            return '<p class="text-muted">Empty array</p>';
        }
        
        let html = '<div class="table-responsive"><table class="table table-sm table-bordered">';
        
        // Get all unique keys from all objects in the array
        const keys = new Set();
        data.forEach(item => {
            if (typeof item === 'object' && item !== null) {
                Object.keys(item).forEach(key => keys.add(key));
            }
        });
        
        if (keys.size > 0) {
            // Create table with headers
            html += '<thead><tr>';
            keys.forEach(key => {
                html += `<th>${formatKey(key)}</th>`;
            });
            html += '</tr></thead><tbody>';
            
            // Add rows
            data.forEach(item => {
                html += '<tr>';
                keys.forEach(key => {
                    const value = item[key] !== undefined ? item[key] : '';
                    html += `<td>${formatValue(value)}</td>`;
                });
                html += '</tr>';
            });
            
            html += '</tbody></table></div>';
            return html;
        } else {
            // Simple array of primitive values
            let html = '<ul class="list-group">';
            data.forEach(item => {
                html += `<li class="list-group-item">${formatValue(item)}</li>`;
            });
            html += '</ul>';
            return html;
        }
    } else if (typeof data === 'object' && data !== null) {
        let html = '<table class="table table-sm">';
        for (const key in data) {
            html += '<tr>';
            html += `<th>${formatKey(key)}</th>`;
            html += `<td>${formatValue(data[key])}</td>`;
            html += '</tr>';
        }
        html += '</table>';
        return html;
    } else {
        return `<p>${formatValue(data)}</p>`;
    }
}

/**
 * Format a key for display
 * @param {string} key - The key to format
 * @returns {string} Formatted key
 */
function formatKey(key) {
    // Convert camelCase to Title Case with spaces
    return key
        .replace(/([A-Z])/g, ' $1')
        .replace(/^./, str => str.toUpperCase())
        .trim();
}

/**
 * Format a value for display
 * @param {any} value - The value to format
 * @returns {string} Formatted value
 */
function formatValue(value) {
    if (value === null || value === undefined) {
        return '<span class="text-muted">N/A</span>';
    }
    
    if (typeof value === 'object') {
        return `<pre class="mb-0" style="max-height: 100px; overflow-y: auto;">${JSON.stringify(value, null, 2)}</pre>`;
    }
    
    if (typeof value === 'boolean') {
        return value ? 
            '<span class="badge bg-success">Yes</span>' : 
            '<span class="badge bg-danger">No</span>';
    }
    
    // Check if it's a date string
    if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(value)) {
        try {
            const date = new Date(value);
            return date.toLocaleString();
        } catch (e) {
            return value;
        }
    }
    
    return value.toString();
}

/**
 * Create a section for displaying a category of patient data
 * @param {string} title - The section title
 * @param {Object} data - The data to display
 * @returns {string} HTML for the section
 */
function createPatientSection(title, data) {
    return `
        <div class="card mb-4">
            <div class="card-header">
                <h5>${title}</h5>
            </div>
            <div class="card-body">
                <pre>${JSON.stringify(data, null, 2)}</pre>
            </div>
        </div>
    `;
}

/**
 * Show an error message
 * @param {string} message - The error message to display
 */
function showError(message) {
    patientDataError.textContent = message;
    patientDataError.classList.remove('d-none');
    patientDataContainer.classList.add('d-none');
}

/**
 * Format a repository name for display
 * @param {string} repoName - The repository name
 * @returns {string} Formatted repository name
 */
function formatRepositoryName(repoName) {
    // Remove "Repository" suffix and add spaces between words
    return repoName
        .replace('Repository', '')
        .replace(/([A-Z])/g, ' $1')
        .trim();
}

/**
 * Get a description for a method based on its name
 * @param {string} methodName - The method name
 * @returns {string} Method description
 */
function getMethodDescription(methodName) {
    // Generate descriptions based on method name patterns
    if (methodName.startsWith('Get')) {
        return 'Retrieves data from the repository';
    } else if (methodName.startsWith('Add')) {
        return 'Adds new data to the repository';
    } else if (methodName.startsWith('Update')) {
        return 'Updates existing data in the repository';
    } else if (methodName.startsWith('Delete') || methodName.startsWith('Remove')) {
        return 'Removes data from the repository';
    } else if (methodName.startsWith('Schedule')) {
        return 'Schedules a new appointment';
    } else if (methodName.startsWith('Cancel')) {
        return 'Cancels an existing appointment';
    } else if (methodName.startsWith('Process')) {
        return 'Processes a transaction';
    } else {
        return 'Performs an operation on the repository';
    }
}
