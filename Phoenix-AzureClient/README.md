# Phoenix-Azure Client

## Overview
The Phoenix-Azure Client is a web-based frontend for the Phoenix-Azure application. It provides a user-friendly interface for viewing patient information, medical records, and exploring data repositories.

## Features
- **Patient List**: View a list of all patients with search and sort capabilities
- **Patient Details**: View detailed patient information and medical records
- **Repository Explorer**: Explore available data repositories and execute repository methods

## Architecture
The client follows a simple, modular architecture:
- **HTML**: Defines the structure and UI components
- **JavaScript**: Handles data fetching, manipulation, and UI interactions
- **Bootstrap**: Provides responsive styling and UI components

## Pages
- **index.html**: Main page for viewing patient information
- **repository-explorer.html**: Dedicated page for exploring data repositories

## JavaScript Modules
- **app.js**: Handles patient data and UI interactions for the main page
- **repository-explorer.js**: Manages repository exploration functionality

## API Integration
The client connects to the Phoenix-Azure API to retrieve data:
- Base URL: `http://localhost:5300/api`
- Endpoints for patients, medical records, and repository exploration
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

## Development
- The client uses vanilla JavaScript without frameworks
- Bootstrap 5 for responsive UI components
- Fetch API for asynchronous data retrieval
