using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using System.Windows.Forms;

namespace dialektor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            WaveFileAudioSource source = new Accord.DirectSound.WaveFileAudioSource("test_sound.wav");

            // Specify the callback function which will be
            // called once a sample is completely available
            source.NewFrame += source_NewFrame;

            source.Start();
            while (true) { Console.ReadKey(true); }
        }

        private static async void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Read current frame...
            Signal signal = eventArgs.Signal;

            String data = String.Join(",", signal.RawData.Select(p => p.ToString()).ToArray());
            Console.WriteLine("Loaded signal: [" + data + "]");
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}
