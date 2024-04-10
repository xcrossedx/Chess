using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
    class Piece
    {
        public int type { get; set; }
        public int color { get; set; }
        public int[] position { get; set; }
        public int[] previousPosition { get; set; }
        public bool moved = false;

        public List<int[]> availableMoves = new List<int[]>();

        public Piece(int t, int c, int[] p)
        {
            type = t;
            color = c;
            position = p;
            previousPosition = p;
        }

        public void CheckMoves()
        {
            availableMoves.Clear();

            if (type == 0)
            {
                if (!IsBlocked(new int[] { position[0] + (1 * color), position[1] }).blocked)
                {
                    availableMoves.Add(new int[] { position[0] + (1 * color), position[1] });
                }
                if (((position[0] == 1 & color == 1) | (position[0] == 6 & color == -1)) & !IsBlocked(new int[] { position[0] + (2 * color), position[1] }).blocked & !IsBlocked(new int[] { position[0] + (1 * color), position[1] }).blocked)
                {
                    availableMoves.Add(new int[] { position[0] + (2 * color), position[1] });
                }
                Block b = IsBlocked(new int[] { position[0] + (1 * color), position[1] + 1 });
                if (b.blocked)
                {
                    if (b.piece.color != color) { availableMoves.Add(new int[] { position[0] + (1 * color), position[1] + 1 }); }
                }
                b = IsBlocked(new int[] { position[0] + (1 * color), position[1] - 1 });
                if (b.blocked)
                {
                    if (b.piece.color != color) { availableMoves.Add(new int[] { position[0] + (1 * color), position[1] - 1 }); }
                }
                b = IsBlocked(new int[] { position[0], position[1] + 1 });
                if (b.blocked)
                {
                    if (b.piece.color != color & b.piece.type == 0 & Math.Abs(b.piece.previousPosition[0] - b.piece.position[0]) == 2) { availableMoves.Add(new int[] { position[0] + (1 * color), position[1] + 1 }); }
                }
                b = IsBlocked(new int[] { position[0], position[1] - 1 });
                if (b.blocked)
                {
                    if (b.piece.color != color & b.piece.type == 0 & Math.Abs(b.piece.previousPosition[0] - b.piece.position[0]) == 2) { availableMoves.Add(new int[] { position[0] + (1 * color), position[1] - 1 }); }
                }
            }
            if (type == 1 | type == 4)
            {
                CheckPath(new int[] { 0, 1 });
                CheckPath(new int[] { 0, -1 });
                CheckPath(new int[] { 1, 0 });
                CheckPath(new int[] { -1, 0 });
            }
            if (type == 2)
            {
                CheckKnightMoves();
            }
            if (type == 3 | type == 4)
            {
                CheckPath(new int[] { 1, 1 });
                CheckPath(new int[] { 1, -1 });
                CheckPath(new int[] { -1, 1 });
                CheckPath(new int[] { -1, -1 });
            }
            if (type == 5)
            {
                CheckKingMoves();
            }
        }

        public bool LegalCheck(List<int[]> moves, bool forCastle)
        {
            int[] king;
            List<int[]> newAvailableMoves = new List<int[]>();
            newAvailableMoves.AddRange(availableMoves);
            int[] realPosition = position;
            bool canCastle = true;

            foreach (int[] move in moves)
            {
                if (move[0] >= 0 & move[0] < 8 & move[1] >= 0 & move[1] < 8)
                {
                    List<int[]> realMoves = new List<int[]>();
                    Piece takenPiece = null;
                    int index = 0;

                    if (!forCastle)
                    {
                        takenPiece = Program.pieces.Find(x => x.position[0] == move[0] & x.position[1] == move[1]);
                        index = Program.pieces.IndexOf(takenPiece);
                        if (takenPiece != null) { Program.pieces.Remove(takenPiece); }
                    }

                    position = move;

                    king = Program.pieces.Find(x => x.color == color & x.type == 5).position;

                    foreach (Piece piece in Program.pieces)
                    {
                        bool threatened = false;

                        if (piece.color != color)
                        {
                            realMoves.Clear();
                            realMoves.AddRange(piece.availableMoves);

                            piece.CheckMoves();

                            if (piece.availableMoves.Exists(x => x[0] == king[0] & x[1] == king[1]))
                            {
                                newAvailableMoves.Remove(move);
                                threatened = true;
                            }

                            piece.availableMoves.Clear();
                            piece.availableMoves.AddRange(realMoves);

                            if (threatened) { canCastle = false; break; }
                        }
                    }

                    if (takenPiece != null) { Program.pieces.Insert(index, takenPiece); }
                }
                else { newAvailableMoves.Remove(move); }
            }

            position = realPosition;
            if (!forCastle)
            {
                availableMoves.Clear();
                availableMoves.AddRange(newAvailableMoves);
            }

            return canCastle;
        }

        void CheckPath(int[] d)
        {
            int[] pos = new int[] { position[0] + d[0], position[1] + d[1] };

            while (pos[0] >= 0 & pos[0] < 8 & pos[1] >= 0 & pos[1] < 8)
            {
                Block b = IsBlocked(pos);

                if (b.blocked)
                {
                    if (b.piece.color == color & Program.pieces.FindAll(x => x.position[0] == pos[0] & x.position[1] == pos[1]).Count() == 1) { break; }
                    else
                    {
                        availableMoves.Add(pos);
                        break;
                    }
                }
                else { availableMoves.Add(pos); }

                pos = new int[] { pos[0] + d[0], pos[1] + d[1] };
            }
        }

        void CheckKnightMoves()
        {
            for (int dr = -2; dr < 3; dr++)
            {
                for (int dc = -2; dc < 3; dc++)
                {
                    if (Math.Abs(dr * dc) == 2)
                    {
                        int[] pos = new int[] { position[0] + dr, position[1] + dc };
                        if (pos[0] >= 0 & pos[0] < 8 & pos[1] >= 0 & pos[1] < 8)
                        {
                            Block b = IsBlocked(pos);

                            if (b.blocked)
                            {
                                if (b.piece.color != color) { availableMoves.Add(pos); }
                            }
                            else { availableMoves.Add(pos); }
                        }
                    }
                }
            }
        }

        void CheckKingMoves()
        {
            for (int dr = -1; dr < 2; dr++)
            {
                for (int dc = -1; dc < 2; dc++)
                {
                    if (dr != 0 | dc != 0)
                    {
                        int[] pos = new int[] { position[0] + dr, position[1] + dc };
                        Block b = IsBlocked(pos);

                        if (b.blocked)
                        {
                            if (b.piece.color != color) { availableMoves.Add(pos); }
                        }
                        else { availableMoves.Add(pos); }
                    }
                }
            }
        }

        public void CheckForCastling()
        {
            if (!moved)
            {
                List<int[]> shortCastle = new List<int[]>();
                List<int[]> longCastle = new List<int[]>();
                bool blockedShort = false;
                bool blockedLong = false;

                if (color == 1)
                {
                    if (Program.pieces.Exists(x => x.position[0] == 0 & x.position[1] == 7 & moved == false))
                    {
                        for (int i = 4; i <= 6; i++)
                        {
                            shortCastle.Add(new int[] { 0, i });
                        }
                    }
                    else { blockedShort = true; }

                    if (Program.pieces.Exists(x => x.position[0] == 0 & x.position[1] == 0 & moved == false))
                    {
                        for (int i = 4; i >= 1; i--)
                        {
                            longCastle.Add(new int[] { 0, i });
                        }
                    }
                    else { blockedLong = true; }
                }
                else
                {
                    if (Program.pieces.Exists(x => x.position[0] == 7 & x.position[1] == 7 & moved == false))
                    {
                        for (int i = 4; i <= 6; i++)
                        {
                            shortCastle.Add(new int[] { 7, i });
                        }
                    }
                    else { blockedShort = true; }

                    if (Program.pieces.Exists(x => x.position[0] == 7 & x.position[1] == 0 & moved == false))
                    {
                        for (int i = 4; i >= 1; i--)
                        {
                            longCastle.Add(new int[] { 7, i });
                        }
                    }
                    else { blockedLong = true; }
                }

                if (!blockedShort) { blockedShort = CastleBlock(shortCastle); }
                if (!blockedLong) { blockedLong = CastleBlock(longCastle); }

                if (!blockedShort)
                {
                    if (LegalCheck(shortCastle, true))
                    {
                        if (color == 1)
                        {
                            availableMoves.Add(new int[] { 0, 6 });
                        }
                        else
                        {
                            availableMoves.Add(new int[] { 7, 6 });
                        }
                    }
                }
                if (!blockedLong)
                {
                    longCastle.RemoveAt(longCastle.Count() - 1);

                    if (LegalCheck(longCastle, true))
                    {
                        if (color == 1)
                        {
                            availableMoves.Add(new int[] { 0, 2 });
                        }
                        else
                        {
                            availableMoves.Add(new int[] { 7, 2 });
                        }
                    }
                }
            }



            bool CastleBlock(List<int[]> moves)
            {
                bool block = false;

                foreach (int[] i in moves)
                {
                    if (Program.pieces.Exists(x => x.position[0] == i[0] & x.position[1] == i[1] & (x.color != color | x.type != type)))
                    {
                        block = true;
                        break;
                    }
                }

                return block;
            }
        }

        Block IsBlocked(int[] p)
        {
            if (Program.pieces.Exists(x => x.position[0] == p[0] & x.position[1] == p[1])) { return new Block(true, Program.pieces.Find(x => x.position[0] == p[0] & x.position[1] == p[1])); }
            else { return new Block(false, null); }
        }

        class Block
        {
            public bool blocked { get; set; }
            public Piece piece { get; set; }

            public Block(bool b, Piece p)
            {
                blocked = b;
                piece = p;
            }
        }

        public void Move(int[] p)
        {
            if (type == 0 & ((color == 1 & p[0] == 5) | (color == -1 & p[0] == 2)) & p[1] != position[1] & !Program.pieces.Exists(x => x.position[0] == p[0] & x.position[1] == p[1]))
            {
                if (color == 1) { Program.pieces.Remove(Program.pieces.Find(x => x.position[0] == 4 & x.position[1] == p[1])); }
                else { Program.pieces.Remove(Program.pieces.Find(x => x.position[0] == 3 & x.position[1] == p[1])); }
            }
            Program.pieces.Remove(Program.pieces.Find(x => x.position[0] == p[0] & x.position[1] == p[1]));
            if (type == 5 & moved == false)
            {
                if (p[1] == 6)
                {
                    if (color == 1) { Program.pieces.Find(x => x.position[0] == 0 & x.position[1] == 7).Move(new int[] { 0, 5 }); }
                    else { Program.pieces.Find(x => x.position[0] == 7 & x.position[1] == 7).Move(new int[] { 7, 5 }); }
                }
                else if (p[1] == 2)
                {
                    if (color ==1) { Program.pieces.Find(x => x.position[0] == 0 & x.position[1] == 0).Move(new int[] { 0, 3 }); }
                    else { Program.pieces.Find(x => x.position[0] == 7 & x.position[1] == 0).Move(new int[] { 7, 3 }); }
                }
            }
            previousPosition = position;
            position = new int[] { p[0], p[1] };
            if (type == 0 & (position[0] == 0 | position[0] == 7)) { type = 4; }
            moved = true;
        }
    }
}
