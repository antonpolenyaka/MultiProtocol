
Imports System.IO.Ports
Imports System.Threading
Imports System.Windows


Public Class Form1


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Button3.BackColor = Color.Red
        Button3.Text = "Buses Detenidos"



    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs)


    End Sub

    'Add Bus
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim PortConfiguration As Miscelaneous.MySerialPort.SerialPortConfiguration
        'Configure bus serial port
        PortConfiguration.PortBaudRate = Miscelaneous.MySerialPort.DataRates.BR9600
        PortConfiguration.PortDataBits = Miscelaneous.MySerialPort.DataBits.DB8
        PortConfiguration.PortParity = Parity.Even
        PortConfiguration.PortStopBits = StopBits.One
        PortConfiguration.ReadTimeout_Msec = 300
        PortConfiguration.PortName = "COM31"
        PortConfiguration.Interval_Msec = 250
        Dim TemporalBus As New SimulatedBus(SimulatedBus.BusType.Procome, PortConfiguration)
        Dim BusAddition As New AddABus(TemporalBus)
        BusAddition.ShowDialog(Me)
        If BusAddition.CancelButtonPressed = True Then
            'Nothing to add 
            Exit Sub
        End If
        MyBuses.Add(TemporalBus)
        RefreshBusList()
    End Sub

    'Refresh the list of Buses configured
    Private Sub RefreshBusList()
        ListBox1.Items.Clear()
        For Each item In MyBuses
            ListBox1.Items.Add("Bus en puerto: " & item.BusSerialPort.ComPortName & "  Tecnologia: " & [Enum].GetName(GetType(SimulatedBus.BusType), item.BusTechnology))
        Next
    End Sub
    'Change running state
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If MyBuses.Count <= 0 Then
            DisableBuses()
            Exit Sub
        End If
        If AreBusesRunning() = False Then
            EnableBuses()
        Else
            DisableBuses()
        End If
    End Sub

    Private Function AreBusesRunning() As Boolean
        If Button3.BackColor = Color.Red Then
            Return False
        End If
        Return True
    End Function

    Private Sub DisableBuses()
        Button3.BackColor = Color.Red
        Button3.Text = "Buses Detenidos"
        'Disconnect all buses
        Try
            For Each item In MyBuses
                item.DisconnectBus()
            Next

        Catch ex As Exception
        End Try
    End Sub
    Private Sub EnableBuses()
        Button3.BackColor = Color.Green
        Button3.Text = "Buses Activos"
        Try
            'Connect all busses
            For Each item In MyBuses
                item.ConnectBus()
            Next
        Catch ex As Exception
        End Try
    End Sub

End Class







