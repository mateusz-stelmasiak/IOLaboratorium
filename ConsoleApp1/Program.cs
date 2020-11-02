using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerLibrary;


namespace SerwerTCP
{

    class Program
    {
        static void Main(string[] args)
        {
            IPAddress adresIP = IPAddress.Parse("127.0.0.1");
            int port = 9000;
            Server server= new Server(adresIP, port);

            server.Start();
        }
    }
}
