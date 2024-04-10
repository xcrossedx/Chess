using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    class Neuron
    {
        int type;
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

        public void Connect(Neuron n)
        {
            weights.Add(new Connection(n));
        }

        public void Connect(double w, Neuron n)
        {
            weights.Add(new Connection(w, n));
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
                    neurons.Add(new Neuron(0));
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

        public void Connect(Neuron previousNeuron)
        {
            foreach(Neuron n in neurons)
            {
                n.weights.Add(new Connection(previousNeuron));
            }
        }

        public void Connect(Layer previousLayer)
        {
            foreach(Neuron n in neurons)
            {
                if (previousLayer.neurons != null)
                {
                    foreach (Neuron pn in previousLayer.neurons)
                    {
                        n.weights.Add(new Connection(pn));
                    }
                }
                else
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
            foreach(Neuron n in neurons)
            {
                n.output = n.output / exponentialsSum;
            }
        }
    }
}
