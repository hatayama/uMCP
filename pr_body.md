## Overview
This PR enhances the Unity MCP server with automatic port adjustment capabilities, improves code maintainability through refactoring, and fixes UI display bugs.

## Details

### Key Features Implemented
1. **Automatic Port Discovery**: When a requested port is occupied, the system automatically searches for the next available port (up to 10 attempts)
2. **User Confirmation Dialog**: Shows a confirmation dialog when port adjustment is needed, allowing users to approve or cancel the operation
3. **Smart Port Selection**: Automatically skips commonly used system ports (80, 443, 22, etc.)
4. **Port Conflict Warning System**: Displays warning messages in the editor window when port conflicts are detected
5. **LLM Tool Settings Integration**: Fixed port mismatch detection between server and configuration files

### Code Quality Improvements
- **Method Extraction Refactoring**: Refactored `McpConfigService.GetConfiguredPort()` method using Martin Fowler's Extract Method pattern
  - Split single responsibility violations into 4 focused methods
  - Improved testability and maintainability
  - Enhanced code readability and future extensibility

### Bug Fixes
- **UI Display Logic**: Fixed exclusive display of warning and normal status messages in LLM Tool Settings
  - Previously both warning and "already configured" messages were shown simultaneously
  - Now shows either warning OR normal status message, never both
- **Port Mismatch Detection**: Improved accuracy of port conflict detection across multiple configurations
- **Logger Synchronization**: Fixed Enable MCP Logs toggle synchronization with McpLogger.EnableDebugLog

### Testing & Tools
- **Comprehensive Test Coverage**: Added 14 test cases covering port management and UI warning functionality
  - Port availability detection tests
  - Automatic port adjustment validation
  - UI warning system verification
- **Development Tools**: Added Python port blocker script for testing automatic port adjustment

### Files Modified
- `McpServerController.cs`: Added automatic port adjustment logic with user confirmation
- `McpConfigService.cs`: Refactored port detection methods using Extract Method pattern
- `McpEditorWindowView.cs`: Fixed exclusive display logic for status messages
- `McpEditorModel.cs`: Added logger synchronization to maintain MVP pattern
- `McpServerPortTests.cs`: New comprehensive test suite for port management
- `McpEditorWindowPortWarningTests.cs`: New test suite for UI warning functionality
- `test_port_blocker.py`: New development tool for testing port conflicts

### Technical Architecture
- Maintained MVP (Model-View-Presenter) architecture patterns
- Followed single responsibility principle in refactored methods
- Implemented fail-fast validation with graceful error handling
- Used immutable value objects for data transfer

## Related Documents
- [Unity MCP Documentation](https://github.com/hatayama/uMCP)
- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [Martin Fowler's Refactoring Catalog](https://refactoring.com/catalog/)