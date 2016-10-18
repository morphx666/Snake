Imports System.Runtime.InteropServices

Public Class Win32Native
    Private Const SWP_NOZORDER As Integer = &H4
    Private Const SWP_NOACTIVATE As Integer = &H10
    Private Const SW_MAXIMIZE As Integer = &H3

    <StructLayout(LayoutKind.Sequential)>
    Private Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    <DllImport("kernel32")>
    Private Shared Function GetConsoleWindow() As IntPtr
    End Function

    <DllImport("user32")>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr,
        x As Integer, y As Integer, cx As Integer, cy As Integer, flags As Integer) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowRect(ByVal hWnd As IntPtr, ByRef lpRect As RECT) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Shared Function ShowWindow(hWnd As IntPtr, cmdShow As Integer) As Boolean
    End Function

    Public Shared Sub SetConsoleWindowPosition(x As Integer, y As Integer)
        Dim r As RECT
        GetWindowRect(GetConsoleWindow(), r)

        SetWindowPos(GetConsoleWindow(), IntPtr.Zero,
                     x, y,
                     r.Left + r.Right, r.Top + r.Bottom,
                     SWP_NOZORDER Or SWP_NOACTIVATE)
    End Sub

    Public Shared Sub MaximizeConsoleWindow()
        If Runtime.Platform = Runtime.Platforms.Windows Then
            Dim p As Process = Process.GetCurrentProcess()
            ShowWindow(p.MainWindowHandle, SW_MAXIMIZE)
        End If
    End Sub
End Class
