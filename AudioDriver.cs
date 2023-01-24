using System;
using System.Threading;
using System.Diagnostics;

namespace RemoteCW
{
    public class AudioDriver
    {
        bool running = true;
        double[] sineTone;
        double[] ramp;
        int offset;
        KeyDriver key;
        Process audioProcess;
        Thread audioThread;
        bool lastTransmit = false;
        int zeroCount = 0;

        public AudioDriver(KeyDriver key)
        {
            this.key = key;
            GenerateSineTone();
            //ProcessStartInfo psi = new ProcessStartInfo("paplay", "-p --device KEY --client-name RemoteCW --stream-name Key --latency-msec 5 --rate 48000 --channels 1 --format s16le --volume 16384 --raw");
            ProcessStartInfo psi = new ProcessStartInfo("pw-play", "--latency 2ms --rate 48000 --channels 1 --volume 0.1 -");
            psi.RedirectStandardInput = true;
            audioProcess = Process.Start(psi);
            audioProcess.Start();
            audioThread = new Thread(new ThreadStart(AudioLoop));
            audioThread.Start();
        }

        private void GenerateSineTone()
        {
            //The nearest tone to 700hz on a 48000sample/sec is 69 samples per sine wave. Nice.
            sineTone = new double[69];
            ramp = new double[69];
            for (int i = 0; i < 69; i++)
            {
                double sinePos = (Math.Tau * i) / 69.0;
                sineTone[i] = Math.Sin(sinePos);
            }
            for (int i = 0; i < 48; i++)
            {
                double sinePos = (Math.Tau * i) / 48.0;
                ramp[i] = Math.Cos(sinePos / 4);
            }
        }

        private void AudioLoop()
        {
            long lastGenerateTime = DateTime.UtcNow.Ticks;
            //Generate in millisecond chunks. s16 is two bytes per sample, 2 * 48000 * 1/1000
            byte[] byte1ms = new byte[96];
            while (running)
            {
                long currentTime = DateTime.UtcNow.Ticks;
                while (currentTime - lastGenerateTime > TimeSpan.TicksPerMillisecond)
                {
                    lastGenerateTime += TimeSpan.TicksPerMillisecond;
                    bool transmit = key.GetState();
                    if (transmit)
                    {
                        zeroCount = 0;
                    }
                    else
                    {
                        zeroCount++;
                    }
                    GenerateAudioChunk(byte1ms, transmit);
                    if (zeroCount < 1000)
                    {
                        audioProcess.StandardInput.BaseStream.Write(byte1ms, 0, byte1ms.Length);
                    }
                }
                Thread.Sleep(1);
            }
        }

        private void GenerateAudioChunk(byte[] input, bool transmit)
        {
            if (transmit)
            {
                zeroCount = 0;
            }
            if (!transmit && !lastTransmit)
            {

                Array.Clear(input);
                return;
            }
            for (int i = 0; i < input.Length / 2; i++)
            {
                double sinValue = sineTone[offset];

                //Ramp up
                if (transmit && !lastTransmit)
                {
                    sinValue = sinValue * ramp[ramp.Length - i - 1];
                }
                //Ramp down
                if (!transmit && lastTransmit)
                {
                    sinValue = sinValue * ramp[i];
                }

                //Convert double to s16
                int writePos = i * 2;
                short writeValue = (short)(sinValue * short.MaxValue);
                input[writePos] = (byte)(writeValue & 0xFF);
                input[writePos + 1] = (byte)(writeValue >> 8);

                offset++;
                if (offset == sineTone.Length)
                {
                    offset = 0;
                }
            }
            lastTransmit = transmit;
        }

        public void Stop()
        {
            running = false;
            audioProcess.Close();
            audioThread.Join();
        }
    }
}
