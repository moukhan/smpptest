using System;
using System.Threading;

namespace SMPPTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SMPP Connectivity Tester v1.0");
            Console.WriteLine("==============================");
            Console.WriteLine();

            // Parse command line arguments
            string host = null;
            int port = 0;
            string username = null;
            string password = null;
            int timeout = 30000; // Default 30 seconds
            BindMode bindMode = BindMode.Transceiver; // Default bind mode
            bool debug = false;

            try
            {
                if (args.Length == 0)
                {
                    ShowUsage();
                    return;
                }

                // Parse arguments
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "-h":
                        case "--host":
                            if (i + 1 < args.Length)
                            {
                                host = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("Error: Host parameter requires a value");
                                return;
                            }
                            break;

                        case "-p":
                        case "--port":
                            if (i + 1 < args.Length)
                            {
                                if (!int.TryParse(args[++i], out port) || port <= 0 || port > 65535)
                                {
                                    Console.WriteLine("Error: Port must be a valid number between 1 and 65535");
                                    return;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error: Port parameter requires a value");
                                return;
                            }
                            break;

                        case "-u":
                        case "--username":
                            if (i + 1 < args.Length)
                            {
                                username = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("Error: Username parameter requires a value");
                                return;
                            }
                            break;

                        case "-w":
                        case "--password":
                            if (i + 1 < args.Length)
                            {
                                password = args[++i];
                            }
                            else
                            {
                                Console.WriteLine("Error: Password parameter requires a value");
                                return;
                            }
                            break;

                        case "-t":
                        case "--timeout":
                            if (i + 1 < args.Length)
                            {
                                if (!int.TryParse(args[++i], out timeout) || timeout <= 0)
                                {
                                    Console.WriteLine("Error: Timeout must be a positive number (milliseconds)");
                                    return;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error: Timeout parameter requires a value");
                                return;
                            }
                            break;

                        case "-m":
                        case "--mode":
                            if (i + 1 < args.Length)
                            {
                                string modeStr = args[++i].ToLower();
                                switch (modeStr)
                                {
                                    case "transmitter":
                                    case "tx":
                                        bindMode = BindMode.Transmitter;
                                        break;
                                    case "receiver":
                                    case "rx":
                                        bindMode = BindMode.Receiver;
                                        break;
                                    case "transceiver":
                                    case "trx":
                                        bindMode = BindMode.Transceiver;
                                        break;
                                    default:
                                        Console.WriteLine($"Error: Invalid bind mode '{args[i]}'. Use: transmitter, receiver, or transceiver");
                                        return;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error: Mode parameter requires a value");
                                return;
                            }
                            break;

                        case "-d":
                        case "--debug":
                            debug = true;
                            break;

                        case "--help":
                            ShowUsage();
                            return;

                        default:
                            Console.WriteLine($"Error: Unknown parameter '{args[i]}'");
                            ShowUsage();
                            return;
                    }
                }

                // Validate required parameters
                if (string.IsNullOrEmpty(host))
                {
                    Console.WriteLine("Error: Host parameter is required");
                    ShowUsage();
                    return;
                }

                if (port == 0)
                {
                    Console.WriteLine("Error: Port parameter is required");
                    ShowUsage();
                    return;
                }

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Error: Username parameter is required");
                    ShowUsage();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("Error: Password parameter is required");
                    ShowUsage();
                    return;
                }

                // Display connection parameters
                Console.WriteLine("Connection Parameters:");
                Console.WriteLine($"  Host: {host}");
                Console.WriteLine($"  Port: {port}");
                Console.WriteLine($"  Username: {username}");
                Console.WriteLine($"  Password: {new string('*', password.Length)}");
                Console.WriteLine($"  Bind Mode: {bindMode}");
                Console.WriteLine($"  Timeout: {timeout} ms");
                Console.WriteLine($"  Debug: {debug}");
                Console.WriteLine();

                // Test SMPP connectivity
                TestSMPPConnection(host, port, username, password, timeout, bindMode, debug);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void TestSMPPConnection(string host, int port, string username, string password, int timeout, BindMode bindMode, bool debug)
        {
            SMPPClient client = null;
            
            try
            {
                Console.WriteLine("=== SMPP Connection Test ===");
                Console.WriteLine();

                // Create SMPP client
                client = new SMPPClient(host, port, username, password, bindMode);

                // Test connection
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Starting connection test...");
                
                bool connected = client.Connect(timeout);
                
                if (connected)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✓ SMPP connection successful!");
                    Console.WriteLine();

                    // Test enquire_link to verify the connection is working
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Testing connection with enquire_link...");
                    bool testResult = client.TestConnection();
                    
                    if (testResult)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✓ Connection test successful!");
                        Console.WriteLine();
                        Console.WriteLine("=== TEST RESULT: SUCCESS ===");
                        Console.WriteLine("Your SMPP server is reachable and accepting connections.");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Connection test failed!");
                        Console.WriteLine();
                        Console.WriteLine("=== TEST RESULT: PARTIAL SUCCESS ===");
                        Console.WriteLine("SMPP bind succeeded but enquire_link failed.");
                        Console.WriteLine("The server may not be fully functional.");
                    }

                    // Keep connection alive for a few seconds to demonstrate
                    Console.WriteLine();
                    Console.WriteLine("Keeping connection alive for 5 seconds...");
                    Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ SMPP connection failed!");
                    Console.WriteLine();
                    Console.WriteLine("=== TEST RESULT: FAILURE ===");
                    Console.WriteLine("Could not establish SMPP connection.");
                    Console.WriteLine();
                    Console.WriteLine("Possible causes:");
                    Console.WriteLine("- Incorrect host or port");
                    Console.WriteLine("- Network connectivity issues");
                    Console.WriteLine("- Firewall blocking the connection");
                    Console.WriteLine("- Invalid username or password");
                    Console.WriteLine("- SMPP server is down or not accepting connections");
                    Console.WriteLine("- Server requires different SMPP version or parameters");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Connection test error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("=== TEST RESULT: ERROR ===");
                Console.WriteLine("An unexpected error occurred during the test.");
                
                if (ex.Message.Contains("No connection could be made"))
                {
                    Console.WriteLine("This typically indicates:");
                    Console.WriteLine("- The server is not running");
                    Console.WriteLine("- Wrong host or port");
                    Console.WriteLine("- Network/firewall issues");
                }
                else if (ex.Message.Contains("timeout"))
                {
                    Console.WriteLine("This indicates:");
                    Console.WriteLine("- The server is not responding");
                    Console.WriteLine("- Network latency issues");
                    Console.WriteLine("- Try increasing the timeout value");
                }
            }
            finally
            {
                // Clean up
                if (client != null)
                {
                    try
                    {
                        client.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during disconnect: {ex.Message}");
                    }
                }
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage: SMPPTester.exe -h <host> -p <port> -u <username> -w <password> [options]");
            Console.WriteLine();
            Console.WriteLine("Required Parameters:");
            Console.WriteLine("  -h, --host      SMPP server hostname or IP address");
            Console.WriteLine("  -p, --port      SMPP server port number (typically 2775)");
            Console.WriteLine("  -u, --username  SMPP username/system_id");
            Console.WriteLine("  -w, --password  SMPP password");
            Console.WriteLine();
            Console.WriteLine("Optional Parameters:");
            Console.WriteLine("  -m, --mode      Bind mode: transmitter, receiver, or transceiver (default: transceiver)");
            Console.WriteLine("  -t, --timeout   Connection timeout in milliseconds (default: 30000)");
            Console.WriteLine("  -d, --debug     Enable debug output");
            Console.WriteLine("      --help      Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SMPPTester.exe -h 192.168.1.100 -p 2775 -u testuser -w testpass");
            Console.WriteLine("  SMPPTester.exe -h 172.16.11.71 -p 10000 -u username -w password -m transmitter -d");
            Console.WriteLine("  SMPPTester.exe --host smpp.example.com --port 2775 --username myuser --password mypass --mode receiver --timeout 60000");
            Console.WriteLine();
            Console.WriteLine("This tool will:");
            Console.WriteLine("1. Establish a TCP connection to the SMPP server");
            Console.WriteLine("2. Send a bind PDU (transmitter/receiver/transceiver) with your credentials");
            Console.WriteLine("3. Test the connection with an enquire_link PDU");
            Console.WriteLine("4. Report the results and disconnect cleanly");
            Console.WriteLine();
            Console.WriteLine("Bind Modes:");
            Console.WriteLine("  transmitter  - Can send SMS messages");
            Console.WriteLine("  receiver     - Can receive SMS messages");
            Console.WriteLine("  transceiver  - Can both send and receive SMS messages (default)");
        }
    }
}