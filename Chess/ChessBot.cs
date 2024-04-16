using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace Chess
{
    class ChessBot
    {
        public List<Layer> network;
        public int iteration = 0;

        List<Move> moves;
        int reward;

        public Trainer Kasparov;
        public Trainer targetKasparov;

        Recorder Levy;

        public ChessBot()
        {
            network = new List<Layer>();
            network.Add(new Layer(0, 8));
            network.Add(new Layer(1, 8));
            network[1].Connect(network[0], new int[] { -2, 2 }, 1);
            network.Add(new Layer(2, 4));
            network[2].Connect(network[1], new int[] { 0, 1 }, 2);
            network.Add(new Layer(1, 4));
            network[3].Connect(network[2], new int[] { -1, 1 }, 1);
            network.Add(new Layer(3, 128));
            network[4].Connect(network[3]);
            network.Add(new Layer(3, 128));
            network[5].Connect(network[4]);

            Kasparov = new Trainer();
            targetKasparov = new Trainer();
            Levy = new Recorder();
        }

        public void TakeTurn(bool vsSelf)
        {
            moves = new List<Move>();
            double max = 0;
            int moveIndex = 0;

            double epsilon = 10 + 990 * Math.Exp(-0.25 * iteration);

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = Program.pieces.Find(pi => pi.position[0] == x & pi.position[1] == y);

                    if (p != null)
                    {
                        network[0].neuronGrid[x][y].output = (p.type + 1) * p.color * Program.currentTurn;
                        Kasparov.network[0].neuronGrid[x][y].output = (p.type + 1) * p.color * Program.currentTurn;
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
                        Kasparov.network[0].neuronGrid[x][y].output = 0;
                    }
                }
            }

            if (Program.rng.Next(0, 1000) > epsilon)
            {
                if (network.Count == 6)
                {
                    network.Add(new Layer(4, moves.Count()));
                }
                else
                {
                    network[6] = new Layer(4, moves.Count());
                }
                network[6].Connect(network[5]);

                network[1].Activate();
                network[2].Activate();
                network[3].Activate();
                network[4].Activate();
                network[5].Activate();
                network[6].Activate();

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

            Kasparov.Evaluate(moves.Count());
            targetKasparov.Evaluate(moves.Count());

            Levy.Record(network[0], moveIndex, vsSelf);

            moves[moveIndex].piece.Move(moves[moveIndex].move);
        }

        public void Train()
        {
            iteration++;
            int max = Levy.FindMaxReplayID();
            List<int> batch = new List<int>();

            while (batch.Count() < 100)
            {
                int sample = Program.rng.Next(0, max);

                if (!batch.Contains(sample)) { batch.Add(sample); }
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
