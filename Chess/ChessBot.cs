using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Chess
{
    class ChessBot
    {
        Layer input;
        Layer convolution1;
        Layer pool1;
        Layer convolution2;
        Layer fullyConnected1;
        Layer fullyConnected2;
        Layer output;

        List<Move> moves;

        Trainer Kasparov;

        public ChessBot()
        {
            input = new Layer(0, 8);
            convolution1 = new Layer(1, 8);
            convolution1.Connect(input, new int[] { -2, 2 }, 1);
            pool1 = new Layer(2, 4);
            pool1.Connect(convolution1, new int[] { 0, 1 }, 2);
            convolution2 = new Layer(1, 4);
            convolution2.Connect(pool1, new int[] { -1, 1 }, 1);
            fullyConnected1 = new Layer(3, 128);
            fullyConnected1.Connect(convolution2);
            fullyConnected1.Connect(input.neurons[0]);
            fullyConnected2 = new Layer(3, 128);
            fullyConnected2.Connect(fullyConnected1);

            Kasparov = new Trainer();
        }

        public void TakeTurn()
        {
            moves = new List<Move>();
            input.neurons[0].output = Program.currentTurn;
            Kasparov.input.neurons[0].output = Program.currentTurn;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Piece p = Program.pieces.Find(pi => pi.position[0] == x & pi.position[1] == y);

                    if (p != null)
                    {
                        input.neuronGrid[x][y].output = p.type * p.color;
                        Kasparov.input.neuronGrid[x][y].output = p.type * p.color;
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
                        input.neuronGrid[x][y].output = 0;
                        Kasparov.input.neuronGrid[x][y].output = 0;
                    }
                }
            }

            output = new Layer(4, moves.Count());
            output.Connect(fullyConnected2);

            Kasparov.Evaluate(moves.Count());

            convolution1.Activate();
            pool1.Activate();
            convolution2.Activate();
            fullyConnected1.Activate();
            fullyConnected2.Activate();
            output.Activate();

            double max = 0;
            int moveIndex = 0;

            foreach(Neuron n in output.neurons)
            {
                if (n.output > max)
                {
                    max = n.output;
                    moveIndex = output.neurons.IndexOf(n);
                }
            }

            moves[moveIndex].piece.Move(moves[moveIndex].move);
        }

        public void SaveValues()
        {

        }

        public void LoadValues()
        {
            
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
