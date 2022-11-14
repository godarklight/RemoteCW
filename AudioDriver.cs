using System;
using System.Threading;
using System.Diagnostics;

namespace RemoteCW
{
    public class AudioDriver
    {
        bool running = true;
        byte[] sineTone;
        byte[] muteTone;
        int offset;
        KeyDriver key;
        Process audioProcess;
        Thread audioThread;

        public AudioDriver(KeyDriver key)
        {
            this.key = key;
            GenerateSineTone();
            ProcessStartInfo psi = new ProcessStartInfo("paplay", "-p --client-name RemoteCW --stream-name Key --latency-msec 5 --rate 48000 --channels 1 --format s16le --volume 16384 --raw");
            psi.RedirectStandardInput = true;
            audioProcess = Process.Start(psi);
            audioProcess.Start();
            audioThread = new Thread(new ThreadStart(AudioLoop));
            audioThread.Start();
        }

        private void GenerateSineTone()
        {
            //The nearest tone to 700hz on a 48000sample/sec is 69 samples per sine wave. Nice.
            sineTone = new byte[138];
            for (int i = 0; i < 69; i++)
            {
                double sinePos = (Math.Tau * i) / 69.0;
                double sinValue = Math.Sin(sinePos);
                int writePos = i * 2;
                short writeValue = (short)(sinValue * short.MaxValue);
                sineTone[writePos] = (byte)(writeValue & 0xFF);
                sineTone[writePos + 1] = (byte)(writeValue >> 8);
            }
        }

        private void AudioLoop()
        {

            long lastGenerateTime = DateTime.UtcNow.Ticks;
            //Generate in millisecond chunks. 48 samples of s16 is 96 bytes per millisecond
            byte[] byte1ms = new byte[96];
            while (running)
            {
                long currentTime = DateTime.UtcNow.Ticks;
                while (currentTime - lastGenerateTime > TimeSpan.TicksPerMillisecond)
                {
                    lastGenerateTime += TimeSpan.TicksPerMillisecond;
                    GenerateAudioChunk(byte1ms);
                    audioProcess.StandardInput.BaseStream.Write(byte1ms, 0, byte1ms.Length);
                }
                Thread.Sleep(1);
            }
        }

        private void GenerateAudioChunk(byte[] input)
        {
            if (key.GetState())
            {
                int bytesLeft = input.Length;
                while (bytesLeft > 0)
                {
                    input[input.Length - bytesLeft] = sineTone[offset];
                    offset++;
                    if (offset == sineTone.Length)
                    {
                        offset = 0;
                    }
                    bytesLeft--;
                }
            }
            else
            {
                Array.Clear(input);
            }
        }

        public void Stop()
        {
            running = false;
            audioProcess.Close();
            audioThread.Join();
        }
    }
}
