using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerLibrary;

namespace ServerLibrary
{
    class Communicator
    {
        private Server server;
        public Server Server { get => server; set => server = value; }

        public Communicator(Server s)
        {
            this.server = s;
        }

        /// <summary>
        /// Greets given user and allows him to choose whether to log in or sign up.
        /// Returns "s" if user chooses to sign up, and other strings for log in.
        /// </summary>
        /// <param  Client structure="client"></param>
        /// <returns>Returns s if user chooses to sign up, and other strings for log in.</returns>
        public String greetAndChooseOption(Server.Client client)
        {
            int messageLenght = 0;

            //greet the user, giving him an option to either sign up or log in
            byte[] message = Encoding.ASCII.GetBytes("\nWelcome to the server! Log in or Sign up (s-sign up/anything else-log in)");
            client.Stream.Write(message, 0, message.Length);

            //wait for the response    
            messageLenght = client.Stream.Read(server.Buffers[client.Id], 0, server.Buffer_size);
            return Encoding.UTF8.GetString(server.Buffers[client.Id], 0, messageLenght);
        }



        /// <summary>
        /// Puts given Client in echo state, whatever he writes is repeated back to him.
        /// If the client doesn't respond for 10 seconds, he timesout.
        /// </summary>
        /// <param Client structure="client"></param>
        public  void Echo(Server.Client client)
        {

            client.Stream.ReadTimeout = 10000;
            byte[] message = Encoding.ASCII.GetBytes("\n\rWelcome to the echo zone! (You have 10 seconds to shout something)");
            client.Stream.Write(message, 0, message.Length);
            while (true)
            {
                try
                {
                    int messageLenght = client.Stream.Read(server.Buffers[client.Id], 0, server.Buffer_size);
                    client.Stream.Write(server.Buffers[client.Id], 0, messageLenght);
                }
                catch (System.IO.IOException) { Console.Write("\rClient " + client.Id + " has disconected!"); break; }
            }

        }

        /// <summary>
        /// Allows user to log into an existing account.
        /// </summary>
        /// <param  Client structure="client"></param>
        public void LogIn(Server.Client client)
        {
            Authentication auth = new Authentication();
            int messageLenght = 0;
            byte[] message;
            String username = "\r\n";
            String password = "\r\n";


            message = Encoding.ASCII.GetBytes("\r\nUsername: ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (username == "\r\n")
            {
                messageLenght = client.Stream.Read(server.Buffers[client.Id], 0, server.Buffer_size);
                username = Encoding.UTF8.GetString(server.Buffers[client.Id], 0, messageLenght);
            }

            message = Encoding.ASCII.GetBytes("\rPassword: ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (password == "\r\n")
            {
                messageLenght = client.Stream.Read(server.Buffers[client.Id], 0, server.Buffer_size);
                password = Encoding.UTF8.GetString(server.Buffers[client.Id], 0, messageLenght); ;
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
        public void SignUp(Server.Client client)
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
                messageLenght = client.Stream.Read(server.Buffers[client.Id], 0, server.Buffer_size);
                username = Encoding.UTF8.GetString(server.Buffers[client.Id], 0, messageLenght);
            }


            message = Encoding.ASCII.GetBytes("\rPassword (one upercase Letter and number required): ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (password == "\r\n")
            {
                messageLenght = client.Stream.Read(server.Buffers[client.Id], 0, server.Buffer_size);
                password = Encoding.UTF8.GetString(server.Buffers[client.Id], 0, messageLenght); ;
            }

            message = Encoding.ASCII.GetBytes("\rConfirm Password: ");
            client.Stream.Write(message, 0, message.Length);
            //in order to avoid \r\n randomly sent by Putty being taken as an input
            while (confpassword == "\r\n")
            {
                messageLenght = client.Stream.Read(server.Buffers[client.Id], 0, server.Buffer_size);
                confpassword = Encoding.UTF8.GetString(server.Buffers[client.Id], 0, messageLenght); ;
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
                Console.WriteLine("\nNew account created [user:" + username + " password: " + password + "]");
                message = Encoding.ASCII.GetBytes("\n\rAccount created succesfully!");
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
                    SignUp(client);
                }
            }
        }

    }
}
