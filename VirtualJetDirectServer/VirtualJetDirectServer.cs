using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;  
  
// State object for reading client data asynchronously  

namespace VirtualJetDirectServer
{
    public class VirtualJetDirectServer
    {
        #region Mbers
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static ManualResetEvent _allDone = new ManualResetEvent(false);
        private static bool _stopServer = false;
        private static TcpListener _printServerSocket = new TcpListener(new IPAddress(0), Properties.Settings.Default.ServerPort);
        #endregion

        #region Public event declaration
        public delegate void DelegateJobReceived(StringBuilder document);
        public event DelegateJobReceived OnNewJob;
        #endregion

        #region Ctor
        public VirtualJetDirectServer()
        {
        }
        #endregion

        #region Public method
        public void Start()
        {
            _log.Trace("Create a print server on port 9100");
            _printServerSocket.Start();
            _log.Trace("Print server started");

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
            _log.Trace("Print server stopped.");
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

            _log.Trace($"New connection from: {handler.RemoteEndPoint}");

            // Create the state object.  
            StateObject state = new StateObject();
            state.WorkSocket = handler;
            // Start receiving data
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadPrintJob), state);
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

            // There might be more data, so store the data received so far.  
            state.Data.Append(Encoding.ASCII.GetString(
                state.Buffer, 0, bytesRead));

            string content = state.Data.ToString();

            // check command if reading must continue
            if (!CheckCommand(state)) return;

            // client is disconnected
            if (!handler.Connected) return;

            // Not all data received. Get more.  
            try
            {
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadPrintJob), state);
            }
            catch(SocketException)
            {
                // client have closed his connection, end of print
                _log.Info("Client connection closed");
                OnNewJob?.Invoke(state.Data);
                return; 
            }
            catch(Exception ex)
            {
                _log.Fatal(ex);
            }
        }

        private bool CheckCommand(StateObject state)
        {
            /* 
             * Check the content for PJL command:
             * <ESC>%-12345X@PJL INFO STATUS > return an ack like @PJL INFO STATUS CODE=10001 ONLINE=TRUE
             * <ESC>%-12345X@PJL JOB ... > RAW data to print until receive @PJL EOJ or socket closed
             * <ESC>%-12345X@PJL EOJ > no more data to print
             */
            string currentContent = Encoding.ASCII.GetString(state.Buffer, 0, state.Buffer.Length);
            if (!currentContent.Contains("\u001B%-12345X@PJL")) return true; // not a JPL command
            if (currentContent.Contains("@PJL JOB")) return true; // print job
            if (currentContent.Contains("@PJL INFO STATUS")) // info request
            {
                _log.Trace("Received a status request, send OK status");
                Send(state.WorkSocket, "@PJL INFO STATUS CODE=10001 ONLINE=TRUE");
                return true; 
            }
            if (currentContent.Contains("@PJL EOJ"))
            {
                // end of print
                _log.Trace("End of Job");
                OnNewJob?.Invoke(state.Data);
                return false; 
            }

            throw new NotImplementedException("Not implemented PJL command");
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

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
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
            // Client  socket.  
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