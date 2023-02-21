' * Design by DragonFly
' *
' * Best-effort attempt at number conversion up to 128 bits.
' *
' * All the displayed values revolve around the binary representation and/or integer promotion, with the following logic being used:
' *
' * 1) 128-bit = 2 x 64-bit = 4 x 32-bit = 8 x 16-bit = 16 x 8-bit ... 64-bit = 2 x 32-bit = 4 x 16-bit = 8 x 8-bit ... etc
' * 2) Any number will be looked at with the required number of bits for the data type of the display
' * 3) All the data types that can represent the value will be displayed directly, otherwise multiple values will be displayed
' * 4) The required number of bits will be achieved by either splitting the original bits into groups or extending it while observing the sign
' * 5) The weight of all the displayed values: Hi <--- Lo
' * 6) Floating-point number has the binary representation format of: Sign bit - Exponent bits - Fraction bits (32-bit = 1-8-23 ; 64-bit = 1-11-52)
' * 7) Floating-point equivalent integer representation is packed into the BigInteger number
' * 8) Floating-point equivalent integer representation observes only the Integral part and disregards the Fractional part of the number

Imports System.Numerics

Public Class Form1

    Private ReadOnly AllToolTip As New ToolTip
    Private ElementSize As Integer
    Private labelValueText As String = ""

    Private WithEvents Context As New ContextMenuStrip
    Private flagContextSet, floatTransform As Boolean

    Private BigIntegerMin As BigInteger = BigInteger.Parse("-170141183460469231731687303715884105728")
    Private BigIntegerMax As BigInteger = BigInteger.Parse("170141183460469231731687303715884105727")
    Private BigUIntegerMin As BigInteger = BigInteger.Parse("0")
    Private BigUIntegerMax As BigInteger = BigInteger.Parse("340282366920938463463374607431768211455")

#Region "Private Methods"

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ComboBoxNumberType.SelectedIndex = 0
        lblIntegerBinaryFormat.Location = New Point(304, 590)

        AddHandler Context.ItemClicked, AddressOf Context_ItemClicked
    End Sub

    Private Sub ClearValues()
        For Each ctrl As Control In Controls
            If TypeOf ctrl Is Label Then
                Dim lbl = DirectCast(ctrl, Label)
                If lbl.Name.Contains("Value") OrElse lbl.Name = "lblNumberOfBits" OrElse lbl.Name = "lblNumberOfBits128" Then
                    ctrl.Text = ""
                End If
            End If
        Next
    End Sub

    Private Sub ComboBoxNumberType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxNumberType.SelectedIndexChanged
        Select Case ComboBoxNumberType.SelectedIndex
            Case 3, 4
                ElementSize = 1
            Case 5, 6
                ElementSize = 2
            Case 7, 8, 13
                ElementSize = 4
            Case 9, 10, 14
                ElementSize = 8
            Case Else
                ElementSize = 16
        End Select

        ClearValues()

        ' Enable corresponding virtual keyboard keys that can be used for the selected number type
        Select Case ComboBoxNumberType.SelectedIndex
            Case 0 'Binary
                For Each ctrl As Control In Controls
                    If TypeOf ctrl Is Button Then
                        Dim tempBtn = DirectCast(ctrl, Button)
                        If tempBtn.Name.StartsWith("Key") Then
                            If tempBtn.Name = "Key0" OrElse tempBtn.Name = "Key1" Then
                                ctrl.Enabled = True
                            Else
                                ctrl.Enabled = False
                            End If
                        End If
                    End If
                Next
            Case 1 'Hex
                For Each ctrl As Control In Controls
                    If TypeOf ctrl Is Button Then
                        Dim tempBtn = DirectCast(ctrl, Button)
                        If tempBtn.Name.StartsWith("Key") Then
                            If tempBtn.Name = "KeyDot" OrElse tempBtn.Name = "KeySign" Then
                                ctrl.Enabled = False
                            Else
                                ctrl.Enabled = True
                            End If
                        End If
                    End If
                Next
            Case 2 'Octal
                For Each ctrl As Control In Controls
                    If TypeOf ctrl Is Button Then
                        Dim tempBtn = DirectCast(ctrl, Button)
                        If tempBtn.Name.StartsWith("Key") Then
                            If tempBtn.Name = "Key0" OrElse tempBtn.Name = "Key1" OrElse tempBtn.Name = "Key2" OrElse tempBtn.Name = "Key3" OrElse
                            tempBtn.Name = "Key4" OrElse tempBtn.Name = "Key5" OrElse tempBtn.Name = "Key6" OrElse tempBtn.Name = "Key7" Then
                                ctrl.Enabled = True
                            Else
                                ctrl.Enabled = False
                            End If
                        End If
                    End If
                Next
            Case 3 'SByte / Int8 (-128 through 127) (signed)
                EnableKeys(False, True)
            Case 4 'Byte / UInt8 (0 through 255) (unsigned)
                EnableKeys(False, False)
            Case 5 'Short / Int16 (-32,768 through 32,767) (signed)
                EnableKeys(False, True)
            Case 6 'UShort / UInt16 (0 through 65,535) (unsigned)
                EnableKeys(False, False)
            Case 7 'Integer / Int32 (-2,147,483,648 through 2,147,483,647) (signed)
                EnableKeys(False, True)
            Case 8 'UInteger / UInt32 (0 through 4,294,967,295) (unsigned)
                EnableKeys(False, False)
            Case 9 'Long / Int64 (-9,223,372,036,854,775,808 through 9,223,372,036,854,775,807 (9.2...E+18))
                EnableKeys(False, True)
            Case 10 'ULong / UInt64 (0 through 18,446,744,073,709,551,615 (1.8...E+19)) (unsigned)
                EnableKeys(False, False)
            Case 11 'Int128 (−170,141,183,460,469,231,731,687,303,715,884,105,728 (−2^127) through 170,141,183,460,469,231,731,687,303,715,884,105,727 (2^127 − 1)) (signed)
                EnableKeys(False, True)
            Case 12 'UInt128 (0 through 340,282,366,920,938,463,463,374,607,431,768,211,455 (2^128 − 1)) (unsigned)
                EnableKeys(False, False)
            Case 13 'Single / Float32 (-3.4028235E+38 through -1.401298E-45 for negative values; 1.401298E-45 through 3.4028235E+38 for positive values)
                EnableKeys(True, True)
            Case Else 'Double / Float64 (-1.79769313486231570E+308 through -4.94065645841246544E-324 for negative values; 4.94065645841246544E-324 through 1.79769313486231570E+308 for positive values)
                EnableKeys(True, True)
        End Select

        TextBoxNumber.Text = ""
        TextBoxNumber.Focus()
    End Sub

    Private Sub EnableKeys(EnableDotKey As Boolean, EnableSignKey As Boolean)
        For Each ctrl As Control In Controls
            If TypeOf ctrl Is Button Then
                Dim tempBtn = DirectCast(ctrl, Button)

                If tempBtn.Name.StartsWith("Key") Then
                    Dim dummy As Integer

                    If Integer.TryParse(tempBtn.Text, dummy) Then
                        ctrl.Enabled = True
                    Else
                        ctrl.Enabled = False
                    End If

                    If EnableDotKey Then
                        If tempBtn.Name = "KeyDot" Then ctrl.Enabled = True
                    End If

                    If EnableSignKey Then
                        If tempBtn.Name = "KeySign" Then ctrl.Enabled = True
                    End If

                    If ComboBoxNumberType.SelectedIndex = 13 OrElse ComboBoxNumberType.SelectedIndex = 14 Then
                        If tempBtn.Name = "KeyE" Then ctrl.Enabled = True
                    End If
                End If
            End If
        Next
    End Sub

    Private Sub ButtonConvert_Click(sender As Object, e As EventArgs) Handles ButtonConvert.Click
        ClearValues()

        If Not String.IsNullOrWhiteSpace(TextBoxNumber.Text) Then
            Try
                Dim tempVar As String = ""

                If TextBoxNumber.Text = "0" Then
                    tempVar = TextBoxNumber.Text
                Else
                    tempVar = TextBoxNumber.Text.ToUpperInvariant.TrimStart("0"c)
                End If

                Dim tempBinary As String = ""
                Dim tempHex As String = ""

                Select Case ComboBoxNumberType.SelectedIndex
                    Case 0 '***** Binary *****
                        For i = 0 To TextBoxNumber.Text.Length - 1
                            If Not (TextBoxNumber.Text(i) = "0" OrElse TextBoxNumber.Text(i) = "1") Then
                                MessageBox.Show("Not a binary number!")
                                Exit Sub
                            End If
                        Next

                        If tempVar.Length > 128 Then
                            MessageBox.Show("Too large of a binary number!")
                            Exit Sub
                        End If

                        If tempVar.Length > 64 Then
                            ElementSize = 16
                            tempBinary = tempVar.PadLeft(128, "0"c)
                            For i = 0 To 127 Step 4
                                tempHex &= Convert.ToUInt64(tempBinary.Substring(i, 4), 2).ToString("X")
                            Next
                        ElseIf tempVar.Length > 32 Then
                            ElementSize = 8
                            tempBinary = tempVar.PadLeft(64, "0"c)
                            tempHex = Convert.ToInt64(tempBinary, 2).ToString("X")
                        ElseIf tempVar.Length > 16 Then
                            ElementSize = 4
                            tempBinary = tempVar.PadLeft(32, "0"c)
                            tempHex = Convert.ToInt32(tempBinary, 2).ToString("X")
                        ElseIf tempVar.Length > 8 Then
                            ElementSize = 2
                            tempBinary = tempVar.PadLeft(16, "0"c)
                            tempHex = Convert.ToInt16(tempBinary, 2).ToString("X")
                        Else
                            ElementSize = 1
                            tempBinary = tempVar.PadLeft(8, "0"c)
                            tempHex = Convert.ToSByte(tempBinary, 2).ToString("X")
                        End If
                    Case 1 '***** Hex *****
                        For Each ch As Char In tempVar
                            'Allow only digits 0 to 9 and letters A to F
                            If Asc(ch) < 48 OrElse (Asc(ch) > 57 AndAlso Asc(ch) < 65) OrElse Asc(ch) > 71 Then
                                MessageBox.Show("Not a hexadecimal number!")
                                Exit Sub
                            End If
                        Next

                        If tempVar.Length > 32 Then
                            MessageBox.Show("Too large of a hexadecimal number!")
                            Exit Sub
                        End If

                        If tempVar.Length > 16 Then
                            ElementSize = 16
                            For i = 0 To tempVar.Length - 1
                                tempBinary &= Convert.ToString(Convert.ToInt32(tempVar(i), 16), 2).PadLeft(4, "0"c)
                            Next
                            tempBinary = tempBinary.PadLeft(128, "0"c)
                        ElseIf tempVar.Length > 8 Then
                            ElementSize = 8
                            tempBinary = Convert.ToString(Convert.ToInt64(tempVar, 16), 2).PadLeft(64, "0"c)
                        ElseIf tempVar.Length > 4 Then
                            ElementSize = 4
                            tempBinary = Convert.ToString(Convert.ToInt32(tempVar, 16), 2).PadLeft(32, "0"c)
                        ElseIf tempVar.Length > 2 Then
                            ElementSize = 2
                            tempBinary = Convert.ToString(Convert.ToInt16(tempVar, 16), 2).PadLeft(16, "0"c)
                        Else
                            ElementSize = 1
                            tempBinary = Convert.ToString(Convert.ToByte(tempVar, 16), 2).PadLeft(8, "0"c)
                        End If

                        tempHex = tempVar.PadLeft(2 * ElementSize, "0"c)
                    Case 2 '***** Octal *****
                        For Each ch As Char In TextBoxNumber.Text.ToUpper
                            'Allow only digits in the 0 to 7 range
                            If Asc(ch) < 48 OrElse Asc(ch) > 55 Then
                                MessageBox.Show("Not an octal number!")
                                Exit Sub
                            End If
                        Next

                        If tempVar.Length > 43 OrElse (tempVar.Length = 43 AndAlso CInt(tempVar.Substring(0, 1)) > 3) Then
                            MessageBox.Show("Too large of an octal number!")
                            Exit Sub
                        End If

                        For i = 0 To tempVar.Length - 1
                            tempBinary &= Convert.ToString(Convert.ToInt32(tempVar(i), 8), 2).PadLeft(3, "0"c)
                        Next

                        tempBinary = tempBinary.TrimStart("0"c)

                        If tempBinary.Length > 64 Then
                            ElementSize = 16
                        ElseIf tempBinary.Length > 32 Then
                            ElementSize = 8
                        ElseIf tempBinary.Length > 16 Then
                            ElementSize = 4
                        ElseIf tempBinary.Length > 8 Then
                            ElementSize = 2
                        Else
                            ElementSize = 1
                        End If

                        tempBinary = tempBinary.PadLeft(8 * ElementSize, "0"c)

                        If tempVar.Length > 22 Then
                            For i = 0 To tempBinary.Length - 1 Step 4
                                tempHex &= Convert.ToString(Convert.ToInt32(tempBinary.Substring(i, 4), 2), 16).ToUpperInvariant
                            Next
                        Else
                            tempHex = Convert.ToUInt64(tempBinary, 2).ToString("X")
                        End If
                    Case 3 '***** Int8 / SByte *****
                        Dim dummy As SByte

                        If Not SByte.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not an Int8 (SByte) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(CByte(CSByte(tempVar) And &HFF), 2).PadLeft(8, "0"c)
                        tempHex = Convert.ToSByte(tempBinary, 2).ToString("X")
                    Case 4 '***** UInt8 / Byte *****
                        Dim dummy As Byte

                        If Not Byte.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not a UInt8 (Byte) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(CByte(tempVar), 2).PadLeft(8, "0"c)
                        tempHex = Convert.ToByte(tempBinary, 2).ToString("X")
                    Case 5 '***** Int16 / Short *****
                        Dim dummy As Short

                        If Not Short.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not an Int16 (Short) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(CShort(tempVar), 2).PadLeft(16, "0"c)
                        tempHex = Convert.ToInt16(tempBinary, 2).ToString("X")
                    Case 6 '***** UInt16 / UShort *****
                        Dim dummy As UShort

                        If Not UShort.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not a UInt16 (UShort) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(CUShort(tempVar), 2).PadLeft(16, "0"c)
                        tempHex = Convert.ToUInt16(tempBinary, 2).ToString("X")
                    Case 7 '***** Int32 / Integer *****
                        Dim dummy As Integer

                        If Not Integer.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not an Int32 (Integer) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(CInt(tempVar), 2).PadLeft(32, "0"c)
                        tempHex = Convert.ToInt32(tempBinary, 2).ToString("X")
                    Case 8 '***** UInt32 / UInteger *****
                        Dim dummy As UInteger

                        If Not UInteger.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not a UInt32 (UInteger) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(CUInt(tempVar), 2).PadLeft(32, "0"c)
                        tempHex = Convert.ToUInt32(tempBinary, 2).ToString("X")
                    Case 9 '***** Int64 / Long *****
                        Dim dummy As Long

                        If Not Long.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not an Int64 (Long) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(CLng(tempVar), 2).PadLeft(64, "0"c)
                        tempHex = Convert.ToUInt64(tempBinary, 2).ToString("X")
                    Case 10 '***** UInt64 / ULong *****
                        Dim dummy As ULong

                        If Not ULong.TryParse(TextBoxNumber.Text, dummy) Then
                            MessageBox.Show("Not a UInt64 (ULong) number!")
                            Exit Sub
                        End If

                        tempBinary = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToUInt64(tempVar)), 0), 2).PadLeft(64, "0"c)
                        tempHex = Convert.ToUInt64(tempBinary, 2).ToString("X")
                    Case 11 '***** Int128 / QINT *****
                        Dim dummy As BigInteger = 0

                        'If negative value then use its absolute value
                        If Convert.ToInt32(tempVar(0)) = 8722 OrElse Convert.ToInt32(tempVar(0)) = 45 Then
                            If BigInteger.TryParse(tempVar.Substring(1), dummy) Then
                                If dummy > BigIntegerMax + BigInteger.One Then
                                    MessageBox.Show("Not an Int128 (QINT) number!")
                                    Exit Sub
                                End If

                                tempBinary = ChangeBigInteger2BinaryString(BigUIntegerMax + BigInteger.One - dummy)
                                tempHex = Convert.ToUInt64(tempBinary.Substring(0, 64), 2).ToString("X").PadLeft(16, "0"c) & Convert.ToUInt64(tempBinary.Substring(64), 2).ToString("X").PadLeft(16, "0"c)
                            Else
                                MessageBox.Show("The value could not be parsed!")
                                Exit Sub
                            End If
                        Else
                            If BigInteger.TryParse(tempVar, dummy) Then
                                If dummy > BigIntegerMax Then
                                    MessageBox.Show("Not an Int128 (QINT) number!")
                                    Exit Sub
                                End If

                                tempBinary = ChangeBigInteger2BinaryString(dummy)
                                tempHex = Convert.ToUInt64(tempBinary.Substring(0, 64), 2).ToString("X").PadLeft(16, "0"c) & Convert.ToUInt64(tempBinary.Substring(64), 2).ToString("X").PadLeft(16, "0"c)
                            Else
                                MessageBox.Show("The value could not be parsed!")
                                Exit Sub
                            End If
                        End If
                    Case 12 '***** UInt128 / UQINT *****
                        Dim dummy As BigInteger = 0

                        If BigInteger.TryParse(tempVar, dummy) Then
                            If dummy < 0 OrElse dummy > BigUIntegerMax Then
                                MessageBox.Show("Not a UInt128 (UQINT) number!")
                                Exit Sub
                            End If
                        Else
                            MessageBox.Show("The value could not be parsed!")
                            Exit Sub
                        End If

                        tempBinary = ChangeBigInteger2BinaryString(dummy)
                        tempHex = Convert.ToUInt64(tempBinary.Substring(0, 64), 2).ToString("X") & Convert.ToUInt64(tempBinary.Substring(64), 2).ToString("X")
                    Case 13 '***** Float32 / Single *****
                        If tempVar = "0" Then tempVar = "+0" 'Workaround since the IEEE 754 Standard requires signed zero (+0 or -0)

                        Dim dummy As Single

                        If Not Single.TryParse(tempVar, dummy) Then
                            MessageBox.Show("Not a Float32 (Single) number!")
                            Exit Sub
                        End If

                        Dim tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(dummy), 0), 2).PadLeft(32, "0"c)
                        Set_BinaryF32_Value(tempBinaryF32)

                        lblFloat32Value.Text = dummy.ToString("G9")
                        lblFloat64Value.Text = Convert.ToDouble(lblFloat32Value.Text).ToString("G17")

                        Dim tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToDouble(lblFloat64Value.Text)), 0), 2).PadLeft(64, "0"c)
                        Set_BinaryF64_Value(tempBinaryF64)

                        floatTransform = True
                        ElementSize = 16

                        Dim tempBigInteger As BigInteger = CType(Convert.ToSingle(tempVar), BigInteger)

                        If tempBigInteger < BigIntegerMin OrElse tempBigInteger > BigIntegerMax Then
                            SetOutOfRangeLabels()
                            Exit Sub
                        Else
                            If Convert.ToInt32(tempVar(0)) = 8722 OrElse Convert.ToInt32(tempVar(0)) = 45 Then
                                tempBinary = ChangeBigInteger2BinaryString(BigUIntegerMax + BigInteger.One + tempBigInteger)
                                tempHex = Convert.ToUInt64(tempBinary.Substring(0, 64), 2).ToString("X").PadLeft(16, "0"c) & Convert.ToUInt64(tempBinary.Substring(64), 2).ToString("X").PadLeft(16, "0"c)
                            Else
                                tempBinary = ChangeBigInteger2BinaryString(tempBigInteger)
                                tempHex = Convert.ToUInt64(tempBinary.Substring(0, 64), 2).ToString("X").PadLeft(16, "0"c) & Convert.ToUInt64(tempBinary.Substring(64), 2).ToString("X").PadLeft(16, "0"c)
                            End If
                        End If
                    Case Else '***** Float64 / Double *****
                        If tempVar = "0" Then tempVar = "+0" 'Workaround since the IEEE 754 Standard requires signed zero (+0 or -0)

                        Dim dummy As Double

                        If Not Double.TryParse(tempVar, dummy) Then
                            MessageBox.Show("Not a Float64 (Double) number!")
                            Exit Sub
                        End If

                        Dim tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(dummy), 0), 2).PadLeft(64, "0"c)
                        Set_BinaryF64_Value(tempBinaryF64)
                        lblFloat64Value.Text = dummy.ToString("G17")

                        floatTransform = True
                        ElementSize = 16

                        Dim tempBigInteger As BigInteger = CType(dummy, BigInteger)

                        If tempBigInteger < BigIntegerMin OrElse tempBigInteger > BigIntegerMax Then
                            SetOutOfRangeLabels()
                            lblFloat32Value.Text &= "Out of Range"
                            lblBinaryF32Value.Text &= "Out of Range"
                            Exit Sub
                        Else
                            If Convert.ToInt32(tempVar(0)) = 8722 OrElse Convert.ToInt32(tempVar(0)) = 45 Then
                                tempBinary = ChangeBigInteger2BinaryString(BigUIntegerMax + BigInteger.One + tempBigInteger)
                                tempHex = Convert.ToUInt64(tempBinary.Substring(0, 64), 2).ToString("X").PadLeft(16, "0"c) & Convert.ToUInt64(tempBinary.Substring(64), 2).ToString("X").PadLeft(16, "0"c)
                            Else
                                tempBinary = ChangeBigInteger2BinaryString(tempBigInteger)
                                tempHex = Convert.ToUInt64(tempBinary.Substring(0, 64), 2).ToString("X").PadLeft(16, "0"c) & Convert.ToUInt64(tempBinary.Substring(64), 2).ToString("X").PadLeft(16, "0"c)
                            End If
                        End If

                        If (dummy >= -3.4028235E+38 AndAlso dummy <= -1.401298E-45) OrElse (dummy >= 1.401298E-45 AndAlso dummy <= 3.4028235E+38) Then
                            lblFloat32Value.Text = Convert.ToSingle(lblFloat64Value.Text).ToString("G9")
                        Else
                            For i = 0 To 1
                                lblFloat32Value.Text = Convert.ToSingle(Convert.ToInt32(tempBinary.Substring(i * 32, 32), 2)).ToString("G9")

                                If i <> 1 Then
                                    lblFloat32Value.Text &= " , "
                                End If
                            Next
                        End If

                        Dim tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(lblFloat32Value.Text)), 0), 2).PadLeft(32, "0"c)
                        Set_BinaryF32_Value(tempBinaryF32)
                End Select

                Set_Binary_Value(tempBinary)
                Set_Hex_Value(tempHex.PadLeft(2 * ElementSize, "0"c))
                Set_Octal_Value(tempBinary)
                Set_8_16_32_64_128_Values(tempBinary)
            Catch ex As Exception
                MessageBox.Show(ex.Message)
            End Try
        End If
    End Sub

    Private Sub Set_8_16_32_64_128_Values(tempBinary As String)
        Try
            Dim BigIntValue As BigInteger

            Select Case ElementSize
                Case 16
                    lblUInt128Value.Text = BitConverterUInt128(tempBinary).ToString
                    lblInt128Value.Text = BitConverterInt128(tempBinary).ToString

                    BigIntValue = BigInteger.Parse(lblInt128Value.Text)

                    If floatTransform Then
                        floatTransform = False
                    Else
                        If (BigIntValue >= New BigInteger(-1.7976931348623157E+308) AndAlso BigIntValue <= New BigInteger(-4.94065645841247E-324)) OrElse (BigIntValue >= New BigInteger(4.94065645841247E-324) AndAlso BigIntValue <= New BigInteger(1.7976931348623157E+308)) Then
                            lblFloat64Value.Text = Convert.ToDouble(BigIntValue.ToString).ToString("G17")

                            Dim tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToDouble(lblFloat64Value.Text)), 0), 2).PadLeft(64, "0"c)
                            Set_BinaryF64_Value(tempBinaryF64)
                        Else
                            For i = 0 To 1
                                lblFloat64Value.Text = Convert.ToDouble(Convert.ToInt64(tempBinary.Substring(i * 64, 64), 2)).ToString("G17")

                                If i <> 1 Then
                                    lblFloat64Value.Text &= " , "
                                End If
                            Next
                        End If

                        If (BigIntValue >= New BigInteger(-3.4028235E+38) AndAlso BigIntValue <= New BigInteger(-1.401298E-45)) OrElse (BigIntValue >= New BigInteger(1.401298E-45) AndAlso BigIntValue <= New BigInteger(3.4028235E+38)) Then
                            lblFloat32Value.Text = Convert.ToSingle(lblFloat64Value.Text).ToString("G9")

                            Dim tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(lblFloat32Value.Text)), 0), 2).PadLeft(32, "0"c)
                            Set_BinaryF32_Value(tempBinaryF32)
                        Else
                            For i = 0 To 1
                                lblFloat32Value.Text = Convert.ToSingle(Convert.ToInt32(tempBinary.Substring(i * 32, 32), 2)).ToString("G9")

                                If i <> 1 Then
                                    lblFloat32Value.Text &= " , "
                                End If
                            Next
                        End If
                    End If

                    If BigIntValue > BigInteger.Parse("-9223372036854775809") AndAlso BigIntValue < BigInteger.Parse("9223372036854775808") Then
                        lblInt64Value.Text = lblInt128Value.Text
                        lblUInt64Value.Text = BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToInt64(lblInt64Value.Text)), 0).ToString
                    Else
                        For i = 0 To 1
                            lblUInt64Value.Text &= Convert.ToUInt64(tempBinary.Substring(i * 64, 64), 2).ToString
                            lblInt64Value.Text &= Convert.ToInt64(tempBinary.Substring(i * 64, 64), 2).ToString

                            If i <> 1 Then
                                lblUInt64Value.Text &= " , "
                                lblInt64Value.Text &= " , "
                            End If
                        Next
                    End If

                    If BigIntValue > -2147483649 AndAlso BigIntValue < 2147483648 Then
                        lblInt32Value.Text = lblInt128Value.Text
                        lblUInt32Value.Text = BitConverter.ToUInt32(BitConverter.GetBytes(Convert.ToInt32(lblInt32Value.Text)), 0).ToString
                    Else
                        For i = 0 To 3
                            lblUInt32Value.Text &= Convert.ToUInt32(tempBinary.Substring(i * 32, 32), 2).ToString
                            lblInt32Value.Text &= Convert.ToInt32(tempBinary.Substring(i * 32, 32), 2).ToString

                            If i <> 3 Then
                                lblUInt32Value.Text &= " , "
                                lblInt32Value.Text &= " , "
                            End If
                        Next
                    End If

                    If BigIntValue > -32769 AndAlso BigIntValue < 32768 Then
                        lblInt16Value.Text = lblInt128Value.Text
                        lblUInt16Value.Text = BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToInt16(lblInt16Value.Text)), 0).ToString
                    Else
                        For i = 0 To 7
                            lblUInt16Value.Text &= Convert.ToUInt16(tempBinary.Substring(i * 16, 16), 2).ToString
                            lblInt16Value.Text &= Convert.ToInt16(tempBinary.Substring(i * 16, 16), 2).ToString

                            If i <> 7 Then
                                lblUInt16Value.Text &= " , "
                                lblInt16Value.Text &= " , "
                            End If
                        Next
                    End If

                    If BigIntValue > -129 AndAlso BigIntValue < 128 Then
                        lblInt8Value.Text = lblInt128Value.Text
                        lblUInt8Value.Text = BitConverter.GetBytes(Convert.ToSByte(lblInt8Value.Text))(0).ToString
                    Else
                        For i = 0 To 15
                            lblUInt8Value.Text &= Convert.ToByte(tempBinary.Substring(i * 8, 8), 2).ToString
                            lblInt8Value.Text &= Convert.ToSByte(tempBinary.Substring(i * 8, 8), 2).ToString

                            If i <> 15 Then
                                lblUInt8Value.Text &= " , "
                                lblInt8Value.Text &= " , "
                            End If
                        Next
                    End If
                Case 8
                    Dim doubleValue As Double
                    Dim temp2Binary = tempBinary
                    Dim temp2Hex, tempBinaryF32, tempBinaryF64 As String

                    If ComboBoxNumberType.SelectedIndex = 14 Then 'Double / Float64
                        doubleValue = Convert.ToDouble(TextBoxNumber.Text)

                        lblFloat64Value.Text = doubleValue.ToString("G17")

                        If (doubleValue < 0 AndAlso doubleValue < -3.4028235E+38 AndAlso doubleValue > -1.401298E-45) OrElse (doubleValue > 0 AndAlso doubleValue < 1.401298E-45 AndAlso doubleValue > 3.4028235E+38) Then
                            lblFloat32Value.Text = "Out of Range"
                        Else
                            lblFloat32Value.Text = Convert.ToSingle(doubleValue).ToString("G9")

                            tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(lblFloat32Value.Text)), 0), 2).PadLeft(32, "0"c)
                            Set_BinaryF32_Value(tempBinaryF32)
                        End If

                        BigIntValue = CType(doubleValue, BigInteger)

                        If BigIntValue < BigIntegerMin OrElse BigIntValue > BigIntegerMax Then
                            SetOutOfRangeLabels()
                            Exit Sub
                        End If

                        If BigIntValue < BigIntegerMax + BigInteger.One Then
                            lblInt128Value.Text = BigIntValue.ToString
                            If BigIntValue < 0 Then
                                lblUInt128Value.Text = BitConverterUInt128(ChangeBigInteger2BinaryString(BigUIntegerMax + BigInteger.One + BigIntValue)).ToString
                            Else
                                lblUInt128Value.Text = lblInt128Value.Text
                            End If
                        Else
                            lblUInt128Value.Text = BigIntValue.ToString
                            lblInt128Value.Text = BitConverterInt128(ChangeBigInteger2BinaryString(BigIntValue)).ToString
                        End If

                        temp2Binary = ChangeBigInteger2BinaryString(BigInteger.Parse(lblUInt128Value.Text))
                        temp2Hex = Convert.ToUInt64(temp2Binary.Substring(0, 64), 2).ToString("X") & Convert.ToUInt64(temp2Binary.Substring(64), 2).ToString("X")

                        If BigIntValue > BigInteger.Parse("-9223372036854775809") AndAlso BigIntValue < BigInteger.Parse("9223372036854775808") Then
                            lblInt64Value.Text = BigIntValue.ToString
                            lblUInt64Value.Text = BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToInt64(lblInt64Value.Text)), 0).ToString

                            temp2Binary = Convert.ToString(Convert.ToInt64(lblInt64Value.Text), 2).PadLeft(64, "0"c)
                            temp2Hex = Convert.ToUInt64(temp2Binary, 2).ToString("X")

                            If BigIntValue > -2147483649 AndAlso BigIntValue < 2147483648 Then
                                lblInt32Value.Text = lblInt128Value.Text
                                lblUInt32Value.Text = BitConverter.ToUInt32(BitConverter.GetBytes(Convert.ToInt32(lblInt32Value.Text)), 0).ToString

                                temp2Binary = Convert.ToString(Convert.ToInt32(lblInt32Value.Text), 2).PadLeft(32, "0"c)
                                temp2Hex = Convert.ToUInt64(temp2Binary, 2).ToString("X")

                                If BigIntValue > -32769 AndAlso BigIntValue < 32768 Then
                                    lblInt16Value.Text = lblInt128Value.Text
                                    lblUInt16Value.Text = BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToInt16(lblInt16Value.Text)), 0).ToString

                                    temp2Binary = Convert.ToString(Convert.ToInt16(lblInt16Value.Text), 2).PadLeft(16, "0"c)

                                    If BigIntValue > -129 AndAlso BigIntValue < 128 Then
                                        lblInt8Value.Text = lblInt128Value.Text
                                        lblUInt8Value.Text = BitConverter.GetBytes(Convert.ToSByte(lblInt8Value.Text))(0).ToString

                                        temp2Binary = Convert.ToString(Convert.ToSByte(lblInt8Value.Text), 2).PadLeft(8, "0"c)
                                        temp2Hex = Convert.ToUInt64(temp2Binary, 2).ToString("X")
                                    Else
                                        For i = 0 To 7
                                            lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                            lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                            If i <> 7 Then
                                                lblUInt8Value.Text &= " , "
                                                lblInt8Value.Text &= " , "
                                            End If
                                        Next
                                    End If
                                Else
                                    For i = 0 To 3
                                        lblUInt16Value.Text &= Convert.ToUInt16(temp2Binary.Substring(i * 16, 16), 2).ToString
                                        lblInt16Value.Text &= Convert.ToInt16(temp2Binary.Substring(i * 16, 16), 2).ToString

                                        If i <> 3 Then
                                            lblUInt16Value.Text &= " , "
                                            lblInt16Value.Text &= " , "
                                        End If
                                    Next

                                    For i = 0 To 7
                                        lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                        lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                        If i <> 7 Then
                                            lblUInt8Value.Text &= " , "
                                            lblInt8Value.Text &= " , "
                                        End If
                                    Next
                                End If

                            Else
                                For i = 0 To 1
                                    lblUInt32Value.Text &= Convert.ToUInt32(temp2Binary.Substring(i * 32, 32), 2).ToString
                                    lblInt32Value.Text &= Convert.ToInt32(temp2Binary.Substring(i * 32, 32), 2).ToString

                                    If i <> 1 Then
                                        lblUInt32Value.Text &= " , "
                                        lblInt32Value.Text &= " , "
                                    End If
                                Next

                                For i = 0 To 3
                                    lblUInt16Value.Text &= Convert.ToUInt16(temp2Binary.Substring(i * 16, 16), 2).ToString
                                    lblInt16Value.Text &= Convert.ToInt16(temp2Binary.Substring(i * 16, 16), 2).ToString

                                    If i <> 3 Then
                                        lblUInt16Value.Text &= " , "
                                        lblInt16Value.Text &= " , "
                                    End If
                                Next

                                For i = 0 To 7
                                    lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                    lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                    If i <> 7 Then
                                        lblUInt8Value.Text &= " , "
                                        lblInt8Value.Text &= " , "
                                    End If
                                Next
                            End If
                        Else
                            For i = 0 To 1
                                lblUInt64Value.Text &= Convert.ToUInt64(temp2Binary.Substring(i * 64, 64), 2).ToString
                                lblInt64Value.Text &= Convert.ToInt64(temp2Binary.Substring(i * 64, 64), 2).ToString

                                If i <> 1 Then
                                    lblUInt64Value.Text &= " , "
                                    lblInt64Value.Text &= " , "
                                End If
                            Next

                            For i = 0 To 3
                                lblUInt32Value.Text &= Convert.ToUInt32(temp2Binary.Substring(i * 32, 32), 2).ToString
                                lblInt32Value.Text &= Convert.ToInt32(temp2Binary.Substring(i * 32, 32), 2).ToString

                                If i <> 3 Then
                                    lblUInt32Value.Text &= " , "
                                    lblInt32Value.Text &= " , "
                                End If
                            Next

                            For i = 0 To 7
                                lblUInt16Value.Text &= Convert.ToUInt16(temp2Binary.Substring(i * 16, 16), 2).ToString
                                lblInt16Value.Text &= Convert.ToInt16(temp2Binary.Substring(i * 16, 16), 2).ToString

                                If i <> 7 Then
                                    lblUInt16Value.Text &= " , "
                                    lblInt16Value.Text &= " , "
                                End If
                            Next

                            For i = 0 To 15
                                lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                If i <> 15 Then
                                    lblUInt8Value.Text &= " , "
                                    lblInt8Value.Text &= " , "
                                End If
                            Next
                        End If

                        Set_Binary_Value(temp2Binary)
                        Set_Hex_Value(temp2Hex.PadLeft(CInt(temp2Binary.Length / 4), "0"c))
                        Set_Octal_Value(temp2Binary)

                        Exit Sub
                    ElseIf ComboBoxNumberType.SelectedIndex = 9 Then 'Long / Int64

                        Dim LongValue = Convert.ToInt64(TextBoxNumber.Text)

                        lblInt64Value.Text = LongValue.ToString
                        lblUInt64Value.Text = BitConverter.ToUInt64(BitConverter.GetBytes(LongValue), 0).ToString

                        doubleValue = Convert.ToDouble(lblInt64Value.Text)

                        lblFloat64Value.Text = doubleValue.ToString("G17")

                        If temp2Binary.StartsWith("1") Then
                            lblInt128Value.Text = BitConverterInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                            lblUInt128Value.Text = BitConverterUInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                        Else
                            lblInt128Value.Text = lblInt64Value.Text
                            lblUInt128Value.Text = BitConverterUInt128(ChangeBigInteger2BinaryString(BigInteger.Parse(lblInt128Value.Text))).ToString
                        End If

                    ElseIf ComboBoxNumberType.SelectedIndex = 10 Then 'ULong / UInt64

                        Dim ULongValue = Convert.ToUInt64(TextBoxNumber.Text)

                        lblUInt64Value.Text = ULongValue.ToString
                        lblInt64Value.Text = BitConverter.ToInt64(BitConverter.GetBytes(ULongValue), 0).ToString

                        doubleValue = Convert.ToDouble(lblInt64Value.Text)

                        lblFloat64Value.Text = doubleValue.ToString("G17")

                        If tempBinary.StartsWith("1") Then
                            lblInt128Value.Text = BitConverterInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                            lblUInt128Value.Text = BitConverterUInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                        Else
                            lblUInt128Value.Text = lblUInt64Value.Text
                            lblInt128Value.Text = BitConverterInt128(ChangeBigInteger2BinaryString(BigInteger.Parse(lblUInt128Value.Text))).ToString
                        End If

                    Else 'Binary or Hex or Octal

                        If temp2Binary.StartsWith("1") Then
                            lblInt128Value.Text = BitConverterInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                            lblUInt128Value.Text = BitConverterUInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                        Else
                            lblInt128Value.Text = BitConverterInt128(temp2Binary.PadLeft(128, "0"c)).ToString
                            lblUInt128Value.Text = BitConverterUInt128(temp2Binary.PadLeft(128, "0"c)).ToString
                        End If

                        lblUInt64Value.Text = Convert.ToUInt64(temp2Binary, 2).ToString
                        lblInt64Value.Text = Convert.ToInt64(temp2Binary, 2).ToString

                        doubleValue = Convert.ToDouble(lblInt128Value.Text)

                        lblFloat64Value.Text = doubleValue.ToString("G17")

                    End If

                    tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(doubleValue), 0), 2).PadLeft(64, "0"c)
                    Set_BinaryF64_Value(tempBinaryF64)

                    If doubleValue = 0 OrElse (doubleValue >= -3.4028235E+38 AndAlso doubleValue <= -1.401298E-45) OrElse (doubleValue >= 1.401298E-45 AndAlso doubleValue <= 3.4028235E+38) Then
                        lblFloat32Value.Text = Convert.ToSingle(doubleValue).ToString("G9")

                        tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(lblFloat32Value.Text)), 0), 2).PadLeft(32, "0"c)
                        Set_BinaryF32_Value(tempBinaryF32)
                    Else
                        For i = 0 To 1
                            lblFloat32Value.Text = BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(tempBinary.Substring(i * 32, 32), 2)), 0).ToString("G9")

                            If i <> 1 Then
                                lblFloat32Value.Text &= " , "
                            End If
                        Next
                    End If

                    If Long.Parse(lblInt64Value.Text) > -2147483649 AndAlso Long.Parse(lblInt64Value.Text) < 2147483648 Then
                        lblInt32Value.Text = lblInt64Value.Text
                        lblUInt32Value.Text = BitConverter.ToUInt32(BitConverter.GetBytes(Convert.ToInt32(lblInt32Value.Text)), 0).ToString
                    Else
                        For i = 0 To 1
                            lblUInt32Value.Text &= Convert.ToUInt32(temp2Binary.Substring(i * 32, 32), 2).ToString
                            lblInt32Value.Text &= Convert.ToInt32(temp2Binary.Substring(i * 32, 32), 2).ToString

                            If i <> 1 Then
                                lblUInt32Value.Text &= " , "
                                lblInt32Value.Text &= " , "
                            End If
                        Next
                    End If

                    If Long.Parse(lblInt64Value.Text) > -32769 AndAlso Long.Parse(lblInt64Value.Text) < 32768 Then
                        lblInt16Value.Text = lblInt64Value.Text
                        lblUInt16Value.Text = BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToInt16(lblInt16Value.Text)), 0).ToString
                    Else
                        For i = 0 To 3
                            lblUInt16Value.Text &= Convert.ToUInt16(temp2Binary.Substring(i * 16, 16), 2).ToString
                            lblInt16Value.Text &= Convert.ToInt16(temp2Binary.Substring(i * 16, 16), 2).ToString

                            If i <> 3 Then
                                lblUInt16Value.Text &= " , "
                                lblInt16Value.Text &= " , "
                            End If
                        Next
                    End If

                    If Long.Parse(lblInt64Value.Text) > -129 AndAlso Long.Parse(lblInt64Value.Text) < 128 Then
                        lblInt8Value.Text = lblInt64Value.Text
                        lblUInt8Value.Text = BitConverter.GetBytes(Convert.ToSByte(lblInt8Value.Text))(0).ToString
                    Else
                        For i = 0 To 7
                            lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                            lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                            If i <> 7 Then
                                lblUInt8Value.Text &= " , "
                                lblInt8Value.Text &= " , "
                            End If
                        Next
                    End If
                Case 4
                    Dim temp2Binary = tempBinary
                    Dim temp2Hex, tempBinaryF32, tempBinaryF64 As String

                    If ComboBoxNumberType.SelectedIndex = 13 Then 'Single / Float32
                        Dim singleValue = Convert.ToSingle(TextBoxNumber.Text)

                        lblFloat32Value.Text = singleValue.ToString("G9")
                        lblFloat64Value.Text = Convert.ToDouble(singleValue).ToString("G17")

                        tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToDouble(lblFloat64Value.Text)), 0), 2).PadLeft(64, "0"c)
                        Set_BinaryF64_Value(tempBinaryF64)

                        BigIntValue = CType(singleValue, BigInteger)

                        If BigIntValue < BigIntegerMin OrElse BigIntValue > BigIntegerMax Then
                            SetOutOfRangeLabels()
                            Exit Sub
                        End If

                        If BigIntValue < BigIntegerMax + BigInteger.One Then
                            lblInt128Value.Text = BigIntValue.ToString
                            If BigIntValue < 0 Then
                                lblUInt128Value.Text = BitConverterUInt128(ChangeBigInteger2BinaryString(BigUIntegerMax + BigInteger.One + BigIntValue)).ToString
                            Else
                                lblUInt128Value.Text = lblInt128Value.Text
                            End If
                        Else
                            lblUInt128Value.Text = BigIntValue.ToString
                            lblInt128Value.Text = BitConverterInt128(ChangeBigInteger2BinaryString(BigIntValue)).ToString
                        End If

                        temp2Binary = ChangeBigInteger2BinaryString(BigInteger.Parse(lblUInt128Value.Text))
                        temp2Hex = Convert.ToUInt64(temp2Binary.Substring(0, 64), 2).ToString("X") & Convert.ToUInt64(temp2Binary.Substring(64), 2).ToString("X")

                        If BigIntValue > BigInteger.Parse("-9223372036854775809") AndAlso BigIntValue < BigInteger.Parse("9223372036854775808") Then
                            lblInt64Value.Text = BigIntValue.ToString
                            lblUInt64Value.Text = BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToInt64(lblInt64Value.Text)), 0).ToString

                            temp2Binary = Convert.ToString(Convert.ToInt64(lblInt64Value.Text), 2).PadLeft(64, "0"c)
                            temp2Hex = Convert.ToUInt64(temp2Binary, 2).ToString("X")

                            If BigIntValue > -2147483649 AndAlso BigIntValue < 2147483648 Then
                                lblInt32Value.Text = lblInt128Value.Text
                                lblUInt32Value.Text = BitConverter.ToUInt32(BitConverter.GetBytes(Convert.ToInt32(lblInt32Value.Text)), 0).ToString

                                temp2Binary = Convert.ToString(Convert.ToInt32(lblInt32Value.Text), 2).PadLeft(32, "0"c)
                                temp2Hex = Convert.ToUInt64(temp2Binary, 2).ToString("X")

                                If BigIntValue > -32769 AndAlso BigIntValue < 32768 Then
                                    lblInt16Value.Text = lblInt128Value.Text
                                    lblUInt16Value.Text = BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToInt16(lblInt16Value.Text)), 0).ToString

                                    temp2Binary = Convert.ToString(Convert.ToInt16(lblInt16Value.Text), 2).PadLeft(16, "0"c)
                                    temp2Hex = Convert.ToUInt64(temp2Binary, 2).ToString("X")

                                    If BigIntValue > -129 AndAlso BigIntValue < 128 Then
                                        lblInt8Value.Text = lblInt128Value.Text
                                        lblUInt8Value.Text = BitConverter.GetBytes(Convert.ToSByte(lblInt8Value.Text))(0).ToString

                                        temp2Binary = Convert.ToString(Convert.ToSByte(lblInt8Value.Text), 2).PadLeft(8, "0"c)
                                        temp2Hex = Convert.ToUInt64(temp2Binary, 2).ToString("X")
                                    Else
                                        For i = 0 To 3
                                            lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                            lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                            If i <> 3 Then
                                                lblUInt8Value.Text &= " , "
                                                lblInt8Value.Text &= " , "
                                            End If
                                        Next
                                    End If
                                Else
                                    For i = 0 To 1
                                        lblUInt16Value.Text &= Convert.ToUInt16(temp2Binary.Substring(i * 16, 16), 2).ToString
                                        lblInt16Value.Text &= Convert.ToInt16(temp2Binary.Substring(i * 16, 16), 2).ToString

                                        If i <> 1 Then
                                            lblUInt16Value.Text &= " , "
                                            lblInt16Value.Text &= " , "
                                        End If
                                    Next

                                    For i = 0 To 3
                                        lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                        lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                        If i <> 3 Then
                                            lblUInt8Value.Text &= " , "
                                            lblInt8Value.Text &= " , "
                                        End If
                                    Next
                                End If

                            Else
                                For i = 0 To 1
                                    lblUInt32Value.Text &= Convert.ToUInt32(temp2Binary.Substring(i * 32, 32), 2).ToString
                                    lblInt32Value.Text &= Convert.ToInt32(temp2Binary.Substring(i * 32, 32), 2).ToString

                                    If i <> 1 Then
                                        lblUInt32Value.Text &= " , "
                                        lblInt32Value.Text &= " , "
                                    End If
                                Next

                                For i = 0 To 3
                                    lblUInt16Value.Text &= Convert.ToUInt16(temp2Binary.Substring(i * 16, 16), 2).ToString
                                    lblInt16Value.Text &= Convert.ToInt16(temp2Binary.Substring(i * 16, 16), 2).ToString

                                    If i <> 3 Then
                                        lblUInt16Value.Text &= " , "
                                        lblInt16Value.Text &= " , "
                                    End If
                                Next

                                For i = 0 To 7
                                    lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                    lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                    If i <> 7 Then
                                        lblUInt8Value.Text &= " , "
                                        lblInt8Value.Text &= " , "
                                    End If
                                Next
                            End If
                        Else
                            For i = 0 To 1
                                lblUInt64Value.Text &= Convert.ToUInt64(temp2Binary.Substring(i * 64, 64), 2).ToString
                                lblInt64Value.Text &= Convert.ToInt64(temp2Binary.Substring(i * 64, 64), 2).ToString

                                If i <> 1 Then
                                    lblUInt64Value.Text &= " , "
                                    lblInt64Value.Text &= " , "
                                End If
                            Next

                            For i = 0 To 3
                                lblUInt32Value.Text &= Convert.ToUInt32(temp2Binary.Substring(i * 32, 32), 2).ToString
                                lblInt32Value.Text &= Convert.ToInt32(temp2Binary.Substring(i * 32, 32), 2).ToString

                                If i <> 3 Then
                                    lblUInt32Value.Text &= " , "
                                    lblInt32Value.Text &= " , "
                                End If
                            Next

                            For i = 0 To 7
                                lblUInt16Value.Text &= Convert.ToUInt16(temp2Binary.Substring(i * 16, 16), 2).ToString
                                lblInt16Value.Text &= Convert.ToInt16(temp2Binary.Substring(i * 16, 16), 2).ToString

                                If i <> 7 Then
                                    lblUInt16Value.Text &= " , "
                                    lblInt16Value.Text &= " , "
                                End If
                            Next

                            For i = 0 To 15
                                lblUInt8Value.Text &= Convert.ToByte(temp2Binary.Substring(i * 8, 8), 2).ToString
                                lblInt8Value.Text &= Convert.ToSByte(temp2Binary.Substring(i * 8, 8), 2).ToString

                                If i <> 15 Then
                                    lblUInt8Value.Text &= " , "
                                    lblInt8Value.Text &= " , "
                                End If
                            Next
                        End If

                        Set_Binary_Value(temp2Binary)
                        Set_Hex_Value(temp2Hex.PadLeft(CInt(temp2Binary.Length / 4), "0"c))
                        Set_Octal_Value(temp2Binary)

                        Exit Sub
                    ElseIf ComboBoxNumberType.SelectedIndex = 7 Then 'Integer / Int32

                        Dim IntegerValue = Convert.ToInt32(TextBoxNumber.Text)

                        lblInt32Value.Text = IntegerValue.ToString
                        lblUInt32Value.Text = BitConverter.ToUInt32(BitConverter.GetBytes(IntegerValue), 0).ToString

                        lblFloat32Value.Text = Convert.ToSingle(lblInt32Value.Text).ToString("G9")
                        lblFloat64Value.Text = Convert.ToDouble(lblInt32Value.Text).ToString("G17")

                        If tempBinary.StartsWith("1") Then
                            lblInt128Value.Text = BitConverterInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                            lblUInt128Value.Text = BitConverterUInt128(temp2Binary.PadLeft(128, "1"c)).ToString

                            lblInt64Value.Text = BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToInt64(temp2Binary.PadLeft(64, "1"c), 2)), 0).ToString
                            lblUInt64Value.Text = BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToUInt64(temp2Binary.PadLeft(64, "1"c), 2)), 0).ToString
                        Else
                            lblInt128Value.Text = CType(IntegerValue, BigInteger).ToString
                            lblUInt128Value.Text = BitConverterUInt128(ChangeBigInteger2BinaryString(BigInteger.Parse(lblInt128Value.Text))).ToString

                            lblInt64Value.Text = BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToInt64(temp2Binary, 2)), 0).ToString
                            lblUInt64Value.Text = BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToUInt64(temp2Binary, 2)), 0).ToString
                        End If

                    ElseIf ComboBoxNumberType.SelectedIndex = 8 Then 'UInteger / UInt32

                        Dim UIntegerValue = Convert.ToUInt32(TextBoxNumber.Text)

                        lblInt128Value.Text = CType(UIntegerValue, BigInteger).ToString
                        lblUInt128Value.Text = BitConverterUInt128(ChangeBigInteger2BinaryString(BigInteger.Parse(lblInt128Value.Text))).ToString

                        lblUInt64Value.Text = CType(UIntegerValue, ULong).ToString
                        lblInt64Value.Text = CType(UIntegerValue, Long).ToString

                        lblUInt32Value.Text = UIntegerValue.ToString
                        lblInt32Value.Text = CType(UIntegerValue, Integer).ToString

                        lblFloat32Value.Text = Convert.ToSingle(lblInt32Value.Text).ToString("G9")
                        lblFloat64Value.Text = Convert.ToDouble(lblInt32Value.Text).ToString("G17")

                    Else 'Binary or Hex or Octal

                        If tempBinary.StartsWith("1") Then
                            lblInt128Value.Text = BitConverterInt128(temp2Binary.PadLeft(128, "1"c)).ToString
                            lblUInt128Value.Text = BitConverterUInt128(temp2Binary.PadLeft(128, "1"c)).ToString

                            lblUInt64Value.Text = Convert.ToUInt64(temp2Binary.PadLeft(64, "1"c), 2).ToString
                            lblInt64Value.Text = Convert.ToInt64(temp2Binary.PadLeft(64, "1"c), 2).ToString
                        Else
                            lblInt128Value.Text = BitConverterInt128(temp2Binary).ToString
                            lblUInt128Value.Text = BitConverterUInt128(temp2Binary).ToString

                            lblUInt64Value.Text = Convert.ToUInt64(temp2Binary, 2).ToString
                            lblInt64Value.Text = Convert.ToInt64(temp2Binary, 2).ToString
                        End If

                        lblUInt32Value.Text = Convert.ToUInt32(temp2Binary, 2).ToString
                        lblInt32Value.Text = Convert.ToInt32(temp2Binary, 2).ToString

                        lblFloat32Value.Text = Convert.ToSingle(lblInt32Value.Text).ToString("G9")
                        lblFloat64Value.Text = Convert.ToDouble(lblInt32Value.Text).ToString("G17")

                    End If

                    tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToDouble(lblFloat64Value.Text)), 0), 2).PadLeft(64, "0"c)
                    Set_BinaryF64_Value(tempBinaryF64)

                    tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(lblFloat32Value.Text)), 0), 2).PadLeft(32, "0"c)
                    Set_BinaryF32_Value(tempBinaryF32)

                    If CInt(lblInt32Value.Text) > -32769 AndAlso CInt(lblInt32Value.Text) < 32768 Then
                        lblInt16Value.Text = lblInt32Value.Text
                        lblUInt16Value.Text = BitConverter.ToUInt16(BitConverter.GetBytes(Convert.ToInt16(lblInt16Value.Text)), 0).ToString
                    Else
                        For i = 0 To 1
                            lblUInt16Value.Text &= Convert.ToUInt16(tempBinary.Substring(i * 16, 16), 2).ToString
                            lblInt16Value.Text &= Convert.ToInt16(tempBinary.Substring(i * 16, 16), 2).ToString

                            If i <> 1 Then
                                lblUInt16Value.Text &= " , "
                                lblInt16Value.Text &= " , "
                            End If
                        Next
                    End If

                    If CInt(lblInt32Value.Text) > -129 AndAlso CInt(lblInt32Value.Text) < 128 Then
                        lblInt8Value.Text = lblInt32Value.Text
                        lblUInt8Value.Text = BitConverter.GetBytes(Convert.ToSByte(lblInt8Value.Text))(0).ToString
                    Else
                        For i = 0 To 3
                            lblUInt8Value.Text &= Convert.ToByte(tempBinary.Substring(i * 8, 8), 2).ToString
                            lblInt8Value.Text &= Convert.ToSByte(tempBinary.Substring(i * 8, 8), 2).ToString

                            If i <> 3 Then
                                lblUInt8Value.Text &= " , "
                                lblInt8Value.Text &= " , "
                            End If
                        Next
                    End If
                Case 2
                    If tempBinary.StartsWith("1") Then
                        lblUInt128Value.Text = BitConverterUInt128(tempBinary.PadLeft(128, "1"c)).ToString
                        lblInt128Value.Text = BitConverterInt128(tempBinary.PadLeft(128, "1"c)).ToString
                        lblUInt64Value.Text = Convert.ToUInt64(tempBinary.PadLeft(64, "1"c), 2).ToString
                        lblInt64Value.Text = Convert.ToInt64(tempBinary.PadLeft(64, "1"c), 2).ToString
                        lblUInt32Value.Text = Convert.ToUInt32(tempBinary.PadLeft(32, "1"c), 2).ToString
                        lblInt32Value.Text = Convert.ToInt32(tempBinary.PadLeft(32, "1"c), 2).ToString
                    Else
                        lblUInt128Value.Text = BitConverterUInt128(tempBinary.PadLeft(128, "0"c)).ToString
                        lblInt128Value.Text = BitConverterInt128(tempBinary.PadLeft(128, "0"c)).ToString
                        lblUInt64Value.Text = Convert.ToUInt64(tempBinary.PadLeft(64, "0"c), 2).ToString
                        lblInt64Value.Text = Convert.ToInt64(tempBinary.PadLeft(64, "0"c), 2).ToString
                        lblUInt32Value.Text = Convert.ToUInt32(tempBinary.PadLeft(32, "0"c), 2).ToString
                        lblInt32Value.Text = Convert.ToInt32(tempBinary.PadLeft(32, "0"c), 2).ToString
                    End If

                    lblUInt16Value.Text = Convert.ToUInt16(tempBinary, 2).ToString
                    lblInt16Value.Text = Convert.ToInt16(tempBinary, 2).ToString

                    lblFloat64Value.Text = Convert.ToDouble(lblInt16Value.Text).ToString("G17")
                    lblFloat32Value.Text = Convert.ToSingle(lblInt16Value.Text).ToString("G9")

                    Dim tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToDouble(lblFloat64Value.Text)), 0), 2).PadLeft(64, "0"c)
                    Set_BinaryF64_Value(tempBinaryF64)

                    Dim tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(lblFloat32Value.Text)), 0), 2).PadLeft(32, "0"c)
                    Set_BinaryF32_Value(tempBinaryF32)

                    If CInt(lblInt16Value.Text) > -129 AndAlso CInt(lblInt16Value.Text) < 128 Then
                        lblUInt8Value.Text = lblUInt16Value.Text
                        lblInt8Value.Text = lblInt16Value.Text
                    Else
                        For i = 0 To 1
                            lblUInt8Value.Text &= Convert.ToByte(tempBinary.Substring(i * 8, 8), 2).ToString
                            lblInt8Value.Text &= Convert.ToSByte(tempBinary.Substring(i * 8, 8), 2).ToString

                            If i <> 1 Then
                                lblUInt8Value.Text &= " , "
                                lblInt8Value.Text &= " , "
                            End If
                        Next
                    End If
                Case Else '1
                    If tempBinary.StartsWith("1") Then
                        lblUInt128Value.Text = BitConverterUInt128(tempBinary.PadLeft(128, "1"c)).ToString
                        lblInt128Value.Text = BitConverterInt128(tempBinary.PadLeft(128, "1"c)).ToString
                        lblUInt64Value.Text = Convert.ToUInt64(tempBinary.PadLeft(64, "1"c), 2).ToString
                        lblInt64Value.Text = Convert.ToInt64(tempBinary.PadLeft(64, "1"c), 2).ToString
                        lblUInt32Value.Text = Convert.ToUInt32(tempBinary.PadLeft(32, "1"c), 2).ToString
                        lblInt32Value.Text = Convert.ToInt32(tempBinary.PadLeft(32, "1"c), 2).ToString
                        lblUInt16Value.Text = Convert.ToUInt16(tempBinary.PadLeft(16, "1"c), 2).ToString
                        lblInt16Value.Text = Convert.ToInt16(tempBinary.PadLeft(16, "1"c), 2).ToString
                    Else
                        lblUInt128Value.Text = BitConverterUInt128(tempBinary.PadLeft(128, "0"c)).ToString
                        lblInt128Value.Text = BitConverterInt128(tempBinary.PadLeft(128, "0"c)).ToString
                        lblUInt64Value.Text = Convert.ToUInt64(tempBinary.PadLeft(64, "0"c), 2).ToString
                        lblInt64Value.Text = Convert.ToInt64(tempBinary.PadLeft(64, "0"c), 2).ToString
                        lblUInt32Value.Text = Convert.ToUInt32(tempBinary.PadLeft(32, "0"c), 2).ToString
                        lblInt32Value.Text = Convert.ToInt32(tempBinary.PadLeft(32, "0"c), 2).ToString
                        lblUInt16Value.Text = Convert.ToUInt16(tempBinary.PadLeft(16, "0"c), 2).ToString
                        lblInt16Value.Text = Convert.ToInt16(tempBinary.PadLeft(16, "0"c), 2).ToString
                    End If

                    lblUInt8Value.Text = Convert.ToByte(tempBinary, 2).ToString
                    lblInt8Value.Text = Convert.ToSByte(tempBinary, 2).ToString

                    lblFloat64Value.Text = Convert.ToDouble(lblInt8Value.Text).ToString("G17")
                    lblFloat32Value.Text = Convert.ToSingle(lblInt8Value.Text).ToString("G9")

                    Dim tempBinaryF64 = Convert.ToString(BitConverter.ToInt64(BitConverter.GetBytes(Convert.ToDouble(lblFloat64Value.Text)), 0), 2).PadLeft(64, "0"c)
                    Set_BinaryF64_Value(tempBinaryF64)

                    Dim tempBinaryF32 = Convert.ToString(BitConverter.ToInt32(BitConverter.GetBytes(Convert.ToSingle(lblFloat32Value.Text)), 0), 2).PadLeft(32, "0"c)
                    Set_BinaryF32_Value(tempBinaryF32)
            End Select
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try

    End Sub

    Private Sub Set_Hex_Value(tempHex As String)
        Dim m As Integer

        For j = 2 To tempHex.Length - 1 Step 2
            tempHex = tempHex.Insert(j + m, " ")
            m += 1
        Next

        lblHexValue.Text = tempHex
    End Sub

    Private Sub Set_Binary_Value(tempBinary As String)
        If tempBinary.Length > 64 Then
            lblNumberOfBits128.Text = "< 128-bits"
            lblNumberOfBits.Text = ""
            lblBinary128Value.Visible = True

            Dim k As Integer
            Dim tempBinary1 As String = tempBinary.Substring(0, 64)
            Dim tempBinary2 As String = tempBinary.Substring(64)

            For j = 4 To 63 Step 4
                tempBinary1 = tempBinary1.Insert(j + k, " ")
                tempBinary2 = tempBinary2.Insert(j + k, " ")
                k += 1
            Next

            lblBinaryValue.Text = tempBinary1
            lblBinary128Value.Text = tempBinary2
        Else
            lblNumberOfBits.Text = "< " & tempBinary.Length.ToString & "-bits"
            lblNumberOfBits128.Text = ""
            lblBinary128Value.Visible = False

            Dim k As Integer
            For j = 4 To tempBinary.Length - 1 Step 4
                tempBinary = tempBinary.Insert(j + k, " ")
                k += 1
            Next

            lblBinaryValue.Text = tempBinary
        End If
    End Sub

    Private Sub Set_BinaryF32_Value(tempBinaryF32 As String)
        tempBinaryF32 = tempBinaryF32.Insert(1, " ")
        tempBinaryF32 = tempBinaryF32.Insert(10, " ")

        lblBinaryF32Value.Text = tempBinaryF32
    End Sub

    Private Sub Set_BinaryF64_Value(tempBinaryF64 As String)
        tempBinaryF64 = tempBinaryF64.Insert(1, " ")
        tempBinaryF64 = tempBinaryF64.Insert(13, " ")

        lblBinaryF64Value.Text = tempBinaryF64
    End Sub

    Private Sub Set_Octal_Value(tempBinary As String)
        If BigInteger.Parse(tempBinary) = 0 Then
            lblOctalValue.Text = "0"
        Else
            Dim tempOctal As String = ""
            Dim tempVal As Integer

            For i = 0 To CInt(Math.Ceiling(tempBinary.Length / 3)) - 1
                For j = 0 To 2
                    If i * 3 + j = tempBinary.Length Then Exit For
                    tempVal += CInt(Val(tempBinary(tempBinary.Length - 1 - (i * 3 + j))) * (2 ^ j))
                Next
                tempOctal &= tempVal.ToString
                tempVal = 0
            Next

            lblOctalValue.Text = StrReverse(tempOctal).TrimStart("0"c)
        End If
    End Sub

    Private Sub SetOutOfRangeLabels()
        lblInt128Value.Text = "Out of Range"
        lblUInt128Value.Text = "Out of Range"
        lblInt64Value.Text = "Out of Range"
        lblUInt64Value.Text = "Out of Range"
        lblInt32Value.Text = "Out of Range"
        lblUInt32Value.Text = "Out of Range"
        lblInt16Value.Text = "Out of Range"
        lblUInt16Value.Text = "Out of Range"
        lblInt8Value.Text = "Out of Range"
        lblUInt8Value.Text = "Out of Range"
        lblHexValue.Text = "Out of Range"
        lblOctalValue.Text = "Out of Range"
        lblBinaryValue.Text = "Out of Range"
        lblBinary128Value.Text = "Out of Range"
    End Sub

    Private Sub Key_Click(sender As Object, e As EventArgs) Handles KeySign.Click, KeyF.Click, KeyE.Click, KeyDot.Click, KeyD.Click, KeyC.Click, KeyB.Click, KeyA.Click, Key9.Click, Key8.Click, Key7.Click, Key6.Click, Key5.Click, Key4.Click, Key3.Click, Key2.Click, Key1.Click, Key0.Click
        Dim sndr = DirectCast(sender, Button)
        Dim currentCursorPosition As Integer = TextBoxNumber.SelectionStart

        If TextBoxNumber.Text.Length < TextBoxNumber.MaxLength Then
            If TextBoxNumber.Text = "" Then
                TextBoxNumber.Text = sndr.Text
            Else
                If TextBoxNumber.SelectionLength > 0 Then
                    TextBoxNumber.Text = TextBoxNumber.Text.Substring(0, currentCursorPosition) & sndr.Text & TextBoxNumber.Text.Substring(currentCursorPosition + TextBoxNumber.SelectionLength)
                Else
                    TextBoxNumber.Text = TextBoxNumber.Text.Substring(0, currentCursorPosition) & sndr.Text & TextBoxNumber.Text.Substring(currentCursorPosition)
                End If
            End If
        Else
            If TextBoxNumber.SelectionLength > 0 Then
                TextBoxNumber.Text = TextBoxNumber.Text.Substring(0, currentCursorPosition) & sndr.Text & TextBoxNumber.Text.Substring(currentCursorPosition + TextBoxNumber.SelectionLength)
            End If
        End If

        TextBoxNumber.SelectionLength = 0
        TextBoxNumber.SelectionStart = currentCursorPosition + 1
        TextBoxNumber.Focus()
    End Sub

    Private Sub KeyBackSpace_Click(sender As Object, e As EventArgs) Handles BackSpace.Click
        Dim currentCursorPosition As Integer = TextBoxNumber.SelectionStart

        If currentCursorPosition > 0 Then
            If TextBoxNumber.SelectionLength = 0 Then
                TextBoxNumber.Text = TextBoxNumber.Text.Substring(0, TextBoxNumber.SelectionStart - 1) & TextBoxNumber.Text.Substring(TextBoxNumber.SelectionStart)
                TextBoxNumber.SelectionStart = currentCursorPosition - 1
            Else
                TextBoxNumber.Text = TextBoxNumber.Text.Substring(0, TextBoxNumber.SelectionStart) & TextBoxNumber.Text.Substring(TextBoxNumber.SelectionLength + TextBoxNumber.SelectionStart)
                TextBoxNumber.SelectionStart = currentCursorPosition
            End If
        Else
            If TextBoxNumber.SelectionLength > 0 Then
                TextBoxNumber.Text = TextBoxNumber.Text.Substring(TextBoxNumber.SelectionLength)
            End If
        End If

        TextBoxNumber.Focus()
    End Sub

    Private Sub LabelValue_MouseClick(sender As Object, e As MouseEventArgs) Handles lblUInt8Value.MouseClick, lblUInt64Value.MouseClick, lblUInt32Value.MouseClick, lblUInt16Value.MouseClick, lblUInt128Value.MouseClick, lblOctalValue.MouseClick, lblInt8Value.MouseClick, lblInt64Value.MouseClick, lblInt32Value.MouseClick, lblInt16Value.MouseClick, lblInt128Value.MouseClick, lblHexValue.MouseClick, lblBinaryF32Value.MouseClick, lblFloat64Value.MouseClick, lblFloat32Value.MouseClick, lblBinaryValue.MouseClick, lblBinaryF64Value.MouseClick, lblBinary128Value.MouseClick
        Dim lbl = DirectCast(sender, Label)

        If e.Button = MouseButtons.Right Then
            If lbl.Text <> "" AndAlso Not (lbl.Text.Contains("NaN") OrElse lbl.Text = "Out of Range") AndAlso lbl.Text.IndexOf(","c) = -1 Then
                'Allow copying of a single value to the Clipboard

                If lblBinary128Value.Visible AndAlso (lbl.Name = "lblBinaryValue" OrElse lbl.Name = "lblBinary128Value") Then
                    labelValueText = lblBinaryValue.Text & lblBinary128Value.Text
                Else
                    labelValueText = lbl.Text
                End If

                'Remove spaces from Binary or Hex values
                If lbl.Name = "lblHexValue" OrElse lbl.Name = "lblHexFValue" OrElse lbl.Name = "lblBinaryValue" OrElse lbl.Name = "lblBinaryF32Value" OrElse lbl.Name = "lblBinaryF64Value" OrElse lbl.Name = "lblBinary128Value" Then
                    Dim j As Integer
                    For i = 0 To labelValueText.Length - 1
                        If labelValueText(i - j) = " " Then
                            labelValueText = labelValueText.Remove(i - j, 1)
                            j += 1
                        End If
                    Next
                End If

                If Not flagContextSet Then
                    'Create context menu with "Copy" option
                    Dim menuItem = Context.Items.Add("Copy")
                    flagContextSet = True
                End If
                'Show context menu with "Copy" option
                Context.Show(Me, New Point(lbl.Location.X + e.X, lbl.Location.Y + e.Y))
            End If
        End If
    End Sub

    Private Sub Context_ItemClicked(sender As Object, e As System.Windows.Forms.ToolStripItemClickedEventArgs)
        My.Computer.Clipboard.SetText(labelValueText)
    End Sub

    Private Sub LabelValue_MouseEnter(sender As Object, e As EventArgs) Handles lblUInt8Value.MouseEnter, lblUInt64Value.MouseEnter, lblUInt32Value.MouseEnter, lblUInt16Value.MouseEnter, lblUInt128Value.MouseEnter, lblOctalValue.MouseEnter, lblInt8Value.MouseEnter, lblInt64Value.MouseEnter, lblInt32Value.MouseEnter, lblInt16Value.MouseEnter, lblInt128Value.MouseEnter, lblHexValue.MouseEnter, lblBinaryF32Value.MouseEnter, lblFloat64Value.MouseEnter, lblFloat32Value.MouseEnter, lblBinaryValue.MouseEnter, lblBinaryF64Value.MouseEnter, lblBinary128Value.MouseEnter
        Dim lbl = DirectCast(sender, Label)

        If lbl.Text <> "" Then
            If lblBinary128Value.Visible AndAlso (lbl.Name = "lblBinaryValue" OrElse lbl.Name = "lblBinary128Value") Then
                lblBinaryValue.BackColor = Color.Blue
                lblBinary128Value.BackColor = Color.Blue
            Else
                lbl.BackColor = Color.Blue
            End If
        End If
    End Sub

    Private Sub LabelValue_MouseLeave(sender As Object, e As EventArgs) Handles lblUInt8Value.MouseLeave, lblUInt64Value.MouseLeave, lblUInt32Value.MouseLeave, lblUInt16Value.MouseLeave, lblUInt128Value.MouseLeave, lblOctalValue.MouseLeave, lblInt8Value.MouseLeave, lblInt64Value.MouseLeave, lblInt32Value.MouseLeave, lblInt16Value.MouseLeave, lblInt128Value.MouseLeave, lblHexValue.MouseLeave, lblBinaryF32Value.MouseLeave, lblFloat64Value.MouseLeave, lblFloat32Value.MouseLeave, lblBinaryValue.MouseLeave, lblBinaryF64Value.MouseLeave, lblBinary128Value.MouseLeave
        Dim lbl = DirectCast(sender, Label)

        If lbl.Text <> "" Then
            If lblBinary128Value.Visible AndAlso (lbl.Name = "lblBinaryValue" OrElse lbl.Name = "lblBinary128Value") Then
                lblBinaryValue.BackColor = Color.Navy
                lblBinary128Value.BackColor = Color.Navy
            Else
                lbl.BackColor = Color.Navy
            End If
        End If
    End Sub

    Private Sub LabelBinary128Value_VisibleChanged(sender As Object, e As EventArgs) Handles lblBinary128Value.VisibleChanged
        If lblBinary128Value.Visible Then
            lblIntegerBinaryFormat.Location = New Point(304, 610)
        Else
            lblIntegerBinaryFormat.Location = New Point(304, 590)
        End If
    End Sub

#End Region

#Region "Functions"

    Private ReadOnly m_Lock As New Object

    Private Function BitConverterInt128(binaryString As String) As BigInteger
        SyncLock m_Lock
            Dim Int128 As BigInteger = 0

            For i = 0 To binaryString.Length - 2
                If binaryString(binaryString.Length - 1 - i) = "1"c Then
                    Int128 += CType(2 ^ i, BigInteger)
                End If
            Next

            If binaryString(0) = "0"c Then
                Return Int128
            Else
                Return CType(-2 ^ (binaryString.Length - 1), BigInteger) + Int128
            End If
        End SyncLock
    End Function

    Private Function BitConverterUInt128(binaryString As String) As BigInteger
        SyncLock m_Lock
            Dim UInt128 As BigInteger = 0
            For i = 0 To binaryString.Length - 1
                If binaryString(binaryString.Length - 1 - i) = "1"c Then
                    UInt128 += CType(2 ^ i, BigInteger)
                End If
            Next
            Return UInt128
        End SyncLock
    End Function

    Private Function ChangeBigInteger2BinaryString(BigInt As BigInteger) As String 'For positive values
        SyncLock m_Lock
            Dim bytes(16) As Byte
            Array.Copy(BigInt.ToByteArray, bytes, BigInt.ToByteArray.Length)

            Dim binaryString As String = ""

            For i = bytes.Length - 2 To 0 Step -1
                binaryString &= Convert.ToString(bytes(i), 2).PadLeft(8, "0"c)
            Next

            Return binaryString
        End SyncLock
    End Function

#End Region

#Region "ToolTips"

    Private Sub ButtonBackSpace_MouseHover(sender As Object, e As EventArgs) Handles BackSpace.MouseHover
        AllToolTip.SetToolTip(BackSpace, "BackSpace")
    End Sub

    Private Sub LabelFloat64_MouseHover(sender As Object, e As EventArgs) Handles lblFloat64.MouseHover
        AllToolTip.SetToolTip(lblFloat64, "64-bit floating-point number, aka Double or LREAL. Range: -1.79769313486231570E+308 to -4.94065645841246544E-324 and 4.94065645841246544E-324 to 1.79769313486231570E+308." & Environment.NewLine & "Displayed as either a single or multiple Float64 numbers.")
    End Sub

    Private Sub LabelFloat32_MouseHover(sender As Object, e As EventArgs) Handles lblFloat32.MouseHover
        AllToolTip.SetToolTip(lblFloat32, "32-bit floating-point number, aka Single or REAL. Range: -3.4028235E+38 to -1.401298E-45 and 1.401298E-45 to 3.4028235E+38." & Environment.NewLine & "Displayed as either a single or multiple Float32 numbers.")
    End Sub

    Private Sub LabelUInt128_MouseHover(sender As Object, e As EventArgs) Handles lblUInt128.MouseHover
        AllToolTip.SetToolTip(lblUInt128, "Unsigned 128-bit Integer number, aka UQINT. Range: 0 to 340,282,366,920,938,463,463,374,607,431,768,211,455.")
    End Sub

    Private Sub LabelInt128_MouseHover(sender As Object, e As EventArgs) Handles lblInt128.MouseHover
        AllToolTip.SetToolTip(lblInt128, "Signed 128-bit Integer number, aka QINT. Range: −170,141,183,460,469,231,731,687,303,715,884,105,728 to 170,141,183,460,469,231,731,687,303,715,884,105,727.")
    End Sub

    Private Sub LabelUInt64_MouseHover(sender As Object, e As EventArgs) Handles lblUInt64.MouseHover
        AllToolTip.SetToolTip(lblUInt64, "Unsigned 64-bit Integer number, aka ULong or ULINT. Range: 0 to 18,446,744,073,709,551,615." & Environment.NewLine & "Displayed as either a single or multiple UInt64 numbers.")
    End Sub

    Private Sub LabelInt64_MouseHover(sender As Object, e As EventArgs) Handles lblInt64.MouseHover
        AllToolTip.SetToolTip(lblInt64, "Signed 64-bit Integer number, aka Long or LINT. Range: -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807." & Environment.NewLine & "Displayed as either a single or multiple Int64 numbers.")
    End Sub

    Private Sub LabelUInt32_MouseHover(sender As Object, e As EventArgs) Handles lblUInt32.MouseHover
        AllToolTip.SetToolTip(lblUInt32, "Unsigned 32-bit Integer number, aka UInteger or UDINT. Range: 0 to 4,294,967,295." & Environment.NewLine & "Displayed as either a single or multiple UInt32 numbers.")
    End Sub

    Private Sub LabelInt32_MouseHover(sender As Object, e As EventArgs) Handles lblInt32.MouseHover
        AllToolTip.SetToolTip(lblInt32, "Signed 32-bit Integer number, aka Integer or DINT. Range: -2,147,483,648 to 2,147,483,647." & Environment.NewLine & "Displayed as either a single or multiple Int32 numbers.")
    End Sub

    Private Sub LabelUInt16_MouseHover(sender As Object, e As EventArgs) Handles lblUInt16.MouseHover
        AllToolTip.SetToolTip(lblUInt16, "Unsigned 16-bit Integer number, aka UShort or UINT. Range: 0 to 65,535." & Environment.NewLine & "Displayed as either a single or multiple UInt16 numbers.")
    End Sub

    Private Sub LabelInt16_MouseHover(sender As Object, e As EventArgs) Handles lblInt16.MouseHover
        AllToolTip.SetToolTip(lblInt16, "Signed 16-bit Integer number, aka Short or INT. Range: -32,768 to 32,767." & Environment.NewLine & "Displayed as either a single or multiple Int16 numbers.")
    End Sub

    Private Sub LabelUInt8_MouseHover(sender As Object, e As EventArgs) Handles lblUInt8.MouseHover
        AllToolTip.SetToolTip(lblUInt8, "Unsigned 8-bit Integer number, aka Byte or USINT. Range: 0 to 255." & Environment.NewLine & "Displayed as either a single or multiple UInt8 numbers.")
    End Sub

    Private Sub LabelInt8_MouseHover(sender As Object, e As EventArgs) Handles lblInt8.MouseHover
        AllToolTip.SetToolTip(lblInt8, "Signed 8-bit Integer number, aka SByte or SINT. Range: -128 to 127." & Environment.NewLine & "Displayed as either a single or multiple Int8 numbers.")
    End Sub

    Private Sub LabelOctal_MouseHover(sender As Object, e As EventArgs) Handles lblOctal.MouseHover
        AllToolTip.SetToolTip(lblOctal, "Octal representation of the integer value." & Environment.NewLine & "Base 8 (8^n). Each digit number, representing 3 bits, is either of 0 to 7.")
    End Sub

    Private Sub LabelHex_MouseHover(sender As Object, e As EventArgs) Handles lblHex.MouseHover
        AllToolTip.SetToolTip(lblHex, "Hexadecimal representation of the integer value." & Environment.NewLine & "Base 16 (16^n). Each digit, representing 4 bits (nibble), is either of 0 to 9 or A to F." & Environment.NewLine & "Displayed as groups of hexadecimal bytes.")
    End Sub

    Private Sub LabelBinary_MouseHover(sender As Object, e As EventArgs) Handles lblBinary.MouseHover
        AllToolTip.SetToolTip(lblBinary, "Binary representation of the integer value." & Environment.NewLine & "Base 2 (2^n). Each digit number is either 0 or 1." & Environment.NewLine & "Displayed as the lowest of 8/16/32/64/128 bits in groups of 4 bits (nibble).")
    End Sub

    Private Sub LabelBinaryF32_MouseHover(sender As Object, e As EventArgs) Handles lblBinaryF32.MouseHover
        AllToolTip.SetToolTip(lblBinaryF32, "32-bit Floating-point binary representation." & Environment.NewLine & "Base 2, each digit number is either 0 or 1." & Environment.NewLine & "Displayed as Sign-Exponent-Fraction (1-8-23 bits).")
    End Sub

    Private Sub LabelBinaryF64_MouseHover(sender As Object, e As EventArgs) Handles lblBinaryF64.MouseHover
        AllToolTip.SetToolTip(lblBinaryF64, "64-bit Floating-point binary representation." & Environment.NewLine & "Base 2, each digit number is either 0 or 1." & Environment.NewLine & "Displayed as Sign-Exponent-Fraction (1-11-52 bits).")
    End Sub

    Private Sub LabelIntegerBinaryFormat_MouseHover(sender As Object, e As EventArgs) Handles lblIntegerBinaryFormat.MouseHover
        AllToolTip.SetToolTip(lblIntegerBinaryFormat, "Where n = 0 to (totalNumberOfBits - 1).")
    End Sub

    Private Sub LabelFloatBinaryFormat_MouseHover(sender As Object, e As EventArgs) Handles lblFloatBinaryFormat.MouseHover
        AllToolTip.SetToolTip(lblFloatBinaryFormat, "32-bit bits format: 1-8-23." & Environment.NewLine & "64-bit bits format: 1-11-52.")
    End Sub

#End Region

End Class
