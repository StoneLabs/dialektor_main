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
using Accord.Neuro.Learning;

namespace dialektor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            var tuple = getData();

            // Split data to training and testing data
            double[][] data_train_x = new double[(int)(tuple.mfcc_inputs.Length * 0.66)][];
            double[][] data_test_x = new double[(int)(tuple.mfcc_inputs.Length * 0.33)][];

            bool[] data_train_y = new bool[(int)(tuple.classes.Length * 0.66)];
            bool[] data_test_y = new bool[(int)(tuple.classes.Length * 0.33)];

            // Copy data to arrays
            Array.Copy(tuple.mfcc_inputs, 0, data_train_x, 0, (int)(tuple.mfcc_inputs.Length * 0.66));
            Array.Copy(tuple.mfcc_inputs, 0, data_test_x, (int)(tuple.mfcc_inputs.Length * 0.66), tuple.mfcc_inputs.Length);

            Array.Copy(tuple.classes, 0, data_train_y, 0, (int)(tuple.classes.Length * 0.66));
            Array.Copy(tuple.classes, 0, data_test_y, (int)(tuple.classes.Length * 0.66), tuple.classes.Length);
            
            // Plot dimensions
            Console.WriteLine("Training dimension: (" + data_train_x.Length + ", " + data_train_y.Length + ")");
            Console.WriteLine("Test dimension: (" + data_test_x.Length + ", " + data_test_y.Length + ")");

            //Train & test
            DeepBeliefNetwork network = train(ref data_train_x, ref data_train_y);
            double[][] prediction_proba = predict(network, data_test_x);

            double similarity = 0;
            for (int i = 0; i < prediction_proba.Length; i++)
            {
                //Class is 1 if propablity for class 1 is greater than 0.5
                bool class_predicted = prediction_proba[i][0] > 0.5;
                bool class_real = data_test_y[i];
                similarity += Convert.ToInt32(class_predicted == class_real);
            }
            similarity /= prediction_proba.Length;
            
            Console.WriteLine("Similarity: " + similarity + "%");

            while (true) { Console.ReadKey(true); }
        }

        public static unsafe DeepBeliefNetwork train(ref double[][] inputs, ref bool[] outputs_classes)
        {
            double[][] outputs = (from output in outputs_classes
                               select output == true ? new double[] { 1, 0 } : new double[] { 0, 1 }).ToArray();

            DeepBeliefNetwork network = new DeepBeliefNetwork(inputsCount: inputs.Length,
                                                hiddenNeurons: new int[] { 250, 200, 200, 25 }); // 隠れ層と出力層の次元
            // DNNの学習アルゴリズムの生成
            var teacher = new DeepNeuralNetworkLearning(network)
            {
                Algorithm = (ann, i) => new ParallelResilientBackpropagationLearning(ann),
                LayerIndex = network.Machines.Count - 1
            };

            // 5000回学習
            var layerData = teacher.GetLayerInput(inputs);
            for (int i = 0; i < 5000; i++)
            {
                teacher.RunEpoch(layerData, outputs);
            }

            // 重みの更新
            network.UpdateVisibleWeights();
            return network;
        }

        public static double[][] predict(DeepBeliefNetwork network, double[][] data)
        {
            return (from input in data select network.Compute(input)).ToArray();
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