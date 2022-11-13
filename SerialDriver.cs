using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;

namespace RemoteCW
{
    public class SerialDriver
    {
        private SerialPort port;
        StringBuilder sb = new StringBuilder();
        private Action<string> uiCallback;
        private Action<string> keyCallback;
        private object lockObject = new object();
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
            port.Open();
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
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
                            if (charInt == '\n')
                            {
                                string sendText = sb.ToString();
                                sb.Clear();
                                if (uiCallback != null)
                                {
                                    uiCallback(sendText);
                                }
                                if (keyCallback != null)
                                {
                                    keyCallback(sendText);
                                }
                                sb.Clear();
                            }
                            else
                            {
                                sb.Append((char)charInt);
                            }
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
    }
}