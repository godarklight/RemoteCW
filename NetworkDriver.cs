using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace RemoteCW
{
    public class NetworkDriver
    {
        UdpClient udpc;
        object lockObj = new Object();
        byte[] activeBuffer = new byte[450];
        byte[] inactiveBuffer = new byte[450];
        Thread sendThread;
        bool running = true;
        public bool send = false;
        IPEndPoint cwServer;

        public NetworkDriver()
        {
            cwServer = new IPEndPoint(IPAddress.Parse("2403:580b:34:20::6"), 5005);
            udpc = new UdpClient(AddressFamily.InterNetworkV6);
            udpc.Client.DualMode = true;
            udpc.Client.Bind(new IPEndPoint(IPAddress.Parse("2403:580b:34:20::2"), 5005));
            sendThread = new Thread(new ThreadStart(SendLoop));
            sendThread.Start();
        }

        public void Update(long time, bool state)
        {
            if (!send)
            {
                return;
            }
            //Write to inactive buffer
            //Console.WriteLine($"{time} = {state}");
            long timeNetwork = IPAddress.HostToNetworkOrder(time);
            BitConverter.GetBytes(timeNetwork).CopyTo(inactiveBuffer, 0);
            inactiveBuffer[8] = 0;
            if (state)
            {
                inactiveBuffer[8] = 1;
            }

            //Swap buffer
            byte[] temp = inactiveBuffer;
            inactiveBuffer = activeBuffer;
            activeBuffer = temp;
            Buffer.BlockCopy(activeBuffer, 0, inactiveBuffer, 9, inactiveBuffer.Length - 9);
        }

        private void SendLoop()
        {
            while (running)
            {
                if (send)
                {
                    lock (activeBuffer)
                    {
                        udpc.Send(activeBuffer, cwServer);
                    }
                }
                Thread.Sleep(50);
            }
        }

        public void Stop()
        {
            running = false;
            sendThread.Join();
        }
    }
}