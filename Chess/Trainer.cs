using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Chess
{
    class Trainer
    {
        public List<Layer> network;

        public Trainer()
        {
            network = new List<Layer>();

            network.Add(new Layer(0, 8));
            network.Add(new Layer(3, 128));
            network[1].Connect(network[0]);
            network.Add(new Layer(3, 128));
            network[2].Connect(network[1]);
            network.Add(new Layer(3, 128));
            network[3].Connect(network[2]);
        }

        public void Evaluate(int size)
        {
            if (network.Count() == 4)
            {
                network.Add(new Layer(4, size));
            }
            else { network[4] = new Layer(4, size); }
            network[4].Connect(network[3]);

            network[1].Activate();
            network[2].Activate();
            network[3].Activate();
            network[4].Activate();
        }
    }
}
