#!/bin/bash

# Initialize first
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{"roots":{"listChanged":false},"sampling":{},"experimental":{},"logging":{}},"clientInfo":{"name":"test-client","version":"1.0.0"}}}' | node dist/server.bundle.js &

# Wait for initialization
sleep 2

# Then send resources/list
echo '{"jsonrpc":"2.0","id":2,"method":"resources/list","params":{}}' | node dist/server.bundle.js