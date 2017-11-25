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

            var tuple = getData();

            double[][] data_train_x = new double[(int)(tuple.mfcc_inputs.Length * 0.66)][];
            double[][] data_test_x = new double[(int)(tuple.mfcc_inputs.Length * 0.33)][];

            bool[] data_train_y = new bool[(int)(tuple.classes.Length * 0.66)];
            bool[] data_test_y = new bool[(int)(tuple.classes.Length * 0.33)];

            Array.Copy(tuple.mfcc_inputs, 0, data_train_x, 0, (int)(tuple.mfcc_inputs.Length * 0.66));
            Array.Copy(tuple.mfcc_inputs, 0, data_test_x, (int)(tuple.mfcc_inputs.Length * 0.66), tuple.mfcc_inputs.Length);

            Array.Copy(tuple.classes, 0, data_train_y, 0, (int)(tuple.classes.Length * 0.66));
            Array.Copy(tuple.classes, 0, data_test_y, (int)(tuple.classes.Length * 0.66), tuple.classes.Length);

            Console.WriteLine("Training dimension: (" + data_train_x.Length + ", " + data_train_y.Length + ")");
            Console.WriteLine("Test dimension: (" + data_test_x.Length + ", " + data_test_y.Length + ")");

            var teacher = new SequentialMinimalOptimization<Gaussian>()
            {
                UseComplexityHeuristic = true,
                UseKernelEstimation = true // estimate the kernel from the data
            };

            // Teach the vector machine
            var svm = teacher.Learn(data_train_x, data_train_y);

            // Classify the samples using the model
            bool[] prediction = svm.Decide(data_test_x);

            int matches = 0;
            for (int i = 0; i < prediction.Length; i++)
                if (prediction[i] == data_test_y[i]) matches++;

            Console.WriteLine("Similarity: " + (prediction.Length / matches));

            while (true) { Console.ReadKey(true); }
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