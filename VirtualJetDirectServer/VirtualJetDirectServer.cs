using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// State object for reading client data asynchronously  

namespace VirtualJetDirectServer
{
    public class VirtualJetDirectServer
    {
        #region Mbers
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ManualResetEvent _allDone = new ManualResetEvent(false);
        private static bool _stopServer = false;
        private static readonly TcpListener _printServerSocket = new TcpListener(new IPAddress(0), Properties.Settings.Default.ServerPort);
        #endregion

        #region Public event declaration
        public delegate void DelegateJobReceived(StringBuilder document);
        public event DelegateJobReceived OnNewJob;
        public event DelegateJobReceived OnClientDisconnected;
        #endregion

        #region Ctor
        public VirtualJetDirectServer()
        {
        }
        #endregion

        #region Public method
        public void Start()
        {
            _log.Info("Create a print server on port 9100");
            _printServerSocket.Start();
            _log.Info("Print server started");

            while (!_stopServer)
            {
                try
                {
                    // Set the event to nonsignaled state.  
                    _allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    _log.Trace("Waiting for a connection...");
                    _printServerSocket.BeginAcceptSocket(new AsyncCallback(AcceptPrintJob), _printServerSocket);

                    // Wait until a connection is made before continuing.  
                    _allDone.WaitOne();
                }
                catch (Exception ex)
                {
                    _log.Fatal(ex);
                }
            }
        }

        public void Stop()
        {
            _stopServer = true;
            Thread.Sleep(500);
            _printServerSocket.Stop();
            _log.Info("Print server stopped.");
        }
        #endregion

        #region Private method
        private void AcceptPrintJob(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            _allDone.Set();

            if (_stopServer) return;

            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)ar.AsyncState;

            // Get the socket that handles the client request.  
            Socket handler = listener.EndAcceptSocket(ar);

            _log.Info($"New connection from: {handler.RemoteEndPoint}");

            // Create the state object.  
            StateObject state = new StateObject
            {
                WorkSocket = handler
            };
            // Start receiving data
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadPrintJob), state);
        }

        private void ReadPrintJob(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;

            if(!handler.Connected)
            {
                _log.Error("Connection to client lost");
                return;
            }

            // Read data from the client socket.   
            int bytesRead = handler.EndReceive(ar);

            // No data to read, exit
            if (bytesRead <= 0) return;

            _log.Trace($"Data readed ({bytesRead} bytes): {Encoding.ASCII.GetString(state.Buffer, 0, bytesRead)}");

            string dataReceived = Encoding.ASCII.GetString(state.Buffer, 0, bytesRead);
            state.Data.Append(dataReceived);

            // check command if reading must continue
            if (!ParseData(state)) return;

            // Not all data received. Get more.  
            try
            {
                if (!handler.Connected) throw new SocketException(); // client is disconnected
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadPrintJob), state);
            }
            catch(SocketException)
            {
                // client have closed his connection, end of print
                _log.Info("Client connection closed");
                OnClientDisconnected?.Invoke(state.Data);
                return; 
            }
            catch(Exception ex)
            {
                _log.Fatal(ex);
            }
        }

        private bool ParseData(StateObject state)
        {
            /* 
             * Check the content for PJL command:
             * <ESC>%-12345X@PJL INFO STATUS    -> return an ack like @PJL INFO STATUS CODE=10001 ONLINE=TRUE
             * <ESC>%-12345X@PJL JOB ...        -> RAW data to print until receive @PJL EOJ or socket closed
             * <ESC>%-12345X@PJL EOJ            -> no more data to print
             * @PJL ENTER LAN                   -> ignore it
             * 
             * Update 20200123: a command can be split over multiple buffer, search for command in all data
             * Update 20200131: in some case, I didn't receive the PJL EOJ command, but the data contains %%EOF. Job can be send to the printer
             */

            if (state.Data.ToString().Contains("%%EOF"))
            {
                // end of file
                _log.Info("End of Job");
                OnNewJob?.Invoke(state.Data);
                state.Data = new StringBuilder(); // clear data
                return false;
            }

            List<string> commands = ExtractCommand(state.Data);
            if (commands == null || commands.Count == 0) return true; // no JPL command found

            string lastCommand = commands.Last();
            if (lastCommand.Contains("JOB") || lastCommand.Contains("ENTER")) return true; // print job
            if (lastCommand.Contains("@PJL INFO STATUS")) // info request
            {
                _log.Info("Received a status request, send OK status");
                Send(state.WorkSocket, "@PJL INFO STATUS CODE=10001 ONLINE=TRUE");
                state.Data = new StringBuilder(); // clear data
                return true; 
            }
            if (lastCommand.Contains("@PJL EOJ"))
            {
                // end of print
                _log.Info("End of Job");
                OnNewJob?.Invoke(state.Data);
                state.Data = new StringBuilder(); // clear data
                return false; 
            }
 
            _log.Error("Not implemented PJL command");
            return false;
        }

        private List<string> ExtractCommand(StringBuilder data)
        {
            string pattern = "@PJL (.*)";
            var found = Regex.Matches(data.ToString(), pattern);
            if (found.Count == 0) return null;
            
            var commands = new List<string>();
            foreach(Match item in found)
               commands.Add(item.Value);
            return commands;
        }

        private void Send(Socket handler, string data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            _log.Trace($"Send: {data}");

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                _log.Trace($"Sent {bytesSent} bytes to client.");
            }
            catch (Exception ex)
            {
                _log.Fatal(ex);
            }
        }
        #endregion

        #region StateObject
        private class StateObject
        {
            #region Public constante
            // Size of receive buffer.  
            public const int BufferSize = 1024;
            #endregion

            #region Public properties
            // Client socket.  
            public Socket WorkSocket { get; set; } = null;
            // Receive buffer.  
            public byte[] Buffer { get; set; } = new byte[BufferSize];
            // Received data string.  
            public StringBuilder Data { get; set; } = new StringBuilder();
            #endregion
        }
        #endregion
    }
}