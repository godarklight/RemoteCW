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
        int wpm = 15;
        int unitMs = 80;
        int toneLeft = 0;
        int totalLeft = 0;
        //Hardware
        bool leftKey = false;
        bool leftKeyEvent = false;
        bool rightKey = false;
        bool rightKeyEvent = false;
        bool lastKeyLeft = false;
        bool keyState = false;
        //Program state
        bool running = true;
        SerialDriver serial;
        public KeyDriver(SerialDriver serial)
        {
            this.serial = serial;
            serial.SetKeyCallback(ProcessEvent);
        }

        public void ProcessEvent(string input)
        {
            string[] inputSplit = input.Split(' ');
            if (inputSplit.Length != 3)
            {
                //Console.WriteLine($"Faulty data: {input}");
                return;
            }
            string channel = inputSplit[0];
            ulong time = ulong.Parse(inputSplit[1]);
            bool newState = inputSplit[2] == "1";
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
        }

        public bool GetState()
        {
            //Straight key mode
            if (!iambicMode)
            {
                return leftKey || rightKey;
            }
            //Iambic mode, start keying
            if (!wasKeying && (leftKey || rightKey))
            {
                wasKeying = true;
                if (leftKey)
                {
                    leftKeyEvent = false;
                    lastKeyLeft = true;
                    totalLeft = 2 * unitMs;
                    toneLeft = 1 * unitMs;
                }
                if (rightKey)
                {
                    rightKeyEvent = false;
                    lastKeyLeft = false;
                    totalLeft = 4 * unitMs;
                    toneLeft = 3 * unitMs;
                }
            }
            if (wasKeying && totalLeft == 0)
            {
                //Left only
                if ((leftKey || leftKeyEvent) && !(rightKey || rightKeyEvent))
                {
                    leftKeyEvent = false;
                    totalLeft = 2 * unitMs;
                    toneLeft = 1 * unitMs;
                }
                //Right only
                if (!(leftKey | leftKeyEvent) && (rightKey || rightKeyEvent))
                {
                    rightKeyEvent = false;
                    totalLeft = 4 * unitMs;
                    toneLeft = 3 * unitMs;
                }
                //Both
                if ((leftKey || leftKeyEvent) && (rightKey || rightKeyEvent))
                {
                    if (lastKeyLeft)
                    {
                        rightKeyEvent = false;
                        //Play right side now - dah
                        totalLeft = 4 * unitMs;
                        toneLeft = 3 * unitMs;
                    }
                    else
                    {
                        leftKeyEvent = false;
                        //Play left side - dit
                        totalLeft = 2 * unitMs;
                        toneLeft = 1 * unitMs;
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

        public void Stop()
        {
            running = false;
        }
    }
}