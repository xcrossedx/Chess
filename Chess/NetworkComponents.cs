﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Chess
{
    class Neuron
    {
        public int type;
        public double bias;
        public double output;
        public List<Connection> weights;

        public Neuron(int t)
        {
            type = t;
            if (t == 1 | t == 3) { bias = 0.1; }
            else { bias = 0; }
            weights = new List<Connection>();
        }

        public Neuron(int t, double b)
        {
            type = t;
            bias = b;
            weights = new List<Connection>();
        }

        public void CalculateOutput()
        {
            double tempOutput = 0;
            List<double> weightedInputs = new List<double>();

            foreach (Connection c in weights)
            {
                tempOutput += c.weight * c.neuron.output;
                weightedInputs.Add(c.weight * c.neuron.output);
            }

            tempOutput += bias;

            if (type == 1 | type == 3)
            {
                if (tempOutput < 0) { tempOutput *= 0.1; }
            }
            else if (type == 2)
            {
                tempOutput = weightedInputs.Max();
            }

            output = tempOutput;
        }
    }

    class Connection
    {
        public double weight;
        public Neuron neuron;

        public Connection(Neuron n)
        {
            weight = (double)Program.rng.Next(-100, 100) / (double)1000;
            neuron = n;
        }

        public Connection(double w, Neuron n)
        {
            weight = w;
            neuron = n;
        }
    }

    class Layer
    {
        public int type;
        public List<Neuron> neurons;
        public List<List<Neuron>> neuronGrid;

        public Layer(int t, int size)
        {
            type = t;

            if (type < 3)
            {
                if (type == 0)
                {
                    neurons = new List<Neuron>();
                }

                neuronGrid = new List<List<Neuron>>();

                for (int x = 0; x < size; x++)
                {
                    neuronGrid.Add(new List<Neuron>());

                    for (int y = 0; y < size; y++)
                    {
                        neuronGrid[x].Add(new Neuron(type));
                    }
                }
            }
            else
            {
                neurons = new List<Neuron>();

                for (int x = 0; x < size; x++)
                {
                    neurons.Add(new Neuron(type));
                }
            }
        }

        public void Connect(Layer previousLayer)
        {
            foreach (Neuron n in neurons)
            {
                if (previousLayer.neurons != null)
                {
                    foreach (Neuron pn in previousLayer.neurons)
                    {
                        n.weights.Add(new Connection(pn));
                    }
                }

                if (previousLayer.neuronGrid != null)
                {
                    for (int x = 0; x < previousLayer.neuronGrid.Count(); x++)
                    {
                        for (int y = 0; y < previousLayer.neuronGrid.Count(); y++)
                        {
                            n.weights.Add(new Connection(previousLayer.neuronGrid[x][y]));
                        }
                    }
                }
            }
        }

        public void Connect(Layer previousLayer, int[] range, int stride)
        {
            for (int x = 0; x < neuronGrid.Count(); x++)
            {
                for (int y = 0; y < neuronGrid.Count(); y++)
                {
                    for (int px = (x * stride) - range[0]; px <= (x * stride) + range[1]; px++)
                    {
                        for (int py = (y * stride) - range[0]; py <= (y * stride) + range[1]; py++)
                        {
                            if (px >= 0 & py >= 0 & px < previousLayer.neuronGrid.Count() & py < previousLayer.neuronGrid.Count()) { neuronGrid[x][y].weights.Add(new Connection(previousLayer.neuronGrid[px][py])); }
                        }
                    }
                }
            }
        }

        public void Activate()
        {
            if (neurons != null)
            {
                foreach (Neuron n in neurons)
                {
                    n.CalculateOutput();
                }
            }

            if (neuronGrid != null)
            {
                foreach (List<Neuron> l in neuronGrid)
                {
                    foreach (Neuron n in l)
                    {
                        n.CalculateOutput();
                    }
                }
            }
        }

        public void Softmax()
        {
            double[] outputExponentials = neurons.Select(n => Math.Exp(n.output)).ToArray();
            double exponentialsSum = outputExponentials.Sum();
            foreach (Neuron n in neurons)
            {
                n.output = n.output / exponentialsSum;
            }
        }
    }

    class Recorder
    {
        int[] state1A;
        int[] state1B;
        int actionA;
        int actionB;
        double rewardA;
        double rewardB;
        int[] state2A;
        int[] state2B;
        int id;
        char set;

        List<double[]> exponentials;

        public Recorder()
        {
            actionA = -1;
            actionB = -1;
            rewardA = -1;
            rewardB = -1;
            ClearStates('A');
            ClearStates('B');
            id = 0;
            set = 'A';
            CreateDatabase();
            exponentials = new List<double[]>();
            exponentials.AddRange(new double[10][] { new double[2] { -5, 4 }, new double[2] { -4, 2 }, new double[2] { -3, 2 }, new double[2] { -2, 3 }, new double[2] { -1, 1 }, new double[2] { 1, 1.25 }, new double[2] { 2, 3.25 }, new double[2] { 3, 2.25 }, new double[2] { 4, 2.25 }, new double[2] { 5, 4.25 } });
        }

        public void ClearStates(char states)
        {
            if (states == 'A')
            {
                state1A = new int[64];
                state2A = new int[64];
                state1A[0] = -1;
                state2A[1] = -1;
            }
            if (states == 'B')
            {
                state1B = new int[64];
                state2B = new int[64];
                state1B[0] = -1;
                state2B[0] = -1;
            }
        }

        void CreateDatabase()
        {
            if (!File.Exists("replayBuffer.db"))
            {
                File.Create("replayBuffer.db");
            }
            else
            {
                try
                {
                    id = FindMaxReplayID();
                }
                catch { id = 0; }
            }
        }

        public int FindMaxReplayID()
        {
            using (var connection = new SQLiteConnection(@"Data Source=replayBuffer.db;Version=3;"))
            {
                connection.Open();

                using (var command = new SQLiteCommand("SELECT MAX(Id) FROM ReplayBuffer", connection))
                {
                    return Convert.ToInt32(command.ExecuteScalar()) + 1;
                }
            }
        }

        public void Record(Layer input, int action, bool vsSelf)
        {
            if (state1A[0] == -1)
            {
                state1A = (int[])ConvertInputs().Clone();
                actionA = action;
            }
            else if (state1B[0] == -1 & set == 'B')
            {
                state1B = (int[])ConvertInputs().Clone();
                actionB = action;
            }
            else if (set == 'A')
            {
                state2A = (int[])ConvertInputs().Clone();
                rewardA = FindReward(state1A, state2A);
                Save();
                id++;
                state1A = (int[])state2A.Clone();
            }
            else if (set == 'B')
            {
                state2B = (int[])ConvertInputs().Clone();
                rewardB = FindReward(state1B, state2B);
                Save();
                id++;
                state1B = (int[])state2B.Clone();
            }

            if (vsSelf)
            {
                if (set == 'A') { set = 'B'; }
                else if (set == 'B') { set = 'A'; }
            }

            int[] ConvertInputs()
            {
                int i = 0;
                int[] inputs = new int[64];

                foreach (List<Neuron> l in input.neuronGrid)
                {
                    foreach (Neuron n in l)
                    {
                        inputs[i] = (int)n.output;
                        i++;
                    }
                }

                return inputs;
            }

            double FindReward(int[] s1, int[] s2)
            {
                double reward = 0;

                for (int i = -5; i < 6; i++)
                {
                    if (i != 0)
                    {
                        int s1Count = s1.Count(p => p == i);
                        int s2Count = s2.Count(p => p == i);
                        int difference = s1Count - s2Count;

                        if (difference != 0)
                        {
                            reward += Math.Sign(i) * -1 * (difference * (Math.Pow(1.5, 2 * exponentials.Find(x => x[0] == i)[1])));
                        }
                    }
                }

                return reward;
            }
        }

        public void Save()
        {
            using(var connection = new SQLiteConnection(@"Data Source=replayBuffer.db;Version=3;"))
            {
                connection.Open();

                using(var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS ReplayBuffer (Id INTEGER PRIMARY KEY, State TEXT, Action INTEGER, Reward DOUBLE, NextState TEXT)", connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SQLiteCommand("INSERT INTO ReplayBuffer (Id, State, Action, Reward, NextState) VALUES (@Id, @State, @Action, @Reward, @NextState)", connection))
                {
                    if (set == 'A')
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@State", JsonConvert.SerializeObject(state1A));
                        command.Parameters.AddWithValue("@Action", actionA);
                        command.Parameters.AddWithValue("@Reward", rewardA);
                        command.Parameters.AddWithValue("@NextState", JsonConvert.SerializeObject(state2A));
                    }
                    else if (set == 'B')
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@State", JsonConvert.SerializeObject(state1B));
                        command.Parameters.AddWithValue("@Action", actionB);
                        command.Parameters.AddWithValue("@Reward", rewardB);
                        command.Parameters.AddWithValue("@NextState", JsonConvert.SerializeObject(state2B));
                    }

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
