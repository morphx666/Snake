Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports Snake

Public Class TextRenderer
    Private Class CharType
        Implements IEquatable(Of CharType)

        Public ReadOnly Property Character As Char
        Public ReadOnly Property FontFamily As String
        Public ReadOnly Property FontSize As Single
        Public ReadOnly Property CharSize As Size

        Private hashCode As Integer

        Public Sub New(character As Char, fontFamily As String, fontSize As Single)
            Me.Character = character
            Me.FontFamily = fontFamily
            Me.FontSize = fontSize

            Using sha As New SHA256Managed()
                Dim key As String = String.Format("{0}{1}{2}", character, fontFamily, fontSize)
                Dim b() As Byte = System.Text.Encoding.Unicode.GetBytes(key)
                Dim r() As Byte = sha.ComputeHash(b)

                hashCode = r.ToList().Sum(Function(k) k)
            End Using

            Using f As New Font(fontFamily, fontSize, FontStyle.Regular)
                Me.CharSize = New Size(f.Height - 1, f.Height - 1)
            End Using
        End Sub

        Public Shared Operator =(ct1 As CharType, ct2 As CharType) As Boolean
            Return ct1.Character = ct2.Character AndAlso
                    ct1.FontFamily = ct2.FontFamily AndAlso
                    ct1.FontSize = ct2.FontSize
        End Operator

        Public Shared Operator <>(ct1 As CharType, ct2 As CharType) As Boolean
            Return Not ct1 = ct2
        End Operator

        Public Overrides Function GetHashCode() As Integer
            Return hashCode
        End Function

        Public Shadows Function Equals(other As CharType) As Boolean Implements IEquatable(Of CharType).Equals
            Return Me = other
        End Function
    End Class

    Private mDefaultFont As String
    Private alphabet As New Dictionary(Of CharType, String)

    Public Sub New(Optional defaultFont As String = "Consolas")
        mDefaultFont = defaultFont
    End Sub

    Public ReadOnly Property DefaultFont As String
        Get
            Return mDefaultFont
        End Get
    End Property

    Public Function MeassureText(value As String,
                                 Optional fontFamily As String = "",
                                 Optional fontSize As Single = 8,
                                 Optional kerning As Double = 0.6) As Size
        If fontFamily = "" Then fontFamily = mDefaultFont
        CreateBitmaps(value(0), fontFamily, fontSize)
        Dim ct As New CharType(value(0), fontFamily, fontSize)

        ' Apparently, this formula no longer works on Windows 10.
        ' The +0.6 adjust it for the Consolas' Windows 10 font.
        Return New Size(ct.CharSize.Width * kerning * (value.Length + 0.6), ct.CharSize.Height)
    End Function

    Public Sub Write(value As String, x As Integer, y As Integer, fc As ConsoleColor,
                     Optional fontFamily As String = "",
                     Optional fontSize As Single = 8,
                     Optional f As String = "█",
                     Optional kerning As Double = 0.6)
        If fontFamily = "" Then fontFamily = mDefaultFont
        CreateBitmaps(value, fontFamily, fontSize)
        Console.ForegroundColor = fc
        Try
            For i As Integer = 0 To value.Length - 1
                Dim ct As New CharType(value(i), fontFamily, fontSize)
                Dim s As String = alphabet(ct)
                For y1 As Integer = 0 To ct.CharSize.Height - 1
                    Console.CursorTop = y1 + y
                    Console.CursorLeft = x
                    For x1 As Integer = 0 To ct.CharSize.Width - 1
                        If s(x1 + y1 * ct.CharSize.Width) = "1" Then
                            Console.Write(f)
                        Else
                            Console.CursorLeft += 1
                        End If
                    Next
                Next
                x += ct.CharSize.Width * kerning
            Next
        Catch ex As Exception
        End Try
    End Sub

    Private Sub CreateBitmaps(value As String, fontFamily As String, fontSize As Single)
        Dim p As New Point(0, 0)
        Dim c As Char
        Dim charSize As Size

        Using b As New SolidBrush(Color.White)
            Using f As New Font(fontFamily, fontSize, FontStyle.Regular)
                charSize = New Size(f.Height, f.Height)
                charSize.Width -= 1
                charSize.Height -= 1

                Using bmp As New Bitmap(charSize.Width, charSize.Height, PixelFormat.Format16bppRgb555)
                    Dim r As New Rectangle(0, 0, bmp.Width, bmp.Height)
                    Using g As Graphics = Graphics.FromImage(bmp)
                        For i As Integer = 0 To value.Length - 1
                            c = value(i)
                            Dim ct As New CharType(c, fontFamily, fontSize)
                            If alphabet.ContainsKey(ct) Then Continue For
                            g.Clear(Color.Black)
                            g.DrawString(c, f, b, p)

                            'bmp.Save("d:\Users\Xavier\Desktop\tmp.png", ImageFormat.Png)

                            Dim sourceData = bmp.LockBits(r, ImageLockMode.ReadOnly, bmp.PixelFormat)
                            Dim sourcePointer = sourceData.Scan0
                            Dim sourceStride = sourceData.Stride
                            Dim bytesPerPixel = sourceStride \ bmp.Width
                            Dim offset As Integer

                            Dim bmpStr As String = ""
                            For y As Integer = 0 To bmp.Height - 1
                                For x As Integer = 0 To bmp.Width - 1
                                    offset = x * bytesPerPixel + y * sourceStride

                                    If Marshal.ReadByte(sourcePointer, offset + 0) = 0 Then
                                        bmpStr += "0"
                                    Else
                                        bmpStr += "1"
                                    End If
                                Next
                                bmpStr += vbCrLf
                            Next
                            alphabet.Add(ct, bmpStr.Replace(vbCrLf, ""))

                            bmp.UnlockBits(sourceData)
                        Next
                    End Using
                End Using
            End Using
        End Using
    End Sub
End Class
