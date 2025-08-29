using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SMPPTester
{
    public enum BindMode
    {
        Transmitter,
        Receiver,
        Transceiver
    }

    public class SMPPClient
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private string host;
        private int port;
        private string systemId;
        private string password;
        private bool isConnected = false;
        private int sequenceNumber = 1;
        private BindMode bindMode;

        // SMPP Command IDs
        private const uint BIND_TRANSMITTER = 0x00000001;
        private const uint BIND_TRANSMITTER_RESP = 0x80000001;
        private const uint BIND_RECEIVER = 0x00000002;
        private const uint BIND_RECEIVER_RESP = 0x80000002;
        private const uint BIND_TRANSCEIVER = 0x00000009;
        private const uint BIND_TRANSCEIVER_RESP = 0x80000009;
        private const uint UNBIND = 0x00000006;
        private const uint UNBIND_RESP = 0x80000006;
        private const uint ENQUIRE_LINK = 0x00000015;
        private const uint ENQUIRE_LINK_RESP = 0x80000015;

        // SMPP Status codes
        private const uint ESME_ROK = 0x00000000;

        public SMPPClient(string host, int port, string systemId, string password, BindMode bindMode = BindMode.Transceiver)
        {
            this.host = host;
            this.port = port;
            this.systemId = systemId;
            this.password = password;
            this.bindMode = bindMode;
        }

        public bool Connect(int timeoutMs = 30000)
        {
            try
            {
                Console.WriteLine($"Connecting to SMPP server at {host}:{port}...");
                
                tcpClient = new TcpClient();
                
                // Set connection timeout
                var result = tcpClient.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
                
                if (!success)
                {
                    tcpClient.Close();
                    Console.WriteLine("Connection timeout");
                    return false;
                }

                tcpClient.EndConnect(result);
                stream = tcpClient.GetStream();
                
                Console.WriteLine($"TCP connection established. Attempting SMPP bind ({bindMode})...");
                
                // Send bind PDU based on mode
                bool bindResult = SendBind();
                if (bindResult)
                {
                    isConnected = true;
                    Console.WriteLine("SMPP bind successful!");
                    return true;
                }
                else
                {
                    Console.WriteLine("SMPP bind failed!");
                    Disconnect();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                return false;
            }
        }

        private bool SendBind()
        {
            try
            {
                // Create bind PDU based on mode
                var pdu = CreateBindPDU();
                
                // Send PDU
                stream.Write(pdu, 0, pdu.Length);
                Console.WriteLine($"Bind {bindMode} PDU sent (Length: {pdu.Length} bytes)");
                
                // Add debug output
                Console.WriteLine($"PDU Hex: {BitConverter.ToString(pdu).Replace("-", " ")}");
                
                // Read response
                byte[] response = ReadPDU();
                if (response != null)
                {
                    return ParseBindResponse(response);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bind error: {ex.Message}");
                return false;
            }
        }

        private byte[] CreateBindPDU()
        {
            // Get command ID based on bind mode
            uint commandId;
            switch (bindMode)
            {
                case BindMode.Transmitter:
                    commandId = BIND_TRANSMITTER;
                    break;
                case BindMode.Receiver:
                    commandId = BIND_RECEIVER;
                    break;
                case BindMode.Transceiver:
                    commandId = BIND_TRANSCEIVER;
                    break;
                default:
                    commandId = BIND_TRANSCEIVER;
                    break;
            }

            var bodyStream = new MemoryStream();
            
            // System_id (null-terminated string, max 16 bytes including null)
            WriteNullTerminatedString(bodyStream, systemId, 16);
            
            // Password (null-terminated string, max 9 bytes including null)  
            WriteNullTerminatedString(bodyStream, password, 9);
            
            // System_type (null-terminated string, max 13 bytes including null)
            WriteNullTerminatedString(bodyStream, "", 13); // Empty system type
            
            // Interface_version (1 byte)
            bodyStream.WriteByte(0x34); // SMPP version 3.4
            
            // Addr_ton (1 byte)
            bodyStream.WriteByte(0x00);
            
            // Addr_npi (1 byte) 
            bodyStream.WriteByte(0x00);
            
            // Address_range (null-terminated string, max 41 bytes including null)
            WriteNullTerminatedString(bodyStream, "", 41); // Empty address range
            
            byte[] body = bodyStream.ToArray();
            
            // Create header
            var headerStream = new MemoryStream();
            
            // Command_length (4 bytes)
            uint commandLength = (uint)(16 + body.Length); // Header is 16 bytes
            WriteUint32BigEndian(headerStream, commandLength);
            
            // Command_id (4 bytes)
            WriteUint32BigEndian(headerStream, commandId);
            
            // Command_status (4 bytes)
            WriteUint32BigEndian(headerStream, 0);
            
            // Sequence_number (4 bytes)
            WriteUint32BigEndian(headerStream, (uint)sequenceNumber++);
            
            byte[] header = headerStream.ToArray();
            
            // Combine header and body
            byte[] pdu = new byte[header.Length + body.Length];
            Array.Copy(header, 0, pdu, 0, header.Length);
            Array.Copy(body, 0, pdu, header.Length, body.Length);
            
            Console.WriteLine($"Created bind PDU: Command=0x{commandId:X8}, Length={commandLength}, SeqNum={sequenceNumber-1}");
            Console.WriteLine($"SystemId='{systemId}', Password='{new string('*', password.Length)}'");
            
            return pdu;
        }

        private void WriteNullTerminatedString(MemoryStream stream, string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Write null bytes to fill the field
                for (int i = 0; i < maxLength; i++)
                {
                    stream.WriteByte(0);
                }
            }
            else
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value);
                int bytesToWrite = Math.Min(bytes.Length, maxLength - 1); // Reserve space for null terminator
                
                // Write the string bytes
                stream.Write(bytes, 0, bytesToWrite);
                
                // Fill remaining space with null bytes
                for (int i = bytesToWrite; i < maxLength; i++)
                {
                    stream.WriteByte(0);
                }
            }
        }

        private void WriteUint32BigEndian(MemoryStream stream, uint value)
        {
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }

        private byte[] ReadPDU()
        {
            try
            {
                // Read header first (16 bytes)
                byte[] header = new byte[16];
                int bytesRead = 0;
                
                while (bytesRead < 16)
                {
                    int read = stream.Read(header, bytesRead, 16 - bytesRead);
                    if (read == 0)
                    {
                        Console.WriteLine("Connection closed by server");
                        return null;
                    }
                    bytesRead += read;
                }
                
                // Parse command length
                uint commandLength = BitConverter.ToUInt32(ReverseBytes(new byte[] { header[0], header[1], header[2], header[3] }), 0);
                
                if (commandLength < 16 || commandLength > 65535)
                {
                    Console.WriteLine($"Invalid command length: {commandLength}");
                    return null;
                }
                
                // Read remaining PDU body if any
                byte[] pdu = new byte[commandLength];
                Array.Copy(header, pdu, 16);
                
                if (commandLength > 16)
                {
                    bytesRead = 16;
                    while (bytesRead < commandLength)
                    {
                        int read = stream.Read(pdu, bytesRead, (int)(commandLength - bytesRead));
                        if (read == 0)
                        {
                            Console.WriteLine("Connection closed by server");
                            return null;
                        }
                        bytesRead += read;
                    }
                }
                
                return pdu;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading PDU: {ex.Message}");
                return null;
            }
        }

        private bool ParseBindResponse(byte[] pdu)
        {
            try
            {
                if (pdu.Length < 16)
                {
                    Console.WriteLine("Invalid PDU length");
                    return false;
                }
                
                // Parse command_id
                uint commandId = BitConverter.ToUInt32(ReverseBytes(new byte[] { pdu[4], pdu[5], pdu[6], pdu[7] }), 0);
                
                // Parse command_status
                uint commandStatus = BitConverter.ToUInt32(ReverseBytes(new byte[] { pdu[8], pdu[9], pdu[10], pdu[11] }), 0);
                
                Console.WriteLine($"Received PDU - Command ID: 0x{commandId:X8}, Status: 0x{commandStatus:X8}");
                
                // Check if this is the expected bind response
                bool isExpectedResponse = false;
                switch (bindMode)
                {
                    case BindMode.Transmitter:
                        isExpectedResponse = (commandId == BIND_TRANSMITTER_RESP);
                        break;
                    case BindMode.Receiver:
                        isExpectedResponse = (commandId == BIND_RECEIVER_RESP);
                        break;
                    case BindMode.Transceiver:
                        isExpectedResponse = (commandId == BIND_TRANSCEIVER_RESP);
                        break;
                }
                
                if (isExpectedResponse)
                {
                    if (commandStatus == ESME_ROK)
                    {
                        Console.WriteLine($"Bind {bindMode} successful!");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Bind {bindMode} failed with status: 0x{commandStatus:X8} ({GetStatusDescription(commandStatus)})");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"Unexpected response command ID: 0x{commandId:X8} (expected bind {bindMode} response)");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing bind response: {ex.Message}");
                return false;
            }
        }

        public bool TestConnection()
        {
            if (!isConnected)
            {
                Console.WriteLine("Not connected to SMPP server");
                return false;
            }
            
            try
            {
                Console.WriteLine("Sending enquire_link to test connection...");
                
                // Create enquire_link PDU
                var headerStream = new MemoryStream();
                var headerWriter = new BinaryWriter(headerStream);
                
                // Command_length (4 bytes) - header only, no body
                headerWriter.Write(ReverseBytes(BitConverter.GetBytes((uint)16)));
                
                // Command_id (4 bytes)
                headerWriter.Write(ReverseBytes(BitConverter.GetBytes(ENQUIRE_LINK)));
                
                // Command_status (4 bytes)
                headerWriter.Write(ReverseBytes(BitConverter.GetBytes((uint)0)));
                
                // Sequence_number (4 bytes)
                headerWriter.Write(ReverseBytes(BitConverter.GetBytes((uint)sequenceNumber++)));
                
                byte[] pdu = headerStream.ToArray();
                
                // Send enquire_link
                stream.Write(pdu, 0, pdu.Length);
                
                // Read response
                byte[] response = ReadPDU();
                if (response != null)
                {
                    uint commandId = BitConverter.ToUInt32(ReverseBytes(new byte[] { response[4], response[5], response[6], response[7] }), 0);
                    uint commandStatus = BitConverter.ToUInt32(ReverseBytes(new byte[] { response[8], response[9], response[10], response[11] }), 0);
                    
                    if (commandId == ENQUIRE_LINK_RESP && commandStatus == ESME_ROK)
                    {
                        Console.WriteLine("Enquire link successful - connection is alive!");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Enquire link failed - Command ID: 0x{commandId:X8}, Status: 0x{commandStatus:X8}");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection test error: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (isConnected && stream != null)
                {
                    Console.WriteLine("Sending unbind...");
                    
                    // Create unbind PDU
                    var headerStream = new MemoryStream();
                    var headerWriter = new BinaryWriter(headerStream);
                    
                    // Command_length (4 bytes) - header only, no body
                    headerWriter.Write(ReverseBytes(BitConverter.GetBytes((uint)16)));
                    
                    // Command_id (4 bytes)
                    headerWriter.Write(ReverseBytes(BitConverter.GetBytes(UNBIND)));
                    
                    // Command_status (4 bytes)
                    headerWriter.Write(ReverseBytes(BitConverter.GetBytes((uint)0)));
                    
                    // Sequence_number (4 bytes)
                    headerWriter.Write(ReverseBytes(BitConverter.GetBytes((uint)sequenceNumber++)));
                    
                    byte[] pdu = headerStream.ToArray();
                    
                    // Send unbind
                    stream.Write(pdu, 0, pdu.Length);
                    
                    // Wait a bit for unbind response
                    Thread.Sleep(1000);
                }
                
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
                
                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }
                
                isConnected = false;
                Console.WriteLine("Disconnected from SMPP server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect error: {ex.Message}");
            }
        }

        private byte[] ReverseBytes(byte[] bytes)
        {
            // SMPP uses big-endian byte order
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private string GetStatusDescription(uint status)
        {
            switch (status)
            {
                case 0x00000000: return "No Error";
                case 0x00000001: return "Message Length is invalid";
                case 0x00000002: return "Command Length is invalid";
                case 0x00000003: return "Invalid Command ID";
                case 0x00000004: return "Incorrect BIND Status for given command";
                case 0x00000005: return "ESME Already in Bound State";
                case 0x00000006: return "Invalid Priority Flag";
                case 0x00000007: return "Invalid Registered Delivery Flag";
                case 0x00000008: return "System Error";
                case 0x0000000A: return "Invalid Source Address";
                case 0x0000000B: return "Invalid Dest Addr";
                case 0x0000000C: return "Message ID is invalid";
                case 0x0000000D: return "Bind Failed";
                case 0x0000000E: return "Invalid Password";
                case 0x0000000F: return "Invalid System ID";
                case 0x00000011: return "Cancel SM Failed";
                case 0x00000013: return "Replace SM Failed";
                case 0x00000014: return "Message Queue Full";
                case 0x00000015: return "Invalid Service Type";
                case 0x00000033: return "Invalid number of destinations";
                case 0x00000034: return "Invalid Distribution List name";
                case 0x00000040: return "Destination flag is invalid";
                case 0x00000042: return "Invalid 'submit with replace' request";
                case 0x00000043: return "Invalid esm_class field data";
                case 0x00000044: return "Cannot Submit to Distribution List";
                case 0x00000045: return "submit_sm or submit_multi failed";
                case 0x00000048: return "Invalid Source address TON";
                case 0x00000049: return "Invalid Source address NPI";
                case 0x00000050: return "Invalid Destination address TON";
                case 0x00000051: return "Invalid Destination address NPI";
                case 0x00000053: return "Invalid system_type field";
                case 0x00000054: return "Invalid replace_if_present flag";
                case 0x00000055: return "Invalid number of messages";
                case 0x00000058: return "Throttling error (ESME has exceeded allowed message limits)";
                case 0x00000061: return "Invalid Scheduled Delivery Time";
                case 0x00000062: return "Invalid message validity period value";
                case 0x00000063: return "Predefined Message Invalid or Not Found";
                case 0x00000064: return "ESME Receiver Temporary App Error Code";
                case 0x00000065: return "ESME Receiver Permanent App Error Code";
                case 0x00000066: return "ESME Receiver Reject Message Error Code";
                case 0x00000067: return "query_sm request failed";
                case 0x000000C0: return "Error in the optional part of the PDU Body";
                case 0x000000C1: return "Optional Parameter not allowed";
                case 0x000000C2: return "Invalid Parameter Length";
                case 0x000000C3: return "Expected Optional Parameter missing";
                case 0x000000C4: return "Invalid Optional Parameter Value";
                case 0x000000FE: return "Delivery Failure (used for data_sm_resp)";
                case 0x000000FF: return "Unknown Error";
                default: return $"Unknown Status Code: 0x{status:X8}";
            }
        }
    }
}