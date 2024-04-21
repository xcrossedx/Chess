using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Chess
{
    class ChessBot
    {
        public List<Layer> network;
        public int iteration = 0;

        List<Move> moves;

        public Recorder Levy;

        public ChessBot()
        {
            network = new List<Layer>();
            network.Add(new Layer(0, 8));
            network.Add(new Layer(1, 8));
            network[1].Connect(network[0], new int[] { -2, 2 }, 1);
            network.Add(new Layer(1, 4));
            network[2].Connect(network[1], new int[] { -1, 2 }, 2);
            network.Add(new Layer(1, 4));
            network[3].Connect(network[2], new int[] { -1, 1 }, 1);
            network.Add(new Layer(3, 128));
            network[4].Connect(network[3]);
            network.Add(new Layer(3, 128));
            network[5].Connect(network[4]);

            Levy = new Recorder();
        }

        public void SetInputs()
        {
            moves = new List<Move>();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = Program.pieces.Find(pi => pi.position[0] == x & pi.position[1] == y);

                    if (p != null)
                    {
                        network[0].neuronGrid[x][y].output = (p.type + 1) * p.color * Program.currentTurn;
                        if (p.color == Program.currentTurn)
                        {
                            foreach (int[] m in p.availableMoves)
                            {
                                moves.Add(new Move(p, m));
                            }
                        }
                    }
                    else
                    {
                        network[0].neuronGrid[x][y].output = 0;
                    }

                    network[0].neuronGrid[x][y].output += 6;
                    network[0].neuronGrid[x][y].output /= 12;
                }
            }
        }

        public void SetOutputs(int size)
        {
            if (network.Count == 6)
            {
                network.Add(new Layer(4, size));
            }
            else
            {
                network[6] = new Layer(4, size);
            }
            network[6].Connect(network[5]);
        }

        void Activate()
        {
            network[1].Activate();
            network[2].Activate();
            network[3].Activate();
            network[4].Activate();
            network[5].Activate();
            network[6].Activate();
        }

        public void TakeTurn(bool vsSelf)
        {
            double max = 0;
            int moveIndex = 0;

            double epsilon = 10000 / 1 + (0.1 * iteration);

            SetInputs();

            if (Program.rng.Next(0, 10000) > epsilon)
            {
                SetOutputs(moves.Count());

                Activate();

                foreach (Neuron n in network[6].neurons)
                {
                    if (n.output > max)
                    {
                        max = n.output;
                        moveIndex = network[6].neurons.IndexOf(n);
                    }
                }
            }
            else { moveIndex = Program.rng.Next(0, moves.Count()); }

            Levy.Record(network[0], moveIndex, moves.Count(), vsSelf);

            moves[moveIndex].piece.Move(moves[moveIndex].move);
        }

        public void Train()
        {
            iteration++;
            int max = Levy.FindMaxReplayID();
            List<int> batch = new List<int>();

            while (batch.Count() < 250)
            {
                int sample = Program.rng.Next(0, max);

                if (!batch.Contains(sample)) { batch.Add(sample); }
            }

            foreach (Layer l in network)
            {
                if (l.neurons != null)
                {
                    foreach (Neuron n in l.neurons)
                    {
                        n.partialDerivative = 0;
                        n.biasGradient = 0;
                        n.partialBiasGradients.Clear();

                        foreach (Connection c in n.weights)
                        {
                            c.partialGradients.Clear();
                        }
                    }
                }

                if (l.neuronGrid != null)
                {
                    foreach (List<Neuron> li in l.neuronGrid)
                    {
                        foreach (Neuron n in li)
                        {
                            n.partialDerivative = 0;
                            n.biasGradient = 0;
                            n.partialBiasGradients.Clear();

                            foreach (Connection c in n.weights)
                            {
                                c.partialGradients.Clear();
                            }
                        }
                    }
                }
            }

            foreach (int id in batch)
            {
                var sample = GetSample(id);

                int[] state = sample.Item1;
                int availableActions = sample.Item2;
                int action = sample.Item3;
                double reward = sample.Item4;
                int[] nextState = sample.Item5;
                int nextAvailableActions = sample.Item6;

                double Qvalue = 0;
                double targetQvalue = 0;

                int i = 0;

                foreach (List<Neuron> l in network[0].neuronGrid)
                {
                    foreach (Neuron n in l)
                    {
                        n.output = nextState[i];
                        i++;
                    }
                }

                SetOutputs(nextAvailableActions);
                Activate();

                foreach (Neuron n in network[6].neurons)
                {
                    if (n.output > targetQvalue) { targetQvalue = n.output; }
                }

                i = 0;

                foreach (List<Neuron> l in network[0].neuronGrid)
                {
                    foreach (Neuron n in l)
                    {
                        n.output = state[i];
                        i++;
                    }
                }

                SetOutputs(availableActions);
                Activate();

                Qvalue = network[6].neurons[action].output;

                targetQvalue += reward;

                for (int l = network.Count() - 1; l > 0; l--)
                {
                    if (l == network.Count() - 1)
                    {
                        foreach (Connection c in network[6].neurons[action].weights)
                        {
                            c.neuron.partialBiasGradients.Add(2 * (Qvalue - targetQvalue));
                            c.neuron.partialDerivative = 2 * (Qvalue - targetQvalue);
                        }
                    }
                    else
                    {
                        if (network[l].neurons != null)
                        {
                            foreach (Neuron n in network[l].neurons)
                            {
                                if ((n.type == 1 | n.type == 3) & n.output < 0) { n.partialDerivative = 0; }
                                if (n.bias != 0) { n.partialBiasGradients.Add(n.partialDerivative); }

                                foreach (Connection c in n.weights)
                                {
                                    c.partialGradients.Add(n.partialDerivative * c.neuron.output);
                                    c.neuron.partialDerivative += n.partialDerivative * c.weight;
                                }
                            }
                        }

                        if (network[l].neuronGrid != null)
                        {
                            foreach (List<Neuron> list in network[l].neuronGrid)
                            {
                                foreach (Neuron n in list)
                                {
                                    if ((n.type == 1 | n.type == 3) & n.output < 0) { n.partialDerivative = 0; }
                                    if (n.bias != 0) { n.partialBiasGradients.Add(n.partialDerivative); }

                                    foreach (Connection c in n.weights)
                                    {
                                        c.partialGradients.Add(n.partialDerivative * c.neuron.output);
                                        c.neuron.partialDerivative += n.partialDerivative * c.weight;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            /*double tempBiasNorm = 0;
            double tempWeightNorm = 0;
            double biasNormAdjustment = 1;
            double weightNormAdjustment = 1;

            foreach (Layer l in network)
            {
                if (l.neurons != null)
                {
                    foreach (Neuron n in l.neurons)
                    {
                        if (n.bias != 0)
                        {
                            int numberOfGradients = n.partialBiasGradients.Count();
                            n.biasGradient = n.partialBiasGradients.Sum() / numberOfGradients;
                            tempBiasNorm += Math.Pow(n.biasGradient, 2);
                        }

                        foreach (Connection c in n.weights)
                        {
                            int numberOfGradients = c.partialGradients.Count();
                            c.gradient = c.partialGradients.Sum() / numberOfGradients;
                            tempWeightNorm += Math.Pow(c.gradient, 2);
                        }
                    }
                }

                if (l.neuronGrid != null)
                {
                    foreach (List<Neuron> list in l.neuronGrid)
                    {
                        foreach (Neuron n in list)
                        {
                            if (n.bias != 0)
                            {
                                int numberOfGradients = n.partialBiasGradients.Count();
                                n.biasGradient = n.partialBiasGradients.Sum() / numberOfGradients;
                                tempBiasNorm += Math.Pow(n.biasGradient, 2);
                            }

                            foreach (Connection c in n.weights)
                            {
                                int numberOfGradients = c.partialGradients.Count();
                                c.gradient = c.partialGradients.Sum() / numberOfGradients;
                                tempWeightNorm += Math.Pow(c.gradient, 2);
                            }
                        }
                    }
                }
            }

            double biasNorm = Math.Sqrt(tempBiasNorm);
            double weightNorm = Math.Sqrt(tempWeightNorm);
            Debug.WriteLine($"{biasNorm}, {weightNorm}");
            if (biasNorm > 1) { biasNormAdjustment = 1 / biasNorm; }
            if (weightNorm > 1) { weightNormAdjustment = 1 / weightNorm; } */

            double biasLearningRate = 0.00001;
            double weightLearningRate = 0.0001;

            foreach (Layer l in network)
            {
                if (l.neurons != null)
                {
                    foreach (Neuron n in l.neurons)
                    {
                        if (n.bias != 0)
                        {
                            int numberOfGradients = n.partialBiasGradients.Count();
                            n.biasGradient = n.partialBiasGradients.Sum() / numberOfGradients;
                            n.bias -= biasLearningRate * n.biasGradient;// * biasNormAdjustment;
                        }

                        foreach (Connection c in n.weights)
                        {
                            int numberOfGradients = c.partialGradients.Count();
                            c.gradient = c.partialGradients.Sum() / numberOfGradients;
                            c.weight -= weightLearningRate * c.gradient;// * weightNormAdjustment;
                        }
                    }
                }

                if (l.neuronGrid != null)
                {
                    foreach (List<Neuron> list in l.neuronGrid)
                    {
                        foreach (Neuron n in list)
                        {
                            if (n.bias != 0)
                            {
                                int numberOfGradients = n.partialBiasGradients.Count();
                                n.biasGradient = n.partialBiasGradients.Sum() / numberOfGradients;
                                n.bias -= biasLearningRate * n.biasGradient;// * biasNormAdjustment;
                            }

                            foreach (Connection c in n.weights)
                            {
                                int numberOfGradients = c.partialGradients.Count();
                                c.gradient = c.partialGradients.Sum() / numberOfGradients;
                                c.weight -= weightLearningRate * c.gradient;// * weightNormAdjustment;
                            }
                        }
                    }
                }
            }
        }

        Tuple<int[], int, int, double, int[], int> GetSample(int id)
        {
            using (var connection = new SQLiteConnection(@"Data Source=replayBuffer.db;Version=3;"))
            {
                connection.Open();

                using (var command = new SQLiteCommand("SELECT State, AvailableActions, Action, Reward, NextState, NextAvailableActions FROM ReplayBuffer WHERE Id = @PrimaryKey", connection))
                {
                    command.Parameters.AddWithValue("@PrimaryKey", id);

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        int[] state = JsonConvert.DeserializeObject<int[]>(reader.GetString(0));
                        int availableActions = reader.GetInt32(1);
                        int action = reader.GetInt32(2);
                        double reward = reader.GetDouble(3);
                        int[] nextState = JsonConvert.DeserializeObject<int[]>(reader.GetString(4));
                        int nextAvailableActions = reader.GetInt32(5);
                        return Tuple.Create(state, availableActions, action, reward, nextState, nextAvailableActions);
                    }
                }
            }
        }
    }

    class Move
    {
        public Piece piece;
        public int[] move;

        public Move(Piece p, int[] m)
        {
            piece = p;
            move = m;
        }
    }
}
