

Imports System.IO.Ports
Imports System.Threading


Public Class AddABus

    'Variables
    Private TemporalSlaveArray As New List(Of ISlave)
    Private Cancelling As Boolean
    Private TemporalBus As SimulatedBus

    'Properties
    ReadOnly Property CancelButtonPressed As Boolean
        Get
            Return Cancelling
        End Get
    End Property

    Sub New(ByRef Obj As SimulatedBus)
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()
        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        TemporalBus = Obj
        TemporalBus.DisconnectBus()
    End Sub

    'Methods
    Private Sub AddABus_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim AvailablePorts() As String = SerialPort.GetPortNames()
        Dim TemporalPortNumber As New List(Of Integer)
        Dim Counter2 As Integer = 0

        Cancelling = False
        ListBox1.Items.Clear()
        ComboBox1.Items.Clear()
        For Each port In AvailablePorts
            TemporalPortNumber.Add(Val(Mid(port, 4, port.Length)))
        Next
        TemporalPortNumber.Sort()
        For Each portnumber In TemporalPortNumber
            ComboBox1.Items.Add("COM" & portnumber.ToString)
        Next
        ComboBox1.SelectedIndex = 0
        ComboBox2.Items.Clear()
        For Each i In [Enum].GetValues(GetType(SimulatedBus.BusType))
            ComboBox2.Items.Add(i)
        Next
        ComboBox2.SelectedIndex = 0
        ComboBox6.Items.Clear()
        For Each i In [Enum].GetValues(GetType(System.IO.Ports.StopBits))
            ComboBox6.Items.Add(i)
        Next
        ComboBox6.SelectedIndex = 0
        ComboBox3.Items.Clear()
        For Each i In [Enum].GetValues(GetType(System.IO.Ports.Parity))
            ComboBox3.Items.Add(i)
        Next
        ComboBox3.SelectedIndex = 0
        ComboBox5.Items.Clear()
        For Each i In [Enum].GetValues(GetType(Miscelaneous.MySerialPort.DataRates))
            ComboBox5.Items.Add(i)
        Next
        ComboBox5.SelectedIndex = 0
        ComboBox4.Items.Clear()
        For Each i In [Enum].GetValues(GetType(Miscelaneous.MySerialPort.DataBits))
            ComboBox4.Items.Add(i)
        Next
        ComboBox4.SelectedIndex = 0
    End Sub
    'Agregar Bus configurado
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If IsNumeric(TextBox1.Text) = False Then
            TemporalBus.BusSerialPort.InterByteTimeout = Miscelaneous.MySerialPort.Default_Interval_Msec
        Else
            TemporalBus.BusSerialPort.InterByteTimeout = Val(TextBox1.Text)
        End If
        TemporalBus.BusSerialPort.ComBaudRate = ComboBox5.SelectedItem
        TemporalBus.BusSerialPort.ComDataBits = ComboBox4.SelectedItem
        TemporalBus.BusSerialPort.ComParity = ComboBox3.SelectedItem
        TemporalBus.BusSerialPort.ComStopBits = ComboBox6.SelectedItem
        TemporalBus.BusSerialPort.ComPortName = ComboBox1.SelectedItem
        TemporalBus.BusTechnology = ComboBox2.SelectedItem
        Select Case ComboBox2.SelectedItem
            Case SimulatedBus.BusType.Procome
                Dim TemporalProcomeSalve As ProcomeSlave
                For Each slave In TemporalSlaveArray
                    TemporalProcomeSalve = slave
                    TemporalBus.AddSlaveToBus(TemporalProcomeSalve.MySlaveAddress, TemporalProcomeSalve.SlaveModel)
                Next
            Case Else
                'Not supported technology at the moment
        End Select
        Me.Close()
    End Sub
    'Agregar Esclavo
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Select Case ComboBox2.SelectedItem
            Case SimulatedBus.BusType.Procome
                Dim TemporalSlave As New ProcomeSlave(0, ProcomeSlave.ProcomeSlaveModel.ekorRPG)
                Dim NuevoEsclavo As New AddProcomeSlave(TemporalSlave)
                NuevoEsclavo.ShowDialog(Me)
                If NuevoEsclavo.CancelButtonPressed = True Then
                    'Nothing to do
                    Exit Sub
                End If
                TemporalSlaveArray.Add(TemporalSlave)
                NuevoEsclavo.Dispose()
            Case Else
                'Not supported technology at the moment
        End Select
        RefreshSlaveList()
    End Sub
    'Technology change in the bus!!!
    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged
        ListBox1.Items.Clear()
        TemporalSlaveArray.Clear()
    End Sub

    Private Sub RefreshSlaveList()
        ListBox1.Items.Clear()
        For Each item In TemporalSlaveArray
            Select Case item.SlaveProtocolType
                Case ISlave.SlaveType.ProcomeSlave
                    Dim TemporalSlave As ProcomeSlave
                    TemporalSlave = item
                    ListBox1.Items.Add("Esclavo: " & TemporalSlave.MySlaveAddress & " Modelo: " & [Enum].GetName(GetType(ProcomeSlave.ProcomeSlaveModel), TemporalSlave.SlaveModel))
                Case Else
                    'Nothing to print
            End Select
        Next
    End Sub
    'Cancel
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Cancelling = True
        Me.Close()
    End Sub


End Class