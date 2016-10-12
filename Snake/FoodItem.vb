Public Class FoodItem
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
            Case 1 : Return ConsoleColor.DarkGray
            Case 2 : Return ConsoleColor.DarkYellow
            Case 3 : Return ConsoleColor.DarkGreen
            Case 4 : Return ConsoleColor.DarkBlue
            Case 5 : Return ConsoleColor.DarkRed
            Case 6 : Return ConsoleColor.Gray
            Case 7 : Return ConsoleColor.Yellow
            Case 8 : Return ConsoleColor.Green
            Case 9 : Return ConsoleColor.Blue
            Case 10 : Return ConsoleColor.Red
            Case 11 : Return ConsoleColor.White
            Case Else : Return ConsoleColor.Black
        End Select
    End Function

    Public Function Clone() As FoodItem
        Return New FoodItem(Item.X, Item.Y, Level)
    End Function
End Class