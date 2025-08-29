# SMPP Connectivity Tester for .NET Framework 4.0

A simple console application to test SMPP (Short Message Peer-to-Peer) connectivity with SMS servers. This application is specifically designed for .NET Framework 4.0 environments.

## Features

- TCP connection testing to SMPP servers
- SMPP bind_transceiver authentication
- Connection health testing with enquire_link
- Comprehensive error reporting
- Command-line interface with parameter validation
- Compatible with .NET Framework 4.0

## Requirements

- .NET Framework 4.0 or later
- Visual Studio 2010 or later (for building from source)
- Or MSBuild/.NET Framework 4.0 SDK

## Building the Application

### Option 1: Using the Build Script (Windows)
```cmd
build.bat
```

### Option 2: Using MSBuild Directly
```cmd
msbuild SMPPTester.sln /p:Configuration=Release /p:Platform=x86
```

### Option 3: Using Visual Studio
1. Open `SMPPTester.sln` in Visual Studio
2. Build the solution (Build → Build Solution)

### Option 4: Using C# Compiler Directly
```cmd
%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:exe /out:SMPPTester.exe SMPPTester\*.cs
```

## Usage

```cmd
SMPPTester.exe -h <host> -p <port> -u <username> -w <password> [options]
```

### Required Parameters
- `-h, --host`: SMPP server hostname or IP address
- `-p, --port`: SMPP server port number (typically 2775)
- `-u, --username`: SMPP username/system_id
- `-w, --password`: SMPP password

### Optional Parameters
- `-t, --timeout`: Connection timeout in milliseconds (default: 30000)
- `--help`: Show help message

### Examples

```cmd
# Basic connectivity test
SMPPTester.exe -h 192.168.1.100 -p 2775 -u testuser -w testpass

# Test with custom timeout
SMPPTester.exe -h smpp.example.com -p 2775 -u myuser -w mypass -t 60000

# Using long parameter names
SMPPTester.exe --host 10.0.0.50 --port 2775 --username smsuser --password smspass --timeout 45000
```

## What the Tool Does

1. **TCP Connection**: Establishes a TCP connection to the specified SMPP server
2. **SMPP Bind**: Sends a `bind_transceiver` PDU with provided credentials
3. **Connection Test**: Sends an `enquire_link` PDU to verify the connection is active
4. **Clean Disconnect**: Sends an `unbind` PDU and closes the connection properly

## Test Results

The application provides clear feedback on the connection status:

- **SUCCESS**: Full SMPP connectivity established and tested
- **PARTIAL SUCCESS**: SMPP bind succeeded but enquire_link failed
- **FAILURE**: Could not establish SMPP connection
- **ERROR**: Unexpected error occurred during testing

## Common SMPP Ports

- **2775**: Standard SMPP port
- **9999**: Alternative SMPP port
- **8080**: HTTP-based SMPP (rare)

## Troubleshooting

### Connection Refused
- Verify the host and port are correct
- Check if the SMPP server is running
- Ensure no firewall is blocking the connection

### Authentication Failed
- Verify username and password are correct
- Check if your account is enabled on the SMPP server
- Confirm you have the right to bind as a transceiver

### Timeout Issues
- Increase the timeout value with `-t` parameter
- Check network connectivity and latency
- Verify the server is responding

### Build Issues
- Ensure .NET Framework 4.0 is installed
- Try using the alternative build methods listed above
- Check that all source files are present

## SMPP Protocol Details

This tool implements SMPP v3.4 and uses:
- `bind_transceiver` for authentication (allows both sending and receiving)
- `enquire_link` for connection health testing
- `unbind` for clean disconnection

## Files Structure

```
SMPPTester/
├── SMPPTester.sln          # Visual Studio solution
├── build.bat               # Build script for Windows
├── README.md               # This documentation
└── SMPPTester/
    ├── SMPPTester.csproj    # Project file
    ├── Program.cs           # Main application logic
    ├── SMPPClient.cs        # SMPP protocol implementation
    └── Properties/
        └── AssemblyInfo.cs  # Assembly metadata
```

## License

This is a utility tool for testing SMPP connectivity. Use it responsibly and in accordance with your SMPP server provider's terms of service.

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Verify your SMPP server configuration
3. Test with a known working SMPP client to confirm server functionality