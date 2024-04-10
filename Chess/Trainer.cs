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
        public Layer input;
        Layer fullyConnected1;
        Layer fullyConnected2;
        Layer fullyConnected3;
        public Layer output;

        public Trainer()
        {
            input = new Layer(0, 8);
            fullyConnected1 = new Layer(3, 128);
            fullyConnected1.Connect(input);
            fullyConnected2 = new Layer(3, 128);
            fullyConnected2.Connect(fullyConnected1);
            fullyConnected3 = new Layer(3, 128);
            fullyConnected3.Connect(fullyConnected2);
        }

        public void Evaluate(int size)
        {
            output = new Layer(4, size);
            output.Connect(fullyConnected3);

            fullyConnected1.Activate();
            fullyConnected2.Activate();
            fullyConnected3.Activate();
            output.Activate();
        }
    }
}
