﻿Public Class Segment
    Implements ICloneable

    Public Enum Directions
        Left
        Right
        Up
        Down
        Same
    End Enum

    Private mX As Integer
    Private mY As Integer
    Private mLastX As Integer
    Private mLastY As Integer
    Private mDirection As Directions
    Private mColor As ConsoleColor

    Public Sub New(x As Integer, y As Integer, d As Directions, c As ConsoleColor)
        mX = x
        mY = y
        mDirection = d
        mColor = c
    End Sub

    Public Sub Move(Optional d As Directions = Directions.Same)
        If d <> Directions.Same Then mDirection = d

        mLastX = mX
        mLastY = mY

        Select Case mDirection
            Case Directions.Left : mX -= 1
            Case Directions.Right : mX += 1
            Case Directions.Up : mY -= 1
            Case Directions.Down : mY += 1
        End Select
    End Sub

    Public Sub Draw(Optional c As Char = "█", Optional bc As ConsoleColor = ConsoleColor.Black)
        Console.CursorLeft = mX
        Console.CursorTop = mY

        Console.BackgroundColor = bc
        Console.ForegroundColor = mColor

        Console.Write(c, mX, mY)
    End Sub

    Public Sub Draw()
        Console.CursorLeft = mX
        Console.CursorTop = mY

        Console.BackgroundColor = mColor

        Console.Write(" ", mX, mY)
    End Sub

    Public Function IntersectsWith(s As Segment) As Boolean
        Return mX = s.X AndAlso mY = s.Y
    End Function

    Public Function Clone() As Object Implements ICloneable.Clone
        Return New Segment(mX, mY, mDirection, mColor)
    End Function

    Public Function Clone(newColor As ConsoleColor) As Object
        Return New Segment(mX, mY, mDirection, newColor)
    End Function

    Public Property X As Integer
        Get
            Return mX
        End Get
        Set(value As Integer)
            mLastX = mX
            mX = value
        End Set
    End Property

    Public Property Y As Integer
        Get
            Return mY
        End Get
        Set(value As Integer)
            mLastY = mY
            mY = value
        End Set
    End Property

    Public Property Direction As Directions
        Get
            Return mDirection
        End Get
        Set(value As Directions)
            mDirection = value
        End Set
    End Property

    Public Property Color As ConsoleColor
        Get
            Return mColor
        End Get
        Set(value As ConsoleColor)
            mColor = value
        End Set
    End Property

    Public ReadOnly Property LastX As Integer
        Get
            Return mLastX
        End Get
    End Property

    Public ReadOnly Property LastY As Integer
        Get
            Return mLastY
        End Get
    End Property

    Public Shared Operator =(s1 As Segment, s2 As Segment) As Boolean
        Return s1.X = s2.X AndAlso s1.Y = s2.Y AndAlso s1.Direction = s2.Direction
    End Operator

    Public Shared Operator <>(s1 As Segment, s2 As Segment) As Boolean
        Return Not (s1 = s2)
    End Operator
End Class
