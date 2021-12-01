Imports System
Imports System.Timers
Imports System.IO.Ports
Imports System.Threading


Namespace Miscelaneous

    'Timer handler class
    Public Class TimerInClass

        'Constantes
        Private Const DefaultInterval_Msec As Double = 1

        'Variables
        Public WithEvents TheTimer As System.Timers.Timer

        'Eventos
        Public Event TimerTick()

        'Propiedades
        Property Interval_Msec As Double
            Set(value As Double)
                If value > 0 Then
                    TheTimer.Interval = value
                Else
                    TheTimer.Interval = DefaultInterval_Msec
                End If
            End Set
            Get
                Return TheTimer.Interval
            End Get
        End Property

        'Constructor
        Public Sub New()
            TheTimer = New System.Timers.Timer()
            TheTimer.AutoReset = True
            TheTimer.Interval = DefaultInterval_Msec
            TheTimer.Enabled = False
        End Sub

        'Metodos
        Public Sub StartTimer()
            TheTimer.Enabled = True
        End Sub

        Public Sub StopTimer()
            TheTimer.Enabled = False
        End Sub

        'Manejadores de eventos
        Public Sub TheTimer_Elapsed(ByVal sender As Object, ByVal e As ElapsedEventArgs) Handles TheTimer.Elapsed
            RaiseEvent TimerTick()
        End Sub
    End Class

    'Serial port handler class
    Public Class MySerialPort

        'Enums
        Public Enum DataRates
            BR1200 = 1200
            BR9600 = 9600
            BR115200 = 115200
        End Enum
        Public Enum DataBits
            DB8 = 8
            DB9 = 9
        End Enum

        'Estructuras
        Public Structure SerialPortConfiguration
            Public PortName As String
            Public PortBaudRate As DataRates
            Public PortDataBits As DataBits
            Public PortStopBits As System.IO.Ports.StopBits
            Public PortParity As System.IO.Ports.Parity
            Public ReadTimeout_Msec As Integer
            Public Interval_Msec As Double
        End Structure

        'Constantes
        Public Const Default_Interval_Msec As Double = 250
        Public Const Default_ReadTimeout_Msec As Integer = 300
        Public Const Default_PortName As String = "COM1"
        Public Const Default_StopBits As System.IO.Ports.StopBits = StopBits.One
        Public Const Default_DataBits As DataBits = DataBits.DB8
        Public Const Default_Parity As System.IO.Ports.Parity = Parity.None
        Public Const Default_BaudRate As Integer = DataRates.BR9600
        Public Const MaxRxPacketSize As Integer = 2000

        'Variables
        Private WithEvents comPort As SerialPort
        Private _MyPortConfig As SerialPortConfiguration
        Private WithEvents TimerIn As Miscelaneous.TimerInClass
        Private _LasPacketRead(MaxRxPacketSize) As Byte
        Private _LastPacketLen As Integer

        'Eventos
        Public Event PacketReceived(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer)

        'Propiedades
        Property ComBaudRate As DataRates
            Get
                Return _MyPortConfig.PortBaudRate
            End Get
            Set(value As DataRates)
                _MyPortConfig.PortBaudRate = value
            End Set
        End Property
        Property ComStopBits As System.IO.Ports.StopBits
            Get
                Return _MyPortConfig.PortStopBits
            End Get
            Set(value As System.IO.Ports.StopBits)
                _MyPortConfig.PortStopBits = value
            End Set
        End Property

        Property ComParity As System.IO.Ports.Parity
            Get
                Return _MyPortConfig.PortParity
            End Get
            Set(value As System.IO.Ports.Parity)
                _MyPortConfig.PortParity = value
            End Set
        End Property

        Property ComDataBits As DataBits
            Get
                Return _MyPortConfig.PortDataBits
            End Get
            Set(value As DataBits)
                _MyPortConfig.PortDataBits = value
            End Set
        End Property

        Property ComIntervalMsec As Double
            Get
                Return _MyPortConfig.Interval_Msec
            End Get
            Set(value As Double)
                _MyPortConfig.Interval_Msec = value
            End Set
        End Property

        Property ComPortName As String
            Get
                Return _MyPortConfig.PortName
            End Get
            Set(value As String)
                _MyPortConfig.PortName = value
            End Set
        End Property

        Property InterByteTimeout As Double
            Get
                Return _MyPortConfig.Interval_Msec
            End Get
            Set(value As Double)
                _MyPortConfig.Interval_Msec = value
            End Set
        End Property

        'Constructor
        Public Sub New(ByVal PortConfiguration As MySerialPort.SerialPortConfiguration)
            MyBase.New
            'Create serial port instance
            comPort = New SerialPort
            _MyPortConfig = PortConfiguration
            'Create auxiliary timer
            TimerIn = New Miscelaneous.TimerInClass()
        End Sub

        'Metodos
        Public Function Connect() As Boolean
            Disconnect()
            Try
                comPort.PortName = _MyPortConfig.PortName
                comPort.BaudRate = _MyPortConfig.PortBaudRate
                comPort.DataBits = _MyPortConfig.PortDataBits
                comPort.StopBits = _MyPortConfig.PortStopBits
                comPort.Parity = _MyPortConfig.PortParity
                comPort.ReadTimeout = _MyPortConfig.ReadTimeout_Msec
                comPort.Open()
                FlushPort()
                TimerIn.StopTimer()
                TimerIn.Interval_Msec = _MyPortConfig.Interval_Msec
            Catch ex As Exception
                Return False
            End Try
            Return True
        End Function

        Public Sub Disconnect()
            Try
                comPort.Close()
                comPort.Dispose()
            Catch ex As Exception
            End Try
            Threading.Thread.Sleep(500)
        End Sub

        Public Sub FlushPort()
            comPort.DiscardInBuffer()
            comPort.DiscardOutBuffer()
        End Sub

        Public Sub TransmitData(ByRef Buffer() As Byte, ByVal BufferOffset As Integer, ByVal BufferLen As Integer)
            Try
                FlushPort()
                comPort.Write(Buffer, BufferOffset, BufferLen)
            Catch ex As Exception

            End Try
        End Sub

        Private Sub MyPort_DataReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialDataReceivedEventArgs) Handles comPort.DataReceived
            Dim bytecount As Integer = comPort.BytesToRead
            Dim bytesread As Integer

            TimerIn.StopTimer()
            If bytecount > 0 Then
                Try
                    bytesread = comPort.Read(_LasPacketRead, _LastPacketLen, bytecount)
                    _LastPacketLen = _LastPacketLen + bytesread
                    TimerIn.StartTimer()
                Catch ex As Exception
                End Try
            End If
        End Sub

        Private Sub Mytimer_Tick() Handles TimerIn.TimerTick
            Dim Counter As Integer

            'Stop timer 
            TimerIn.StopTimer()
            'Signal event
            RaiseEvent PacketReceived(_LasPacketRead, _LastPacketLen)
            'Allow other process yield
            Thread.Sleep(150)
            'Prepare buffer for next packet
            For Counter = 0 To _LastPacketLen
                _LasPacketRead(Counter) = 0
            Next
            _LastPacketLen = 0
        End Sub
    End Class


End Namespace

