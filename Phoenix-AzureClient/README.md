# Phoenix-Azure Client

## Overview
The Phoenix-Azure Client is a web-based frontend for the Phoenix-Azure application. It provides a user-friendly interface for viewing patient information, medical records, and exploring data repositories.

## Features
- **Patient List**: View a list of all patients with search and sort capabilities
- **Patient Details**: View detailed patient information and medical records
- **Repository Explorer**: Explore available data repositories and execute repository methods
- **FHIR Explorer**: Browse and validate FHIR resources with automatic validation
  - View Patient resources in FHIR format
  - View Patient Bundle lists with all patients
  - Automatic validation against FHIR standards
  - Detailed validation feedback with severity indicators
  - User-friendly loading states with progress information

## Architecture
The client follows a simple, modular architecture:
- **HTML**: Defines the structure and UI components
- **JavaScript**: Handles data fetching, manipulation, and UI interactions
- **Bootstrap**: Provides responsive styling and UI components

## Pages
- **index.html**: Main page for viewing patient information
- **repository-explorer.html**: Dedicated page for exploring data repositories
- **fhir-explorer.html**: FHIR resource browser and validator
- **fhir-demo.html**: Demonstration of FHIR capabilities

## JavaScript Modules
- **app.js**: Handles patient data and UI interactions for the main page
- **repository-explorer.js**: Manages repository exploration functionality
- **fhir-explorer.js**: Handles FHIR resource fetching, display, and validation

## API Integration
The client connects to the Phoenix-Azure API to retrieve data:
- Base URL: `http://localhost:5300/api`
- FHIR API URL: `http://localhost:5300/api/fhir`
- Endpoints for patients, medical records, repository exploration, and FHIR resources
- All requests use fetch API with proper error handling

## Getting Started

### Prerequisites
- Modern web browser (Chrome, Firefox, Edge, etc.)
- Phoenix-Azure API running on http://localhost:5300

### Running the Client
1. Navigate to the Phoenix-AzureClient directory
2. Start a web server (e.g., `python -m http.server 8080`)
3. Open a browser and navigate to http://localhost:8080

## User Interface
- **Navigation**: Top navigation bar for switching between pages
- **Patient List**: Table with patient ID, name, status, and actions
- **Patient Details**: Comprehensive view of patient information and medical records
- **Repository Explorer**: Interactive interface for exploring repositories and executing methods
- **FHIR Explorer**: Interactive interface for browsing and validating FHIR resources
  - Resource type selector with common FHIR resource types
  - Resource ID input for retrieving specific resources
  - JSON viewer with syntax highlighting
  - Validation results with color-coded severity indicators
  - Direct access to Patient Bundle via navigation link

## Development
- The client uses vanilla JavaScript without frameworks
- Bootstrap 5 for responsive UI components
- Fetch API for asynchronous data retrieval
