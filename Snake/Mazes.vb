Public Class Mazes
    Public Shared Function Empty() As Boolean()
        Dim b(Console.WindowWidth * Console.WindowHeight) As Boolean
        Return b
    End Function

    Public Shared Function Corners() As Boolean()
        Dim cw As Integer = Console.WindowWidth
        Dim ch As Integer = Console.WindowHeight
        Dim b(cw * ch - 1) As Boolean

        Dim x As Integer = cw / 12
        Dim y As Integer = ch / 12

        Dim w As Integer = cw / 8
        Dim h As Integer = ch / 8

        FillRectangle(b, True, x, y, w, h)
        FillRectangle(b, True, cw - w - x, y, w, h)
        FillRectangle(b, True, x, ch - h - y - 1, w, h)
        FillRectangle(b, True, cw - w - x, ch - h - y - 1, w, h)

        Return b
    End Function

    Public Shared Function Boxes1() As Boolean()
        Dim cw As Integer = Console.WindowWidth
        Dim ch As Integer = Console.WindowHeight
        Dim b(cw * ch - 1) As Boolean

        Dim w As Integer = cw / 5
        Dim h As Integer = ch / 5

        DrawLine(b, True, w, h, w * 2, h)
        DrawLine(b, True, w, h, w, h * 2)
        DrawLine(b, True, w * 2, h, w * 2, h * 2)

        DrawLine(b, True, cw - w, h, cw - w * 2, h)
        DrawLine(b, True, cw - w, h, cw - w, h * 2)
        DrawLine(b, True, cw - w * 2, h, cw - w * 2, h * 2)

        DrawLine(b, True, w, ch - h, w * 2, ch - h)
        DrawLine(b, True, w, ch - h, w, ch - h * 2)
        DrawLine(b, True, w * 2, ch - h, w * 2, ch - h * 2)

        DrawLine(b, True, cw - w, ch - h, cw - w * 2, ch - h)
        DrawLine(b, True, cw - w, ch - h, cw - w, ch - h * 2)
        DrawLine(b, True, cw - w * 2, ch - h, cw - w * 2, ch - h * 2)

        Return b
    End Function

    Public Shared Function Boxes2() As Boolean()
        Dim cw As Integer = Console.WindowWidth
        Dim ch As Integer = Console.WindowHeight
        Dim b() As Boolean = Boxes1()

        Dim w As Integer = cw / 5
        Dim h As Integer = ch / 5

        DrawLine(b, True, w + w / 2, h - 1, w + w / 2, ch - h + 1)
        DrawLine(b, True, cw - w - w / 2, h - 1, cw - w - w / 2, ch - h + 1)

        Return b
    End Function

    Public Shared Function Stars() As Boolean()
        Dim cw As Integer = Console.WindowWidth
        Dim ch As Integer = Console.WindowHeight
        Dim b(cw * ch - 1) As Boolean

        Dim star() As Boolean = {False, True, False,
                                 True, True, True,
                                 False, True, False}

        For y As Integer = 6 To ch - 6 Step 12
            If y >= ch / 2 - 1 AndAlso y <= ch / 2 + 1 Then Continue For

            For x As Integer = 6 To cw - 6 Step 12
                DrawLine(b, True, x - 1, y, x + 1, y)
                DrawLine(b, True, x, y - 1, x, y + 1)
            Next
        Next

        Return b
    End Function

    Private Shared Sub SetPixel(b() As Boolean, v As Boolean, x As Integer, y As Integer)
        b(y * Console.WindowWidth + x) = v
    End Sub

    Private Shared Sub DrawLine(b() As Boolean, v As Boolean, x1 As Integer, y1 As Integer, x2 As Integer, y2 As Integer)
        Dim dx As Integer = x2 - x1
        Dim dy As Integer = y2 - y1
        Dim l As Integer = Math.Sqrt(dx ^ 2 + dy ^ 2)
        Dim a As Double = Math.Atan2(dy, dx)
        For r As Integer = 0 To l
            SetPixel(b, v, x1 + r * Math.Cos(-a), y1 + r * Math.Sin(a))
        Next
    End Sub

    Private Shared Sub FillRectangle(b() As Boolean, v As Boolean, x As Integer, y As Integer, width As Integer, height As Integer)
        For y1 As Integer = y To y + height
            DrawLine(b, v, x, y1, x + width, y1)
        Next
    End Sub
End Class
