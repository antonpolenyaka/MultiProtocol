
Imports System.IO.Ports
Imports System.Threading


Public Class SimulatedBus

    'Enumerados
    Public Enum BusType
        ModBus_Rtu = 0
        Procome = 1
    End Enum

    'Constantes
    Public Const MaxNumberOfSlavesPerBus As Integer = 15

    'Variables
    Public WithEvents BusSerialPort As Miscelaneous.MySerialPort
    Private _BusEnabled As Boolean
    Private _BusType As BusType
    Private SlaveArray As New List(Of ISlave)

    'Propiedades
    ReadOnly Property IsBusEnabled As Boolean
        Get
            Return _BusEnabled
        End Get
    End Property
    ReadOnly Property GetNumberOfSlavesInBus As Integer
        Get
            Return SlaveArray.Count
        End Get
    End Property
    Property BusTechnology As BusType
        Get
            Return _BusType
        End Get
        Set(value As BusType)
            _BusType = value
        End Set
    End Property

    'Eventos
    Public Event PacketReceived(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer)

    'Constructor
    Public Sub New(ByVal TypeOfBus As BusType, ByVal PortConfiguration As Miscelaneous.MySerialPort.SerialPortConfiguration)
        BusSerialPort = New Miscelaneous.MySerialPort(PortConfiguration)
        _BusType = TypeOfBus
        _BusEnabled = False
        SlaveArray.Clear()
    End Sub

    'Metodos
    Public Function ConnectBus() As Boolean
        _BusEnabled = BusSerialPort.Connect()
        Return _BusEnabled
    End Function

    Public Sub DisconnectBus()
        BusSerialPort.Disconnect()
        _BusEnabled = False
    End Sub

    Public Function AddSlaveToBus(ByVal SlaveAddress As Integer, ByVal SlaveModel As Integer) As Boolean
        Try
            Select Case _BusType
                Case BusType.Procome
                    If SlaveArray.Count > MaxNumberOfSlavesPerBus Then
                        'Maximum number of slaves per bus reached
                        Return False
                    End If
                    For Each item In SlaveArray
                        If item.MySlaveAddress = SlaveAddress Then
                            'Address duplicated
                            Return False
                        End If
                    Next
                    'Add procome slave
                    SlaveArray.Add(New ProcomeSlave(SlaveAddress, CType(SlaveModel, ProcomeSlave.ProcomeSlaveModel)))
                    Return True
                Case Else


            End Select
        Catch ex As Exception

        End Try
        Return False
    End Function

    'Manejadores de Eventos
    Public Sub NewPacketRec(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer) Handles BusSerialPort.PacketReceived
        Dim TemporalSlaveAddress As Integer = 0

        Select Case _BusType
            Case BusType.Procome
                TemporalSlaveAddress = ProcomeSlave.Get_SlaveAddress(PacketBuffer, PacketLen)
                For Each Slave In SlaveArray
                    If Slave.MySlaveAddress = TemporalSlaveAddress Then
                        'Process packet
                        Slave.ProcessIncomingPacket(PacketBuffer, PacketLen, BusSerialPort)
                    End If
                Next
            Case Else
                'Invalid type detected...
        End Select
        'Notify packet event so external program can print message in a debug box, for example....
        RaiseEvent PacketReceived(PacketBuffer, PacketLen)
    End Sub


End Class

'Slave Interface
Public Interface ISlave
    'Enumerados
    Enum SlaveType
        ProcomeSlave = 0
    End Enum

    'Propiedades
    ReadOnly Property IsSlaveConnected As Boolean
    Property MySlaveAddress As Integer
    Property SlaveProtocolType As SlaveType

    'Metodos
    Sub ProcessIncomingPacket(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer, MyserialLink As Miscelaneous.MySerialPort)
    Sub ForceSlaveDown()
    Sub ForceSlaveUp()
End Interface







