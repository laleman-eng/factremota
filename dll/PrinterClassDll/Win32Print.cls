VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "Win32Print"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
Public Function SetPrinterName(ByVal PrinterName As String)
    Dim prnPrinter As Printer
    
    For Each prnPrinter In Printers
       If prnPrinter.DeviceName = PrinterName Then
          Set Printer = prnPrinter
          Exit For
       End If
    Next
End Function

Public Function SetDeviceFont(ByVal FontSize As Single, ByVal FontName As String, ByVal BoldType As Boolean, ByVal FontColor As Boolean)
    Printer.FontSize = FontSize
    Printer.FontName = FontName
    Printer.FontBold = BoldType
    If FontColor Then
        Printer.ForeColor = RGB(255, 0, 0)
    Else
        Printer.ForeColor = RGB(0, 0, 0)
    End If
End Function

Public Function NewPage()
    Printer.NewPage
End Function

Public Function EndDoc()
    Printer.EndDoc
End Function

Public Function PrintText(ByVal Text As String)
    Printer.Print Text
End Function

Public Function PrintImage(ByVal FileName As String)
    Dim pic As Picture

    Set pic = LoadPicture(FileName)

    Printer.PaintPicture pic, Printer.CurrentX, Printer.CurrentY
    Printer.CurrentY = Printer.CurrentY + pic.Height
End Function

Public Function OpenCashdrawer(ByVal PinNumber As Integer)
    Printer.FontSize = 9.5
    Printer.FontName = "FontControl"
    Printer.FontBold = False
    
    If PinNumber = 2 Then
        Printer.Print "A"
    ElseIf PinNumber = 5 Then
        Printer.Print "a"
    Else
        MsgBox "Don't support port number", vbCritical
    End If
End Function
