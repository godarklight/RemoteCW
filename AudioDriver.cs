using System;
using System.Collections.Concurrent;
using PortAudioSharp;
using System.Runtime.InteropServices;
using System.Threading;

namespace RemoteCW
{
    class AudioDriver
    {
        public bool spot = false;
        Stream audioStream;
        AutoResetEvent okRead = new AutoResetEvent(false);
        KeyDriver keyDriver;
        public bool currentState = false;
        float[] carrier;
        float[] ramp;
        double lastUnitMs = 0;
        int carrierPhase = 0;
        int rampPhase = 0;
        int samplesLeftInUnit = 1;
        const double RAMP_TIME_MS = 2.0;

        public AudioDriver(KeyDriver keyDriver)
        {
            this.keyDriver = keyDriver;
            PortAudio.Initialize();
            DeviceInfo di = PortAudio.GetDeviceInfo(PortAudio.DefaultInputDevice);
            Console.WriteLine($"Reading from {di.name}");
            StreamParameters inParam = new StreamParameters();
            inParam.channelCount = 1;
            inParam.device = PortAudio.DefaultInputDevice;
            inParam.sampleFormat = SampleFormat.Float32;
            inParam.suggestedLatency = 0.01;
            StreamParameters outParam = new StreamParameters();
            outParam.channelCount = 1;
            outParam.device = PortAudio.DefaultOutputDevice;
            outParam.sampleFormat = SampleFormat.Float32;
            outParam.suggestedLatency = 0.01;
            audioStream = new Stream(inParam, outParam, 48000, 0, StreamFlags.NoFlag, AudioCallback, null);
            audioStream.Start();
            Setup();
        }

        public StreamCallbackResult AudioCallback(IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userDataPtr)
        {
            unsafe
            {
                float* floatptr = (float*)output.ToPointer();
                for (int i = 0; i < frameCount; i++)
                {
                    *floatptr = carrier[carrierPhase] * ramp[rampPhase] * 0.2f;
                    floatptr++;
                    samplesLeftInUnit--;
                    if (samplesLeftInUnit == 0)
                    {
                        samplesLeftInUnit = (int)(48000.0 * (keyDriver.unitMs / 1000.0));
                        //Console.WriteLine("New state");
                        currentState = keyDriver.GetState();
                    }
                    //Advance carrier
                    carrierPhase++;
                    if (carrierPhase == carrier.Length)
                    {
                        carrierPhase = 0;
                    }
                    //Advance ramp
                    if ((currentState || spot) && rampPhase < (ramp.Length - 1))
                    {
                        rampPhase++;
                    }
                    if ((!currentState && !spot) && rampPhase > 0)
                    {
                        rampPhase--;
                    }
                }
            }
            return StreamCallbackResult.Continue;
        }

        private void Setup()
        {
            carrierPhase = 0;
            rampPhase = 0;
            //69 samples generates a sidetone of 695hz, nice.
            int samplesPerCarrier = 69;
            int samplesPerRamp = (int)(48000.0 * (RAMP_TIME_MS / 1000.0));
            carrier = new float[samplesPerCarrier];
            ramp = new float[samplesPerRamp];

            for (int i = 0; i < carrier.Length; i++)
            {
                //Not -1, we do not want the start and end to be 0.
                double carrierPos = Math.Tau * (i / (double)carrier.Length);
                carrier[i] = (float)Math.Sin(carrierPos);
            }
            for (int i = 0; i < ramp.Length; i++)
            {
                //We do want to hit 1 here.
                double rampPos = (Math.Tau / 4.0) * (i / ((double)ramp.Length - 1.0));
                ramp[i] = (float)Math.Sin(rampPos);
            }
        }

        public void Stop()
        {
            audioStream.Stop();
            PortAudio.Terminate();
        }
    }
}