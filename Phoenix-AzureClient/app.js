// Configuration
const API_BASE_URL = 'http://localhost:5300/api';

// Global variables
let patients = [];
let currentPatientId = null;

// DOM Elements
const loadingPatientsElement = document.getElementById('loadingPatients');
const alertContainer = document.getElementById('alertContainer');
const noPatients = document.getElementById('noPatients');
const patientTableContainer = document.getElementById('patientTableContainer');
const patientTableBody = document.getElementById('patientTableBody');
const patientListView = document.getElementById('patientListView');
const patientDetailView = document.getElementById('patientDetailView');
const loadingPatientDetail = document.getElementById('loadingPatientDetail');
const patientNotFound = document.getElementById('patientNotFound');
const patientDetailContainer = document.getElementById('patientDetailContainer');
const loadingMedicalRecords = document.getElementById('loadingMedicalRecords');
const noMedicalRecords = document.getElementById('noMedicalRecords');
const medicalRecordsContainer = document.getElementById('medicalRecordsContainer');
const medicalRecordsAccordion = document.getElementById('medicalRecordsAccordion');
const backToListBtn = document.getElementById('backToListBtn');
const searchInput = document.getElementById('searchInput');
const clearSearchBtn = document.getElementById('clearSearchBtn');
const patientsLink = document.getElementById('patientsLink');
const viewFhirBtn = document.getElementById('viewFhirBtn');

// Event Listeners
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM content loaded, initializing application');
    loadPatients();
    
    // Make sure all DOM elements are properly initialized
    if (backToListBtn) {
        backToListBtn.addEventListener('click', () => {
            showPatientList();
        });
    } else {
        console.error('Back to list button not found');
    }
    
    if (patientsLink) {
        patientsLink.addEventListener('click', (e) => {
            e.preventDefault();
            showPatientList();
        });
    } else {
        console.error('Patients link not found');
    }
    
    if (searchInput) {
        console.log('Adding event listener to search input');
        searchInput.addEventListener('input', debounce(() => {
            console.log('Search input changed:', searchInput.value);
            filterPatients();
        }, 500));
    } else {
        console.error('Search input not found');
    }
    
    if (clearSearchBtn) {
        clearSearchBtn.addEventListener('click', () => {
            if (searchInput) {
                searchInput.value = '';
                filterPatients();
            }
        });
    } else {
        console.error('Clear search button not found');
    }
    
    if (viewFhirBtn) {
        viewFhirBtn.addEventListener('click', (e) => {
            e.preventDefault();
            if (currentPatientId) {
                window.location.href = `fhir-explorer.html?patientId=${currentPatientId}`;
            }
        });
    } else {
        console.error('View FHIR button not found');
    }
    
    // Add event listeners for table sorting
    document.querySelectorAll('th[data-sort]').forEach(header => {
        header.addEventListener('click', () => {
            const sortField = header.getAttribute('data-sort');
            sortPatients(sortField);
        });
    });
});

// API Functions
async function loadPatients() {
    try {
        showLoadingPatients(true);
        showPatientTableContainer(false);
        showNoPatients(false);
        
        console.log('Fetching patients from:', `${API_BASE_URL}/Patient`);
        
        const response = await fetch(`${API_BASE_URL}/Patient`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });
        
        console.log('API response status:', response.status);
        
        if (!response.ok) {
            throw new Error(`API error: ${response.status}`);
        }
        
        const responseText = await response.text();
        console.log('Raw API response:', responseText);
        
        try {
            patients = JSON.parse(responseText);
            console.log('Patients loaded:', patients);
            
            if (patients.length === 0) {
                console.log('No patients returned from API');
                showNoPatients(true);
            } else {
                console.log(`Loaded ${patients.length} patients`);
                renderPatientTable(patients);
                showPatientTableContainer(true);
            }
        } catch (jsonError) {
            console.error('Error parsing JSON:', jsonError);
            throw new Error('Invalid JSON response from API');
        }
    } catch (error) {
        console.error('Error loading patients:', error);
        showAlert('danger', `Failed to load patients: ${error.message}`);
        showNoPatients(true);
    } finally {
        showLoadingPatients(false);
    }
}

async function loadPatientDetails(patientId) {
    try {
        currentPatientId = patientId;
        showPatientDetail();
        showLoadingPatientDetail(true);
        showPatientDetailContainer(false);
        showPatientNotFound(false);
        
        const response = await fetch(`${API_BASE_URL}/Patient/${patientId}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });
        
        if (response.status === 404) {
            showPatientNotFound(true);
            return;
        }
        
        if (!response.ok) {
            throw new Error(`API error: ${response.status}`);
        }
        
        const patient = await response.json();
        console.log('Patient details loaded:', patient);
        
        // Populate patient details
        document.getElementById('patientID').textContent = patient.patientID || 'N/A';
        document.getElementById('chartID').textContent = patient.chartID || 'N/A';
        document.getElementById('fullName').textContent = formatPatientName(patient);
        document.getElementById('firstName').textContent = patient.first || 'N/A';
        document.getElementById('middleName').textContent = patient.middle || 'N/A';
        document.getElementById('lastName').textContent = patient.last || 'N/A';
        document.getElementById('suffix').textContent = patient.suffix || 'N/A';
        document.getElementById('salutation').textContent = patient.salutation || 'N/A';
        document.getElementById('gender').textContent = patient.gender || 'N/A';
        document.getElementById('birthDate').textContent = patient.birthDate ? new Date(patient.birthDate).toLocaleDateString() : 'N/A';
        document.getElementById('ssn').textContent = patient.ss || 'N/A';
        document.getElementById('patientAddress').textContent = patient.patientAddress || 'N/A';
        document.getElementById('city').textContent = patient.city || 'N/A';
        document.getElementById('state').textContent = patient.state || 'N/A';
        document.getElementById('zip').textContent = patient.zip || 'N/A';
        document.getElementById('phone').textContent = patient.phone || 'N/A';
        document.getElementById('email').textContent = patient.email || 'N/A';
        document.getElementById('lastVisit').textContent = patient.lastVisit && patient.lastVisit !== '0001-01-01T00:00:00' ? 
            new Date(patient.lastVisit).toLocaleDateString() : 'N/A';
        document.getElementById('primaryCareProvider').textContent = patient.primaryCareProvider || 'N/A';
        document.getElementById('activeStatus').textContent = patient.inactive ? 'Inactive' : 'Active';
        
        // Update the status badge
        const patientStatus = document.getElementById('patientStatus');
        if (patient.inactive) {
            patientStatus.textContent = 'Inactive';
            patientStatus.className = 'badge bg-danger';
        } else {
            patientStatus.textContent = 'Active';
            patientStatus.className = 'badge bg-success';
        }
        
        showPatientDetailContainer(true);
        
        // Load medical records
        loadMedicalRecords(patientId);
    } catch (error) {
        console.error('Error loading patient details:', error);
        showAlert('danger', `Failed to load patient details: ${error.message}`);
    } finally {
        showLoadingPatientDetail(false);
    }
}

async function loadMedicalRecords(patientId) {
    try {
        showLoadingMedicalRecords(true);
        showMedicalRecordsContainer(false);
        showNoMedicalRecords(false);
        
        const response = await fetch(`${API_BASE_URL}/Patient/medical-records/${patientId}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });
        
        if (!response.ok) {
            throw new Error(`API error: ${response.status}`);
        }
        
        const records = await response.json();
        console.log('Medical records loaded:', records);
        
        if (records.length === 0) {
            showNoMedicalRecords(true);
            return;
        }
        
        // Clear previous records
        medicalRecordsAccordion.innerHTML = '';
        
        // Render medical records
        records.forEach((record, index) => {
            const recordData = record.data;
            const recordType = record.type;
            
            const accordionItem = document.createElement('div');
            accordionItem.className = 'accordion-item';
            
            const headerId = `heading${index}`;
            const collapseId = `collapse${index}`;
            
            let title = recordType;
            if (recordData.noteSubject) {
                title += ` - ${recordData.noteSubject}`;
            }
            
            let date = 'Unknown date';
            if (recordData.date) {
                date = new Date(recordData.date).toLocaleString();
            }
            
            accordionItem.innerHTML = `
                <h2 class="accordion-header" id="${headerId}">
                    <button class="accordion-button ${index > 0 ? 'collapsed' : ''}" type="button" data-bs-toggle="collapse" data-bs-target="#${collapseId}" aria-expanded="${index === 0 ? 'true' : 'false'}" aria-controls="${collapseId}">
                        ${title} <span class="ms-auto badge bg-secondary">${date}</span>
                    </button>
                </h2>
                <div id="${collapseId}" class="accordion-collapse collapse ${index === 0 ? 'show' : ''}" aria-labelledby="${headerId}" data-bs-parent="#medicalRecordsAccordion">
                    <div class="accordion-body">
                        ${recordData.noteBody ? `<p>${recordData.noteBody.replace(/\r\n/g, '<br>')}</p>` : ''}
                        ${recordData.savedBy ? `<p class="text-muted">Provider: ${recordData.savedBy}</p>` : ''}
                    </div>
                </div>
            `;
            
            medicalRecordsAccordion.appendChild(accordionItem);
        });
        
        showMedicalRecordsContainer(true);
    } catch (error) {
        console.error('Error loading medical records:', error);
        showAlert('danger', `Failed to load medical records: ${error.message}`);
    } finally {
        showLoadingMedicalRecords(false);
    }
}

// UI Functions
function renderPatientTable(patients) {
    patientTableBody.innerHTML = '';
    
    patients.forEach(patient => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${patient.patientID}</td>
            <td>${formatPatientName(patient)}</td>
            <td>${patient.birthDate ? new Date(patient.birthDate).toLocaleDateString() : 'N/A'}</td>
            <td>${patient.gender || 'N/A'}</td>
            <td>
                ${patient.phone ? `<i class="bi bi-telephone"></i> ${patient.phone}<br>` : ''}
                ${patient.email ? `<i class="bi bi-envelope"></i> ${patient.email}` : ''}
            </td>
            <td>
                <button class="btn btn-sm btn-primary view-patient" data-patient-id="${patient.patientID}">
                    <i class="bi bi-eye"></i> View
                </button>
            </td>
        `;
        
        patientTableBody.appendChild(row);
    });
    
    // Add event listeners to view buttons
    document.querySelectorAll('.view-patient').forEach(button => {
        button.addEventListener('click', (e) => {
            const patientId = e.target.closest('button').getAttribute('data-patient-id');
            loadPatientDetails(patientId);
        });
    });
}

function filterPatients() {
    if (!searchInput) {
        console.error('Search input element not found');
        return;
    }
    
    const searchTerm = searchInput.value.toLowerCase().trim();
    console.log('Filtering patients with search term:', searchTerm);
    console.log('Number of patients before filtering:', patients.length);
    
    if (!searchTerm) {
        console.log('Empty search term, showing all patients');
        renderPatientTable(patients);
        return;
    }
    
    const filtered = patients.filter(patient => {
        // Handle null values safely
        const first = (patient.first || '').toLowerCase();
        const last = (patient.last || '').toLowerCase();
        const fullName = `${last}, ${first}`.toLowerCase();
        const patientId = patient.patientID ? patient.patientID.toString() : '';
        const phone = (patient.phone || '').toLowerCase();
        const email = (patient.email || '').toLowerCase();
        
        const matches = 
            fullName.includes(searchTerm) || 
            first.includes(searchTerm) ||
            last.includes(searchTerm) ||
            patientId.includes(searchTerm) || 
            phone.includes(searchTerm) || 
            email.includes(searchTerm);
        
        if (matches) {
            console.log(`Patient ${patientId} (${fullName}) matches search term`);
        }
        
        return matches;
    });
    
    console.log('Number of patients after filtering:', filtered.length);
    
    if (filtered.length === 0) {
        patientTableBody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center">No patients match your search criteria</td>
            </tr>
        `;
        console.log('No matching patients found');
    } else {
        renderPatientTable(filtered);
        console.log(`Displaying ${filtered.length} matching patients`);
    }
}

function sortPatients(field) {
    const headers = document.querySelectorAll('th[data-sort]');
    let direction = 'asc';
    
    // Find the header that was clicked
    const header = document.querySelector(`th[data-sort="${field}"]`);
    
    // Check if it already has a sort direction
    if (header.classList.contains('sort-asc')) {
        direction = 'desc';
        header.classList.remove('sort-asc');
        header.classList.add('sort-desc');
    } else if (header.classList.contains('sort-desc')) {
        direction = 'asc';
        header.classList.remove('sort-desc');
        header.classList.add('sort-asc');
    } else {
        // Remove sort classes from all headers
        headers.forEach(h => {
            h.classList.remove('sort-asc', 'sort-desc');
        });
        
        // Add sort class to clicked header
        header.classList.add('sort-asc');
    }
    
    // Sort the patients array
    patients.sort((a, b) => {
        let valueA, valueB;
        
        if (field === 'last') {
            valueA = formatPatientName(a).toLowerCase();
            valueB = formatPatientName(b).toLowerCase();
        } else {
            valueA = a[field] || '';
            valueB = b[field] || '';
            
            if (field === 'birthDate') {
                valueA = valueA ? new Date(valueA).getTime() : 0;
                valueB = valueB ? new Date(valueB).getTime() : 0;
            } else if (typeof valueA === 'string') {
                valueA = valueA.toLowerCase();
                valueB = valueB.toLowerCase();
            }
        }
        
        if (valueA < valueB) return direction === 'asc' ? -1 : 1;
        if (valueA > valueB) return direction === 'asc' ? 1 : -1;
        return 0;
    });
    
    // Re-render the table
    renderPatientTable(patients);
}

function formatPatientName(patient) {
    if (!patient) return 'Unknown';
    
    let name = '';
    
    if (patient.last) {
        name += patient.last;
        
        if (patient.first) {
            name += `, ${patient.first}`;
            
            if (patient.middle) {
                name += ` ${patient.middle}`;
            }
        }
    } else if (patient.first) {
        name = patient.first;
        
        if (patient.middle) {
            name += ` ${patient.middle}`;
        }
    } else {
        name = 'Unknown';
    }
    
    return name;
}

function formatAddress(patient) {
    if (!patient) return 'Unknown';
    
    let address = patient.patientAddress || '';
    
    if (patient.city || patient.state || patient.zip) {
        if (address) address += ', ';
        
        if (patient.city) {
            address += patient.city;
        }
        
        if (patient.state) {
            if (patient.city) {
                address += `, ${patient.state}`;
            } else {
                address += patient.state;
            }
        }
        
        if (patient.zip) {
            address += ` ${patient.zip}`;
        }
    }
    
    return address || 'Unknown';
}

function showAlert(type, message) {
    alertContainer.innerHTML = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
}

function showLoadingPatients(show) {
    loadingPatientsElement.style.display = show ? 'block' : 'none';
}

function showPatientTableContainer(show) {
    patientTableContainer.style.display = show ? 'block' : 'none';
}

function showNoPatients(show) {
    noPatients.style.display = show ? 'block' : 'none';
}

function showPatientList() {
    patientListView.style.display = 'block';
    patientDetailView.style.display = 'none';
}

function showPatientDetail() {
    patientListView.style.display = 'none';
    patientDetailView.style.display = 'block';
}

function showLoadingPatientDetail(show) {
    loadingPatientDetail.style.display = show ? 'block' : 'none';
}

function showPatientDetailContainer(show) {
    patientDetailContainer.style.display = show ? 'block' : 'none';
}

function showPatientNotFound(show) {
    patientNotFound.style.display = show ? 'block' : 'none';
}

function showLoadingMedicalRecords(show) {
    loadingMedicalRecords.style.display = show ? 'block' : 'none';
}

function showMedicalRecordsContainer(show) {
    medicalRecordsContainer.style.display = show ? 'block' : 'none';
}

function showNoMedicalRecords(show) {
    noMedicalRecords.style.display = show ? 'block' : 'none';
}

function debounce(func, wait) {
    let timeout;
    return function() {
        const context = this;
        const args = arguments;
        clearTimeout(timeout);
        timeout = setTimeout(() => {
            func.apply(context, args);
        }, wait);
    };
}
