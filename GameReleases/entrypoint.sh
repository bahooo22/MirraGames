#!/bin/sh
# /entrypoint.sh
set -e

# Проверяем браузер по правильному пути
EXPECTED_BROWSER_PATH="/root/.cache/ms-playwright/chromium_headless_shell-1187/chrome-linux/headless_shell"

if [ -f "$EXPECTED_BROWSER_PATH" ]; then
    echo "✅ Playwright browsers are ready at: $EXPECTED_BROWSER_PATH"
else
    echo "❌ Playwright browsers not found at: $EXPECTED_BROWSER_PATH"
    echo "Debug info:"
    echo "Contents of /ms-playwright:"
    ls -la /ms-playwright/
    echo "Contents of /root/.cache/ms-playwright:"
    ls -la /root/.cache/ms-playwright/
fi

echo "=== Starting Application ==="
exec dotnet GameReleases.WebApi.dll
