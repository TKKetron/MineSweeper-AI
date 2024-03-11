using System.Diagnostics;

namespace NueralMinesweeper
{
    public class Minefield : IComparable // Class to keep track of edges with consolidated methods
    {
        private class Tile
        {
            public bool isMine = false;
            public bool isCovered = true;
            public bool isFlagged = false;
            public int adjMineCnt = 0;
            public double Row, Col, Index;
            public Tile(int row, int col, int index)
            {
                Row = row;
                Col = col;
                Index = index;
            }
            public Tile(int newTileVal, int row, int col, int index, bool isMine) // If tileVal is known ahead of time
            {
                adjMineCnt = newTileVal;
                Row = row;
                Col = col;
                Index = index;
                this.isMine = isMine;
            }

            public override string ToString() => $"({Row}, {Col})";
        }

        public readonly int width, height;
        public readonly int fieldSize;    // Width * Height of field
        public readonly int mineCount;    // How many mines are in field
        public readonly int boomCount;   // How many mines have been hit
        private static int notYourId = 0;
        public readonly int Id;

        private bool gameStarted;
        private bool gameFinished;
        private int[] bombIndex;
        private bool multiLife;
        private int bombCount;
        private int repeatTiles;
        private int goodHits;
        private int uncovered; // How many tiles have been uncovered
        private int moveCount;  // How many moves have been made on field
        private int lives;

        private List<Tile> field = new();
        private static Random rng = new Random();

        private int[] layers;
        private NeuralNetwork net;
       
        public void Import(string filePath)
        {
            net.Import(filePath);
        }
        public void Export(string filePath)
        {
            net.Export(filePath);
        }
        public int getNetLayerSize(int index)
        {
            return layers[index];
        }
        public int GetAdjCount(int index)
        {
            if (field[index].isMine)
                return -1;
            return field[index].adjMineCnt;
        }

        /*public Minefield(int newWidth, int newHeight, List<int> Minefield) // Input a pre-generated minefield
        {
            width = newWidth; height = newHeight;
            foreach (int val in Minefield)
            {
                int index = field.Count;
                int x = index % width;
                int y = index / height;
                field.Add(new(val, x, y, index, Minefield[index] == -1 ? true:false));
                if(val == -1){mineCount++; }
            }
            fieldSize = field.Count;
        }*/

        public Minefield(int newWidth, int newHeight, int mineCount,int mutationChance, int mutationSevarity, bool multiLife = false) // Create minefield with dimensions and minecount
        {
            width = newWidth;
            height = newHeight;
            fieldSize = newWidth * newHeight;
            for (int i = 0; i < width * height; i++)
            {
                int x = field.Count % width;
                int y = field.Count / height;
                field.Add(new Tile(x, y, getIndex(x, y)));
            }
            if (fieldSize != field.Count)
            {
                throw new Exception("Count != fieldSize");
            }
            this.mineCount = mineCount;
            bombIndex = new int[mineCount];
            this.multiLife = multiLife;
            layers = new int[] { fieldSize,  50,50, fieldSize };
            net = new(layers, mutationChance, mutationSevarity);
            gameFinished = false;
            bombCount = 0;
            gameStarted = false;
            uncovered = 0;
            repeatTiles = 0;
            moveCount = 0;
            notYourId++;
            Id = notYourId;
            lives = 5;
        }

        public Minefield(int newWidth, int newHeight, int mineCount, int mutationChance, int mutationSevarity, NeuralNetwork Net, bool multiLife = false) // Create minefield with dimensions and minecount
        {
            width = newWidth;
            height = newHeight;
            fieldSize = newWidth * newHeight;
            for (int i = 0; i < width * height; i++)
            {
                int x = field.Count % width;
                int y = field.Count / height;
                field.Add(new Tile(x, y, getIndex(x, y)));
            }
            if (fieldSize != field.Count)
            {
                throw new Exception("Count != fieldSize");
            }
            this.mineCount = mineCount;
            bombIndex = new int[mineCount];
            this.multiLife = multiLife;
            net = new(Net, mutationChance, mutationSevarity);
            gameFinished = false;
            bombCount = 0;
            gameStarted = false;
            uncovered = 0;
            repeatTiles = 0;
            moveCount = 0;
            notYourId++;
            Id = notYourId;
            lives = 5;
        }

        internal void Reset()
        {
            lives = 5;
            uncovered = 0;
            repeatTiles = 0;
            moveCount = 0;
            gameStarted = false;
            gameFinished = false;
            bombCount = 0;
            bombIndex = new int[mineCount];
            field.Clear();
            for (int i = 0; i < width * height; i++)
            {
                int x = field.Count % width;
                int y = field.Count / height;
                field.Add(new Tile(x, y, getIndex(x, y)));
            }
        }
        /// <summary>
        /// Input index to uncover mine
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <returns></returns>
        public bool makeMove(int tileIndex)
        {
            if (gameFinished)
                return false;
            if (gameStarted)
            {
                if (!field[tileIndex].isCovered || field[tileIndex].isFlagged) // Already uncovered?
                    return false;


                field[tileIndex].isCovered = false;
                //setTileValDel[tileIndex].DynamicInvoke(GetAdjCount(tileIndex));
                if (field[tileIndex].isMine)
                {
                    bombCount++;
                    if (!multiLife)
                        for (int i = 0; i < mineCount; i++)
                        {
                            //if (bombIndex[i] != tileIndex)
                            field[bombIndex[i]].isCovered = false;
                            gameFinished = true;
                            //setTileValDel[bombIndex[i]].DynamicInvoke(GetAdjCount(bombIndex[i]));
                            //return true;
                        }
                    if (bombCount == mineCount)
                        gameFinished = true;
                    return true;
                }
                if (field[tileIndex].adjMineCnt == 0 && !field[tileIndex].isMine)
                {
                    (int, int) RowCol = getRowCol(tileIndex);
                    for (int j = -1; j <= 1; j++)
                        for (int k = -1; k <= 1; k++)
                            if (j != 0 || k != 0)
                                tryClearing(RowCol.Item1 + j, RowCol.Item2 + k); //all
                }
                uncovered++;
                if (uncovered >= fieldSize - mineCount)
                {
                    gameFinished = true;
                }
                return true;

            }
            else // Need to generate mines
            {
                for (int i = 0; i < mineCount; i++)
                {
                    int randMineIndex = rng.Next(fieldSize);
                    while (field[randMineIndex].isMine == true // Keep searching for a tile that isnt already a mine
                        || randMineIndex == tileIndex) // Ensure the first move isn't a mine
                    {
                        randMineIndex = rng.Next(fieldSize);
                    }
                    field[randMineIndex].isMine = true;
                    bombIndex[i] = randMineIndex;
                    (int, int) RowCol = getRowCol(randMineIndex);

                    for (int j = -1; j <= 1; j++)
                        for (int k = -1; k <= 1; k++)
                            if (j != 0 || k != 0)
                                tryIncrement(RowCol.Item1 + j, RowCol.Item2 + k);     // all
                }
                gameStarted = true;
                field[tileIndex].isCovered = false;
                if (field[tileIndex].adjMineCnt == 0)
                {
                    (int, int) RowCol = getRowCol(tileIndex);
                    for (int j = -1; j <= 1; j++)
                        for (int k = -1; k <= 1; k++)
                            if (j != 0 || k != 0)
                                tryClearing(RowCol.Item1 + j, RowCol.Item2 + k); //all
                }
                //setTileValDel[tileIndex].DynamicInvoke(GetAdjCount(tileIndex));
                uncovered++;

                return true;
            }
        }

        public bool IterateNet()
        {
            
            var x = net.FeedForward(GetFeildF()).ToList();
            int index = x.IndexOf(x.Max());
            moveCount++;
            while (!makeMove(index))
            {
                if (gameFinished)
                {
                    if (GetFitness() == 0)
                    {
                        Debug.WriteLine("things broke");
                    }
                    return false;
                }
                x[index] = float.NegativeInfinity;
                index = x.IndexOf(x.Max());
                lives--;
                if (lives == 0)
                {
                    gameFinished = true;
                    return true;
                }
                net.AddFitness(-5);
                repeatTiles++;
                moveCount++;
            }
            if (field[index].isMine)
            {
                net.AddFitness(-100);
            }
            else
            {
                net.AddFitness(10);
                goodHits++;
            }
            return true;
        }
        public float GetFitness()
        {
            return net.GetFitness();
        }
        public void CompleteGame()
        {
            while (IterateNet()) ;
        }
        public int GetBombsHit()
        {
            return bombCount;
        }
        public int getRepeatTiles()
        {
            return repeatTiles;
        }
        public int getGoodHits()
        {
            return goodHits;
        }
        public int getUncovered()
        {
            return uncovered;
        }
        public int getMoveCnt()
        {
            return moveCount;
        }
        public void Mutate()
        {
            net.Mutate();
        }
        public void Cross(NeuralNetwork parent)
        {
            net.Breed(parent);
        }
        public void WoC(Minefield[] parents)
        {
            List<NeuralNetwork> nodes = new List<NeuralNetwork>();
            foreach (Minefield m in parents)
                nodes.Add(m.GetNet());
            net.WoC(nodes.ToArray());
        }
        private void tryClearing(int Row, int Col)
        {
            if ((Row <= height && Col <= height && Row >= 0 && Col >= 0) && (getIndex(Row, Col) >= 0 && getIndex(Row, Col) < fieldSize && !field[getIndex(Row, Col)].isMine))
                makeMove(getIndex(Row, Col));
        }
        private void tryIncrement(int Row, int Col)
        {
            if ((Row <= height && Col <= height && Row >= 0 && Col >= 0) && (getIndex(Row, Col) >= 0 && getIndex(Row, Col) < fieldSize && !field[getIndex(Row, Col)].isMine))
                field[getIndex(Row, Col)].adjMineCnt++;
        }

        public int CompareTo(object? obj)
        {
            if (obj is Minefield T)
            {
                if (GetFitness() < T.GetFitness())
                    return 1;
                else if (GetFitness() > T.GetFitness())
                    return -1;
                else
                    return 0;
            }
            return 1;
        }
        public int[] GetFeild()
        {
            List<int> arr = new();
            foreach (Tile t in field)
            {
                if (t.isCovered)
                    arr.Add(-2);
                else if (t.isMine)
                    arr.Add(-1);
                else
                    arr.Add(t.adjMineCnt);
            }
            return arr.ToArray();
        }

        public float[] GetFeildF()
        {
            List<float> arr = new();
            foreach (Tile t in field)
            {
                if (t.isCovered)
                    arr.Add(1);
                else if (t.isMine)
                    arr.Add(.9f);
                else
                    arr.Add(t.adjMineCnt/10.0f);
            }
            return arr.ToArray();
        }

        public bool toggleTileFlag(int tileIndex) => (field[tileIndex].isCovered) ? field[tileIndex].isFlagged = !field[tileIndex].isFlagged : false;

        public double RatioUncovered() => uncovered / field.Count;


        public (int, int) getRowCol(int index) => (index / width, index % width);
        public int getIndex(int row, int col) => (row > height - 1 || col > width - 1 || row < 0 || col < 0 || row * col >= fieldSize) ? -1 : row * width + col;

        internal NeuralNetwork GetNet()
        {
            return net;
        }
    }

}