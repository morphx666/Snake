Module Messages
    Private looseMessages As New Dictionary(Of LooseReason, String()) From {
                                                                {LooseReason.EatOwnTail, {
                                                                                            "You're not supposed to eat yourself!",
                                                                                            "So snake tastes like chicken?",
                                                                                            "We give you food for a reason, you cannibal!"
                                                                                         }
                                                                },
                                                                {LooseReason.HitWall, {
                                                                                            "Don't hit the walls!",
                                                                                            "That was a wall... needing glasses?",
                                                                                            "So you though you could walk through walls, eh?",
                                                                                            "Who cares who built the wall, just avoid it!",
                                                                                            "And now your snake has a headache!"
                                                                                         }
                                                                },
                                                                {LooseReason.HitMaze, {
                                                                                            "Don't hit the maze walls!",
                                                                                            "That was a wall... needing glasses?",
                                                                                            "Who cares who built the wall, just avoid it!",
                                                                                            "And now your snake has a headache!",
                                                                                            "So you though you could walk through walls, eh?"
                                                                                         }
                                                                },
                                                                {LooseReason.Hunger, {
                                                                                            "You just starved your snake to death!",
                                                                                            "Eat or you'll die! Wait...",
                                                                                            "Starvation kills millions of kids... and snakes!" '                                                                                 <--- does it offend u? o'cmon! don't be so fckng politically correct!
                                                                                         }
                                                                }
                                                            }

    Public Function GetMessage(reason As LooseReason, rnd As Random) As String
        If Not looseMessages.ContainsKey(reason) Then Return ""

        Dim r As New Random()
        Dim msgs() As String = looseMessages(reason)

        Return msgs(r.Next(0, msgs.Length))
    End Function
End Module
