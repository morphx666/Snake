Imports System.Drawing
Imports System.Threading

Partial Module ModuleMain
    Public Enum LooseReason
        None
        EatOwnTail
        HitWall
        HitMaze
        Hunger
        UserQuit
    End Enum

    Private Class HighScore
        Public Property Name As String
        Public Property Score As Integer
        Public Property LevelName As String
        Public Property LevelIndex As Integer
        Public Property ExpertMode As Boolean
        Public Property Platform As String

        Public Sub New(name As String, score As Integer,
                       levelName As String, levelIndex As Integer,
                       expertMode As Boolean)
            If name.Length < 3 Then name = name.PadRight(3)
            If name.Length > 3 Then name = name.Substring(0, 3)

            Me.Name = name
            Me.Score = score
            Me.LevelName = levelName
            Me.LevelIndex = levelIndex
            Me.ExpertMode = expertMode
            Me.Platform = Runtime.Platform.ToString()
        End Sub

        Public Function ToXML() As XElement
            Return <highScore>
                       <name><%= Name %></name>
                       <score><%= Score %></score>
                       <levelName><%= LevelName %></levelName>
                       <levelIndex><%= LevelIndex %></levelIndex>
                       <expertMode><%= ExpertMode %></expertMode>
                       <platform><%= Platform %></platform>
                   </highScore>
        End Function
    End Class

    Private gameLoopThread As Thread

    Private suspendRenderer As Boolean = True

    Private snake As New List(Of Segment)
    Private eraseSegment As New Queue(Of Segment)
    Private food As FoodItem
    Private bonuses As New List(Of FoodItem)
    Private youAreWhatYouEat As Boolean = True
    Private lives As Integer
    Private highScores As New List(Of HighScore)(10 - 1)
    Private lastestHighScore As HighScore

    Private levels As New List(Of Level)
    Private currentLevel As Level
    Private foodItemsCount As Integer
    Private levelFoodItemsCount As Integer
    Private reason As LooseReason = LooseReason.None

    Private defaultFont As String
    Private originalForeColor As ConsoleColor
    Private originalBackColor As ConsoleColor

    Private foodItemLifeSpan As TimeSpan
    Private showingTimer As Boolean
    Private expertMode As Boolean

    Private score As Integer
    Private lastScore As Integer = -1

    Private rnd As New Random()

    Private tr As TextRenderer

    Sub Main()
        Console.WriteLine("Starting Snake...")

        originalForeColor = Console.ForegroundColor
        originalBackColor = Console.BackgroundColor
        Select Case Runtime.Platform
            Case Runtime.Platforms.Windows : defaultFont = "Consolas"
            Case Runtime.Platforms.Mac : defaultFont = "Andale Mono"
            Case Else : defaultFont = "Monospace Regular"
        End Select
        tr = New TextRenderer(defaultFont)

        Win32Native.MaximizeConsoleWindow()

        LoadNVRam()

        If Console.WindowWidth < 94 OrElse Console.WindowHeight < 42 Then
            Console.WriteLine()
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine("Unable to start Snake under these conditions...")
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine("Please increase the size of your console/terminal window to at least 94 columns and 42 rows.")
            Console.WriteLine($"Your current console/terminal window has a size of {Console.WindowWidth} columns ({If(Console.WindowWidth >= 94, "√", "×")}) and {Console.WindowHeight} rows ({If(Console.WindowHeight >= 42, "√", "×")}).")
            Console.ReadKey()
            Quit()
        End If

        gameLoopThread = New Thread(AddressOf GameLoop)
        gameLoopThread.Start()

        Console.CursorVisible = False

        Do
            Thread.Sleep(100)
            If Console.KeyAvailable Then Exit Do
        Loop
    End Sub

    Private Sub SaveNVRam()
        Dim xml = <settings>
                      <expertMode><%= expertMode %></expertMode>
                      <youAreWhatYouEat><%= youAreWhatYouEat %></youAreWhatYouEat>
                      <highScores><%= From hs In highScores Select (hs.ToXML()) %></highScores>
                  </settings>
        xml.Save("nvram.dat")
    End Sub

    Private Sub LoadNVRam()
        If IO.File.Exists("nvram.dat") Then
            Dim xml = XDocument.Parse(IO.File.ReadAllText("nvram.dat"))

            Boolean.TryParse(xml.<settings>.<expertMode>.Value, expertMode)
            Boolean.TryParse(xml.<settings>.<youAreWhatYouEat>.Value, youAreWhatYouEat)

            Dim hsName As String
            Dim hsScore As Integer
            Dim hsLevelName As String
            Dim hsLevelIndex As Integer
            Dim hsExpertMode As Boolean
            Dim hsPlatform As String

            For Each hs In xml.<settings>.<highScores>.<highScore>
                hsName = hs.<name>.Value
                Integer.TryParse(hs.<score>.Value, hsScore)
                hsLevelName = hs.<levelName>.Value
                Integer.TryParse(hs.<levelIndex>.Value, hsLevelIndex)
                Boolean.TryParse(hs.<expertMode>.Value, hsExpertMode)
                hsPlatform = If(hs.<platform>.Value, Runtime.Platform.ToString())
                highScores.Add(New HighScore(hsName,
                                             hsScore,
                                             hsLevelName,
                                             hsLevelIndex,
                                             hsExpertMode))
            Next
        Else
            For i As Integer = 0 To 9
                highScores.Add(New HighScore("...", 10, "Warming Up", 1, False))
            Next
        End If
    End Sub

    Private Sub Initialize()
        suspendRenderer = True

        CreateLevels()
        InitializeSnake()

        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.Gray
        Console.Title = "SNAKE"

        score = 0
        lastScore = -1
        lives = 3
        reason = LooseReason.None

        DisplayTitle()
        SaveNVRam()
        RenderBorder()

        suspendRenderer = False
    End Sub

    Private Sub InitializeSnake()
        Dim template As New Segment(Console.WindowWidth / 2 - 1, Console.WindowHeight / 2, Segment.Directions.Right, ConsoleColor.White)

        snake.Clear()
        snake.Add(template.Clone())
        snake.Add(template.Clone(ConsoleColor.Gray))

        snake.ForEach(Sub(s) s.Direction = Segment.Directions.Left)

        bonuses.Clear()
        food = Nothing
        levelFoodItemsCount = 0
    End Sub

    Private Sub CreateLevels()
        levels.Add(New Level(1, "Warming Up", 5, 40, Mazes.Empty))
        levels.Add(New Level(2, "Graduation", 15, 35, Mazes.Corners))
        levels.Add(New Level(3, "Real World", 20, 30, Mazes.Boxes1))
        levels.Add(New Level(4, "Tiny Food", 25, 25, Mazes.Boxes2, "■"))
        levels.Add(New Level(5, "Can U C ME?", 40, 25, Mazes.Stars, "·"))
        levels.Add(New Level(6, "Fast & Wild", 60, 15, Mazes.Boxes1))

        currentLevel = levels.First()
    End Sub

    Private Sub DisplayTitle()
        Console.Clear()

        Dim wh As Size = tr.MeassureText(Console.Title)

        Console.SetCursorPosition((Console.WindowWidth - 29) / 2 - (wh.Width + 2), 1)
        AsciiSnake.Render()

        RenderBanner(Console.Title, ConsoleColor.DarkCyan, ConsoleColor.Black,
                     True, False, 0, 0,, 9)

        Dim DrawCentered = Sub(y As Integer, msg As String, c As ConsoleColor)
                               Console.SetCursorPosition((Console.WindowWidth - msg.Length) / 2, y)
                               Console.ForegroundColor = c
                               Console.Write(msg)
                           End Sub

        DrawCentered(wh.Height,
                     "Press [ENTER] to start", ConsoleColor.White)

        DisplayHighScores(wh.Height + 3)

        Dim k As Integer = 0
        Dim ks As Integer = 1
        Do
            Dim opMsgs() As String = {"──────────────── Options ──────────────── ",
                                     $"({If(youAreWhatYouEat, "√", "·")}) [Y]ou're Are What You Eat".PadRight(30),
                                     $"({If(expertMode, "√", "·")}) [E]xpert Mode".PadRight(30),
                                     "    [ESC] to exit game".PadRight(30)}

            If opMsgs(0)(k) = "─" Then opMsgs(0) = opMsgs(0).Substring(0, k) + "∙" + opMsgs(0).Substring(k + 1)
            Select Case k
                Case opMsgs(0).Length - 1 : ks = -1
                Case 0 : ks = 1
            End Select
            k += ks

            For i As Integer = 0 To opMsgs.Length - 1
                DrawCentered(Console.WindowHeight - (opMsgs.Length - i) - 2,
                         opMsgs(i),
                         If(i = 0,
                            ConsoleColor.DarkGray,
                            If(opMsgs(i).Contains("√"),
                                ConsoleColor.White,
                                ConsoleColor.Gray)))
            Next

            If Console.KeyAvailable Then
                Select Case Console.ReadKey(True).Key
                    Case ConsoleKey.Escape : Quit()
                    Case ConsoleKey.Enter : Exit Do
                    Case ConsoleKey.Y : youAreWhatYouEat = Not youAreWhatYouEat
                    Case ConsoleKey.E : expertMode = Not expertMode
                End Select
            End If

            Thread.Sleep(30)
        Loop

        Console.Clear()
    End Sub

    Private Sub Quit()
        SaveNVRam()

        Console.Clear()
        Console.ForegroundColor = originalForeColor
        Console.BackgroundColor = originalBackColor
        Console.CursorVisible = True

        Environment.Exit(0)
    End Sub

    Private Sub DisplayHighScores(y)
        Console.ForegroundColor = ConsoleColor.Gray

        Dim colors()() As Integer = New Integer()() {
                                        New Integer() {0, 3, ConsoleColor.White},
                                        New Integer() {6, 7, ConsoleColor.Yellow},
                                        New Integer() {16, 14, ConsoleColor.DarkCyan},
                                        New Integer() {30, 4, ConsoleColor.Gray},
                                        New Integer() {36, 99, ConsoleColor.DarkGray}}

        For Each hs In highScores.OrderByDescending(Function(k) k.Score)
            Console.CursorTop = y

            If hs.Equals(lastestHighScore) Then
                Console.BackgroundColor = ConsoleColor.DarkGray
            Else
                Console.BackgroundColor = ConsoleColor.Black
            End If

            Dim msg As String = $"{hs.Name}   {hs.Score.ToString("N0").PadLeft(7)}   {hs.LevelName.PadRight(14).Substring(0, 14)} {hs.LevelIndex.ToString().PadLeft(3)}  {hs.Platform}"
            Console.CursorLeft = (Console.WindowWidth - msg.Length) / 2
            For x As Integer = 0 To msg.Length - 1
                For k As Integer = 0 To colors.Length - 1
                    If x >= colors(k)(0) AndAlso x <= colors(k)(0) + colors(k)(1) Then
                        Console.ForegroundColor = If(hs.ExpertMode AndAlso x <= colors(0)(1), ConsoleColor.Red, colors(k)(2))
                        Exit For
                    End If
                Next
                Console.Write(msg(x))
            Next
            y += 1
        Next

        Console.BackgroundColor = ConsoleColor.Black
    End Sub

    Private Sub DisplayGameOver()
        Dim msg As String = "GAME OVER"
        Dim wh = RenderBanner(msg, ConsoleColor.White, ConsoleColor.Red, True, True)
        PrintReason(wh, (Console.WindowHeight - wh.Height) / 2 - 3, ConsoleColor.Red)

        msg = "Press any key to restart"
        Console.SetCursorPosition((Console.WindowWidth - msg.Length) / 2, (Console.WindowHeight - wh.Height) / 2 + wh.Height + 4)
        Console.WriteLine(msg)
        If Console.ReadKey(True).Key = ConsoleKey.Escape Then Quit()
        Console.Clear()
        RenderBorder()
    End Sub

    Private Sub PrintReason(wh As Size, y As Integer, bc As ConsoleColor)
        Dim reasonStr As String = GetMessage(reason, rnd)
        If reasonStr <> "" Then
            Console.ForegroundColor = bc
            Console.SetCursorPosition((Console.WindowWidth - wh.Width) / 2, wh.Height + 2 + y)
            Console.Write("│" + "│".PadLeft(wh.Width))
            Console.SetCursorPosition((Console.WindowWidth - wh.Width) / 2, wh.Height + 3 + y)
            Console.Write("│" + "│".PadLeft(wh.Width))
            Console.SetCursorPosition((Console.WindowWidth - wh.Width) / 2, wh.Height + 4 + y)
            Console.Write("└" + StrDup(wh.Width - 1, "─") + "┘")
            Console.ForegroundColor = ConsoleColor.White
            Console.SetCursorPosition((Console.WindowWidth - reasonStr.Length) / 2, wh.Height + 3 + y)
            Console.Write(reasonStr)
        End If
    End Sub

    Private Sub DisplayLevel()
        Dim msg1 As String = currentLevel.Name
        Dim msg2 As String = $"LEVEL: {currentLevel.Index}"

        Thread.Sleep(500)

        Dim wh As Size = RenderBanner(msg1,
                                     ConsoleColor.White, ConsoleColor.Blue,
                                     True, False, , 2)

        PrintReason(wh, 0, ConsoleColor.Blue)

        Dim wh2 As Size = tr.MeassureText(msg2)
        RenderBanner(msg2,
                     ConsoleColor.White, ConsoleColor.DarkGreen,
                     True, False, , Console.WindowHeight - wh2.Height + 1, , 6, 0.7)

        DisplayLivesAndLevel()
        For i As Integer = 1 To 3
            RenderBanner((4 - i).ToString(), ConsoleColor.Red, ConsoleColor.Black,
                         True, True,, 1,, 7)
            Thread.Sleep(1000)
        Next

        reason = LooseReason.None

        Console.Clear()
        RenderBorder()
        DisplayScore(True)
        RenderLevelMaze()
        ConsumeKeystrokes()
    End Sub

    Private Function RenderBanner(msg As String, foreColor As ConsoleColor, backColor As ConsoleColor,
                             centerX As Boolean, centerY As Boolean,
                             Optional x As Integer = 0, Optional y As Integer = 0,
                             Optional fontFamily As String = "",
                             Optional fontSize As Integer = 8,
                             Optional kerning As Double = 0.6,
                             Optional animate As Boolean = True) As Size

        If fontFamily = "" Then fontFamily = defaultFont
        Dim wh As Size = tr.MeassureText(msg, fontFamily, fontSize, kerning)

        x = If(centerX, (Console.WindowWidth - wh.Width) \ 2 + x, x)
        y = If(centerY, (Console.WindowHeight - wh.Height) \ 2 + y, y)

        suspendRenderer = True

        Console.BackgroundColor = backColor

        For Each f As Char In If(animate, {"░", "░", "▒", "▓", "█"}, "█")
            For y1 As Integer = y To y + wh.Height - 1
                Console.SetCursorPosition(x, y1)
                For x1 As Integer = x - wh.Width \ 2 To x + wh.Width \ 2
                    Console.Write(" ")
                Next
            Next

            tr.Write(msg, x, y, foreColor, fontFamily, fontSize, f, kerning)
            Thread.Sleep(1)
        Next

        Console.ForegroundColor = ConsoleColor.White
        Console.BackgroundColor = ConsoleColor.Black

        Return wh
    End Function

    Private Sub GameLoop()
        Dim delay As Integer = 10

        Dim moveDelay As Integer
        Dim moveTimer As Integer = 0

        Dim foodItemDelay As Integer = 3000
        Dim foodItemTimer As Integer = 0

        Dim renderDelay As Integer = 30
        Dim renderTimer As Integer = 0

        Do
            ConsumeKeystrokes()
            Initialize()

            Do
                DisplayLevel()
                InitializeSnake()

                Do
                    moveDelay = currentLevel.MoveDelay - If(expertMode, 10, 0) - snake.Count / 4
                    renderDelay = moveDelay

                    Thread.Sleep(delay)

                    renderTimer += delay
                    moveTimer += delay
                    foodItemTimer += delay

                    If renderTimer >= renderDelay Then
                        renderTimer = 0
                        Render()
                    End If

                    If moveTimer >= moveDelay Then
                        moveTimer = 0

                        snake(0).Move()

                        If snake(0).X = 0 OrElse snake(0).X = Console.WindowWidth - 1 Then
                            reason = LooseReason.HitWall
                            Exit Do
                        End If
                        If snake(0).Y = 0 OrElse snake(0).Y = Console.WindowHeight - 1 Then
                            reason = LooseReason.HitWall
                            Exit Do
                        End If
                        If currentLevel.IntersectsWith(snake(0)) Then
                            reason = LooseReason.HitMaze
                            Exit Do
                        End If

                        eraseSegment.Enqueue(snake(snake.Count - 1).Clone())
                        For i As Integer = 1 To snake.Count - 1
                            If snake(0).IntersectsWith(snake(i)) Then
                                reason = LooseReason.EatOwnTail
                                Exit Do
                            End If
                            snake(i).X = snake(i - 1).LastX
                            snake(i).Y = snake(i - 1).LastY
                        Next

                        If food IsNot Nothing Then
                            If food.Item.IntersectsWith(snake(0)) Then
                                AddScore(food.Level)
                                For i As Integer = 0 To food.Level - 1
                                    If youAreWhatYouEat Then
                                        snake.Add(snake.Last().Clone(food.Item.Color))
                                    Else
                                        snake.Add(snake.Last().Clone())
                                    End If
                                Next
                                bonuses.Add(food.Clone())
                                bonuses.Last().Item.X = Console.WindowWidth - 12
                                bonuses.Last().Item.Y = 0
                                EraseFoodItem()
                                food = Nothing
                                CheckLevelChange()
                            ElseIf TimeSpan.FromTicks(Now.Ticks) - food.CreatedOn > foodItemLifeSpan Then
                                EraseFoodItem()
                                If snake.Count > food.Level Then
                                    For i = 0 To food.Level - 1
                                        eraseSegment.Enqueue(snake.Last())
                                        snake.RemoveAt(snake.Count - 1)
                                    Next
                                Else
                                    reason = LooseReason.Hunger
                                    Exit Do
                                End If
                                food = Nothing
                                If snake.Count < 2 Then
                                    reason = LooseReason.Hunger
                                    Exit Do
                                End If
                            End If
                        End If
                    End If

                    If foodItemTimer >= foodItemDelay Then
                        foodItemTimer = rnd.Next(0, foodItemDelay)

                        If food Is Nothing Then
                            Do
                                Dim x As Integer = rnd.Next(2, Console.WindowWidth - 2)
                                Dim y As Integer = rnd.Next(2, Console.WindowHeight - 2)

                                Dim s As New Segment(x, y, Segment.Directions.Same,
                                                           ConsoleColor.White)
                                If currentLevel.IntersectsWith(s) Then
                                    s = Nothing
                                Else
                                    For i As Integer = 0 To snake.Count() - 1
                                        If snake(i).IntersectsWith(s) Then
                                            s = Nothing
                                            Exit For
                                        End If
                                    Next
                                End If

                                If s IsNot Nothing Then
                                    food = New FoodItem(x, y, rnd.Next(1, If(expertMode, 12, 6)))
                                    foodItemLifeSpan = TimeSpan.FromSeconds(20 -
                                                                            food.Level +
                                                                            currentLevel.Index / 2 +
                                                                            snake.Count / 10)
                                    showingTimer = True
                                    Exit Do
                                End If
                            Loop
                        End If
                    End If

                    If Console.KeyAvailable Then
                        Select Case Console.ReadKey(True).Key
                            Case ConsoleKey.LeftArrow : snake(0).Direction = Segment.Directions.Left
                            Case ConsoleKey.RightArrow : snake(0).Direction = Segment.Directions.Right
                            Case ConsoleKey.UpArrow : snake(0).Direction = Segment.Directions.Up
                            Case ConsoleKey.DownArrow : snake(0).Direction = Segment.Directions.Down
                            Case ConsoleKey.Escape
                                reason = LooseReason.UserQuit
                                Exit Do
                        End Select
                    End If
                Loop

                If reason = LooseReason.UserQuit Then Exit Do

                ApplyBonus(0)

                lives -= 1
                If lives = 0 Then
                    DisplayLivesAndLevel()
                    Exit Do
                End If
            Loop

            If reason <> LooseReason.UserQuit Then
                ConsumeKeystrokes()
                ApplyBonus(2)
                DisplayGameOver()
                SetHighScore()
                ConsumeKeystrokes()
            End If
        Loop
    End Sub

    Private Sub AddScore(s As Integer)
        If Int((score + s) / 100) > Int(score / 100) Then
            lives += 1
            DisplayLivesAndLevel()
        End If
        score += s
    End Sub

    Private Sub ApplyBonus(m As Integer)
        suspendRenderer = True

        For i As Integer = 0 To snake.Count - 1
            Thread.Sleep(50)

            If snake(i).X > 0 AndAlso snake(i).X < Console.WindowWidth - 1 AndAlso
                    snake(i).Y > 0 AndAlso snake(i).Y < Console.WindowHeight - 1 AndAlso
                    Not currentLevel.IntersectsWith(snake(i)) Then
                snake(i).Draw(" "c)
            End If

            If m > 0 Then
                AddScore(m)
                DisplayScore(True)
            End If
        Next

        suspendRenderer = False
    End Sub

    Private Sub SetHighScore()
        suspendRenderer = True

        For i As Integer = 0 To highScores.Count - 1
            If score > highScores(i).Score Then
                Dim hs As New HighScore(EnterUserName(),
                                                   score,
                                                   currentLevel.Name,
                                                   currentLevel.Index,
                                                   expertMode)
                highScores.Insert(i, hs)
                lastestHighScore = hs
                highScores.Remove(highScores.Last())
                SaveNVRam()
                Exit For
            End If
        Next

        suspendRenderer = False
    End Sub

    Private Function EnterUserName() As String
        Dim c As New List(Of Char)
        Dim cs As Size = tr.MeassureText("A")
        Dim selection() As Integer = {0, 0, 0}
        Dim selIndex As Integer = 0

        For i As Integer = Asc("A") To Asc("Z") : c.Add(Chr(i)) : Next
        c.AddRange({".", "/", "\", "+", "-"})
        For i As Integer = Asc(0) To Asc(9) : c.Add(Chr(i)) : Next

        Dim msg As String = "High Score!"
        Dim wh As Size = tr.MeassureText(msg)
        RenderBanner(msg, ConsoleColor.White, ConsoleColor.Blue,
                     True, False,, 2)

        Dim mx As Integer = (Console.WindowWidth - wh.Width) / 2
        Dim my As Integer = wh.Height + 3
        Console.BackgroundColor = ConsoleColor.Blue
        Console.SetCursorPosition(mx, my - 1)
        Console.Write(StrDup(wh.Width, "─"))

        For y As Integer = my To my + cs.Height + 1
            Console.SetCursorPosition(mx, y)
            For x As Integer = 0 To wh.Width - 1
                Console.Write(" ")
            Next
        Next

        Do
            For i As Integer = 0 To selection.Length - 1
                RenderBanner(" " + c(selection(i)) + " ",
                             If(i = selIndex, ConsoleColor.White, ConsoleColor.Gray),
                             If(i = selIndex, ConsoleColor.DarkBlue, ConsoleColor.Blue),
                             True, False,
                             i * cs.Width * 3 - cs.Width * 3 - 1, my + 1,
                             ,, 0.5, False)
            Next
            Console.SetCursorPosition(1, 1)

            Do
                If Console.KeyAvailable Then
                    Select Case Console.ReadKey().Key
                        Case ConsoleKey.UpArrow : selection(selIndex) = (selection(selIndex) + 1) Mod c.Count
                        Case ConsoleKey.DownArrow : selection(selIndex) -= 1 : If selection(selIndex) = -1 Then selection(selIndex) = c.Count - 1
                        Case ConsoleKey.LeftArrow : selIndex -= 1 : If selIndex = -1 Then selIndex = selection.Length - 1
                        Case ConsoleKey.RightArrow : selIndex = (selIndex + 1) Mod selection.Length
                        Case ConsoleKey.Enter : Return c(selection(0)) + c(selection(1)) + c(selection(2))
                    End Select
                    Exit Do
                Else
                    Thread.Sleep(10)
                End If
            Loop
        Loop

        Console.ReadKey()
    End Function

    Private Sub EraseFoodItem()
        Console.SetCursorPosition(food.Item.X, food.Item.Y)
        Console.Write(" ")
    End Sub

    Private Sub CheckLevelChange()
        levelFoodItemsCount += 1
        foodItemsCount += 1

        If foodItemsCount = currentLevel.FoodItemsCount Then
            If currentLevel.Index = levels.Count Then ' Is this the last level?
            Else
                currentLevel = levels(currentLevel.Index)
            End If
            ApplyBonus(1)
            DisplayLevel()

            snake(0).X = Console.WindowWidth / 2 - 1
            snake(0).Y = Console.WindowHeight / 2
            snake.ForEach(Sub(s)
                              s.X = snake(0).X
                              s.Y = snake(0).Y
                              s.Direction = Segment.Directions.Left
                          End Sub)
        End If
    End Sub

    Private Sub RenderLevelMaze()
        Console.ForegroundColor = ConsoleColor.Magenta

        For y As Integer = 0 To Console.WindowHeight - 1
            For x As Integer = 0 To Console.WindowWidth - 1
                If currentLevel.Maze(y * Console.WindowWidth + x) Then
                    Console.SetCursorPosition(x, y)
                    Console.Write("▓")
                End If
            Next
        Next
    End Sub

    Private Sub RenderBorder()
        Console.ForegroundColor = ConsoleColor.White

        For x As Integer = 0 To Console.WindowWidth - 1
            Console.SetCursorPosition(x, 0)
            Console.Write("█")
            Console.CursorTop = Console.WindowHeight - 1
            Console.Write("█")
        Next

        For y As Integer = 1 To Console.WindowHeight - 2
            Console.SetCursorPosition(0, y)
            Console.Write("█")
            Console.CursorLeft = Console.WindowWidth - 1
            Console.Write("█")
        Next
    End Sub

    Private Sub Render()
        While eraseSegment.Count() > 0
            eraseSegment.Dequeue().Draw(" "c)
        End While

        If youAreWhatYouEat Then
            snake.ForEach(Sub(s) s.Draw())
        Else
            snake(0).Draw()
            snake(1).Draw()
        End If

        If food IsNot Nothing AndAlso food.IsNew Then
            food.IsNew = False
            food.Item.Draw(currentLevel.FoodItem)
        End If

        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White

        AnimateBonuses()
        DisplayFoodItemTimer()
        DisplayScore()
        DisplayLivesAndLevel()
    End Sub

    Private Sub DisplayFoodItemTimer()
        If food IsNot Nothing Then
            Console.SetCursorPosition(3, Console.WindowHeight - 1)
            Console.BackgroundColor = If(food.Level <= 5, ConsoleColor.Gray, ConsoleColor.DarkGray)
            Console.ForegroundColor = food.Item.Color
            Console.Write(" +{0}: {1:N0} ", food.Level, (foodItemLifeSpan - (TimeSpan.FromTicks(Now.Ticks) - food.CreatedOn)).TotalSeconds)
        ElseIf showingTimer Then
            showingTimer = False
            Console.SetCursorPosition(3, Console.WindowHeight - 1)
            Console.ForegroundColor = ConsoleColor.White
            Console.Write("█████████")
        End If
    End Sub

    Private Sub AnimateBonuses()
        Dim exitDo As Boolean
        Do
            exitDo = True

            For i As Integer = 0 To bonuses.Count - 1
                Console.SetCursorPosition(bonuses(i).Item.X, bonuses(i).Item.Y)
                Console.ForegroundColor = ConsoleColor.White
                Console.Write("███")

                bonuses(i).Item.X -= 1
                If bonuses(i).Item.X <= 3 Then
                    bonuses.RemoveAt(i)
                    exitDo = False
                    Exit For
                End If

                Console.CursorLeft = bonuses(i).Item.X
                Console.ForegroundColor = bonuses(i).Item.Color
                Console.Write("+{0}", bonuses(i).Level)
            Next
        Loop Until exitDo
    End Sub

    Private Sub DisplayScore(Optional force As Boolean = False)
        If lastScore <> score OrElse force Then
            Console.ForegroundColor = ConsoleColor.White
            Console.SetCursorPosition(Console.WindowWidth - 8, 0)
            Console.Write($" {score} ")
            lastScore = score
        End If
    End Sub

    Private Sub DisplayLivesAndLevel()
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = If(expertMode, ConsoleColor.White, ConsoleColor.Gray)
        Console.SetCursorPosition(Console.WindowWidth - 8 - lives - 5, Console.WindowHeight - 1)
        Console.Write($" {StrDup(lives, "▌")} | {currentLevel.Index} | {currentLevel.FoodItemsCount - foodItemsCount} ")
    End Sub

    Private Sub ConsumeKeystrokes()
        Dim n As Integer = 2000
        Do
            Thread.Sleep(10)
            n -= 100
            While Console.KeyAvailable
                Console.ReadKey()
            End While
        Loop While n > 0
    End Sub
End Module