



Public Class ProcomeSlave
    Implements ISlave

    'Enumerados
    Private Enum SlaveState
        SlaveDisconnected = 0
        SlaveInit = 1
        SlaveIdle = 2
        SlavePeriodic = 3


    End Enum
    Private Enum ProcomePacketType
        ProcomeFixPacket = &H10
        ProcomeVariablePacket = &H68
    End Enum
    Public Enum ProcomeSlaveModel
        ekorRPI = 0
        ekorRPG = 1
        ekorRPA = 2
    End Enum
    Public Enum ProcomeAsduCodes
        MedidasYCambios = &H64
        EstadosDigitales = &H67
        AsduInvalido = 0
    End Enum

    'Constantes
    Private Const DefaultSlaveAddress As Integer = 1
    Private Const ProcomeEndOfFrame As Byte = &H16
    Private Const MaxProcomePcktSize As Integer = 2000
    Private Const FixedSizePacket As Integer = 5
    Private Const VariableSizePacket As Integer = &H68

    'Variables
    Private _SlaveState As SlaveState
    Private _IsSlaveUp As Boolean
    Private _SlaveAddress As Integer
    Private _SlaveProtocolType As ISlave.SlaveType
    Private _SlaveModel As ProcomeSlaveModel
    Private SubpacketCounter As Integer
    Private Last_CACByte As Integer

    'Propiedades
    ReadOnly Property IsSlaveConnected As Boolean Implements ISlave.IsSlaveConnected
        Get
            Return _IsSlaveUp
        End Get
    End Property
    Property MySlaveAddress As Integer Implements ISlave.MySlaveAddress
        Get
            Return _SlaveAddress
        End Get
        Set(ByVal value As Integer)
            _SlaveAddress = value
        End Set
    End Property
    Property SlaveProtocolType As ISlave.SlaveType Implements ISlave.SlaveProtocolType
        Get
            Return _SlaveProtocolType
        End Get
        Set(ByVal value As ISlave.SlaveType)
            _SlaveProtocolType = value
        End Set
    End Property
    Property SlaveModel As ProcomeSlaveModel
        Get
            Return _SlaveModel
        End Get
        Set(ByVal value As ProcomeSlaveModel)
            _SlaveModel = value
        End Set
    End Property
    'Constructor
    Public Sub New(ByVal SlaveAddress As Integer, Model As ProcomeSlaveModel)
        'Slave is by default disconnected
        _SlaveState = SlaveState.SlaveInit
        _SlaveProtocolType = ISlave.SlaveType.ProcomeSlave
        _SlaveModel = Model
        SubpacketCounter = 0
        If SlaveAddress > 0 Then
            _SlaveAddress = SlaveAddress
        Else
            _SlaveAddress = DefaultSlaveAddress
        End If

    End Sub

    Public Sub ProcessPacket(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer, ByVal MyserialLink As Miscelaneous.MySerialPort) Implements ISlave.ProcessIncomingPacket

        'Verify that procome packet is valid
        If IsPacketCrcOK(PacketBuffer, PacketLen) = True Then
            Select Case _SlaveState

                ' ***********************************************
                ' ***********************************************
                ' ***********************************************
                ' DISCONNECTED STATE
                ' ***********************************************
                ' ***********************************************
                ' ***********************************************
                Case SlaveState.SlaveDisconnected
                'I am turned off, ignore all traffic


                ' ***********************************************
                ' ***********************************************
                ' ***********************************************
                ' INITIALIZATION STATE
                ' ***********************************************
                ' ***********************************************
                ' ***********************************************
                Case SlaveState.SlaveInit
                    Select Case SubpacketCounter
                        Case 0
                            'Check if I receive starting packet
                            If CheckValidInitPacket1(PacketBuffer, PacketLen) = True Then
                                'Prepare next packet
                                SubpacketCounter = 1
                                Last_CACByte = &H20
                                'Send Packet 1
                                SendInitPacket1(MyserialLink)
                            Else
                                'Stay here 
                                SubpacketCounter = 0
                            End If
                        Case 1
                            'Check if I receive starting packet
                            If CheckValidInitPacket2(PacketBuffer, PacketLen) = True Then
                                'Prepare next packet
                                SubpacketCounter = 2
                                Last_CACByte = GetCACByte(PacketBuffer)
                                'Send Packet 1
                                SendInitPacket2(MyserialLink)
                                _SlaveState = SlaveState.SlavePeriodic
                            Else
                                'Reset subprocess  
                                SubpacketCounter = 0
                            End If
                        Case Else
                            'reset subprocess in this state
                            SubpacketCounter = 0
                    End Select

                    ' ***********************************************
                    ' ***********************************************
                    ' ***********************************************
                    ' PERIODIC STATE
                    ' ***********************************************
                    ' ***********************************************
                    ' ***********************************************
                Case SlaveState.SlavePeriodic
                    Dim ReceivedAsdu = GetAsduCode(PacketBuffer)
                    'Read last CAC byte
                    Last_CACByte = GetCACByte(PacketBuffer)
                    'Respond to packet requested
                    Select Case ReceivedAsdu
                        Case ProcomeAsduCodes.EstadosDigitales
                            AssemblyDigitalStatesPacket(MyserialLink)
                        Case ProcomeAsduCodes.MedidasYCambios
                            AssemblyMedidasandChangesPacket(MyserialLink)
                        Case Else
                            'Invalid asdu ignore packet
                    End Select


                    ' ***********************************************
                    ' ***********************************************
                    ' ***********************************************
                    ' INVALID STATE
                    ' ***********************************************
                    ' ***********************************************
                    ' ***********************************************
                Case Else
                    'Jump to Initialization
                    _SlaveState = SlaveState.SlaveInit
            End Select
        Else
            'Ignore packet
        End If

    End Sub

    Private Sub SendInitPacket2(ByVal MyserialLink As Miscelaneous.MySerialPort)
        Dim SecondPacketTemplate() As Byte = {&H68, &H16, &H16, &H68, &H8, &H2, &H5, &H81, &H3, &H2, &HFE, &H2, &H10, &H33, &H42, &H32, &H30, &H30, &H32, &H38, &H20, &H1, &H0, &H0, &H0, &H3, &H3A, &H16}

        'Fix Slave address
        SecondPacketTemplate(5) = _SlaveAddress
        SecondPacketTemplate(9) = _SlaveAddress
        'Fix Crc
        SecondPacketTemplate(26) = CalculateProcomeCrc(SecondPacketTemplate, 4, 22)
        'Send packet
        MyserialLink.TransmitData(SecondPacketTemplate, 0, 28)
    End Sub
    Private Function CheckValidInitPacket2(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer) As Boolean
        If PacketBuffer(0) = ProcomePacketType.ProcomeFixPacket And PacketBuffer(1) = &H7A And PacketBuffer(2) = _SlaveAddress Then
            Return True
        End If
        Return False
    End Function
    Private Sub SendInitPacket1(ByVal MyserialLink As Miscelaneous.MySerialPort)
        Dim PacketBufferToTx(MaxProcomePcktSize) As Byte

        PacketBufferToTx(0) = ProcomePacketType.ProcomeFixPacket
        PacketBufferToTx(1) = Last_CACByte
        PacketBufferToTx(2) = _SlaveAddress
        'Append crc
        PacketBufferToTx(3) = CalculateProcomeCrc(PacketBufferToTx, 1, 2)
        PacketBufferToTx(4) = ProcomeEndOfFrame
        'Send packet
        MyserialLink.TransmitData(PacketBufferToTx, 0, FixedSizePacket)
    End Sub
    Private Function CheckValidInitPacket1(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer) As Boolean
        If PacketBuffer(0) = ProcomePacketType.ProcomeFixPacket And PacketBuffer(1) = &H47 And PacketBuffer(2) = _SlaveAddress Then
            Return True
        End If
        Return False
    End Function
    Public Sub ForceSlaveDown() Implements ISlave.ForceSlaveDown
        _IsSlaveUp = False
        _SlaveState = SlaveState.SlaveDisconnected
    End Sub
    Public Sub ForceSlaveUp() Implements ISlave.ForceSlaveUp
        _IsSlaveUp = False
        _SlaveState = SlaveState.SlaveInit
    End Sub
    Public Shared Function Get_SlaveAddress(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer) As Integer
        If PacketBuffer(0) = ProcomePacketType.ProcomeFixPacket Then
            Return CInt(PacketBuffer(2))
        Else
            Return CInt(PacketBuffer(5))
        End If
        Return 0
    End Function
    Private Function IsPacketCrcOK(ByVal PacketBuffer() As Byte, ByVal PacketLen As Integer) As Boolean
        Dim Crc As Integer

        If PacketBuffer(0) = ProcomePacketType.ProcomeFixPacket Then
            'Additional check here, not crc but helps!
            If PacketBuffer(PacketLen - 1) <> ProcomeEndOfFrame Then
                Return False
            End If
            Crc = CalculateProcomeCrc(PacketBuffer, 1, 2)
            If PacketBuffer(3) = Crc Then
                Return True
            End If
        Else
            'Additional check here, not crc but helps!
            If PacketBuffer(1) <> PacketBuffer(2) Then
                Return False
            End If
            If PacketBuffer(3) <> ProcomePacketType.ProcomeVariablePacket Then
                Return False
            End If
            If PacketBuffer(PacketLen - 1) <> ProcomeEndOfFrame Then
                Return False
            End If
            Crc = CalculateProcomeCrc(PacketBuffer, 4, PacketLen - 6)
            If PacketBuffer(PacketLen - 2) = Crc Then
                Return True
            End If
        End If
        Return False
    End Function
    Private Function CalculateProcomeCrc(ByVal PacketBuffer() As Byte, ByVal offset As Integer, ByVal PacketLen As Integer) As Byte
        Dim Crc As Integer = 0
        Dim Counter As Integer

        For Counter = 0 To PacketLen - 1
            Crc = Crc + PacketBuffer(offset + Counter)
        Next
        Return CByte(Crc And &HFF)
    End Function
    Private Function GetAsduCode(ByVal PacketBuffer() As Byte) As Byte
        If PacketBuffer(0) = VariableSizePacket Then
            Return PacketBuffer(6)
        End If
        Return ProcomeAsduCodes.AsduInvalido
    End Function
    Private Sub AssemblyDigitalStatesPacket(ByVal MyserialLink As Miscelaneous.MySerialPort)
        Dim TemplateBuffer() As Byte = {&H68, &H1A, &H1A, &H68, &H8, &H2, &H67, &H81, &H67, &H2, &H64, &H0, &H1, &H36, &H1F, &H4, &H8A, &H19, &HA, &H15, &H20, &H0, &HB, &H0, &H0, &H0, &H40, &H0, &H0, &H0, &H46, &H16}
        'Fix slave address
        TemplateBuffer(5) = _SlaveAddress
        TemplateBuffer(9) = _SlaveAddress
        'Send add current state of digital inputs 


        'Fix Crc
        TemplateBuffer(30) = CalculateProcomeCrc(TemplateBuffer, 4, 26)
        'Send packet
        MyserialLink.TransmitData(TemplateBuffer, 0, 32)

    End Sub
    Private Sub AssemblyMedidasandChangesPacket(ByVal MyserialLink As Miscelaneous.MySerialPort)
        Dim TemplateBuffer(MaxProcomePcktSize) As Byte
        Dim Counter As Integer
        Dim Offset As Integer
        Dim Ndc As Byte

        'Packet Header
        TemplateBuffer(0) = VariableSizePacket
        'Packet size empty for now
        TemplateBuffer(1) = &H0
        TemplateBuffer(2) = &H0
        TemplateBuffer(3) = VariableSizePacket
        TemplateBuffer(4) = &H8 'CAC
        TemplateBuffer(5) = _SlaveAddress
        TemplateBuffer(6) = ProcomeAsduCodes.MedidasYCambios
        TemplateBuffer(7) = &H81
        TemplateBuffer(8) = ProcomeAsduCodes.MedidasYCambios
        TemplateBuffer(9) = _SlaveAddress
        TemplateBuffer(10) = ProcomeAsduCodes.MedidasYCambios
        TemplateBuffer(11) = &H0
        TemplateBuffer(12) = &H1   'OCO
        'Send Number of analog measures
        Select Case _SlaveModel



            Case Else
                'Assume an ekorRPG
                TemplateBuffer(13) = &H9   'NOM
        End Select
        Offset = 14
        For Counter = 0 To CInt(TemplateBuffer(13)) - 1
            TemplateBuffer(Offset) = 0
            Offset = Offset + 1
            TemplateBuffer(Offset) = 0
            Offset = Offset + 1
        Next
        'Send Digital Changes if any
        Ndc = 0


        TemplateBuffer(Offset) = Ndc   'NDC
        Offset = Offset + 1
        If Ndc = &H0 Then
            'Add one IDC equal to zero
            TemplateBuffer(Offset) = &H0
            Offset = Offset + 1
            TemplateBuffer(Offset) = &H0
            Offset = Offset + 1
        End If
        'Fix packet size
        TemplateBuffer(1) = Offset - 4
        TemplateBuffer(2) = Offset - 4
        'Fix Crc
        TemplateBuffer(Offset) = CalculateProcomeCrc(TemplateBuffer, 4, Offset - 4)
        Offset = Offset + 1
        'End of frame
        TemplateBuffer(Offset) = ProcomeEndOfFrame
        Offset = Offset + 1
        'Send packet
        MyserialLink.TransmitData(TemplateBuffer, 0, Offset)
    End Sub
    Private Function GetCACByte(ByVal PacketBuffer() As Byte) As Byte
        If PacketBuffer(0) = VariableSizePacket Then
            Return PacketBuffer(4)
        Else
            Return PacketBuffer(1)
        End If
    End Function


End Class


