#!/bin/bash

echo "Stopping any existing processes on ports 5300 and 8080..."
lsof -i :5300 -i :8080 | grep LISTEN | awk '{print $2}' | xargs kill -9 2>/dev/null || echo "No processes to kill"

echo "Starting API server on port 5300..."
cd Phoenix-AzureAPI
dotnet run &
API_PID=$!

echo "Waiting for API server to start..."
sleep 3

echo "Starting client server on port 8080..."
cd ../Phoenix-AzureClient
python3 -m http.server 8080 &
CLIENT_PID=$!

echo "Opening application in browser..."
open http://localhost:8080/index.html

echo "Application is running!"
echo "API server PID: $API_PID"
echo "Client server PID: $CLIENT_PID"
echo ""
echo "Access the application at:"
echo "- SQL Explorer: http://localhost:8080/index.html"
echo "- FHIR Explorer: http://localhost:8080/fhir-explorer.html"
echo "- FHIR Demo: http://localhost:8080/fhir-demo.html"
echo ""
echo "Press Ctrl+C to stop all servers"

# Wait for user to press Ctrl+C
trap "echo 'Stopping servers...'; kill $API_PID $CLIENT_PID; echo 'Servers stopped.'; exit" INT
wait
