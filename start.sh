#!/bin/bash
API_HEALTH="https://localhost:7128/health"

# Start the API first
osascript -e 'tell app "Terminal" to do script "cd \"'$PWD'/API\" && dotnet run"'

# Wait until the API health endpoint responds with HTTP 200
echo "Waiting for API to be ready..."
until curl -sk "$API_HEALTH" | grep -q "healthy"; do
    sleep 2
done
echo "API is ready — starting UI apps."

osascript -e 'tell app "Terminal" to do script "cd \"'$PWD'/UI-User\" && pnpm dev"'
osascript -e 'tell app "Terminal" to do script "cd \"'$PWD'/UI-User\" && dotnet run"'
osascript -e 'tell app "Terminal" to do script "cd \"'$PWD'/UI-Admin\" && pnpm dev"'
osascript -e 'tell app "Terminal" to do script "cd \"'$PWD'/UI-Admin\" && dotnet run"'
