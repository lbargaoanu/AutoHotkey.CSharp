using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Listening...");
            Listen().Wait();
        }

        private static async Task Listen()
        {
            ulong count = 0;
            using(var udpClient = new UdpClient(7000))
            {
                while(true)
                {
                    var data = await udpClient.ReceiveAsync();
                    count += (ulong) data.Buffer.Length;
                    Console.WriteLine(count);
                }
            }
        }
    }
}
