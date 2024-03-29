using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace RemoteCW
{
    public class SerialDriver
    {
        private SerialPort port;
        StringBuilder sb = new StringBuilder();
        private Action<string> uiCallback;
        private Action<string> keyCallback;
        private object lockObject = new object();
        Thread pinThread;
        private bool running = true;

        public SerialDriver()
        {
            string selectedPort = "";
            foreach (string portName in SerialPort.GetPortNames())
            {
                selectedPort = portName;
            }
            port = new SerialPort(selectedPort, 115200, Parity.None, 8, StopBits.One);
            port.ReadTimeout = 10;
            port.WriteTimeout = 10;
            port.DataReceived += DataReceived;
            port.DtrEnable = true;
            port.Open();
            pinThread = new Thread(new ThreadStart(PinThread));
            pinThread.Start();
        }

        private void PinThread()
        {
            bool lastCTS = false;
            bool lastDSR = false;
            string sendText = "";
            while (running)
            {
                if (port.DsrHolding != lastDSR)
                {
                    lastDSR = port.DsrHolding;
                    sendText = "L 0";
                    if (port.DsrHolding)
                    {
                        sendText = "L 1";
                    }
                }
                if (sendText == "" && port.CtsHolding != lastCTS)
                {
                    lastCTS = port.CtsHolding;
                    sendText = "R 0";
                    if (port.CtsHolding)
                    {
                        sendText = "R 1";
                    }
                }
                if (sendText != "")
                {
                    if (uiCallback != null)
                    {
                        uiCallback(sendText);
                    }
                    if (keyCallback != null)
                    {
                        keyCallback(sendText);
                    }
                    sendText = "";
                }
                Thread.Sleep(1);
            }
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Console.WriteLine("DR");
            lock (lockObject)
            {
                if (e.EventType == SerialData.Chars)
                {
                    try
                    {
                        int bytesToRead = port.BytesToRead;
                        while (bytesToRead > 0)
                        {
                            int charInt = port.ReadChar();
                            bytesToRead--;
                        }
                    }
                    catch (TimeoutException)
                    {
                        //Don't care.
                    }
                }

            }
        }

        public void SetUICallback(Action<string> callback)
        {
            this.uiCallback = callback;
        }

        public void SetKeyCallback(Action<string> callback)
        {
            this.keyCallback = callback;
        }

        public void Stop()
        {
            running = false;
            pinThread.Join();
            port.Close();
        }
    }
}