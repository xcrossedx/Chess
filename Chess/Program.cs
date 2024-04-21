using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Net.Mime;
using System.IO;
using Newtonsoft.Json;

namespace Chess
{
    class Program
    {
        public static bool exit = false;
        public static bool playing = true;
        public static int player = 1;
        public static int currentTurn = 1;
        public static bool turnTaken = false;
        public static bool validSelection = true;
        public static int[] selector = new int[] { 1, 4 };
        public static int[] selectedPiece = new int[] { -1, -1 };
        public static Random rng = new Random();
        public static ChessBot Magnus;
        public static int round = 0;
        public static int turnCount = 0;

        public static List<Piece> pieces = new List<Piece>();
        static void Main()
        {
            Init();

            while (!exit)
            {
                AssignPlayers();
                SetPieces();
                DrawBoard();
                while (playing)
                {
                    PlayGame();
                }
                ClearGame();
                if (Magnus.Levy.id > 5000)
                {
                    Magnus.Train();
                    Thread.Sleep(5000);
                    Magnus.Levy.CreateDatabase(true);
                    SaveNetworks();
                }
            }
        }

        static void Init()
        {
            Console.SetWindowSize(24, 12);
            Console.SetBufferSize(24, 12);
            Console.CursorVisible = false;
            Console.OutputEncoding = Encoding.UTF8;
            Magnus = new ChessBot();

            CheckForNetworks();
        }

        static void AssignPlayers()
        {
            if (rng.Next() % 2 == 1) { player *= -1; }
            currentTurn = 1;
            playing = true;
        }

        static void SetPieces()
        {
            if (player == 1) { selector = new int[] { 1, 4 }; }
            else { selector = new int[] { 6, 4 }; }
            pieces.Clear();

            for (int c = 0; c < 8; c++)
            {
                pieces.Add(new Piece(0, 1, new int[] { 1, c }));
                if (c == 0 | c == 7) { pieces.Add(new Piece(1, 1, new int[] { 0, c })); }
                else if (c == 1 | c == 6) { pieces.Add(new Piece(2, 1, new int[] { 0, c })); }
                else if (c == 2 | c == 5) { pieces.Add(new Piece(3, 1, new int[] { 0, c })); }
                else if (c == 3) { pieces.Add(new Piece(4, 1, new int[] { 0, 3 })); }
                else if (c == 4) { pieces.Add(new Piece(5, 1, new int[] { 0, 4 })); }
                pieces.Add(new Piece(0, -1, new int[] { 6, c }));
                if (c == 0 | c == 7) { pieces.Add(new Piece(1, -1, new int[] { 7, c })); }
                else if (c == 1 | c == 6) { pieces.Add(new Piece(2, -1, new int[] { 7, c })); }
                else if (c == 2 | c == 5) { pieces.Add(new Piece(3, -1, new int[] { 7, c })); }
                else if (c == 3) { pieces.Add(new Piece(4, -1, new int[] { 7, 3 })); }
                else if (c == 4) { pieces.Add(new Piece(5, -1, new int[] { 7, 4 })); }
            }
        }

        static void DrawBoard()
        {
            int[] buffer = new int[] { 2, 3 };
            string[] characters = new string[] { "♙ ", "♜ ", "♞ ", "♝ ", "♛ ", "♚ " };
            ConsoleColor[] pieceColors = new ConsoleColor[] { ConsoleColor.White, ConsoleColor.Black };
            ConsoleColor[] boardColors = new ConsoleColor[] { ConsoleColor.Gray, ConsoleColor.DarkGray };

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Console.SetCursorPosition((c * 2) + buffer[1], r + buffer[0]);
                    if ((player == 1 & selector[0] == 7 - r & selector[1] == c) | (player == -1 & selector[0] == r & selector[1] == 7 - c))
                    {
                        if (validSelection) { Console.BackgroundColor = ConsoleColor.Green; }
                        else { Console.BackgroundColor = ConsoleColor.Red; }
                    }
                    else if ((r + c) % 2 == 0) { Console.BackgroundColor = boardColors[0]; }
                    else { Console.BackgroundColor = boardColors[1]; }
                    Piece p = null;

                    if (player == 1 & pieces.Exists(x => x.position[0] == 7 - r & x.position[1] == c))
                    {
                        p = pieces.Find(x => x.position[0] == 7 - r & x.position[1] == c);
                    }
                    else if (player == -1 & pieces.Exists(x => x.position[0] == r & x.position[1] == 7 - c))
                    {
                        p = pieces.Find(x => x.position[0] == r & x.position[1] == 7 - c);
                    }

                    if (p != null)
                    {
                        if (selectedPiece[0] != -1 & p.position[0] == selectedPiece[0] & p.position[1] == selectedPiece[1])
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                        else
                        {
                            if (p.color == 1) { Console.ForegroundColor = pieceColors[0]; }
                            else { Console.ForegroundColor = pieceColors[1]; }
                        }
                        Console.Write(characters[p.type]);
                    }
                    else { Console.Write("  "); }
                }
            }
        }

        static void PlayGame()
        {
            turnCount++;
            turnTaken = false;

            CheckMoves(1);
            CheckMoves(2);
            pieces.Find(x => x.color == 1 & x.type == 5).CheckForCastling();
            pieces.Find(x => x.color == -1 & x.type == 5).CheckForCastling();

            CheckForGameOver();

            if (playing)
            {
                if (currentTurn == player)
                {
                    //TakeTurn();
                    Magnus.TakeTurn(true);
                }
                else
                {
                    //TakeTurn();
                    Magnus.TakeTurn(true);
                }

                if (selectedPiece[0] != -1)
                {
                    pieces.Find(x => x.position[0] == selectedPiece[0] & x.position[1] == selectedPiece[1]).Move(selector);
                }
                selectedPiece = new int[] { -1, -1 };
                DrawBoard();
                currentTurn *= -1;
            }
            else { //Console.ReadKey(true);
                   }
        }

        static void TakeTurn()
        {
            while (!turnTaken)
            {
                TakeInput(true);
                CheckForValidSelection(currentTurn);
                DrawBoard();
            }
        }

        static void CheckMoves(int pass)
        {
            for (int p = 0; p < pieces.Count(); p++)
            {
                if (pass == 1) { pieces[p].CheckMoves(); }
                else
                {
                    pieces[p].previousPosition = pieces[p].position;
                    if (pieces[p].color == currentTurn) { pieces[p].LegalCheck(pieces[p].availableMoves, false); }
                }
            }
        }

        static void TakeInput(bool inGame)
        {
            ConsoleKey input = Console.ReadKey(true).Key;

            if (inGame)
            {
                if ((input == ConsoleKey.UpArrow & player == 1) | (input == ConsoleKey.DownArrow & player == -1))
                {
                    selector[0] = (selector[0] + 1) % 8;
                }
                else if ((input == ConsoleKey.DownArrow & player == 1) | (input == ConsoleKey.UpArrow & player == -1))
                {
                    selector[0] = (selector[0] + 7) % 8;
                }
                else if ((input == ConsoleKey.RightArrow & player == 1) | (input == ConsoleKey.LeftArrow & player == -1))
                {
                    selector[1] = (selector[1] + 1) % 8;
                }
                else if ((input == ConsoleKey.LeftArrow & player == 1) | (input == ConsoleKey.RightArrow & player == -1))
                {
                    selector[1] = (selector[1] + 7) % 8;
                }
                else if (input == ConsoleKey.Enter | input == ConsoleKey.Spacebar)
                {
                    if (selectedPiece[0] == -1 & validSelection)
                    {
                        selectedPiece = new int[] { selector[0], selector[1] };
                    }
                    else if (selector[0] == selectedPiece[0] & selector[1] == selectedPiece[1])
                    {
                        selectedPiece = new int[] { -1, -1 };
                    }
                    else if (validSelection)
                    {
                        turnTaken = true;
                    }
                }
            }
            
            if (input == ConsoleKey.Escape)
            {
                SaveNetworks();
                if (inGame) { turnTaken = true; }
                playing = false;
                exit = true;
            }
        }

        static void CheckForValidSelection(int p)
        {
            if (selectedPiece[0] == -1)
            {
                if (pieces.Exists(x => x.color == p & x.position[0] == selector[0] & x.position[1] == selector[1]))
                {
                    validSelection = true;
                }
                else { validSelection = false; }
            }
            else
            {
                if (pieces.Find(x => x.position[0] == selectedPiece[0] & x.position[1] == selectedPiece[1]).availableMoves.Exists(x => x[0] == selector[0] & x[1] == selector[1]))
                {
                    validSelection = true;
                }
                else { validSelection = false; }
            }
        }

        static void CheckForGameOver()
        {
            int r1 = 0;
            int r2 = 0;

            if (!pieces.Exists(x => x.color == currentTurn & x.availableMoves.Count() > 0))
            {
                playing = false;
                int[] king = pieces.Find(x => x.color == currentTurn & x.type == 5).position;

                Console.SetCursorPosition(3, 10);
                Console.ForegroundColor = ConsoleColor.Black;

                if (pieces.Exists(x => x.color != currentTurn & x.availableMoves.Exists(y => y[0] == king[0] & y[1] == king[1])))
                {
                    r1 = -100;
                    r2 = 150;
                    Console.BackgroundColor = ConsoleColor.Green;
                    if (currentTurn == 1) { Console.Write("Black wins!"); }
                    else { Console.Write("White wins!"); }
                }
                else
                {
                    r1 = -50;
                    r2 = -50;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.Write("Stalemate!");
                }
            }
            else if (pieces.Count() == 2 | turnCount > 50)
            {
                r1 = -50;
                r2 = -50;
                playing = false;
                turnCount = 0;
                Console.SetCursorPosition(3, 10);
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.Write("Stalemate!");
            }

            if (r1 != 0)
            {
                Magnus.SetInputs();
                Magnus.Levy.SetReward(r1);
                Magnus.Levy.Record(Magnus.network[0], 0, 0, true);
                currentTurn *= -1;
                Magnus.SetInputs();
                Magnus.Levy.SetReward(r2);
                Magnus.Levy.Record(Magnus.network[0], 0, 0, true);
                Magnus.Levy.ClearStates('A');
                Magnus.Levy.ClearStates('B');
            }
        }

        static void ClearGame()
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();
        }

        static void SaveNetworks()
        {
            List<double> weights = new List<double>();
            List<double> biases = new List<double>();

            FindValues(Magnus.network);

            File.WriteAllText("policyBias.json", JsonConvert.SerializeObject(biases));
            File.WriteAllText("policyWeights.json", JsonConvert.SerializeObject(weights));

            void FindValues(List<Layer> network)
            {
                foreach (Layer layer in network)
                {
                    if (layer.type != 4)
                    {
                        if (layer.neurons != null)
                        {
                            foreach (Neuron neuron in layer.neurons)
                            {
                                biases.Add(neuron.bias);

                                foreach (Connection connection in neuron.weights)
                                {
                                    weights.Add(connection.weight);
                                }
                            }
                        }

                        if (layer.neuronGrid != null)
                        {
                            foreach (List<Neuron> list in layer.neuronGrid)
                            {
                                foreach (Neuron neuron in list)
                                {
                                    biases.Add(neuron.bias);

                                    foreach (Connection connection in neuron.weights)
                                    {
                                        weights.Add(connection.weight);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Thread.Sleep(3000);
        }

        static void CheckForNetworks()
        {
            List<double> biases = new List<double>();
            List<double> weights = new List<double>();

            if (File.Exists("policyBias.json"))
            {
                List<double> bTemp = JsonConvert.DeserializeObject<List<double>>(File.ReadAllText("policyBias.json"));
                List<double> wTemp = JsonConvert.DeserializeObject<List<double>>(File.ReadAllText("policyWeights.json"));

                if (bTemp != null) { biases.AddRange(bTemp); }
                if (wTemp != null) { weights.AddRange(wTemp); }

                if (bTemp != null) { ApplyValues(Magnus.network); }
            }
            else
            {
                File.Create("policyBias.json");
                File.Create("policyWeights.json");
            }

            void ApplyValues(List<Layer> network)
            {
                int biasIndex = 0;
                int weightIndex = 0;

                foreach (Layer layer in network)
                {
                    if (layer.type != 4)
                    {
                        if (layer.neurons != null)
                        {
                            foreach (Neuron neuron in layer.neurons)
                            {
                                neuron.bias = biases[biasIndex];
                                biasIndex++;

                                foreach (Connection connection in neuron.weights)
                                {
                                    connection.weight = weights[weightIndex];
                                    weightIndex++;
                                }
                            }
                        }

                        if (layer.neuronGrid != null)
                        {
                            foreach (List<Neuron> list in layer.neuronGrid)
                            {
                                foreach (Neuron neuron in list)
                                {
                                    neuron.bias = biases[biasIndex];
                                    biasIndex++;

                                    foreach (Connection connection in neuron.weights)
                                    {
                                        connection.weight = weights[weightIndex];
                                        weightIndex++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
