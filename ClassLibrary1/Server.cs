using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerLibrary
{
    public class Server
    {

        /// <summary>
        /// Structure for holding information about individual server client sessions.
        /// </summary>
        public struct Client
        {
            public Client(int id,NetworkStream stream)
            {
                Id = id;
                Stream = stream;
            }

            public int Id { get; }
            public NetworkStream Stream { get; }
        }



        #region fields
        IPAddress ipAddress;
        int port;
        int buffer_size = 1024;
        bool running;
        List<byte []> buffers = new List<byte[]>();
        int clientCounter = 0;

        TcpListener tcpListener;

        public delegate void TransmissionDataDelegate(Client client);
        #endregion


        #region field_definitions
        public IPAddress IPAddress {
            get => ipAddress;
            set {
                if (!running) ipAddress = value;
                else throw new Exception("The server is not curently running");
            }
        }

        public int Port
        {
            get => port;
            set
            {
                int tmp = port;
                if (!running) port = value; else throw new Exception("Cannot change the port whilst the server is running");
                if (!checkPort())
                {
                    port = tmp;
                    throw new Exception("Illegal port value");
                }

            }

        }

        public int Buffer_size
        {
            get => buffer_size; set
            {
                if (value < 0 || value > 1024 * 1024 * 64) throw new Exception("Illegal packet size");
                if (!running) buffer_size = value; else throw new Exception("Cannot change packet size while the server is running");
            }

        }
        protected TcpListener TcpListener { get => tcpListener; set => tcpListener = value;}
    
        #endregion

        #region Constructors



        public Server(IPAddress IP, int port)

        {
            running = false;
            IPAddress = IP;
            Port = port;
            if (!checkPort())
            {
                Port = 8000;
                throw new Exception("illegal port, port set to 8000");
            }

        }

        #endregion

        #region Functions
        protected bool checkPort()
        {
            if (port < 1024 || port > 49151) return false;
            return true;
        }


        protected void StartListening()
        {
            TcpListener = new TcpListener(IPAddress, Port);
            TcpListener.Start();
        }

        protected void AcceptClient()
        {
            while (true)
            {
                TcpClient tcpClient = TcpListener.AcceptTcpClient();
                Console.Write("\nNew client connected! Client id: "+clientCounter);
                //create a new buffer for the client
                buffers.Add(new byte[buffer_size]);
                NetworkStream stream = tcpClient.GetStream();

                TransmissionDataDelegate transmissionDelegate = new TransmissionDataDelegate(BeginDataTransmission);
                transmissionDelegate.BeginInvoke(new Client(clientCounter,stream), TransmissionCallback, tcpClient);
                clientCounter++;
            }
        }

        private void TransmissionCallback(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient) ar.AsyncState;
            tcpClient.Close();

        }

        /// <summary>
        /// Welcomes given client to the server and allows him to proceed to log in or sign up.
        /// </summary>
        /// <param name="client"></param>
        protected void BeginDataTransmission(Client client)
        {
            String bufferString="";
            int messageLenght = 0;
           

            //greet the user, giving him an option to either sign up or log in
            byte [] message= Encoding.ASCII.GetBytes("\nWelcome to the server! Log in or Sign up (s-sign up/anything else-log in)");
            client.Stream.Write(message, 0, message.Length);

            //wait for the response    
            messageLenght = client.Stream.Read(buffers[client.Id], 0, buffer_size);
            bufferString = Encoding.UTF8.GetString(buffers[client.Id], 0, messageLenght);

            if(bufferString == "s"){ SignUp(client);}
            //after user signed up, make him log in
            LogIn(client);
            //after sucesfull loging in, echo the client
            Echo(client);
        }

        /// <summary>
        /// Puts given Client in echo state, whatever he writes is repeated back to him.
        /// If the client doesn't respond for 10 seconds, he timesout.
        /// </summary>
        /// <param Client structure="client"></param>
        private void Echo(Client client)
        {

            client.Stream.ReadTimeout = 10000;
            byte[] message = Encoding.ASCII.GetBytes("\n\rWelcome to the echo zone! (You have 10 seconds to shout something)");
            client.Stream.Write(message, 0, message.Length);
            while (true)
            {
                try
                {
                    int messageLenght = client.Stream.Read(buffers[client.Id], 0, buffer_size);
                    client.Stream.Write(buffers[client.Id], 0, messageLenght);
                }
                catch(System.IO.IOException e) { Console.Write("\n Client" +client.Id+ "has disconected!"); break; }
            }
           
        }

        /// <summary>
        /// Allows user to log into an existing account.
        /// </summary>
        /// <param  Client structure="client"></param>
        private void LogIn(Client client)
        {
            Authentication auth = new Authentication();
            int messageLenght = 0;
            byte[] message;
            String username = "\r\n";
            String password = "\r\n";


            message = Encoding.ASCII.GetBytes("\rUsername: ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (username == "\r\n")
            {
                messageLenght = client.Stream.Read(buffers[client.Id], 0, buffer_size);
                username = Encoding.UTF8.GetString(buffers[client.Id], 0, messageLenght);
            }

            message = Encoding.ASCII.GetBytes("\rPassword: ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (password == "\r\n")
            {
                messageLenght = client.Stream.Read(buffers[client.Id], 0, buffer_size);
                password = Encoding.UTF8.GetString(buffers[client.Id], 0, messageLenght); ;
            }

            try
            {
                auth.AuthorizeUser(username, password);
                Console.WriteLine("\nA user loged in [username:" + username + "]");
                message = Encoding.ASCII.GetBytes("\n\rLog in succesfull!");
                client.Stream.Write(message, 0, message.Length);
            }
            catch (AuthenticationException e)
            {
                if (e.ErrorCategory == -1)
                {
                    message = Encoding.ASCII.GetBytes("Server malfunction: " + e);
                    client.Stream.Write(message, 0, message.Length);
                    return;
                }
                if (e.ErrorCategory == 1)
                {
                    message = Encoding.ASCII.GetBytes("Error: " + e);
                    client.Stream.Write(message, 0, message.Length);
                    message = Encoding.ASCII.GetBytes("\n\r--Try again!--\n\r");
                    client.Stream.Write(message, 0, message.Length);
                    LogIn(client);
                }
            }
        }

        /// <summary>
        /// Allows user to create a new account.
        /// </summary>
        /// <param  Client structure="client"></param>
        private void SignUp(Client client)
        {
            Authentication auth = new Authentication();
            int messageLenght = 0;
            String password = "\r\n";
            String confpassword = "\r\n";
            String username = "\r\n";

            byte[] message = Encoding.ASCII.GetBytes("\rUsername (max 10 chars): ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (username == "\r\n")
            {
                messageLenght = client.Stream.Read(buffers[client.Id], 0, buffer_size);
                username = Encoding.UTF8.GetString(buffers[client.Id], 0, messageLenght);
            }

                
            message = Encoding.ASCII.GetBytes("\rPassword (one upercase Letter and number required): ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (password == "\r\n")
            {
                messageLenght = client.Stream.Read(buffers[client.Id], 0, buffer_size);
                password = Encoding.UTF8.GetString(buffers[client.Id], 0, messageLenght); ;
            }

            message = Encoding.ASCII.GetBytes("\rConfirm Password: ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (confpassword == "\r\n")
            {
                messageLenght = client.Stream.Read(buffers[client.Id], 0, buffer_size);
                confpassword = Encoding.UTF8.GetString(buffers[client.Id], 0, messageLenght); ;
            }

            //Check if the two passwords are the same
            if (password != confpassword)
            {
                message = Encoding.ASCII.GetBytes("\r\nError: Passwords don't match!");
                client.Stream.Write(message, 0, message.Length);
                SignUp(client);
            }


            try
            {
                auth.CreateUser(username, password);
                Console.WriteLine("\nNew account created [user:" + username + " password: " + password+"]");
                message = Encoding.ASCII.GetBytes("\n\rAccount created succesfully!");
                client.Stream.Write(message, 0, message.Length);
            }
            catch (AuthenticationException e)
            {
                if(e.ErrorCategory == -1)
                {
                    message = Encoding.ASCII.GetBytes("Server malfunction: " + e);
                    client.Stream.Write(message, 0, message.Length);
                    return;
                }
                if (e.ErrorCategory==1)
                {
                    message = Encoding.ASCII.GetBytes("Error: "+e);
                    client.Stream.Write(message, 0, message.Length);
                    message = Encoding.ASCII.GetBytes("\n\r--Try again!--\n\r");
                    client.Stream.Write(message, 0, message.Length);
                    SignUp(client);
                }
            }
        }

        /// <summary>
        /// Starts the operations of the server.
        /// </summary>
        public void Start()
        {
            Console.Write("\nStarting up the server...");
            StartListening();
            Console.Write("\nListening for clients commenced...");
            AcceptClient();
        }

        #endregion
    }
}
