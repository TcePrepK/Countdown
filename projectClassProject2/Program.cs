using System.Buffers.Text;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using static Program.Program;

namespace Program
{
    class Program
    {
        // General Variables
        public static Random rng = new Random();
        
        // Visual Variables
        public static int leftPadding = 2;
        public static int topPadding = 1; // 7
        public static int statsLeftPadding = 1;
        public static int statsTopPadding = 1;
        public static bool isFancy = true;

        // Game Variables
        public struct Player
        {
            public int x;
            public int y;
            public int life;
            public int score;
        }

        public struct ScoreNumber
        {
            public int x;
            public int y;
            public int val;
        }

        public static bool gameIsRunning = true;
        public static bool endScreenRunning = false;

        // Board Variables
        public static int gridWidth = 53;
        public static int gridHeight = 23;
        public static int numberAmount = 70;
        public static char[,] grid = new char[gridHeight, gridWidth];
        public static ScoreNumber[] numbers = new ScoreNumber[numberAmount];
        public static Player player;

        // Game Timers
        public static long playerTimer = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
        public static long enemyTimer = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
        public static long frameTimer = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
        public static long numberDecreaseTimer = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.ResetColor();
            if (isFancy)
            {
                Console.OutputEncoding = System.Text.Encoding.Unicode;
                Console.BackgroundColor = ConsoleColor.DarkGray;
            }
            Console.Clear();
            Console.BufferHeight += 1;

            startTheGame();
            mainGameLoop();
        }

        static void startTheGame()
        {
            // Set Every Space To Empty
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    setGrid(i, j, ' ');
                }
            }

            // Put Outer Walls
            for (int i = 0; i < gridWidth; i++)
            {
                setGrid(i, 0, '#');
                setGrid(i, gridHeight - 1, '#');
            }
            for (int i = 0; i < gridHeight; i++)
            {
                setGrid(0, i, '#');
                setGrid(gridWidth - 1, i, '#');
            }

            // Put Inner Walls
            int totalWalls = 0;
            while (totalWalls < 28)
            {
                int w = 3;
                int h = 1;
                if (totalWalls < 3)
                {
                    w = 11;
                }
                else if (totalWalls < 8)
                {
                    w = 7;
                }

                if (rng.Next(0, 2) == 0)
                {
                    int t = w;
                    w = h;
                    h = t;
                }

                int x = rng.Next(2 + w / 2, gridWidth - (2 + w / 2));
                int y = rng.Next(2 + h / 2, gridHeight - (2 + h / 2));

                bool possibleLocation = true;
                for (int i = -w / 2 - 1; (i <= w / 2 + 1 && possibleLocation); i++)
                {
                    for (int j = -h / 2 - 1; (j <= h / 2 + 1 && possibleLocation); j++)
                    {
                        possibleLocation = isEmpty(x + i, y + j);
                    }
                }

                if (possibleLocation)
                {
                    for (int i = -w / 2; i <= w / 2; i++)
                    {
                        for (int j = -h / 2; j <= h / 2; j++)
                        {
                            setGrid(x + i, y + j, '#');
                        }
                    }
                    totalWalls++;
                }
            }

            // Put Numbers
            for (int i = 0; i < numberAmount; i++)
            {
                // Find an empty space for the number.
                int x;
                int y;
                bool empty;
                do
                {
                    x = rng.Next(0, gridWidth);
                    y = rng.Next(0, gridHeight);
                    empty = isEmpty(x, y);
                } while (!empty);

                // Initialize the number.
                int value = rng.Next(0, 10);
                numbers[i].x = x;
                numbers[i].y = y;
                numbers[i].val = value;
                setGrid(x, y, Convert.ToChar('0' + value));
            }

            // Finally, Put The Player
            player.life = 5;
            player.score = 0;
            do
            {
                player.x = rng.Next(1, gridWidth - 1);
                player.y = rng.Next(1, gridHeight - 1);
            }
            while (!isEmpty(player.x, player.y));
            setGrid(player.x, player.y, 'P');
        }

        public static void mainGameLoop()
        {
            while (gameIsRunning)
            {
                long currentTime = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;

                // Every 50ms
                if (currentTime - playerTimer >= 50)
                {
                    // Player Move
                    if (Console.KeyAvailable)
                    {
                        string key = Console.ReadKey(true).Key.ToString();
                        switch (key)
                        {
                            case "W":
                                tryToMove(0, -1);
                                break;
                            case "A":
                                tryToMove(-1, 0);
                                break;
                            case "S":
                                tryToMove(0, 1);
                                break;
                            case "D":
                                tryToMove(1, 0);
                                break;
                        }
                    }

                    playerTimer = currentTime;
                }

                // Every 15000ms (15 sec)
                if (currentTime - numberDecreaseTimer >= 15000)
                {
                    // Number decrease
                    for (int i = 0; i < numberAmount; i++)
                    {
                        int x = numbers[i].x;
                        int y = numbers[i].y;
                        int val = numbers[i].val;

                        if (val > 1)
                        {
                            numbers[i].val--; // (9-2) to (8-1)
                        }
                        else if (val == 1)
                        {
                            if (rng.Next(0, 100) < 3)
                            {
                                numbers[i].val--; // 1 to 0
                            }
                        }

                        setGrid(x, y, Convert.ToChar(val + '0'));
                    }

                    numberDecreaseTimer = currentTime;
                }

                // Every 1000ms (1 sec)
                if (currentTime - enemyTimer >= 1000)
                {
                    // Enemy Move
                    for (int i = 0; i < numberAmount; i++)
                    {
                        if (numbers[i].val == 0)
                        {
                            int x = numbers[i].x;
                            int y = numbers[i].y;

                            bool[] possibleSides = new bool[4];
                            possibleSides[0] = isEmpty(x + 1, y) || isPlayer(x + 1, y);
                            possibleSides[1] = isEmpty(x, y + 1) || isPlayer(x, y + 1);
                            possibleSides[2] = isEmpty(x - 1, y) || isPlayer(x - 1, y);
                            possibleSides[3] = isEmpty(x, y - 1) || isPlayer(x, y - 1);

                            if (possibleSides[0] || possibleSides[1] || possibleSides[2] || possibleSides[3])
                            {
                                int randomSide = -1;
                                while (randomSide == -1)
                                {
                                    int side = rng.Next(0, 4);
                                    if (possibleSides[side])
                                    {
                                        randomSide = side;
                                    }
                                }

                                setGrid(x, y, ' ');
                                switch (randomSide)
                                {
                                    case 0:
                                        x += 1;
                                        break;
                                    case 1:
                                        y += 1;
                                        break;
                                    case 2:
                                        x -= 1;
                                        break;
                                    case 3:
                                        y -= 1;
                                        break;
                                }

                                if (isPlayer(x, y))
                                {
                                    // 0 Moved over P
                                    player.life--;
                                    Console.Beep();

                                    if (player.life == 0)
                                    {
                                        gameIsRunning = false;
                                        endScreenRunning = true;
                                    }
                                    else
                                    {
                                        do
                                        {
                                            player.x = rng.Next(1, gridWidth - 1);
                                            player.y = rng.Next(1, gridHeight - 1);
                                        }
                                        while (!isEmpty(player.x, player.y));
                                        setGrid(player.x, player.y, 'P');
                                    }
                                }

                                setGrid(x, y, '0');
                                numbers[i].x = x;
                                numbers[i].y = y;
                            }
                        }
                    }
                    enemyTimer = currentTime;
                }

                long deltaTime = currentTime - playerTimer;
                frameTimer = currentTime;
            }

            // while (!endScreenRunning)
            // {

            // }
        }

        // Main Write Function
        public static void writeAt(int x, int y, char s)
        {
            if (isFancy)
            {
                string lastString = Convert.ToString(s);
                switch (s)
                {
                    case ' ':
                        lastString = "  ";
                        break;
                    case 'P':
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                        lastString = "\uFF20";
                        break;
                    case '#':
                        Console.ForegroundColor = ConsoleColor.Gray;
                        lastString = "■";
                        break;
                    default:
                        int num = s - '0';
                        if (num < 5 && num != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                        }
                        else if (num != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                        }
                        lastString = Convert.ToString(Convert.ToChar(0xFF10 + num));
                        break;
                }

                Console.SetCursorPosition(2 * x + leftPadding, y + topPadding);
                Console.Write(lastString);
                Console.ResetColor();
                Console.BackgroundColor = ConsoleColor.DarkGray;
            }
            else
            {
                Console.SetCursorPosition(x, y);
                Console.Write(s);
            }
        }

        // Misc Functions
        public static bool isEmpty(int x, int y)
        {
            return grid[y, x] == ' ';
        }

        public static bool isWall(int x, int y)
        {
            return grid[y, x] == '#';
        }

        public static bool isPlayer(int x, int y)
        {
            return grid[y, x] == 'P';
        }

        public static bool isNumber(int x, int y)
        {
            return !isEmpty(x, y) && !isWall(x, y) && !isPlayer(x, y); 
        }

        public static void setGrid(int x, int y, char val)
        {
            grid[y, x] = val;
            writeAt(x, y, val);
        }

        public static void tryToMove(int dx, int dy)
        {
            int targetX = player.x + dx;
            int targetY = player.y + dy;
            if (isNumber(targetX, targetY))
            {
                // It's a number, try to push.
                bool moved = push(targetX, targetY, dx, dy, 10);
                if (moved)
                {
                    setGrid(player.x, player.y, ' ');
                    setGrid(targetX, targetY, 'P');
                    player.x = targetX;
                    player.y = targetY;
                }
            } else if (isEmpty(targetX, targetY))
            {
                setGrid(player.x, player.y, ' ');
                setGrid(targetX, targetY, 'P');
                player.x = targetX;
                player.y = targetY;
            }
        }

        public static bool push(int baseX, int baseY, int dx, int dy, int prevVal)
        {
            char currentChar = grid[baseY, baseX];
            int targetX = baseX + dx;
            int targetY = baseY + dy;

            if (isNumber(targetX, targetY))
            {
                // Next tile is a number, push.
                int currentVal = currentChar - '0';
                int targetVal = grid[targetY, targetX] - '0';
                if (currentVal >= targetVal)
                {
                    // Either the first number to get pushed or the small one.
                    bool moved = push(targetX, targetY, dx, dy, currentVal);
                    if (moved)
                    {
                        for (int i = 0; i < numberAmount; i++)
                        {
                            if (numbers[i].x == baseX && numbers[i].y == baseY)
                            {
                                numbers[i].x = targetX;
                                numbers[i].y = targetY;
                                break;
                            }
                        }
                        setGrid(baseX, baseY, ' '); // Set current position empty.
                        setGrid(targetX, targetY, currentChar); // Set next position current value.

                        return true;
                    } else
                    {
                        return false;
                    }
                } else
                {
                    return false;
                }
            } else if (isEmpty(targetX, targetY))
            {
                // Next tile is empty, move.
                for (int i = 0; i < numberAmount; i++)
                {
                    if (numbers[i].x == baseX && numbers[i].y == baseY)
                    {
                        numbers[i].x = targetX;
                        numbers[i].y = targetY;
                        break;
                    }
                }
                setGrid(baseX, baseY, ' '); // Set current position empty.
                setGrid(targetX, targetY, currentChar); // Set next position current value.

                return true;
            } else
            {
                // Next tile is a wall, crush.
                if (prevVal != 10)
                {
                    // Not the first number to get pushed.
                    crushNumber(baseX, baseY);
                    return true;
                } else
                {
                    // First number to get pushed.
                    return false;
                }
            }
        }

        public static void crushNumber(int x, int y)
        {
            for (int i = 0; i < numberAmount; i++)
            {
                // Find the number from numbers list.
                if (numbers[i].x == x && numbers[i].y == y)
                {
                    do
                    {
                        numbers[i].x = rng.Next(1, gridWidth - 1);
                        numbers[i].y = rng.Next(1, gridHeight - 1);
                    }
                    while (!isEmpty(numbers[i].x, numbers[i].y));

                    int crushedValue = numbers[i].val;
                    numbers[i].val = rng.Next(5, 10);

                    setGrid(x, y, ' ');
                    setGrid(numbers[i].x, numbers[i].y, Convert.ToChar(numbers[i].val + '0'));
                    
                    // Give score to the player depending on the number.
                    if (crushedValue == 0)
                    {
                        player.score += 20;
                    } else if (crushedValue < 5)
                    {
                        player.score += 2;
                    } else
                    {
                        player.score += 1;
                    }
                    
                    break;
                }
            }
        }
    }
}