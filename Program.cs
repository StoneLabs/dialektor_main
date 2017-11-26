using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.Neuro.Networks;
using System.IO;
using Accord.Neuro.Learning;

namespace dialektor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            var tuple = GetData();

            double[][] dataTrainX = new double[(int) (tuple.mfcc_inputs.Length * 0.66)][];
            double[][] dataTestX = new double[(int) (tuple.mfcc_inputs.Length * 0.33)][];

            bool[] dataTrainY = new bool[(int) (tuple.classes.Length * 0.66)];
            bool[] dataTestY = new bool[(int) (tuple.classes.Length * 0.33)];

            Console.WriteLine("Loaded datasets (x): " + tuple.mfcc_inputs.Length);
            Console.WriteLine("Loaded datasets (y): " + tuple.classes.Length);
            
            Console.WriteLine("MFCC dimension: " + tuple.mfcc_inputs[0].Length);
            
            Console.WriteLine("Coefficient 1: " + (int) (tuple.mfcc_inputs.Length * 0.66));
            Console.WriteLine("Coefficient 2: " + (int) (tuple.mfcc_inputs.Length * 0.33));

            Array.Copy(tuple.mfcc_inputs, 0, dataTrainX, 0, (int) (tuple.mfcc_inputs.Length * 0.66));
            Array.Copy(tuple.mfcc_inputs, 0, dataTestX, (int) (tuple.mfcc_inputs.Length * 0.66),
                tuple.mfcc_inputs.Length);

            Array.Copy(tuple.classes, 0, dataTrainY, 0, (int) (tuple.classes.Length * 0.66));
            Array.Copy(tuple.classes, 0, dataTestY, (int) (tuple.classes.Length * 0.66), tuple.classes.Length);

            Console.WriteLine("Training dimension: (" + dataTrainX.Length + ", " + dataTrainY.Length + ")");
            Console.WriteLine("Test dimension: (" + dataTestX.Length + ", " + dataTestY.Length + ")");

            DeepBeliefNetwork network = Train(ref dataTrainX, ref dataTrainY);
            double[][] predictionProba = Predict(network, dataTestX);

            double similarity = 0;
            for (int i = 0; i < predictionProba.Length; i++)
            {
                bool classPredicted = predictionProba[i][0] > 0.5;
                bool classReal = dataTestY[i];
                similarity += Convert.ToInt32(classPredicted == classReal);
            }
            similarity /= predictionProba.Length;

            Console.WriteLine("Similarity: " + similarity + "%");

            while (true)
            {
                Console.ReadKey(true);
            }
        }

        public static unsafe DeepBeliefNetwork Train(ref double[][] inputs, ref bool[] outputsClasses)
        {
            double[][] outputs = (from output in outputsClasses
                select output == true ? new double[] {1, 0} : new double[] {0, 1}).ToArray();

            DeepBeliefNetwork network = new DeepBeliefNetwork(inputsCount: inputs.Length,
                hiddenNeurons: new int[] {250, 200, 200, 25});
            var teacher = new DeepNeuralNetworkLearning(network)
            {
                Algorithm = (ann, i) => new ParallelResilientBackpropagationLearning(ann),
                LayerIndex = network.Machines.Count - 1
            };

            var layerData = teacher.GetLayerInput(inputs);
            for (int i = 0; i < 5000; i++)
            {
                teacher.RunEpoch(layerData, outputs);
            }
            network.UpdateVisibleWeights();
            return network;
        }

        public static double[][] Predict(DeepBeliefNetwork network, double[][] data)
        {
            return (from input in data select network.Compute(input)).ToArray();
        }

        
        public static (double[][] mfcc_inputs, bool[] classes) GetData()
        {
            List<double[]> input = new List<double[]>();
            List<bool> classes = new List<bool>();
            String[] files = Directory.GetFiles("DATA");
            
            int count = files.Length;
            int current = 0;
            foreach (String file in files)
            {
                input.Add(GetMfcc(file));
                classes.Add(file.Split('_')[2] != "0");
                
                current++;
                if (current % 50 == 0)
                    Console.WriteLine("Reading data... " + (current / count * 100) + "%");
            }
            return (input.ToArray(), classes.ToArray());
        }

        public static double[] GetMfcc(string file)
        {
            WaveDecoder decoder = new WaveDecoder(file);
            Signal signal = decoder.Decode();
            MelFrequencyCepstrumCoefficient mfcc = new MelFrequencyCepstrumCoefficient();
            MelFrequencyCepstrumCoefficientDescriptor[] ra = mfcc.Transform(signal).ToArray();
            var query = from x in ra select x.Descriptor.Average();
            return query.ToArray();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}