using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Audio;
using Accord.Neuro;
using Accord.Audio.Formats;
using Accord.DirectSound;
using System.Windows.Forms;
using Accord.Neuro.Networks;
using Accord.Neuro.ActivationFunctions;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using System.IO;

namespace dialektor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();


            //var teacher = new SequentialMinimalOptimization<Gaussian>()
            //{
            //    UseComplexityHeuristic = true,
            //    UseKernelEstimation = true // estimate the kernel from the data
            //};

            //// Teach the vector machine
            //var svm = teacher.Learn(inputs, outputs);

            //// Classify the samples using the model
            //bool[] answers = svm.Decide(inputs);

            //// Convert to Int32 so we can plot:
            //int[] zeroOneAnswers = answers.ToZeroOne();

            //// Plot the results
            //ScatterplotBox.Show("Expected results", inputs, outputs);
            //ScatterplotBox.Show("GaussianSVM results", inputs, zeroOneAnswers);

            while (true)
            {
                Console.ReadKey(true);
            }
        }

        public static (double[][] mfcc_inputs, bool[] classes) getData()
        {
            List<double[]> input = new List<double[]>();
            List<bool> classes = new List<bool>();
            String[] files = Directory.GetFiles("DATA");
            foreach (String file in files)
            {
                input.Add(getMFCC(file));
                classes.Add(file.Split('_')[2] != "0");
            }
            return (input.ToArray(), classes.ToArray());
        }

        public static double[] getMFCC(string file)
        {
            WaveDecoder decoder = new WaveDecoder(file);
            Signal signal = decoder.Decode();
            MelFrequencyCepstrumCoefficient mfcc = new MelFrequencyCepstrumCoefficient();
            MelFrequencyCepstrumCoefficientDescriptor[] ra = mfcc.Transform(signal).ToArray();
            return ra;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}