// http://mech.math.msu.su/~shvetz/54/inf/perl-examples/PerlExamples_Tetris.xhtml
// https://habr.com/ru/articles/466579/ - История алгоритмов рандомизации «Тетриса»

using System.Threading;

internal class Tetris
{
    #region Data

    static bool gameStarted = false;

    // Размер стакана
    const byte GlassX = 10;
    const byte GlassY = 20;
    static char[,] Glass = new char[GlassY, GlassX];

    // Позиция тетрамино
    static int CurrentX = 0;
    static int CurrentY = 0;

    // Мешок | Bag
    static byte BagIndex = 0;
    static int[]? bag;
    static int[]? nextBag;

    static int CurrentIndex = 0;
    static int direction = 0;

    const byte upNextSize = 6;

    // Фигуры

    readonly static string figureChars = "OILJSZT";

    // 7 - Фигуры
    // 4 - Повороты
    // 4 - Координаты клеток
    // 2 - {y, x} клеток
    readonly static int[,,,] figuresPos = { 
        {
            { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 } },
            { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 } },
            { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 } },
            { { 0, 0 }, { 1, 0 }, { 0, 1 }, { 1, 1 } }
        }, { 
            { { 2, 0 }, { 2, 1 }, { 2, 2 }, { 2, 3 } },
            { { 0, 2 }, { 1, 2 }, { 2, 2 }, { 3, 2 } },
            { { 1, 0 }, { 1, 1 }, { 1, 2 }, { 1, 3 } },
            { { 0, 1 }, { 1, 1 }, { 2, 1 }, { 3, 1 } }
        }, {
            { { 1, 0 }, { 1, 1 }, { 1, 2 }, { 2, 2 } },
            { { 1, 2 }, { 1, 1 }, { 2, 1 }, { 3, 1 } },
            { { 1, 1 }, { 2, 1 }, { 2, 2 }, { 2, 3 } },
            { { 2, 1 }, { 2, 2 }, { 1, 2 }, { 0, 2 } } 
        }, {
            { { 2, 0 }, { 2, 1 }, { 2, 2 }, { 1, 2 } },
            { { 1, 1 }, { 1, 2 }, { 2, 2 }, { 3, 2 } },
            { { 2, 1 }, { 1, 1 }, { 1, 2 }, { 1, 3 } },
            { { 0, 1 }, { 1, 1 }, { 2, 1 }, { 2, 2 } } 
        }, {
            { { 2, 1 }, { 1, 1 }, { 1, 2 }, { 0, 2 } },
            { { 1, 0 }, { 1, 1 }, { 2, 1 }, { 2, 2 } },
            { { 2, 1 }, { 1, 1 }, { 1, 2 }, { 0, 2 } },
            { { 1, 0 }, { 1, 1 }, { 2, 1 }, { 2, 2 } } 
        }, {
            { { 0, 1 }, { 1, 1 }, { 1, 2 }, { 2, 2 } },
            { { 1, 0 }, { 1, 1 }, { 0, 1 }, { 0, 2 } },
            { { 0, 1 }, { 1, 1 }, { 1, 2 }, { 2, 2 } },
            { { 1, 0 }, { 1, 1 }, { 0, 1 }, { 0, 2 } } 
        }, {
            { { 0, 1 }, { 1, 1 }, { 1, 0 }, { 2, 1 } },
            { { 1, 0 }, { 1, 1 }, { 2, 1 }, { 1, 2 } },
            { { 0, 1 }, { 1, 1 }, { 1, 2 }, { 2, 1 } },
            { { 1, 0 }, { 1, 1 }, { 0, 1 }, { 1, 2 } } 
        }
    };

    static char CurrentFig = 'O';

    readonly static private Dictionary<char, ConsoleColor> figureColors = new Dictionary<char, ConsoleColor>()
    {
        { 'I', ConsoleColor.Blue },
        { 'J', ConsoleColor.Yellow },
        { 'L', ConsoleColor.Gray },
        { 'O', ConsoleColor.Green },
        { 'S', ConsoleColor.Magenta },
        { 'T', ConsoleColor.Red },
        { 'Z', ConsoleColor.Cyan }
    };
    static ConsoleColor color;

    // Счет / уровень / статистика
    static int score = 0;
    static int level = 1;
    static int lineAmount = 0;
    static int amount = 0;
    static int scoreForLevel = 100;

    // Таймер подения фигуры
    static double maxTime = 20;
    static double time = 0;

    static ConsoleKeyInfo key;
    #endregion

    public static void Menu()
    {
        Console.WriteLine("1. Играть" +
            "\n2. Правила" +
            "\n3. Выход");
        bool menu = true;

        while (true)
        {
            switch (key.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    if (menu)
                    {
                        gameStarted = true;
                        return;
                    }
                    break;

                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    if (menu)
                    {
                        key = new();
                        Console.Clear();
                        Console.WriteLine("Случайные фигурки тетрамино падают сверху в стакан 10x20 клеток.\n" +
                            "В полёте игрок может поворачивать фигурку на 90° и двигать её по горизонтали.\n" +
                            "Также можно «сбрасывать» фигурку, то есть ускорять её падение, когда уже решено, куда фигурка должна упасть.\n" +
                            "Фигурка летит до тех пор, пока не наткнётся на другую фигурку либо на дно стакана.\n" +
                            "Если при этом заполнился горизонтальный ряд из 10 клеток, он пропадает и всё, что выше него, опускается на одну клетку.\n" +
                            "Дополнительно показывается фигурка, которая будет следовать после текущей — это подсказка, которая позволяет игроку планировать действия.\n" +
                            "Темп игры постепенно ускоряется. Игра заканчивается, когда новая фигурка не может поместиться в стакан.\n" +
                            "Игрок получает очки за каждый заполненный ряд, поэтому его задача — заполнять ряды, не заполняя сам стакан (по вертикали) как можно дольше, чтобы таким образом получить как можно больше очков.\n");
                        Console.WriteLine("\nНажмите 0, чтобы вернуться");
                        menu = false;
                    }
                    break;

                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    if (menu) Environment.Exit(0);
                    break;

                case ConsoleKey.D0:
                case ConsoleKey.NumPad0:
                    menu = true;
                    key = new();
                    Console.Clear();
                    Console.WriteLine("1. Играть" +
                    "\n2. Правила" +
                    "\n3. Выход");
                    break;
            }
        }
        
    }

    static void Main()
    {
        Console.Title = "Tetris";
        Console.CursorVisible = false;

        Thread inputThread = new Thread(InputHandler);

        for (int y = 0; y < GlassY; y++)
            for (int x = 0; x < GlassX; x++)
                Glass[y, x] = '-';

        if (!gameStarted)
        {
            inputThread.Start();
            Menu();
        }

        Console.Clear();
        Console.SetWindowSize(50, 21);
        

        bag = GenerateBag();
        nextBag = GenerateBag();
        NewFigure();
        Console.CursorVisible = false;
        while (true)
        {
            CheckScore(score);
            if (time >= maxTime)
            {
                if (!Collision(CurrentIndex, Glass, CurrentX, CurrentY + 1, direction)) CurrentY++;
                else BlockDownColl();

                time = 0;
            }
            time++;

            Input();
            key = new ConsoleKeyInfo();

            char[,] mapView = GlassView();

            char[,] nextView = NextView();

            Print(mapView, nextView);

            Thread.Sleep(20);
        }

    }

    public static void Input()
    {
        switch (key.Key)
        {
            case ConsoleKey.A:
            case ConsoleKey.LeftArrow:
                if (!Collision(CurrentIndex, Glass, CurrentX - 1, CurrentY, direction)) CurrentX--;
                break;
            case ConsoleKey.D:
            case ConsoleKey.RightArrow:
                if (!Collision(CurrentIndex, Glass, CurrentX + 1, CurrentY, direction)) CurrentX++;
                break;
            case ConsoleKey.W:
            case ConsoleKey.UpArrow:
                int newDir = direction + 1;
                if (newDir >= 4) newDir = 0;
                if (Collision(CurrentIndex, Glass, CurrentX - 1, CurrentY, direction) && !Collision(CurrentIndex, Glass, CurrentX, CurrentY, direction) && !Collision(CurrentIndex, Glass, CurrentX + 2, CurrentY, direction) && Collision(CurrentIndex, Glass, CurrentX, CurrentY, newDir) && CurrentIndex != 1) CurrentX++;
                if (Collision(CurrentIndex, Glass, CurrentX + 1, CurrentY, direction) && !Collision(CurrentIndex, Glass, CurrentX, CurrentY, direction) && !Collision(CurrentIndex, Glass, CurrentX - 1, CurrentY, direction) && Collision(CurrentIndex, Glass, CurrentX, CurrentY, newDir) && CurrentIndex != 1) CurrentX--;
                if (!Collision(CurrentIndex, Glass, CurrentX, CurrentY, newDir)) direction = newDir;
                break;
            case ConsoleKey.S:
            case ConsoleKey.DownArrow:
                time = maxTime;
                score++;
                break;
            case ConsoleKey.Spacebar:
                int i = 0;
                while (true)
                {
                    i++;
                    if (Collision(CurrentIndex, Glass, CurrentX, CurrentY + i, direction))
                    {
                        CurrentY += i - 1;
                        break;
                    }
                    score++;
                }
                time = maxTime;
                break;
        }
    }

    public static bool Collision(int index, char[,] map, int x, int y, int rot)
    {

        for (int i = 0; i < figuresPos.GetLength(2); i++)
        {
            if (figuresPos[index, rot, i, 1] + y >= GlassY || figuresPos[index, rot, i, 0] + x < 0 || figuresPos[index, rot, i, 0] + x >= GlassX)
            {
                return true;
            }
            if (map[figuresPos[index, rot, i, 1] + y, figuresPos[index, rot, i, 0] + x] != '-')
            {
                return true;
            }
        }

        return false;
    }

    public static void BlockDownColl()
    {
        for (int i = 0; i < figuresPos.GetLength(2); i++)
        {
            Glass[figuresPos[CurrentIndex, direction, i, 1] + CurrentY, figuresPos[CurrentIndex, direction, i, 0] + CurrentX] = CurrentFig;
        }

        while (true)
        {
            int lineY = Line(Glass);
            if (lineY != -1)
            {
                ClearLine(lineY);

                continue;
            }
            break;
        }
        NewFigure();

    }

    public static int Line(char[,] Glass)
    {
        for (int i = 0; i < GlassY; i++)
        {
            bool temp = true;
            for (int j = 0; j < GlassX; j++)
            {
                if (Glass[i, j] == '-')
                {
                    temp = false;
                }
            }
            if (temp) return i;
        }

        return -1;
    }

    public static void CheckScore(int score)
    {
        if (maxTime > 0 && score >= scoreForLevel) maxTime -= 0.5;
        if (score >= scoreForLevel)
        {
            scoreForLevel += 100;
            level++;
        }
    }

    public static void ClearLine(int LineY)
    {
        score += 10; lineAmount++;

        for (int i = 0; i < GlassX; i++) Glass[LineY, i] = '-';

        for (int i = LineY - 1; i > 0; i--)
        {
            for (int j = 0; j < GlassX; j++)
            {
                char chr = Glass[i, j];
                if (chr != '-')
                {
                    Glass[i, j] = '-';
                    Glass[i + 1, j] = chr;
                }
            }
        }   
    }

    public static char[,] GlassView()
    {
        char[,] view = new char[GlassY, GlassX];

        for (int y = 0; y < GlassY; y++)
            for (int x = 0; x < GlassX; x++)
                view[y, x] = Glass[y, x];


        for (int i = 0; i < figuresPos.GetLength(2); i++)
            view[figuresPos[CurrentIndex, direction, i, 1] + CurrentY, figuresPos[CurrentIndex, direction, i, 0] + CurrentX] = CurrentFig;

        return view;

    }

    public static char[,] NextView()
    { 
        char[,] next = new char[GlassY, upNextSize];
        for (int y = 0; y < GlassY; y++)
            for (int x = 0; x < upNextSize; x++)
                next[y, x] = ' ';


        int nextBagIndex = 0;
        for (int i = 0; i < 1; i++)
        {

            for (int j = 0; j < figuresPos.GetLength(2); j++)
            {
                if (BagIndex >= 7)
                    next[figuresPos[nextBag[nextBagIndex], 0, j, 1] + 3, figuresPos[nextBag[nextBagIndex], 0, j, 0] + 2] = figureChars[nextBag[nextBagIndex]];
                else
                    next[figuresPos[bag[BagIndex + i], 0, j, 1] + 3, figuresPos[bag[BagIndex + i], 0, j, 0] + 2] = figureChars[bag[BagIndex + i]];


            }
            if (BagIndex >= 7) nextBagIndex++;
        }
        return next;

    }

    public static void NewFigure()
    {
        if (BagIndex >= 7)
        {
            BagIndex = 0;
            bag = nextBag;
            nextBag = GenerateBag();
        }

        CurrentY = 0;
        CurrentX = 4;
        CurrentFig = figureChars[bag[BagIndex]];
        CurrentIndex = bag[BagIndex];

        if (Collision(CurrentIndex, Glass, CurrentX, CurrentY, direction) && amount > 0) End();

        BagIndex++; amount++;
    }

    public static void Print(char[,] map, char[,] next)
    {
        for (int i = 0; i < GlassY; i++)
        {

            for (int j = 0; j < GlassX + upNextSize; j++)
            {
                char temp = ' ';
                if (j >= GlassX) temp = next[i, j - GlassX];
                else temp = map[i, j];

                figureColors.TryGetValue(temp, out color);

                switch (temp)
                {
                    case 'O':
                    case 'I':
                    case 'T':
                    case 'S':
                    case 'Z':
                    case 'L':
                    case 'J':
                        Console.ForegroundColor = color;
                        Console.Write('█');
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(temp);
                        break;
                }

            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            
            switch(i)
            {
                case 1:
                    Console.Write($"   Очки:    {score}  ");
                    break;
                case 2:
                    Console.Write($"   Линий:   {lineAmount}  ");
                    break;
                case 3:
                    Console.Write($"   Уровень: {level}  ");
                    break;
                case 7:
                    Console.Write($"                     Мгновенное");
                    break;
                case 8:
                    Console.Write($"   Управление      падение фигуры");
                    break;
                case 10:
                    Console.Write($"        ^ ");
                    break;
                case 11:
                    Console.Write($"      <   >            Space");
                    break;
                case 12:
                    Console.Write($"        v ");
                    break;
            }
            

            Console.WriteLine();
        }
        Console.SetCursorPosition(0, Console.CursorTop - GlassY);
    }

    // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
    public static int[] GenerateBag()
    {
        int[] bag = { 0, 1, 2, 3, 4, 5, 6 }; // O - 0 I - 1 L - 2 J - 3 S - 4 Z - 5 T - 6
        int len = bag.Length;
        while (len > 1)
        {
            len--;
            int num = new Random().Next(len + 1);
            (bag[num], bag[len]) = (bag[len], bag[num]);
        }
        return bag;
    }

    public static void End()
    {
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine("Игра окончена\n\n" +
            $"Уровень: {level}\n" +
            $"Очки: {score}\n" +
            $"Количество фигур: {amount}\n" +
            $"Количество линий: {lineAmount}\n");
        Console.Write("Начать заново (y/n)?");

        bool notChoosen = true;
        while (notChoosen)
        {
            if (key.Key == ConsoleKey.Y)
            {
                amount = 0; lineAmount = 0; score = 0; level = 1; maxTime = 20; scoreForLevel = 100;
                notChoosen = false;
                Main();
            }
            else if (key.Key == ConsoleKey.N) Environment.Exit(0);
        }
    }

    static void InputHandler()
    {
        while (true)
        {
            key = Console.ReadKey(true);
        }
    }
}

