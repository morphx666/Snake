Public Class Level
    Public ReadOnly Property Name As String
    Public ReadOnly Property FoodItemsCount As Integer
    Public ReadOnly Property MoveDelay As Integer
    Public ReadOnly Property Index As Integer
    Public ReadOnly Property Maze As Boolean()
    Public ReadOnly Property FoodItem As Char

    Public Sub New(index As Integer,
                   name As String,
                   foodItemsCount As Integer,
                   moveDelay As Integer,
                   maze() As Boolean,
                   Optional foodItem As Char = "█")
        Me.Index = index
        Me.Name = name
        Me.FoodItemsCount = foodItemsCount
        Me.MoveDelay = moveDelay
        Me.Maze = maze
        Me.FoodItem = foodItem
    End Sub

    Public ReadOnly Property IntersectsWith(s As Segment) As Boolean
        Get
            Return Maze(s.Y * Console.WindowWidth + s.X)
        End Get
    End Property
End Class
