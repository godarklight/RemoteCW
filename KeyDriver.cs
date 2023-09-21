using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace RemoteCW
{
    public class KeyDriver
    {
        //Iambic
        bool iambicMode = true;
        bool wasKeying = false;
        //Timing
        int wpm = 24;
        public int unitMs = 50;
        int toneLeft = 0;
        int totalLeft = 0;
        //Hardware
        bool leftKey = false;
        bool leftKeyEvent = false;
        bool rightKey = false;
        bool rightKeyEvent = false;
        bool lastKeyLeft = false;
        bool lastNetworkState = false;
        SerialDriver serial;
        NetworkDriver network;
        public KeyDriver(SerialDriver serial, NetworkDriver network)
        {
            this.serial = serial;
            this.network = network;
            serial.SetKeyCallback(ProcessEvent);
            SetWPM(Program.DEFAULT_SPEED);
        }

        public void ProcessEvent(string input)
        {
            string[] inputSplit = input.Split(' ');
            if (inputSplit.Length != 2)
            {
                //Console.WriteLine($"Faulty data: {input}");
                return;
            }
            string channel = inputSplit[0];
            //ulong time = ulong.Parse(inputSplit[1]);
            bool newState = inputSplit[1] == "1";
            if (channel == "L")
            {
                leftKey = newState;
                if (newState)
                {
                    leftKeyEvent = true;
                }
            }
            if (channel == "R")
            {
                rightKey = newState;
                if (newState)
                {
                    rightKeyEvent = true;
                }
            }
        }

        public void SetWPM(int wpm)
        {
            this.wpm = wpm;
            this.unitMs = 60000 / (50 * wpm);
        }


        public void SetMode(bool iambic)
        {
            this.iambicMode = iambic;
            leftKeyEvent = false;
            rightKeyEvent = false;
        }

        public bool GetState()
        {
            bool state = GetStateReal();
            if (state != lastNetworkState)
            {
                network.Update(DateTime.UtcNow.Ticks, state);
                lastNetworkState = state;
            }
            return state;
        }

        private bool GetStateReal()
        {
            //Straight key mode
            if (!iambicMode)
            {
                if (leftKeyEvent)
                {
                    leftKeyEvent = false;
                    return true;
                }
                if (rightKeyEvent)
                {
                    rightKeyEvent = false;
                    return true;
                }
                return leftKey || rightKey;
            }
            //Iambic mode, start keying
            if (!wasKeying && (leftKey || leftKeyEvent || rightKey || rightKeyEvent))
            {
                wasKeying = true;
                if (leftKey)
                {
                    leftKeyEvent = false;
                    lastKeyLeft = true;
                    totalLeft = 2;
                    toneLeft = 1;
                }
                if (rightKey)
                {
                    rightKeyEvent = false;
                    lastKeyLeft = false;
                    totalLeft = 4;
                    toneLeft = 3;
                }
            }
            if (wasKeying && totalLeft == 0)
            {
                //Left only
                if ((leftKey || leftKeyEvent) && !(rightKey || rightKeyEvent))
                {
                    leftKeyEvent = false;
                    totalLeft = 2;
                    toneLeft = 1;
                }
                //Right only
                if (!(leftKey | leftKeyEvent) && (rightKey || rightKeyEvent))
                {
                    rightKeyEvent = false;
                    totalLeft = 4;
                    toneLeft = 3;
                }
                //Both
                if ((leftKey || leftKeyEvent) && (rightKey || rightKeyEvent))
                {
                    if (lastKeyLeft)
                    {
                        rightKeyEvent = false;
                        //Play right side now - dah
                        totalLeft = 4;
                        toneLeft = 3;
                    }
                    else
                    {
                        leftKeyEvent = false;
                        //Play left side - dit
                        totalLeft = 2;
                        toneLeft = 1;
                    }
                    lastKeyLeft = !lastKeyLeft;
                }
            }
            if (totalLeft > 0)
            {
                bool key = toneLeft > 0;
                if (toneLeft > 0)
                {
                    toneLeft--;
                }
                totalLeft--;
                if (totalLeft == 0 && !leftKeyEvent && !leftKey && !rightKeyEvent && !rightKey)
                {
                    wasKeying = false;
                }
                return key;
            }
            //No keys pressed, don't send tone
            return false;
        }
    }
}
