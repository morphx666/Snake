Imports System.Drawing
Imports System.Threading

Partial Module ModuleMain
    Private Class FoodItemDef
        Public ReadOnly Property Item As Segment
        Public ReadOnly Property CreatedOn As TimeSpan
        Public ReadOnly Property Level As Integer
        Public Property IsNew As Boolean

        Public Sub New(x As Integer, y As Integer, l As Integer)
            Level = l
            Item = New Segment(x, y, Segment.Directions.Same, GetColor())
            CreatedOn = TimeSpan.FromTicks(Now.Ticks)
            IsNew = True
        End Sub

        Private Function GetColor() As ConsoleColor
            Select Case Level
                Case 1 : Return ConsoleColor.Gray
                Case 2 : Return ConsoleColor.Yellow
                Case 3 : Return ConsoleColor.Green
                Case 4 : Return ConsoleColor.Blue
                Case 5 : Return ConsoleColor.Red
                Case Else : Return ConsoleColor.Black
            End Select
        End Function

        Public Function Clone() As FoodItemDef
            Return New FoodItemDef(Item.X, Item.Y, Level)
        End Function
    End Class

    Private gameLoopThread As Thread

    Private suspendRenderer As Boolean = True

    Private Snake As New List(Of Segment)
    Private eraseSegment As New Queue(Of Segment)
    Private foodItem As FoodItemDef
    Private bonuses As New List(Of FoodItemDef)
    Private youAreWhatYouEat As Boolean = False

    Private foodItemLifeSpan As TimeSpan
    Private showingTimer As Boolean
    Private expertMode As Boolean

    Private score As Integer
    Private lastScore As Integer = -1

    Private tr As New TextRenderer()

    Sub Main()
        gameLoopThread = New Thread(AddressOf GameLoop)
        gameLoopThread.Start()

        Do
            Thread.Sleep(100)
            If Console.KeyAvailable Then Exit Do
        Loop
    End Sub

    Private Sub Initialize()
        Dim template As New Segment(Console.WindowWidth / 2 - 1, Console.WindowHeight / 2, Segment.Directions.Right, ConsoleColor.White)

        suspendRenderer = True

        Snake.Clear()
        Snake.Add(template.Clone())
        Snake.Add(template.Clone(ConsoleColor.Gray))
        template.Move()

        Snake.ForEach(Sub(s) s.Direction = Segment.Directions.Left)

        bonuses.Clear()
        foodItem = Nothing

        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.Gray
        Console.Clear()
        Console.Title = "SNAKE"
        Console.CursorVisible = False

        score = 0
        lastScore = -1

        DisplayTitle()
        DrawBorder()

        suspendRenderer = False
    End Sub

    Private Sub DisplayTitle()
        Console.Clear()
        Dim wh As Size = tr.MeassureText(Console.Title)
        tr.Write(Console.Title, (Console.WindowWidth - wh.Width) / 2, (Console.WindowHeight - 10 - wh.Height) / 2, ConsoleColor.DarkCyan)

        Dim DrawCentered = Sub(x As Integer, msg As String, c As ConsoleColor)
                               Console.CursorLeft = (Console.WindowWidth - msg.Length) / 2
                               Console.CursorTop = x
                               Console.ForegroundColor = c
                               Console.Write(msg)
                           End Sub

        DrawCentered((Console.WindowHeight - wh.Height) / 2 + wh.Height - 6, "Press [ENTER] to start", ConsoleColor.White)

        Dim k As Integer = 0
        Dim ks As Integer = 1
        Do
            Dim opMsgs() As String = {"──────────────── Options ──────────────── ",
                                     $"({If(youAreWhatYouEat, "√", "x")}) [Y]ou're Are What You Eat",
                                     $"({If(expertMode, "√", "x")}) [E]xpert Mode            "}

            If opMsgs(0)(k) = "─" Then opMsgs(0) = opMsgs(0).Substring(0, k) + "∙" + opMsgs(0).Substring(k + 1)
            Select Case k
                Case opMsgs(0).Length - 1 : ks = -1
                Case 0 : ks = 1
            End Select
            k += ks

            For i As Integer = 0 To opMsgs.Length - 1
                DrawCentered((Console.WindowHeight - wh.Height) / 2 + wh.Height + 2 + i,
                         opMsgs(i),
                         If(i = 0, ConsoleColor.DarkGray, If(opMsgs(i).Contains("√"), ConsoleColor.White, ConsoleColor.Gray)))
            Next

            If Console.KeyAvailable Then
                Select Case Console.ReadKey(True).Key
                    Case ConsoleKey.Escape : Environment.Exit(0)
                    Case ConsoleKey.Enter : Exit Do
                    Case ConsoleKey.Y : youAreWhatYouEat = Not youAreWhatYouEat
                    Case ConsoleKey.E : expertMode = Not expertMode
                End Select
            End If

            Thread.Sleep(30)
        Loop

        Console.Clear()
    End Sub

    Private Sub DisplayGameOver()
        Dim msg As String = "GAME OVER"
        Dim wh As Size = tr.MeassureText(msg)

        Dim x As Integer = (Console.WindowWidth - wh.Width) \ 2
        Dim y As Integer = (Console.WindowHeight - wh.Height) \ 2

        suspendRenderer = True

        Console.BackgroundColor = ConsoleColor.Red

        For y1 As Integer = y To y + wh.Height - 1
            Console.CursorTop = y1
            Console.CursorLeft = x
            For x1 As Integer = x - wh.Width \ 2 To x + wh.Width \ 2
                Console.Write(" ")
            Next
        Next

        tr.Write(msg, x, y, ConsoleColor.White)

        Console.ForegroundColor = ConsoleColor.White
        Console.BackgroundColor = ConsoleColor.Black
        msg = "Press any key to restart"
        Console.CursorLeft = (Console.WindowWidth - msg.Length) / 2
        Console.CursorTop = (Console.WindowHeight - wh.Height) / 2 + wh.Height + 2
        Console.WriteLine(msg)
    End Sub

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

            moveDelay = If(expertMode, 30, 50)
            renderDelay = moveDelay

            Do
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

                    Snake(0).Move()

                    If Snake(0).X = 0 OrElse Snake(0).X = Console.WindowWidth - 1 Then Exit Do
                    If Snake(0).Y = 0 OrElse Snake(0).Y = Console.WindowHeight - 1 Then Exit Do

                    eraseSegment.Enqueue(Snake(Snake.Count - 1).Clone())
                    For i As Integer = 1 To Snake.Count - 1
                        If Snake(0).IntersectsWidth(Snake(i)) Then Exit Do
                        Snake(i).X = Snake(i - 1).LastX
                        Snake(i).Y = Snake(i - 1).LastY
                    Next

                    If foodItem IsNot Nothing Then
                        If foodItem.Item.IntersectsWidth(Snake(0)) Then
                            score += foodItem.Level
                            For i As Integer = 0 To foodItem.Level - 1
                                If youAreWhatYouEat Then
                                    Snake.Add(Snake.Last().Clone(foodItem.Item.Color))
                                Else
                                    Snake.Add(Snake.Last().Clone())
                                End If
                            Next
                            bonuses.Add(foodItem.Clone())
                            bonuses.Last().Item.X = Console.WindowWidth - 12
                            bonuses.Last().Item.Y = 0
                            foodItem = Nothing
                        ElseIf TimeSpan.FromTicks(Now.Ticks) - foodItem.CreatedOn > foodItemLifeSpan Then
                            Console.CursorLeft = foodItem.Item.X
                            Console.CursorTop = foodItem.Item.Y
                            Console.Write(" ")
                            If Snake.Count > foodItem.Level Then
                                For i = 0 To foodItem.Level - 1
                                    eraseSegment.Enqueue(Snake.Last())
                                    Snake.RemoveAt(Snake.Count - 1)
                                Next
                            End If
                            foodItem = Nothing
                            If Snake.Count < 2 Then Exit Do
                        End If
                    End If
                End If

                If foodItemTimer >= foodItemDelay Then
                    Dim rnd As New Random()

                    foodItemTimer = rnd.Next(0, foodItemDelay)

                    If foodItem Is Nothing Then
                        Do
                            Dim x As Integer = rnd.Next(2, Console.WindowWidth - 2)
                            Dim y As Integer = rnd.Next(2, Console.WindowHeight - 2)

                            Dim s As New Segment(x, y, Segment.Directions.Same, ConsoleColor.White)
                            For i As Integer = 0 To Snake.Count() - 1
                                If Snake(i).IntersectsWidth(s) Then
                                    s = Nothing
                                    Exit For
                                End If
                            Next

                            If s IsNot Nothing Then
                                foodItem = New FoodItemDef(x, y, rnd.Next(1, 6))
                                foodItemLifeSpan = TimeSpan.FromSeconds(15 - foodItem.Level)
                                showingTimer = True
                                Exit Do
                            End If
                        Loop
                    End If
                End If

                If Console.KeyAvailable Then
                    Select Case Console.ReadKey(True).Key
                        Case ConsoleKey.LeftArrow : Snake(0).Direction = Segment.Directions.Left
                        Case ConsoleKey.RightArrow : Snake(0).Direction = Segment.Directions.Right
                        Case ConsoleKey.UpArrow : Snake(0).Direction = Segment.Directions.Up
                        Case ConsoleKey.DownArrow : Snake(0).Direction = Segment.Directions.Down
                        Case ConsoleKey.Escape : Environment.Exit(0)
                    End Select
                End If
            Loop

            DisplayGameOver()
            ConsumeKeystrokes()
            If Console.ReadKey(True).Key = ConsoleKey.Escape Then Environment.Exit(0)
        Loop
    End Sub

    Private Sub DrawBorder()
        Console.ForegroundColor = ConsoleColor.White

        For x As Integer = 0 To Console.WindowWidth - 1
            Console.CursorLeft = x
            Console.CursorTop = 0
            Console.Write("█")
            Console.CursorTop = Console.WindowHeight - 1
            Console.Write("█")
        Next

        For y As Integer = 1 To Console.WindowHeight - 2
            Console.CursorTop = y
            Console.CursorLeft = 0
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
            Snake.ForEach(Sub(s) s.Draw())
        Else
            Snake(0).Draw()
            Snake(1).Draw()
        End If

        If foodItem IsNot Nothing AndAlso foodItem.IsNew Then
            foodItem.IsNew = False
            foodItem.Item.Draw()
        End If

        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White

        AnimateBonuses()
        RenderFoodItemTimer()
        RenderScore()
    End Sub

    Private Sub RenderFoodItemTimer()
        If foodItem IsNot Nothing Then
            Console.CursorLeft = 3
            Console.CursorTop = Console.WindowHeight - 1
            Console.ForegroundColor = foodItem.Item.Color
            Console.Write(" +{0}: {1:N0} ", foodItem.Level, (foodItemLifeSpan - (TimeSpan.FromTicks(Now.Ticks) - foodItem.CreatedOn)).TotalSeconds)
        ElseIf showingTimer Then
            showingTimer = False

            Console.CursorLeft = 3
            Console.CursorTop = Console.WindowHeight - 1
            Console.ForegroundColor = ConsoleColor.White
            Console.Write("████████")
        End If
    End Sub

    Private Sub AnimateBonuses()
        Dim exitDo As Boolean
        Do
            exitDo = True

            For i As Integer = 0 To bonuses.Count - 1
                Console.CursorLeft = bonuses(i).Item.X
                Console.CursorTop = bonuses(i).Item.Y
                Console.ForegroundColor = ConsoleColor.White
                Console.Write("██")

                bonuses(i).Item.X -= 1
                If bonuses(i).Item.X <= 2 Then
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

    Private Sub RenderScore()
        If lastScore <> score Then
            Console.ForegroundColor = ConsoleColor.White
            Console.CursorLeft = Console.WindowWidth - 8
            Console.CursorTop = 0
            Console.Write(" {0} ", score)

            lastScore = score
        End If
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