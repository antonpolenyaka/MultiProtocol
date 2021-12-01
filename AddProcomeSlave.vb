

Public Class AddProcomeSlave

    Private TemporalSlave As ProcomeSlave
    Private _Cancellation As Boolean

    ReadOnly Property CancelButtonPressed As Boolean
        Get
            Return _Cancellation
        End Get
    End Property

    'Constructor
    Sub New(ByRef Obj As ProcomeSlave)
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()
        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        TemporalSlave = Obj
        _Cancellation = False
    End Sub
    'Load
    Private Sub AddProcomeSlave_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TextBox1.Text = ""
        ComboBox2.Items.Clear()
        For Each i In [Enum].GetValues(GetType(ProcomeSlave.ProcomeSlaveModel))
            ComboBox2.Items.Add(i)
        Next
        ComboBox2.SelectedIndex = 0
    End Sub
    'Finish
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If IsNumeric(TextBox1.Text) = False Then
            MessageBox.Show("Numero de Esclavo invalido")
            Exit Sub
        End If
        TemporalSlave.MySlaveAddress = Val(TextBox1.Text)
        TemporalSlave.SlaveModel = ComboBox2.SelectedItem
        TemporalSlave.SlaveProtocolType = ISlave.SlaveType.ProcomeSlave
        Me.Close()
    End Sub
    'Cancel
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        _Cancellation = True
        Me.Close()
    End Sub
End Class