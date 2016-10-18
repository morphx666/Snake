Public Class Runtime
#Region "Runtime Detection"
    Public Enum Platforms
        Windows
        Linux
        Mac
        ARMSoft
        ARMHard
    End Enum
    Private Shared mPlatform As Platforms?
    Private Shared mPathSeparator As Char?

    Public Shared ReadOnly Property Platform As Platforms
        Get
            If mPlatform Is Nothing Then DetectPlatform()
            Return mPlatform
        End Get
    End Property

    Public Shared ReadOnly Property PathSeparator As Char
        Get
            If mPathSeparator Is Nothing Then DetectPlatform()
            Return mPathSeparator
        End Get
    End Property

    Private Shared Sub DetectPlatform()
        mPathSeparator = "/"
        Select Case Environment.OSVersion.Platform
            Case PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE, PlatformID.Xbox
                mPlatform = Platforms.Windows
                mPathSeparator = "\"
            Case PlatformID.MacOSX
                mPlatform = Platforms.Mac
            Case Else
                If IO.Directory.Exists("/Applications") AndAlso
                        IO.Directory.Exists("/System") AndAlso
                        IO.Directory.Exists("/Users") AndAlso
                        IO.Directory.Exists("/Volumes") Then
                    mPlatform = Platforms.Mac
                Else
                    mPlatform = Platforms.Linux

                    Dim distro As String = GetLinuxDistro().ToLower()
                    If distro.Contains("raspberrypi") Then
                        mPlatform = Platforms.ARMSoft
                        If distro.Contains("armv7l") Then mPlatform = Platforms.ARMHard
                    End If
                End If
        End Select
    End Sub

    Private Shared Function GetLinuxDistro() As String
        Dim lines As New List(Of String)

        Dim catProcess As New Process()
        With catProcess.StartInfo
            .FileName = "uname"
            .Arguments = "-a"

            .CreateNoWindow = True
            .UseShellExecute = False
            .RedirectStandardOutput = True
            .RedirectStandardError = True
            .RedirectStandardInput = False
        End With
        AddHandler catProcess.OutputDataReceived, Sub(sender As Object, e As Diagnostics.DataReceivedEventArgs)
                                                      lines.Add(e.Data)
                                                  End Sub

        Try
            catProcess.Start()
            catProcess.BeginOutputReadLine()
            catProcess.WaitForExit()
            catProcess.Dispose()

            Threading.Thread.Sleep(500)

            If lines.Count > 0 Then
                Return lines.First()
            Else
                Return "Unknown"
            End If
        Catch ex As Exception
            Return Environment.OSVersion.Platform.ToString()
        End Try
    End Function
#End Region
End Class
