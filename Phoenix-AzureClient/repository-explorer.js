// Constants
const API_BASE_URL = 'http://localhost:5300/api';

// DOM Elements
const repositoryListElement = document.getElementById('repositoryList');
const repositoryDataElement = document.getElementById('repositoryData');
const repositorySelectElement = document.getElementById('repositorySelect');
const methodSelectElement = document.getElementById('methodSelect');
const parameterInputElement = document.getElementById('parameterInput');
const methodExecutionFormElement = document.getElementById('methodExecutionForm');
const methodResultElement = document.getElementById('methodResult');

// Global variables
let availableRepositories = [];
let currentRepository = null;
let currentMethod = null;

// Event listeners
document.addEventListener('DOMContentLoaded', function() {
    // Load available repositories when the page loads
    loadAvailableRepositories();
    
    // Add event listener for repository selection
    repositorySelectElement.addEventListener('change', function() {
        const repositoryName = this.value;
        if (repositoryName) {
            loadRepositoryMethods(repositoryName);
        } else {
            // Clear method select if no repository is selected
            clearMethodSelect();
        }
    });
    
    // Add event listener for method execution form
    methodExecutionFormElement.addEventListener('submit', function(event) {
        event.preventDefault();
        
        const repositoryName = repositorySelectElement.value;
        const methodName = methodSelectElement.value;
        const parameter = parameterInputElement.value;
        
        if (repositoryName && methodName) {
            executeRepositoryMethod(repositoryName, methodName, parameter);
        }
    });
});

// API Functions
async function loadAvailableRepositories() {
    try {
        const response = await fetch(`${API_BASE_URL}/RepositoryExplorer/available`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        availableRepositories = await response.json();
        
        renderRepositoryList(availableRepositories);
        populateRepositorySelect(availableRepositories);
    } catch (error) {
        console.error('Error loading available repositories:', error);
        showErrorLoadingRepositories();
    }
}

async function loadRepositoryData(repositoryName) {
    try {
        const response = await fetch(`${API_BASE_URL}/RepositoryExplorer/${repositoryName}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const repositoryData = await response.json();
        
        currentRepository = repositoryData;
        renderRepositoryData(repositoryData);
    } catch (error) {
        console.error(`Error loading repository data for ${repositoryName}:`, error);
        showErrorLoadingRepositoryData(repositoryName);
    }
}

async function loadRepositoryMethods(repositoryName) {
    try {
        const response = await fetch(`${API_BASE_URL}/RepositoryExplorer/${repositoryName}/methods`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const methods = await response.json();
        populateMethodSelect(methods);
    } catch (error) {
        console.error(`Error loading methods for repository ${repositoryName}:`, error);
        clearMethodSelect();
    }
}

async function executeRepositoryMethod(repositoryName, methodName, parameter) {
    try {
        showLoadingMethodResult();
        
        let url = `${API_BASE_URL}/RepositoryExplorer/explore/${repositoryName}/${methodName}`;
        
        // Add parameter if provided
        if (parameter) {
            url += `?parameters=${encodeURIComponent(parameter)}`;
        }
        
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            mode: 'cors'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const result = await response.json();
        
        const resultStr = JSON.stringify(result, null, 2);
        
        showMethodResult(resultStr);
    } catch (error) {
        console.error(`Error executing method ${methodName} on repository ${repositoryName}:`, error);
        showMethodError(error.message);
    }
}

// UI Functions
function renderRepositoryList(repositories) {
    // Clear existing list
    repositoryListElement.innerHTML = '';
    
    // Add repositories to the list
    repositories.forEach(repo => {
        const item = document.createElement('a');
        item.href = '#';
        item.className = 'list-group-item list-group-item-action';
        item.innerHTML = `
            <div class="d-flex w-100 justify-content-between">
                <h6 class="mb-1">${repo.name}</h6>
            </div>
            <small>${repo.description || 'No description available'}</small>
        `;
        
        item.addEventListener('click', function(event) {
            event.preventDefault();
            
            // Update active state
            document.querySelectorAll('#repositoryList a').forEach(el => {
                el.classList.remove('active');
            });
            this.classList.add('active');
            
            // Load repository data
            loadRepositoryData(repo.name);
        });
        
        repositoryListElement.appendChild(item);
    });
}

function renderRepositoryData(data) {
    let html = `
        <h5>${data.name}</h5>
        <p>${data.description || 'No description available.'}</p>
    `;
    
    if (data.methods && data.methods.length > 0) {
        html += `
            <h6>Available Methods:</h6>
            <ul class="list-group">
        `;
        
        data.methods.forEach(method => {
            html += `
                <li class="list-group-item">
                    <strong>${method}</strong>
                </li>
            `;
        });
        
        html += '</ul>';
    } else {
        html += '<p>No methods available for this repository.</p>';
    }
    
    repositoryDataElement.innerHTML = html;
}

function populateRepositorySelect(repositories) {
    // Clear existing options except the first one
    while (repositorySelectElement.options.length > 1) {
        repositorySelectElement.remove(1);
    }
    
    // Add new options
    repositories.forEach(repo => {
        const option = document.createElement('option');
        option.value = repo.name;
        option.textContent = repo.name;
        repositorySelectElement.appendChild(option);
    });
}

function populateMethodSelect(methods) {
    // Clear existing options except the first one
    while (methodSelectElement.options.length > 1) {
        methodSelectElement.remove(1);
    }
    
    // Add new options
    methods.forEach(method => {
        const option = document.createElement('option');
        option.value = method;
        option.textContent = method;
        methodSelectElement.appendChild(option);
    });
}

function clearMethodSelect() {
    // Clear existing options except the first one
    while (methodSelectElement.options.length > 1) {
        methodSelectElement.remove(1);
    }
}

function showErrorLoadingRepositories() {
    repositoryListElement.innerHTML = `
        <div class="alert alert-danger">
            <i class="bi bi-exclamation-triangle-fill"></i> Error loading repositories. Please try again later.
        </div>
    `;
}

function showErrorLoadingRepositoryData(repositoryName) {
    repositoryDataElement.innerHTML = `
        <div class="alert alert-danger">
            <i class="bi bi-exclamation-triangle-fill"></i> Error loading data for repository '${repositoryName}'. Please try again later.
        </div>
    `;
}

function showLoadingMethodResult() {
    methodResultElement.innerHTML = `
        <div class="d-flex align-items-center">
            <div class="spinner-border text-primary me-2" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <span>Executing method...</span>
        </div>
    `;
}

function showMethodResult(result) {
    methodResultElement.innerHTML = `
        <pre class="mb-0">${result}</pre>
    `;
}

function showMethodError(errorMessage) {
    methodResultElement.innerHTML = `
        <div class="alert alert-danger">
            <i class="bi bi-exclamation-triangle-fill"></i> Error: ${errorMessage}
        </div>
    `;
}
