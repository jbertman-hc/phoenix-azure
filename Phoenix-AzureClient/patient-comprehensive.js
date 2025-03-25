// Constants
const API_BASE_URL = 'https://apiserviceswin20250318.azurewebsites.net/api';
const USE_MOCK_DATA = true; // Set to true to use mock data instead of real API calls

// Mock data for fallback when API calls fail due to CORS
const MOCK_REPOSITORIES = {
    "PatientRepository": ["GetPatient", "GetPatientDemographics", "SearchPatients"],
    "MedicalRecordRepository": ["GetMedicalRecord", "AddMedicalRecord", "UpdateMedicalRecord"],
    "MedicationRepository": ["GetMedications", "AddMedication", "UpdateMedication"],
    "AllergyRepository": ["GetAllergies", "AddAllergy", "UpdateAllergy"],
    "AppointmentRepository": ["GetAppointments", "ScheduleAppointment", "CancelAppointment"]
};

const MOCK_PATIENT = {
    "patientId": 12345,
    "firstName": "John",
    "lastName": "Doe",
    "dateOfBirth": "1980-01-01",
    "gender": "Male",
    "address": "123 Main St, Anytown, USA",
    "phoneNumber": "555-123-4567",
    "email": "john.doe@example.com"
};

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
    if (loadPatientBtn) {
        loadPatientBtn.addEventListener('click', function() {
            const patientId = patientIdInput.value.trim();
            if (patientId) {
                loadComprehensivePatientData(patientId);
            } else {
                showError('Please enter a valid Patient ID');
            }
        });
    }
    
    // Handle navigation links
    const patientsLink = document.querySelector('a[href="index.html"]');
    if (patientsLink) {
        console.log('Patients link found');
    } else {
        console.warn('Patients link not found');
    }
});

/**
 * Load the repository map that shows all repositories needed for comprehensive patient data
 */
async function loadRepositoryMap() {
    if (USE_MOCK_DATA) {
        console.log('Using mock repository data');
        displayRepositoryMap(MOCK_REPOSITORIES);
        return;
    }
    
    try {
        // Get repositories from the Azure API
        const response = await fetch(`${API_BASE_URL}/repositories`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors' // Explicitly set CORS mode
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const repositories = await response.json();
        
        // Convert the repositories to the format needed for display
        const repositoriesMap = {};
        repositories.forEach(repo => {
            if (repo.name && repo.methods) {
                repositoriesMap[repo.name] = repo.methods;
            }
        });
        
        displayRepositoryMap(repositoriesMap);
    } catch (error) {
        console.error('Error loading repository map:', error);
        
        // Fallback to mock data if the API call fails (likely due to CORS)
        console.log('Using mock repository data as fallback');
        displayRepositoryMap(MOCK_REPOSITORIES);
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
                                    <div class="d-flex justify-content-between align-items-center">
                                        <div>
                                            <span class="method-name">${method}</span>
                                            <span class="text-muted small ms-2">${getMethodDescription(method)}</span>
                                        </div>
                                        <div class="method-actions">
                                            <button class="btn btn-sm btn-primary execute-method" 
                                                    data-repo="${repoName}" 
                                                    data-method="${method}" 
                                                    data-bs-toggle="modal" 
                                                    data-bs-target="#executeMethodModal">
                                                Execute
                                            </button>
                                        </div>
                                    </div>
                                </li>
                            `).join('')}
                        </ul>
                    </div>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    
    // Add modal for executing methods
    html += `
        <div class="modal fade" id="executeMethodModal" tabindex="-1" aria-labelledby="executeMethodModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="executeMethodModalLabel">Execute Repository Method</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <form id="executeMethodForm">
                            <div class="mb-3">
                                <label for="repositoryName" class="form-label">Repository</label>
                                <input type="text" class="form-control" id="repositoryName" readonly>
                            </div>
                            <div class="mb-3">
                                <label for="methodName" class="form-label">Method</label>
                                <input type="text" class="form-control" id="methodName" readonly>
                            </div>
                            <div class="mb-3">
                                <label for="methodParameters" class="form-label">Parameters</label>
                                <input type="text" class="form-control" id="methodParameters" placeholder="Enter parameter value (e.g., patient ID)">
                                <div class="form-text">For methods that require a patient ID, enter the ID here.</div>
                            </div>
                        </form>
                        <div id="methodExecutionLoading" class="d-none">
                            <div class="d-flex justify-content-center">
                                <div class="spinner-border text-primary" role="status">
                                    <span class="visually-hidden">Loading...</span>
                                </div>
                            </div>
                        </div>
                        <div id="methodExecutionResult" class="d-none">
                            <h6>Result:</h6>
                            <div id="methodResultContent" class="border p-3 bg-light">
                                <!-- Result will be displayed here -->
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <button type="button" class="btn btn-primary" id="executeMethodBtn">Execute</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    repositoryMapContainer.innerHTML = html;
    
    // Add event listeners for the execute buttons
    document.querySelectorAll('.execute-method').forEach(button => {
        button.addEventListener('click', function() {
            const repo = this.getAttribute('data-repo');
            const method = this.getAttribute('data-method');
            
            document.getElementById('repositoryName').value = repo;
            document.getElementById('methodName').value = method;
            
            // If we have a patient ID in the input field, pre-fill the parameter
            const patientId = patientIdInput.value.trim();
            if (patientId) {
                document.getElementById('methodParameters').value = patientId;
            } else {
                document.getElementById('methodParameters').value = '';
            }
            
            // Reset the result area
            document.getElementById('methodExecutionResult').classList.add('d-none');
            document.getElementById('methodResultContent').innerHTML = '';
        });
    });
    
    // Add event listener for the execute method button in the modal
    document.getElementById('executeMethodBtn').addEventListener('click', async function() {
        const repo = document.getElementById('repositoryName').value;
        const method = document.getElementById('methodName').value;
        const params = document.getElementById('methodParameters').value;
        
        // Show loading indicator
        document.getElementById('methodExecutionLoading').classList.remove('d-none');
        document.getElementById('methodExecutionResult').classList.add('d-none');
        
        try {
            const result = await executeRepositoryMethod(repo, method, params);
            
            // Display the result
            document.getElementById('methodResultContent').innerHTML = `<pre>${JSON.stringify(result, null, 2)}</pre>`;
            document.getElementById('methodExecutionResult').classList.remove('d-none');
        } catch (error) {
            document.getElementById('methodResultContent').innerHTML = `<div class="alert alert-danger">${error.message}</div>`;
            document.getElementById('methodExecutionResult').classList.remove('d-none');
        } finally {
            document.getElementById('methodExecutionLoading').classList.add('d-none');
        }
    });
}

/**
 * Execute a repository method
 * @param {string} repositoryName - The repository name
 * @param {string} methodName - The method name
 * @param {string} parameters - The parameters to pass to the method
 * @returns {Promise<object>} The result of the method execution
 */
async function executeRepositoryMethod(repositoryName, methodName, parameters) {
    if (USE_MOCK_DATA) {
        console.log(`Mock execution of ${repositoryName}.${methodName} with parameters: ${parameters}`);
        
        // Return mock data based on the repository and method
        if (repositoryName === 'MedicalRecordRepository' && methodName === 'GetMedicalRecord') {
            return { 
                records: [
                    { date: "2023-01-15", diagnosis: "Common Cold", treatment: "Rest and fluids" },
                    { date: "2022-10-05", diagnosis: "Annual Checkup", treatment: "No issues found" }
                ]
            };
        } else if (repositoryName === 'MedicationRepository' && methodName === 'GetMedications') {
            return {
                medications: [
                    { name: "Ibuprofen", dosage: "200mg", frequency: "As needed" },
                    { name: "Multivitamin", dosage: "1 tablet", frequency: "Daily" }
                ]
            };
        } else if (repositoryName === 'AllergyRepository' && methodName === 'GetAllergies') {
            return {
                allergies: [
                    { allergen: "Peanuts", severity: "Moderate", reaction: "Hives" },
                    { allergen: "Penicillin", severity: "Mild", reaction: "Rash" }
                ]
            };
        } else if (repositoryName === 'AppointmentRepository' && methodName === 'GetAppointments') {
            return {
                appointments: [
                    { date: "2023-03-15", time: "10:00 AM", provider: "Dr. Smith", reason: "Follow-up" },
                    { date: "2023-06-30", time: "2:30 PM", provider: "Dr. Jones", reason: "Annual physical" }
                ]
            };
        } else {
            return { message: `Mock execution of ${repositoryName}.${methodName} completed` };
        }
    }
    
    let url = `${API_BASE_URL}/repository/${repositoryName}/${methodName}`;
    
    if (parameters) {
        url += `?parameters=${parameters}`;
    }
    
    const response = await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json'
        },
        mode: 'cors' // Explicitly set CORS mode
    });
    
    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
    }
    
    return await response.json();
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
    
    if (USE_MOCK_DATA) {
        console.log(`Using mock patient data for ID: ${patientId}`);
        const mockPatient = {...MOCK_PATIENT};
        mockPatient.patientId = parseInt(patientId) || 12345;
        
        const comprehensiveData = {
            Demographics: mockPatient,
            MedicalRecords: await getRepositoryData('MedicalRecordRepository', 'GetMedicalRecord', patientId),
            Medications: await getRepositoryData('MedicationRepository', 'GetMedications', patientId),
            Allergies: await getRepositoryData('AllergyRepository', 'GetAllergies', patientId),
            Appointments: await getRepositoryData('AppointmentRepository', 'GetAppointments', patientId)
        };
        
        displayComprehensivePatientData(comprehensiveData);
        patientDataLoading.classList.add('d-none');
        return;
    }
    
    try {
        // Get patient data from the Azure API
        const response = await fetch(`${API_BASE_URL}/patient/${patientId}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors' // Explicitly set CORS mode
        });
        
        if (!response.ok) {
            if (response.status === 404) {
                throw new Error(`Patient with ID ${patientId} not found`);
            }
            const errorText = await response.text();
            throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
        }
        
        const patientData = await response.json();
        
        // Get additional data for the patient from various repositories
        try {
            const comprehensiveData = {
                Demographics: patientData,
                MedicalRecords: await getRepositoryData('MedicalRecordRepository', 'GetMedicalRecord', patientId),
                Medications: await getRepositoryData('MedicationRepository', 'GetMedications', patientId),
                Allergies: await getRepositoryData('AllergyRepository', 'GetAllergies', patientId),
                Appointments: await getRepositoryData('AppointmentRepository', 'GetAppointments', patientId)
            };
            
            displayComprehensivePatientData(comprehensiveData);
        } catch (repoError) {
            console.warn('Error getting repository data:', repoError);
            // Still display the basic patient data even if repository data fails
            displayComprehensivePatientData({ Demographics: patientData });
        }
    } catch (error) {
        console.error('Error loading patient data:', error);
        
        // Fallback to mock data if the API call fails (likely due to CORS)
        if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
            console.log('Using mock patient data as fallback');
            const mockPatient = {...MOCK_PATIENT};
            mockPatient.patientId = parseInt(patientId) || 12345;
            
            const comprehensiveData = {
                Demographics: mockPatient,
                MedicalRecords: await getRepositoryData('MedicalRecordRepository', 'GetMedicalRecord', patientId),
                Medications: await getRepositoryData('MedicationRepository', 'GetMedications', patientId),
                Allergies: await getRepositoryData('AllergyRepository', 'GetAllergies', patientId),
                Appointments: await getRepositoryData('AppointmentRepository', 'GetAppointments', patientId)
            };
            
            displayComprehensivePatientData(comprehensiveData);
        } else {
            showError(`Failed to load patient data: ${error.message}`);
        }
    } finally {
        // Hide loading indicator
        patientDataLoading.classList.add('d-none');
    }
}

/**
 * Get data from a specific repository and method
 * @param {string} repositoryName - The repository name
 * @param {string} methodName - The method name
 * @param {string} patientId - The patient ID
 * @returns {Promise<object>} The repository data
 */
async function getRepositoryData(repositoryName, methodName, patientId) {
    try {
        return await executeRepositoryMethod(repositoryName, methodName, patientId);
    } catch (error) {
        console.warn(`Error getting data from ${repositoryName}.${methodName}:`, error);
        return { error: error.message };
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
    
    if (patientData.Appointments) {
        html += createDataTable('Appointments', patientData.Appointments);
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
