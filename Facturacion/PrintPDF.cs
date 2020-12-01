using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
//using PrinterClassDll;

namespace FactRemota
{
    class PrintPDF
    {
        DTEDoc oDteDoc;
        XmlDocument PrinterDefXml;
        XmlDocument TimbreXml;
        bool PrintRecibeConforme = false;

        public PrintPDF()
        {
            try 
            {
                PrinterDefXml = Utils.CargarXML("FormatoFactura.xml");
            }
            catch 
            {
                throw new Exception("E701 - Formato de documento no definido.");
            }
        }

        public void DoPrint(DTEDoc DteDoc, XmlDocument pTimbreXml)
        {
            XmlNode xmlPrinter = PrinterDefXml.SelectSingleNode("/Format/Printer");
            string NombreImpresora = xmlPrinter.Attributes["Nombre"].Value.ToString();
            bool printerFind = false;

            foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                if (printerName == NombreImpresora)
                {
                    printerFind = true;
                    break;
                }
            if (!printerFind)
                throw new Exception("E702 - impresora inexistente : " + NombreImpresora);

            oDteDoc = DteDoc;
            TimbreXml = pTimbreXml;

            PrintDocument docToPrint = new PrintDocument();
            docToPrint.DocumentName = "Customer Receipt";
            docToPrint.PrinterSettings.PrinterName = NombreImpresora;
            if (DteDoc.Encabezado.IdDoc.TipoDTE == 33)
            {
                docToPrint.PrintPage += new PrintPageEventHandler(this.PrintFactura);
                PrintRecibeConforme = true;
                docToPrint.Print();
                PrintRecibeConforme = false;
                docToPrint.Print();
            }
            else if (DteDoc.Encabezado.IdDoc.TipoDTE == 34)
            {
                docToPrint.PrintPage += new PrintPageEventHandler(this.PrintFacturaE);
                PrintRecibeConforme = true;
                docToPrint.Print();
                PrintRecibeConforme = false;
                docToPrint.Print();
            }
            else if (DteDoc.Encabezado.IdDoc.TipoDTE == 39)
            {
                docToPrint.PrintPage += new PrintPageEventHandler(this.PrintBoleta);
                PrintRecibeConforme = false;
                docToPrint.Print();
            }
            else if (DteDoc.Encabezado.IdDoc.TipoDTE == 41)
            {
                docToPrint.PrintPage += new PrintPageEventHandler(this.PrintBoletaE);
                PrintRecibeConforme = false;
                docToPrint.Print();
            }
            else if (DteDoc.Encabezado.IdDoc.TipoDTE == 52)
            {
                docToPrint.PrintPage += new PrintPageEventHandler(this.PrintGuia);
                //PrintRecibeConforme = true;
                //docToPrint.Print();
                PrintRecibeConforme = false;
                docToPrint.Print();
            }
            else if (DteDoc.Encabezado.IdDoc.TipoDTE == 61)
            {
                docToPrint.PrintPage += new PrintPageEventHandler(this.PrintNC);
                //PrintRecibeConforme = true;
                //docToPrint.Print();
                PrintRecibeConforme = false;
                docToPrint.Print();
            }
        }

        private void PrintFacturaE(object sender, PrintPageEventArgs e)
        {
            FontStyle Estilo;

            int pos, left, width, margen, VSpace, height, posCol, jj;
            Rectangle bounds = new Rectangle(); // (left, top, width, height)
            StringFormat stringFormat = new StringFormat();
            String Texto;
            List<string> CodigoImpuesto = new List<string>();
            List<string> DescripcionImpuesto = new List<string>();
            List<decimal> ValorImpuesto = new List<decimal>();
            decimal ImpuestosAdicionales = 0;

            // Leer impuestos
            foreach (XmlNode nodo in PrinterDefXml.SelectNodes("/Format/Impuestos/Impuesto"))
            {
                CodigoImpuesto.Add(nodo.Attributes["Codigo"].Value.ToString());
                DescripcionImpuesto.Add(nodo.Attributes["Texto"].Value.ToString());
                ValorImpuesto.Add(0);
            }
            if (CodigoImpuesto.IndexOf("ND") == -1)
            {
                CodigoImpuesto.Add("ND");
                DescripcionImpuesto.Add("Otro");
                ValorImpuesto.Add(0);
            }

            // Get FONTS
            string Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Font"].Value.ToString();
            float FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Size"].Value);
            bool FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTimes = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTitulo = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontArial = new Font(Fname, FSize, Estilo);

            // Cabecera
            margen = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Margen").InnerText);
            SizeF maxSize = new SizeF(Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Width").InnerText),
                                      Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Height").InnerText));

            bounds = e.PageBounds;
            left = bounds.Left;
            width = bounds.Width;
            bounds.Height = (int)(maxSize.Height) * 4;
            VSpace = (int)(maxSize.Height) / 2;

            if ((int)(maxSize.Width) < bounds.Width)
            {
                int le = (bounds.Width - (int)(maxSize.Width) - 2 * margen) / 2;
                Point p = new Point(bounds.Left + le, bounds.Top + 10);
                bounds.Location = p;
                bounds.Width = (int)(maxSize.Width) + 2 * margen;
            }

            e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            stringFormat.Alignment = StringAlignment.Center;

            e.Graphics.DrawString("R.U.T.  " + globals.gpRUTEmisor, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("FACTURA EXENTA ELECTRONICA", fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("Nro:  " + oDteDoc.Encabezado.IdDoc.Folio, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) * 2 + VSpace, bounds.Width, bounds.Height), stringFormat);

            pos = bounds.Top + bounds.Height + margen;

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SII").InnerText;
            stringFormat.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(Texto, fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontTimes.Height), stringFormat);
            pos = pos + fontTimes.Height;

            // Logo
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Logo").InnerText.ToUpper() == "SI")
            {
                pos = pos + 5;
                Image img = Image.FromFile("logo_cc.png");
                e.Graphics.DrawImage(img, left, pos, img.Width, img.Height);
                pos = pos + img.Height;
            }

            //Datos Emisor
            pos = pos + 10;
            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Empresa").InnerText, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz2").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Giro").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Sucursal").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Vendedor").InnerText.ToUpper() == "SI")
            {
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/TitVendedor").InnerText + oDteDoc.Encabezado.Emisor.CdgVendedor;
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Receptor - Cliente
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            // Datos cliente
            e.Graphics.DrawString(oDteDoc.Encabezado.Receptor.RznSocRecep, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString("RUT: " + oDteDoc.Encabezado.Receptor.RUTRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString("Dirección: " + oDteDoc.Encabezado.Receptor.DirRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (oDteDoc.Encabezado.Receptor.CiudadRecep != "")
            {
                e.Graphics.DrawString("Ciudad: " + oDteDoc.Encabezado.Receptor.CiudadRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.CmnaRecep != "")
            {
                e.Graphics.DrawString("Comuna: " + oDteDoc.Encabezado.Receptor.CmnaRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.GiroRecep != "")
            {
                e.Graphics.DrawString("Giro: " + oDteDoc.Encabezado.Receptor.GiroRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Fecha
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Utils.DateFormat(oDteDoc.Encabezado.IdDoc.FchEmis), fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + margen + fontTitulo.Height;

            // Titulo Detalle
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/Salto").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLeft").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);

            stringFormat.Alignment = StringAlignment.Far;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoRight").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLinea2").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
            pos = pos + 2;

            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/SaltoALinea").InnerText);
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Linea/Cod").InnerText);
            bounds = e.PageBounds;

            for (int linea = 0; linea < oDteDoc.Detalle.Count; linea++)
            {
                string Cant_PUnit = oDteDoc.Detalle[linea].QtyItem.ToString(CultureInfo.CurrentUICulture) + "  X  " + oDteDoc.Detalle[linea].PrcItem.ToString("0,0", CultureInfo.CurrentUICulture);
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(Cant_PUnit, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                e.Graphics.DrawString(oDteDoc.Detalle[linea].CdgItem[0].VlrCodigo.ToString(), fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].MontoItem.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].NmbItem.ToString(), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                //UDF
                if (oDteDoc.Detalle[linea].UDFs != null)
                    for (int i = 0; i < oDteDoc.Detalle[linea].UDFs.Count; i++)
                        pos = PrintUDF(e, pos, "Linea", fontArial, width, left, "F", oDteDoc.Detalle[linea].UDFs[i], i);
                // End UDF
                pos = pos + fontArial.Height;
                if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                    e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
                pos = pos + 2;
            }

            if (oDteDoc.Encabezado.Totales.ImptoReten != null)
                for (int linea = 0; linea < oDteDoc.Encabezado.Totales.ImptoReten.Count; linea++)
                {
                    jj = CodigoImpuesto.IndexOf(oDteDoc.Encabezado.Totales.ImptoReten[linea].TipoImp.Trim());
                    if (jj == -1)
                        jj = CodigoImpuesto.IndexOf("ND");

                    ValorImpuesto[jj] = ValorImpuesto[jj] + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                    ImpuestosAdicionales = ImpuestosAdicionales + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                }

            //Totales
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            //Redondeo
            decimal aDes = 0;
            int aIVA = 0;
            int aNet = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntNeto);
            int aExe = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntExe);
            int aTot = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntTotal);
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
            {
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                    aDes = aDes + oDteDoc.DscRcgGlobal[i].ValorDR;
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "R")
                    aDes = aDes - oDteDoc.DscRcgGlobal[i].ValorDR;
            }
            int aSubto = aNet + aExe + (int)Utils.Round_0(aDes);
            aIVA = aTot - aNet - aExe - decimal.ToInt32(ImpuestosAdicionales);

            if ((int)Utils.Round_0(aDes) != 0)
            {
                stringFormat.Alignment = StringAlignment.Near;
                posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Col"].Value.ToString());
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Texto"].Value.ToString();
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(aSubto.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;

                decimal aDesVal = 0.0M;
                for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Descuento").Attributes["Col"].Value.ToString());
                    Texto = oDteDoc.DscRcgGlobal[i].GlosaDR;
                    if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                        Texto = "( - )" + Texto;
                    else
                        Texto = "( + )" + Texto;
                    aDesVal = oDteDoc.DscRcgGlobal[i].ValorDR;
                    e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(aDesVal.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aNet.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            //ImpuestosAdicionales Adicionales
            for (int i = 0; i < ValorImpuesto.Count; i++)
            {
                if (ValorImpuesto[i] > 0)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    e.Graphics.DrawString(DescripcionImpuesto[i], fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(Utils.Round_0(ValorImpuesto[i]).ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aIVA.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aTot.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // Monto escrito
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pre").Attributes["Texto"].Value.ToString();
            Texto = Texto + montoEscrito.GetMontoEscrito(oDteDoc.Encabezado.Totales.MntTotal);
            Texto = Texto + PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pos").Attributes["Texto"].Value.ToString();
            SizeF oSize;
            oSize = e.Graphics.MeasureString(Texto, fontArial);
            jj = (int)Math.Ceiling(oSize.Width / width);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * jj), stringFormat);
            pos = pos + fontArial.Height * jj;

            // Comentarios UDFs
            if (oDteDoc.Encabezado.UDFs != null)
                for (int i = 0; i < oDteDoc.Encabezado.UDFs.Count; i++)
                    pos = PrintUDF(e, pos, "General", fontArial, width, left, "F", oDteDoc.Encabezado.UDFs[i], i);

            // PDF417
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Separacion").InnerText);
            height = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Height").InnerText);
            PrintPDF_Image(sender, e, left, pos, width, height);
            e.Graphics.DrawString("  -  ", fontArial, Brushes.Black, new Rectangle(left, pos, width, height), stringFormat);
            pos = pos + height + 30;

            // Resolucion SII
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion1").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion2").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

             //recibi conforme
            if (PrintRecibeConforme)
            {
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Nombre:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("RUT:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Fecha:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Firma:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 3;
            }

            pos = pos + fontArial.Height;
            e.Graphics.DrawString("-", fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
        }


        private void PrintFactura(object sender, PrintPageEventArgs e)
        {
            FontStyle Estilo;

            int pos, left, width, margen, VSpace, height, posCol, jj;
            Rectangle bounds = new Rectangle(); // (left, top, width, height)
            StringFormat stringFormat = new StringFormat();
            String Texto;
            List<string> CodigoImpuesto = new List<string>();
            List<string> DescripcionImpuesto = new List<string>();
            List<decimal> ValorImpuesto = new List<decimal>();
            decimal ImpuestosAdicionales = 0;

            // Leer impuestos
            foreach (XmlNode nodo in PrinterDefXml.SelectNodes("/Format/Impuestos/Impuesto"))
            {
                CodigoImpuesto.Add(nodo.Attributes["Codigo"].Value.ToString());
                DescripcionImpuesto.Add(nodo.Attributes["Texto"].Value.ToString());
                ValorImpuesto.Add(0);
            }
            if (CodigoImpuesto.IndexOf("ND") == -1)
            {
                CodigoImpuesto.Add("ND");
                DescripcionImpuesto.Add("Otro");
                ValorImpuesto.Add(0);
            }

            // Get FONTS
            string Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Font"].Value.ToString();
            float FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Size"].Value);
            bool FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTimes = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTitulo = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontArial = new Font(Fname, FSize, Estilo);

            // Cabecera
            margen = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Margen").InnerText);
            SizeF maxSize = new SizeF(Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Width").InnerText),
                                      Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Height").InnerText));

            bounds = e.PageBounds;
            left = bounds.Left;
            width = bounds.Width;
            bounds.Height = (int)(maxSize.Height) * 4;
            VSpace = (int)(maxSize.Height) / 2;

            if ((int)(maxSize.Width) < bounds.Width)
            {
                int le = (bounds.Width - (int)(maxSize.Width) - 2 * margen) / 2;
                Point p = new Point(bounds.Left + le, bounds.Top + 10);
                bounds.Location = p;
                bounds.Width = (int)(maxSize.Width) + 2 * margen;
            }

            e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            stringFormat.Alignment = StringAlignment.Center;

            e.Graphics.DrawString("R.U.T.  " + globals.gpRUTEmisor, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("FACTURA ELECTRONICA", fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("Nro:  " + oDteDoc.Encabezado.IdDoc.Folio, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) * 2 + VSpace, bounds.Width, bounds.Height), stringFormat);

            pos = bounds.Top + bounds.Height + margen;

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SII").InnerText;
            stringFormat.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(Texto, fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontTimes.Height), stringFormat);
            pos = pos + fontTimes.Height;

            // Logo
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Logo").InnerText.ToUpper() == "SI")
            {
                pos = pos + 5;
                Image img = Image.FromFile("logo_cc.png");
                e.Graphics.DrawImage(img, left, pos, img.Width, img.Height);
                pos = pos + img.Height;
            }

            //Datos Emisor
            pos = pos + 10;
            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Empresa").InnerText, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz2").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Giro").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Sucursal").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Vendedor").InnerText.ToUpper() == "SI")
            {
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/TitVendedor").InnerText + oDteDoc.Encabezado.Emisor.CdgVendedor;
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Receptor - Cliente
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            // Datos cliente
            e.Graphics.DrawString(oDteDoc.Encabezado.Receptor.RznSocRecep, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString("RUT: " + oDteDoc.Encabezado.Receptor.RUTRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString("Dirección: " + oDteDoc.Encabezado.Receptor.DirRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (oDteDoc.Encabezado.Receptor.CiudadRecep != "")
            {
                e.Graphics.DrawString("Ciudad: " + oDteDoc.Encabezado.Receptor.CiudadRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.CmnaRecep != "")
            {
                e.Graphics.DrawString("Comuna: " + oDteDoc.Encabezado.Receptor.CmnaRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.GiroRecep != "")
            {
                e.Graphics.DrawString("Giro: " + oDteDoc.Encabezado.Receptor.GiroRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Fecha
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Utils.DateFormat(oDteDoc.Encabezado.IdDoc.FchEmis), fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + margen + fontTitulo.Height;

            // Titulo Detalle
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/Salto").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLeft").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);

            stringFormat.Alignment = StringAlignment.Far;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoRight").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLinea2").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
            pos = pos + 2;

            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/SaltoALinea").InnerText);
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Linea/Cod").InnerText);
            bounds = e.PageBounds;

            for (int linea = 0; linea < oDteDoc.Detalle.Count; linea++)
            {
                string Cant_PUnit = oDteDoc.Detalle[linea].QtyItem.ToString(CultureInfo.CurrentUICulture) + "  X  " + oDteDoc.Detalle[linea].PrcItem.ToString("0,0", CultureInfo.CurrentUICulture);
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(Cant_PUnit, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                e.Graphics.DrawString(oDteDoc.Detalle[linea].CdgItem[0].VlrCodigo.ToString(), fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].MontoItem.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].NmbItem.ToString(), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                //UDF
                if (oDteDoc.Detalle[linea].UDFs != null)
                    for (int i = 0; i < oDteDoc.Detalle[linea].UDFs.Count; i++)
                        pos = PrintUDF(e, pos, "Linea", fontArial, width, left, "F", oDteDoc.Detalle[linea].UDFs[i], i);
                // End UDF
                pos = pos + fontArial.Height;
                if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                    e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
                pos = pos + 2;
            }

            if (oDteDoc.Encabezado.Totales.ImptoReten != null)
                for (int linea = 0; linea < oDteDoc.Encabezado.Totales.ImptoReten.Count; linea++)
                {
                    jj = CodigoImpuesto.IndexOf(oDteDoc.Encabezado.Totales.ImptoReten[linea].TipoImp.Trim());
                    if (jj == -1)
                        jj = CodigoImpuesto.IndexOf("ND");

                    ValorImpuesto[jj] = ValorImpuesto[jj] + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                    ImpuestosAdicionales = ImpuestosAdicionales + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                }

            //Totales
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            //Redondeo
            decimal aDes = 0;
            int aIVA = 0;
            int aNet = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntNeto);
            int aExe = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntExe);
            int aTot = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntTotal);
            for (int i=0; i < oDteDoc.DscRcgGlobal.Count ; i++)
            {
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                   aDes = aDes + oDteDoc.DscRcgGlobal[i].ValorDR;
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "R")
                   aDes = aDes - oDteDoc.DscRcgGlobal[i].ValorDR;                
            }
            int aSubto = aNet + aExe + (int)Utils.Round_0(aDes);
            aIVA = aTot - aNet - aExe - decimal.ToInt32(ImpuestosAdicionales);

           if ((int)Utils.Round_0(aDes) != 0)
           {
               stringFormat.Alignment = StringAlignment.Near;
               posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Col"].Value.ToString());
               Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Texto"].Value.ToString();
               e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
               stringFormat.Alignment = StringAlignment.Far;
               e.Graphics.DrawString(aSubto.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
               pos = pos + fontArial.Height;

               decimal aDesVal = 0.0M;
               for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
               {
                   stringFormat.Alignment = StringAlignment.Near;
                   posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Descuento").Attributes["Col"].Value.ToString());
                   Texto = oDteDoc.DscRcgGlobal[i].GlosaDR;
                   if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                       Texto = "( - )" + Texto;
                   else
                       Texto = "( + )" + Texto;
                   aDesVal = oDteDoc.DscRcgGlobal[i].ValorDR;
                   e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                   stringFormat.Alignment = StringAlignment.Far;
                   e.Graphics.DrawString(aDesVal.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                   pos = pos + fontArial.Height;
               }
           }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aNet.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            //ImpuestosAdicionales Adicionales
            for (int i = 0; i < ValorImpuesto.Count ; i++)
            {
                if (ValorImpuesto[i] > 0)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    e.Graphics.DrawString(DescripcionImpuesto[i], fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(Utils.Round_0(ValorImpuesto[i]).ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aIVA.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aTot.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // Monto escrito
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pre").Attributes["Texto"].Value.ToString();
            Texto = Texto + montoEscrito.GetMontoEscrito(oDteDoc.Encabezado.Totales.MntTotal);
            Texto = Texto + PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pos").Attributes["Texto"].Value.ToString();
            SizeF oSize;
            oSize = e.Graphics.MeasureString(Texto, fontArial);
            jj = (int)Math.Ceiling(oSize.Width / width);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height*jj), stringFormat);
            pos = pos + fontArial.Height*jj;

            // Comentarios UDFs
            if (oDteDoc.Encabezado.UDFs != null)
                for (int i = 0; i < oDteDoc.Encabezado.UDFs.Count; i++)
                    pos = PrintUDF(e, pos, "General", fontArial, width, left, "F", oDteDoc.Encabezado.UDFs[i], i);

            // PDF417
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Separacion").InnerText);
            height = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Height").InnerText);
            PrintPDF_Image(sender, e, left, pos, width, height);
            e.Graphics.DrawString("  -  ", fontArial, Brushes.Black, new Rectangle(left, pos, width, height), stringFormat);
            pos = pos + height + 30;

            // Resolucion SII
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion1").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion2").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // recibi conforme
            if (PrintRecibeConforme)
            {
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Nombre:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("RUT:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Fecha:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Firma:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 3;
            }

            pos = pos + fontArial.Height;
            e.Graphics.DrawString("-", fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
        }

        private void PrintBoleta(object sender, PrintPageEventArgs e)
        {
            FontStyle Estilo;

            int pos, left, width, margen, VSpace, height, posCol;
            Rectangle bounds = new Rectangle(); // (left, top, width, height)
            StringFormat stringFormat = new StringFormat();
            String Texto;
            List<string> CodigoImpuesto = new List<string>();
            List<string> DescripcionImpuesto = new List<string>();
            List<decimal> ValorImpuesto = new List<decimal>();
            decimal ImpuestosAdicionales = 0;

            // Leer impuestos
            foreach (XmlNode nodo in PrinterDefXml.SelectNodes("/Format/Impuestos/Impuesto"))
            {
                CodigoImpuesto.Add(nodo.Attributes["Codigo"].Value.ToString());
                DescripcionImpuesto.Add(nodo.Attributes["Texto"].Value.ToString());
                ValorImpuesto.Add(0);
            }
            if (CodigoImpuesto.IndexOf("ND") == -1)
            {
                CodigoImpuesto.Add("ND");
                DescripcionImpuesto.Add("Otro");
                ValorImpuesto.Add(0);
            }

            // Get FONTS
            string Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Font"].Value.ToString();
            float FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Size"].Value);
            bool FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTimes = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTitulo = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontArial = new Font(Fname, FSize, Estilo);

            // Cabecera
            margen = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Margen").InnerText);
            SizeF maxSize = new SizeF(Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Width").InnerText),
                                      Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Height").InnerText));

            bounds = e.PageBounds;
            left = bounds.Left;
            width = bounds.Width;
            bounds.Height = (int)(maxSize.Height) * 4;
            VSpace = (int)(maxSize.Height) / 2;

            if ((int)(maxSize.Width) < bounds.Width)
            {
                int le = (bounds.Width - (int)(maxSize.Width) - 2 * margen) / 2;
                Point p = new Point(bounds.Left + le, bounds.Top + 10);
                bounds.Location = p;
                bounds.Width = (int)(maxSize.Width) + 2 * margen;
            }

            e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            stringFormat.Alignment = StringAlignment.Center;

            e.Graphics.DrawString("R.U.T.  " + globals.gpRUTEmisor, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("BOLETA ELECTRONICA", fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("Nro:  " + oDteDoc.Encabezado.IdDoc.Folio, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) * 2 + VSpace, bounds.Width, bounds.Height), stringFormat);

            pos = bounds.Top + bounds.Height + margen;

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SII").InnerText;
            stringFormat.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(Texto, fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontTimes.Height), stringFormat);
            pos = pos + fontTimes.Height;

            // Logo
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Logo").InnerText.ToUpper() == "SI")
            {
                pos = pos + 5;
                Image img = Image.FromFile("logo_cc.png");
                e.Graphics.DrawImage(img, left, pos, img.Width, img.Height);
                pos = pos + img.Height;
            }

            //Datos Emisor
            pos = pos + 10;
            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Empresa").InnerText, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz2").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Giro").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Sucursal").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Vendedor").InnerText.ToUpper() == "SI")
            {
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/TitVendedor").InnerText + oDteDoc.Encabezado.Emisor.CdgVendedor;
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Fecha
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Utils.DateFormat(oDteDoc.Encabezado.IdDoc.FchEmis), fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + margen + fontTitulo.Height;

            // Titulo Detalle
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/Salto").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLeft").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);

            stringFormat.Alignment = StringAlignment.Far;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoRight").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLinea2").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
            pos = pos + 2;

            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/SaltoALinea").InnerText);
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Linea/Cod").InnerText);
            bounds = e.PageBounds;

            for (int linea = 0; linea < oDteDoc.Detalle.Count; linea++)
            {
                string Cant_PUnit = oDteDoc.Detalle[linea].QtyItem.ToString(CultureInfo.CurrentUICulture) + "  X  " + oDteDoc.Detalle[linea].PrcItem.ToString("0,0", CultureInfo.CurrentUICulture);
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(Cant_PUnit, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                e.Graphics.DrawString(oDteDoc.Detalle[linea].CdgItem[0].VlrCodigo.ToString(), fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].MontoItem.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].NmbItem.ToString(), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                //UDF
                if (oDteDoc.Detalle[linea].UDFs != null)
                    for (int i = 0; i < oDteDoc.Detalle[linea].UDFs.Count; i++)
                        pos = PrintUDF(e, pos, "Linea", fontArial, width, left, "B", oDteDoc.Detalle[linea].UDFs[i], i);
                // End UDF
                pos = pos + fontArial.Height;
                if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                    e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
                pos = pos + 2;
            }

            // NO va en Boletas
            /*
            for (int linea = 0; linea < oDteDoc.Encabezado.Totales.ImptoReten.Count; linea++)
            {
                jj = CodigoImpuesto.IndexOf(oDteDoc.Encabezado.Totales.ImptoReten[linea].TipoImp.Trim());
                if (jj == -1)
                    jj = CodigoImpuesto.IndexOf("ND");

                ValorImpuesto[jj] = ValorImpuesto[jj] + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                ImpuestosAdicionales = ImpuestosAdicionales + decimal.ToInt32(oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp);
            }
            */

            //Totales
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            //Redondeo
            decimal aDes = 0;
            int aIVA = 0;
            int aNet = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntNeto);
            int aExe = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntExe);
            int aTot = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntTotal);
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
            {
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                    aDes = aDes + oDteDoc.DscRcgGlobal[i].ValorDR;
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "R")
                    aDes = aDes - oDteDoc.DscRcgGlobal[i].ValorDR;
            }
            int aSubto = aNet + aExe + (int)Utils.Round_0(aDes);
            aIVA = aTot - aNet - aExe - decimal.ToInt32(ImpuestosAdicionales);

            if ((int)Utils.Round_0(aDes) != 0)
            {
                stringFormat.Alignment = StringAlignment.Near;
                posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Col"].Value.ToString());
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Texto"].Value.ToString();
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(aSubto.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;

                decimal aDesVal = 0.0M;
                for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Descuento").Attributes["Col"].Value.ToString());
                    Texto = oDteDoc.DscRcgGlobal[i].GlosaDR;
                    if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                        Texto = "( - )" + Texto;
                    else
                        Texto = "( + )" + Texto;
                    aDesVal = oDteDoc.DscRcgGlobal[i].ValorDR;
                    e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(aDesVal.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            /*
            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aNet.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            //ImpuestosAdicionales Adicionales
            for (int i = 0; i < ValorImpuesto.Count; i++)
            {
                if (ValorImpuesto[i] > 0)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    e.Graphics.DrawString(DescripcionImpuesto[i], fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(Utils.Round_0(ValorImpuesto[i]).ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aIVA.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;          
            */ 

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aTot.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // Monto escrito
            /*
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pre").Attributes["Texto"].Value.ToString();
            Texto = Texto + montoEscrito.GetMontoEscrito(oDteDoc.Encabezado.Totales.MntTotal);
            Texto = Texto + PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pos").Attributes["Texto"].Value.ToString();oDteDoc.Encabezado.UDFs[line]
            oSize = e.Graphics.MeasureString(Texto, fontArial);
            jj = (int)Math.Ceiling(oSize.Width / width);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * jj), stringFormat);
            pos = pos + fontArial.Height * jj;
            */

            // Comentarios UDFs
            if (oDteDoc.Encabezado.UDFs != null)
                for (int i = 0; i < oDteDoc.Encabezado.UDFs.Count; i++)
                    pos = PrintUDF(e, pos, "General", fontArial, width, left, "B", oDteDoc.Encabezado.UDFs[i], i);

            // PDF417
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Separacion").InnerText);
            height = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Height").InnerText);
            PrintPDF_Image(sender, e, left, pos, width, height);
            e.Graphics.DrawString("  -  ", fontArial, Brushes.Black, new Rectangle(left, pos, width, height), stringFormat);
            pos = pos + height + 30;

            // Resolucion SII
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion1").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion2").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            pos = pos + fontArial.Height;
            e.Graphics.DrawString("-", fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
        }

        private void PrintBoletaE(object sender, PrintPageEventArgs e)
        {
            FontStyle Estilo;

            int pos, left, width, margen, VSpace, height, posCol;
            Rectangle bounds = new Rectangle(); // (left, top, width, height)
            StringFormat stringFormat = new StringFormat();
            String Texto;
            List<string> CodigoImpuesto = new List<string>();
            List<string> DescripcionImpuesto = new List<string>();
            List<decimal> ValorImpuesto = new List<decimal>();
            decimal ImpuestosAdicionales = 0;

            // Leer impuestos
            foreach (XmlNode nodo in PrinterDefXml.SelectNodes("/Format/Impuestos/Impuesto"))
            {
                CodigoImpuesto.Add(nodo.Attributes["Codigo"].Value.ToString());
                DescripcionImpuesto.Add(nodo.Attributes["Texto"].Value.ToString());
                ValorImpuesto.Add(0);
            }
            if (CodigoImpuesto.IndexOf("ND") == -1)
            {
                CodigoImpuesto.Add("ND");
                DescripcionImpuesto.Add("Otro");
                ValorImpuesto.Add(0);
            }

            // Get FONTS
            string Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Font"].Value.ToString();
            float FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Size"].Value);
            bool FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTimes = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTitulo = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontArial = new Font(Fname, FSize, Estilo);

            // Cabecera
            margen = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Margen").InnerText);
            SizeF maxSize = new SizeF(Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Width").InnerText),
                                      Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Height").InnerText));

            bounds = e.PageBounds;
            left = bounds.Left;
            width = bounds.Width;
            bounds.Height = (int)(maxSize.Height) * 4;
            VSpace = (int)(maxSize.Height) / 2;

            if ((int)(maxSize.Width) < bounds.Width)
            {
                int le = (bounds.Width - (int)(maxSize.Width) - 2 * margen) / 2;
                Point p = new Point(bounds.Left + le, bounds.Top + 10);
                bounds.Location = p;
                bounds.Width = (int)(maxSize.Width) + 2 * margen;
            }

            e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            stringFormat.Alignment = StringAlignment.Center;

            e.Graphics.DrawString("R.U.T.  " + globals.gpRUTEmisor, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("BOLETA EXENTA ELECTRONICA", fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("Nro:  " + oDteDoc.Encabezado.IdDoc.Folio, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) * 2 + VSpace, bounds.Width, bounds.Height), stringFormat);

            pos = bounds.Top + bounds.Height + margen;

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SII").InnerText;
            stringFormat.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(Texto, fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontTimes.Height), stringFormat);
            pos = pos + fontTimes.Height;

            // Logo
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Logo").InnerText.ToUpper() == "SI")
            {
                pos = pos + 5;
                Image img = Image.FromFile("logo_cc.png");
                e.Graphics.DrawImage(img, left, pos, img.Width, img.Height);
                pos = pos + img.Height;
            }

            //Datos Emisor
            pos = pos + 10;
            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Empresa").InnerText, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz2").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Giro").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Sucursal").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Vendedor").InnerText.ToUpper() == "SI")
            {
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/TitVendedor").InnerText + oDteDoc.Encabezado.Emisor.CdgVendedor;
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Fecha
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Utils.DateFormat(oDteDoc.Encabezado.IdDoc.FchEmis), fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + margen + fontTitulo.Height;

            // Titulo Detalle
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/Salto").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLeft").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);

            stringFormat.Alignment = StringAlignment.Far;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoRight").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLinea2").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
            pos = pos + 2;

            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/SaltoALinea").InnerText);
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Linea/Cod").InnerText);
            bounds = e.PageBounds;

            for (int linea = 0; linea < oDteDoc.Detalle.Count; linea++)
            {
                string Cant_PUnit = oDteDoc.Detalle[linea].QtyItem.ToString(CultureInfo.CurrentUICulture) + "  X  " + oDteDoc.Detalle[linea].PrcItem.ToString("0,0", CultureInfo.CurrentUICulture);
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(Cant_PUnit, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                e.Graphics.DrawString(oDteDoc.Detalle[linea].CdgItem[0].VlrCodigo.ToString(), fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].MontoItem.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].NmbItem.ToString(), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                //UDF
                if (oDteDoc.Detalle[linea].UDFs != null)
                    for (int i = 0; i < oDteDoc.Detalle[linea].UDFs.Count; i++)
                        pos = PrintUDF(e, pos, "Linea", fontArial, width, left, "B", oDteDoc.Detalle[linea].UDFs[i], i);
                // End UDF
                pos = pos + fontArial.Height;
                if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                    e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
                pos = pos + 2;
            }

            // NO va en Boletas
            /*
            for (int linea = 0; linea < oDteDoc.Encabezado.Totales.ImptoReten.Count; linea++)
            {
                jj = CodigoImpuesto.IndexOf(oDteDoc.Encabezado.Totales.ImptoReten[linea].TipoImp.Trim());
                if (jj == -1)
                    jj = CodigoImpuesto.IndexOf("ND");

                ValorImpuesto[jj] = ValorImpuesto[jj] + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                ImpuestosAdicionales = ImpuestosAdicionales + decimal.ToInt32(oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp);
            }
            */

            //Totales
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            //Redondeo
            decimal aDes = 0;
            int aIVA = 0;
            int aNet = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntNeto);
            int aExe = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntExe);
            int aTot = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntTotal);
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
            {
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                    aDes = aDes + oDteDoc.DscRcgGlobal[i].ValorDR;
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "R")
                    aDes = aDes - oDteDoc.DscRcgGlobal[i].ValorDR;
            }
            int aSubto = aNet + aExe + (int)Utils.Round_0(aDes);
            aIVA = aTot - aNet - aExe - decimal.ToInt32(ImpuestosAdicionales);

            if ((int)Utils.Round_0(aDes) != 0)
            {
                stringFormat.Alignment = StringAlignment.Near;
                posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Col"].Value.ToString());
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Texto"].Value.ToString();
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(aSubto.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;

                decimal aDesVal = 0.0M;
                for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Descuento").Attributes["Col"].Value.ToString());
                    Texto = oDteDoc.DscRcgGlobal[i].GlosaDR;
                    if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                        Texto = "( - )" + Texto;
                    else
                        Texto = "( + )" + Texto;
                    aDesVal = oDteDoc.DscRcgGlobal[i].ValorDR;
                    e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(aDesVal.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            /*
            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aNet.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            //ImpuestosAdicionales Adicionales
            for (int i = 0; i < ValorImpuesto.Count; i++)
            {
                if (ValorImpuesto[i] > 0)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    e.Graphics.DrawString(DescripcionImpuesto[i], fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(Utils.Round_0(ValorImpuesto[i]).ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aIVA.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;          
            */

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aTot.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // Monto escrito
            /*
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pre").Attributes["Texto"].Value.ToString();
            Texto = Texto + montoEscrito.GetMontoEscrito(oDteDoc.Encabezado.Totales.MntTotal);
            Texto = Texto + PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pos").Attributes["Texto"].Value.ToString();oDteDoc.Encabezado.UDFs[line]
            oSize = e.Graphics.MeasureString(Texto, fontArial);
            jj = (int)Math.Ceiling(oSize.Width / width);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * jj), stringFormat);
            pos = pos + fontArial.Height * jj;
            */

            // Comentarios UDFs
            if (oDteDoc.Encabezado.UDFs != null)
                for (int i = 0; i < oDteDoc.Encabezado.UDFs.Count; i++)
                    pos = PrintUDF(e, pos, "General", fontArial, width, left, "B", oDteDoc.Encabezado.UDFs[i], i);

            // PDF417
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Separacion").InnerText);
            height = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Height").InnerText);
            PrintPDF_Image(sender, e, left, pos, width, height);
            e.Graphics.DrawString("  -  ", fontArial, Brushes.Black, new Rectangle(left, pos, width, height), stringFormat);
            pos = pos + height + 30;

            // Resolucion SII
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion1").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion2").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            pos = pos + fontArial.Height;
            e.Graphics.DrawString("-", fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
        }

        private int PrintUDF(PrintPageEventArgs e, int pos, String tipo, Font oFont, int width, int left, String TipoDoc, oUDFs oUDF, int line)
        {
            String oXMLBase;
            SizeF oSize;
            StringFormat oStringFormat = new StringFormat();
            Rectangle bounds = new Rectangle();
            int oPos = pos;
            int jj;
            Image img;
            int LeftAux = 0;
            int barcodeHeight;

            if (TipoDoc == "B")
                TipoDoc = "Boleta";
            if ((TipoDoc == "F") || (TipoDoc == "FR"))
                TipoDoc = "Factura";
            if (TipoDoc == "NC")
                TipoDoc = "NC";
            if (TipoDoc == "GD")
                TipoDoc = "Guía despacho";

            if (tipo == "Linea")
                oXMLBase = "/Format/UDF/" + TipoDoc + "/Lineas";
            else
                oXMLBase = "/Format/UDF/" + TipoDoc + "/General";

            if (PrinterDefXml.SelectSingleNode(oXMLBase) == null)
                return pos;
            if (oUDF.Codigo.Trim() == "") // Sin codigo no imprime la linea
                return pos;

            int.TryParse(PrinterDefXml.SelectSingleNode("/Format/UDF/BarCodeHeight").InnerText, out barcodeHeight);
            oXMLBase = oXMLBase + "/Linea_" + (line + 1).ToString() + "_";
            bounds = e.PageBounds;

            if ("SI" == PrinterDefXml.SelectSingleNode(oXMLBase + "C").Attributes["Print"].Value.ToString().ToUpper())
            {
                oStringFormat.Alignment = StringAlignment.Center;
                left = bounds.Left;
                width = bounds.Width - left;

                if ("RIGHT" != PrinterDefXml.SelectSingleNode(oXMLBase + "C").Attributes["Align"].Value.ToString().ToUpper())
                    int.TryParse(PrinterDefXml.SelectSingleNode(oXMLBase + "C").Attributes["LeftMg"].Value.ToString(), out left);
                if ("RIGHT" == PrinterDefXml.SelectSingleNode(oXMLBase + "C").Attributes["Align"].Value.ToString().ToUpper())
                    oStringFormat.Alignment = StringAlignment.Far;
                else if ("LEFT" == PrinterDefXml.SelectSingleNode(oXMLBase + "C").Attributes["Align"].Value.ToString().ToUpper())
                    oStringFormat.Alignment = StringAlignment.Near;               

                if ("TEXTO" == PrinterDefXml.SelectSingleNode(oXMLBase + "C").Attributes["Modo"].Value.ToString().ToUpper())
                {
                    oSize = e.Graphics.MeasureString(oUDF.Codigo.Trim(), oFont);
                    jj = (int)Math.Ceiling(oSize.Width / width);

                    e.Graphics.DrawString(oUDF.Codigo.Trim(), oFont, Brushes.Black, new Rectangle(left, pos, width, oFont.Height * jj), oStringFormat);
                    oPos = (oPos < pos + oFont.Height * jj) ? pos + oFont.Height * jj : oPos;
                }
                else
                {
                    Zen.Barcode.Code128BarcodeDraw barcode = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
                    img = barcode.Draw(oUDF.Codigo.Trim(), barcodeHeight);

                    if ((img.Width < bounds.Width) && (oStringFormat.Alignment == StringAlignment.Center))
                        LeftAux = (bounds.Width - img.Width) / 2 + left;
                    else if ((img.Width < bounds.Width) && (oStringFormat.Alignment == StringAlignment.Far))
                        LeftAux = (bounds.Width - img.Width) + left;
                    else
                        LeftAux = left;

                    e.Graphics.DrawImage(img, LeftAux, pos, img.Width, img.Height);
                    oPos = (oPos < pos + barcodeHeight + 5) ? pos + barcodeHeight + 5 : oPos;
                }
            }
            if ("SI" == PrinterDefXml.SelectSingleNode(oXMLBase + "D").Attributes["Print"].Value.ToString().ToUpper())
            {
                oStringFormat.Alignment = StringAlignment.Center;
                left = bounds.Left;
                width = bounds.Width - left;

                if ("RIGHT" != PrinterDefXml.SelectSingleNode(oXMLBase + "D").Attributes["Align"].Value.ToString().ToUpper())
                    int.TryParse(PrinterDefXml.SelectSingleNode(oXMLBase + "D").Attributes["LeftMg"].Value.ToString(), out left);
                if ("RIGHT" == PrinterDefXml.SelectSingleNode(oXMLBase + "D").Attributes["Align"].Value.ToString().ToUpper())
                    oStringFormat.Alignment = StringAlignment.Far;
                else if ("LEFT" == PrinterDefXml.SelectSingleNode(oXMLBase + "D").Attributes["Align"].Value.ToString().ToUpper())
                    oStringFormat.Alignment = StringAlignment.Near;

                if ("TEXTO" == PrinterDefXml.SelectSingleNode(oXMLBase + "D").Attributes["Modo"].Value.ToString().ToUpper())
                {
                    oSize = e.Graphics.MeasureString(oUDF.Descripcion.Trim(), oFont);
                    jj = (int)Math.Ceiling(oSize.Width / width);

                    oStringFormat.Alignment = oStringFormat.Alignment;
                    e.Graphics.DrawString(oUDF.Descripcion.Trim(), oFont, Brushes.Black, new Rectangle(left, pos, width, oFont.Height * jj), oStringFormat);
                    oPos = (oPos < pos + oFont.Height * jj) ? pos + oFont.Height * jj : oPos;
                }
                else
                {
                    Zen.Barcode.Code128BarcodeDraw barcode = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
                    img = barcode.Draw(oUDF.Descripcion.Trim(), barcodeHeight);

                    if ((img.Width < bounds.Width) && (oStringFormat.Alignment == StringAlignment.Center))
                        LeftAux = (bounds.Width - img.Width) / 2 + left;
                    else if ((img.Width < bounds.Width) && (oStringFormat.Alignment == StringAlignment.Far))
                        LeftAux = (bounds.Width - img.Width) + left;
                    else
                        LeftAux = left;

                    e.Graphics.DrawImage(img, LeftAux, pos, img.Width, img.Height);
                    oPos = (oPos < pos + barcodeHeight + 5) ? pos + barcodeHeight + 5 : oPos;
                }
            }
            if ("SI" == PrinterDefXml.SelectSingleNode(oXMLBase + "V").Attributes["Print"].Value.ToString().ToUpper())
            {
                oStringFormat.Alignment = StringAlignment.Center;
                left = bounds.Left;
                width = bounds.Width - left;

                if ("RIGHT" != PrinterDefXml.SelectSingleNode(oXMLBase + "V").Attributes["Align"].Value.ToString().ToUpper())
                    int.TryParse(PrinterDefXml.SelectSingleNode(oXMLBase + "V").Attributes["LeftMg"].Value.ToString(), out left);
                if ("RIGHT" == PrinterDefXml.SelectSingleNode(oXMLBase + "V").Attributes["Align"].Value.ToString().ToUpper())
                    oStringFormat.Alignment = StringAlignment.Far;
                else if ("LEFT" == PrinterDefXml.SelectSingleNode(oXMLBase + "V").Attributes["Align"].Value.ToString().ToUpper())
                    oStringFormat.Alignment = StringAlignment.Near;

                if ("TEXTO" == PrinterDefXml.SelectSingleNode(oXMLBase + "V").Attributes["Modo"].Value.ToString().ToUpper())
                {
                    oSize = e.Graphics.MeasureString(oUDF.Valor.ToString(), oFont);
                    jj = (int)Math.Ceiling(oSize.Width / width);

                    oStringFormat.Alignment = oStringFormat.Alignment;
                    e.Graphics.DrawString(oUDF.Valor.ToString(), oFont, Brushes.Black, new Rectangle(left, pos, width, oFont.Height * jj), oStringFormat);
                    oPos = (oPos < pos + oFont.Height * jj) ? pos + oFont.Height * jj : oPos;
                }
                else
                {
                    Zen.Barcode.Code128BarcodeDraw barcode = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
                    img = barcode.Draw(oUDF.Valor.ToString(), barcodeHeight);

                    if ((img.Width < bounds.Width) && (oStringFormat.Alignment == StringAlignment.Center))
                        LeftAux = (bounds.Width - img.Width) / 2 + left;
                    else if ((img.Width < bounds.Width) && (oStringFormat.Alignment == StringAlignment.Far))
                        LeftAux = (bounds.Width - img.Width) + left;
                    else
                        LeftAux = left;

                    e.Graphics.DrawImage(img, LeftAux, pos, img.Width, img.Height);
                    oPos = (oPos < pos + barcodeHeight + 5) ? pos + barcodeHeight + 5 : oPos;
                }
            }
            return oPos;
        }

        private void PrintNC(object sender, PrintPageEventArgs e)
        {
            FontStyle Estilo;

            int pos, left, width, margen, VSpace, height, posCol, jj;
            Rectangle bounds = new Rectangle(); // (left, top, width, height)
            StringFormat stringFormat = new StringFormat();
            String Texto;
            List<string> CodigoImpuesto = new List<string>();
            List<string> DescripcionImpuesto = new List<string>();
            List<decimal> ValorImpuesto = new List<decimal>();
            decimal ImpuestosAdicionales = 0;

            // Leer impuestos
            foreach (XmlNode nodo in PrinterDefXml.SelectNodes("/Format/Impuestos/Impuesto"))
            {
                CodigoImpuesto.Add(nodo.Attributes["Codigo"].Value.ToString());
                DescripcionImpuesto.Add(nodo.Attributes["Texto"].Value.ToString());
                ValorImpuesto.Add(0);
            }
            if (CodigoImpuesto.IndexOf("ND") == -1)
            {
                CodigoImpuesto.Add("ND");
                DescripcionImpuesto.Add("Otro");
                ValorImpuesto.Add(0);
            }

            // Get FONTS
            string Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Font"].Value.ToString();
            float FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Size"].Value);
            bool FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTimes = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTitulo = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontArial = new Font(Fname, FSize, Estilo);

            // Cabecera
            margen = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Margen").InnerText);
            SizeF maxSize = new SizeF(Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Width").InnerText),
                                      Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Height").InnerText));

            bounds = e.PageBounds;
            left = bounds.Left;
            width = bounds.Width;
            bounds.Height = (int)(maxSize.Height) * 4;
            VSpace = (int)(maxSize.Height) / 2;

            if ((int)(maxSize.Width) < bounds.Width)
            {
                int le = (bounds.Width - (int)(maxSize.Width) - 2 * margen) / 2;
                Point p = new Point(bounds.Left + le, bounds.Top + 10);
                bounds.Location = p;
                bounds.Width = (int)(maxSize.Width) + 2 * margen;
            }

            e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            stringFormat.Alignment = StringAlignment.Center;

            e.Graphics.DrawString("R.U.T.  " + globals.gpRUTEmisor, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("NOTA DE CREDITO ELECTRONICA", fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("Nro:  " + oDteDoc.Encabezado.IdDoc.Folio, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) * 2 + VSpace, bounds.Width, bounds.Height), stringFormat);

            pos = bounds.Top + bounds.Height + margen;

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SII").InnerText;
            stringFormat.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(Texto, fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontTimes.Height), stringFormat);
            pos = pos + fontTimes.Height;

            // Logo
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Logo").InnerText.ToUpper() == "SI")
            {
                pos = pos + 5;
                Image img = Image.FromFile("logo_cc.png");
                e.Graphics.DrawImage(img, left, pos, img.Width, img.Height);
                pos = pos + img.Height;
            }

            //Datos Emisor
            pos = pos + 10;
            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Empresa").InnerText, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz2").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Giro").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Sucursal").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Vendedor").InnerText.ToUpper() == "SI")
            {
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/TitVendedor").InnerText + oDteDoc.Encabezado.Emisor.CdgVendedor;
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Receptor - Cliente
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            // Datos cliente
            e.Graphics.DrawString(oDteDoc.Encabezado.Receptor.RznSocRecep, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString("RUT: " + oDteDoc.Encabezado.Receptor.RUTRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString("Dirección: " + oDteDoc.Encabezado.Receptor.DirRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (oDteDoc.Encabezado.Receptor.CiudadRecep != "")
            {
                e.Graphics.DrawString("Ciudad: " + oDteDoc.Encabezado.Receptor.CiudadRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.CmnaRecep != "")
            {
                e.Graphics.DrawString("Comuna: " + oDteDoc.Encabezado.Receptor.CmnaRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.GiroRecep != "")
            {
                e.Graphics.DrawString("Giro: " + oDteDoc.Encabezado.Receptor.GiroRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Fecha
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Utils.DateFormat(oDteDoc.Encabezado.IdDoc.FchEmis), fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + margen + fontTitulo.Height;

            // Titulo Detalle
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/Salto").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLeft").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);

            stringFormat.Alignment = StringAlignment.Far;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoRight").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLinea2").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
            pos = pos + 2;

            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/SaltoALinea").InnerText);
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Linea/Cod").InnerText);
            bounds = e.PageBounds;

            for (int linea = 0; linea < oDteDoc.Detalle.Count; linea++)
            {
                string Cant_PUnit = oDteDoc.Detalle[linea].QtyItem.ToString(CultureInfo.CurrentUICulture) + "  X  " + oDteDoc.Detalle[linea].PrcItem.ToString("0,0", CultureInfo.CurrentUICulture);
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(Cant_PUnit, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                e.Graphics.DrawString(oDteDoc.Detalle[linea].CdgItem[0].VlrCodigo.ToString(), fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].MontoItem.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].NmbItem.ToString(), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                //UDF
                if (oDteDoc.Detalle[linea].UDFs != null)
                    for (int i = 0; i < oDteDoc.Detalle[linea].UDFs.Count; i++)
                        pos = PrintUDF(e, pos, "Linea", fontArial, width, left, "NC", oDteDoc.Detalle[linea].UDFs[i], i);
                // End UDF
                pos = pos + fontArial.Height;
                if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                    e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
                pos = pos + 2;
            }

            if (oDteDoc.Encabezado.Totales.ImptoReten != null)
                for (int linea = 0; linea < oDteDoc.Encabezado.Totales.ImptoReten.Count; linea++)
                {
                    jj = CodigoImpuesto.IndexOf(oDteDoc.Encabezado.Totales.ImptoReten[linea].TipoImp.Trim());
                    if (jj == -1)
                        jj = CodigoImpuesto.IndexOf("ND");

                    ValorImpuesto[jj] = ValorImpuesto[jj] + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                    ImpuestosAdicionales = ImpuestosAdicionales + decimal.ToInt32(oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp);
                }

            // Referencias NC
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);
            Texto = "Referencias:";
            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            if (oDteDoc.Referencia[0].CodRef == 2)
                Texto = "Corrige texto de documento de la referencia - ";
            else if (oDteDoc.Referencia[0].CodRef == 3)
                Texto = "Corrige montos de documento de la referencia - ";
            else
                Texto = "Anula documento de la referencia - ";

            if (oDteDoc.Referencia[0].TpoDocRef.Trim() == "33")
                Texto = Texto + "Factura Numero: " + oDteDoc.Referencia[0].FolioRef.Trim() + " - del " + oDteDoc.Referencia[0].FchRef.Substring(0, 10);
            else
                Texto = Texto + "Boleta Numero: " + oDteDoc.Referencia[0].FolioRef.Trim() + " - del " + oDteDoc.Referencia[0].FchRef.Substring(0, 10);

            SizeF oSize = e.Graphics.MeasureString(Texto, fontArial);
            jj = (int)Math.Ceiling(oSize.Width / width);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * jj), stringFormat);
            pos = pos + fontArial.Height * jj;

            //Totales
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            //Redondeo
            decimal aDes = 0;
            int aIVA = 0;
            int aNet = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntNeto);
            int aExe = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntExe);
            int aTot = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntTotal);
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
            {
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                    aDes = aDes + oDteDoc.DscRcgGlobal[i].ValorDR;
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "R")
                    aDes = aDes - oDteDoc.DscRcgGlobal[i].ValorDR;
            }
            int aSubto = aNet + aExe + (int)Utils.Round_0(aDes);
            aIVA = aTot - aNet - aExe - decimal.ToInt32(ImpuestosAdicionales);

            if ((int)Utils.Round_0(aDes) != 0)
            {
                stringFormat.Alignment = StringAlignment.Near;
                posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Col"].Value.ToString());
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Texto"].Value.ToString();
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(aSubto.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;

                decimal aDesVal = 0.0M;
                for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Descuento").Attributes["Col"].Value.ToString());
                    Texto = oDteDoc.DscRcgGlobal[i].GlosaDR;
                    if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                        Texto = "( - )" + Texto;
                    else
                        Texto = "( + )" + Texto;
                    aDesVal = oDteDoc.DscRcgGlobal[i].ValorDR;
                    e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(aDesVal.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aNet.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            //ImpuestosAdicionales Adicionales
            for (int i = 0; i < ValorImpuesto.Count; i++)
            {
                if (ValorImpuesto[i] > 0)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    e.Graphics.DrawString(DescripcionImpuesto[i], fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(Utils.Round_0(ValorImpuesto[i]).ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aIVA.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aTot.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // Monto escrito
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pre").Attributes["Texto"].Value.ToString();
            Texto = Texto + montoEscrito.GetMontoEscrito(oDteDoc.Encabezado.Totales.MntTotal);
            Texto = Texto + PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pos").Attributes["Texto"].Value.ToString();

            oSize = e.Graphics.MeasureString(Texto, fontArial);
            jj = (int)Math.Ceiling(oSize.Width / width);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * jj), stringFormat);
            pos = pos + fontArial.Height * jj;

            // Comentarios UDFs
            if (oDteDoc.Encabezado.UDFs != null)
                for (int i = 0; i < oDteDoc.Encabezado.UDFs.Count; i++)
                    pos = PrintUDF(e, pos, "General", fontArial, width, left, "NC", oDteDoc.Encabezado.UDFs[i], i);

            // PDF417
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Separacion").InnerText);
            height = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Height").InnerText);
            PrintPDF_Image(sender, e, left, pos, width, height);
            e.Graphics.DrawString("  -  ", fontArial, Brushes.Black, new Rectangle(left, pos, width, height), stringFormat);
            pos = pos + height + 30;

            // Resolucion SII
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion1").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion2").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // recibi conforme
            if (PrintRecibeConforme)
            {
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Nombre:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("RUT:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Fecha:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Firma:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 3;
            }

            pos = pos + fontArial.Height;
            e.Graphics.DrawString("-", fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
        }

        private void PrintGuia(object sender, PrintPageEventArgs e)
        {
            FontStyle Estilo;

            int pos, left, width, margen, VSpace, height, posCol, jj;
            Rectangle bounds = new Rectangle(); // (left, top, width, height)
            StringFormat stringFormat = new StringFormat();
            String Texto;
            List<string> CodigoImpuesto = new List<string>();
            List<string> DescripcionImpuesto = new List<string>();
            List<decimal> ValorImpuesto = new List<decimal>();
            decimal ImpuestosAdicionales = 0;

            // Leer impuestos
            foreach (XmlNode nodo in PrinterDefXml.SelectNodes("/Format/Impuestos/Impuesto"))
            {
                CodigoImpuesto.Add(nodo.Attributes["Codigo"].Value.ToString());
                DescripcionImpuesto.Add(nodo.Attributes["Texto"].Value.ToString());
                ValorImpuesto.Add(0);
            }
            if (CodigoImpuesto.IndexOf("ND") == -1)
            {
                CodigoImpuesto.Add("ND");
                DescripcionImpuesto.Add("Otro");
                ValorImpuesto.Add(0);
            }

            // Get FONTS
            string Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Font"].Value.ToString();
            float FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Size"].Value);
            bool FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Cabecera").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTimes = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Titulo").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontTitulo = new Font(Fname, FSize, Estilo);

            Fname = PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Font"].Value.ToString();
            FSize = float.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Size"].Value);
            FBold = bool.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Fonts/Detalle").Attributes["Bold"].Value);
            Estilo = FontStyle.Regular;
            if (FBold)
                Estilo = FontStyle.Bold;
            Font fontArial = new Font(Fname, FSize, Estilo);

            // Cabecera
            margen = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Margen").InnerText);
            SizeF maxSize = new SizeF(Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Width").InnerText),
                                      Single.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/Height").InnerText));

            bounds = e.PageBounds;
            left = bounds.Left;
            width = bounds.Width;
            bounds.Height = (int)(maxSize.Height) * 4;
            VSpace = (int)(maxSize.Height) / 2;

            if ((int)(maxSize.Width) < bounds.Width)
            {
                int le = (bounds.Width - (int)(maxSize.Width) - 2 * margen) / 2;
                Point p = new Point(bounds.Left + le, bounds.Top + 10);
                bounds.Location = p;
                bounds.Width = (int)(maxSize.Width) + 2 * margen;
            }

            e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(bounds));
            stringFormat.Alignment = StringAlignment.Center;

            e.Graphics.DrawString("R.U.T.  " + globals.gpRUTEmisor, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("GUIA DE DESPACHO ELECTRONICA", fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) + VSpace, bounds.Width, bounds.Height), stringFormat);
            e.Graphics.DrawString("Nro:  " + oDteDoc.Encabezado.IdDoc.Folio, fontTimes, Brushes.Black, new Rectangle(bounds.Left, bounds.Top + margen + (int)(maxSize.Height) * 2 + VSpace, bounds.Width, bounds.Height), stringFormat);

            pos = bounds.Top + bounds.Height + margen;

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SII").InnerText;
            stringFormat.Alignment = StringAlignment.Center;
            e.Graphics.DrawString(Texto, fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontTimes.Height), stringFormat);
            pos = pos + fontTimes.Height;

            // Logo
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Logo").InnerText.ToUpper() == "SI")
            {
                pos = pos + 5;
                Image img = Image.FromFile("logo_cc.png");
                e.Graphics.DrawImage(img, left, pos, img.Width, img.Height);
                pos = pos + img.Height;
            }

            //Datos Emisor
            pos = pos + 10;
            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Empresa").InnerText, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Matriz2").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Giro").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString(PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Sucursal").InnerText, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/Vendedor").InnerText.ToUpper() == "SI")
            {
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Emisor/TitVendedor").InnerText + oDteDoc.Encabezado.Emisor.CdgVendedor;
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Receptor - Cliente
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            // Datos cliente
            e.Graphics.DrawString(oDteDoc.Encabezado.Receptor.RznSocRecep, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontTitulo.Height;
            e.Graphics.DrawString("RUT: " + oDteDoc.Encabezado.Receptor.RUTRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            e.Graphics.DrawString("Dirección: " + oDteDoc.Encabezado.Receptor.DirRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            if (oDteDoc.Encabezado.Receptor.CiudadRecep != "")
            {
                e.Graphics.DrawString("Ciudad: " + oDteDoc.Encabezado.Receptor.CiudadRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.CmnaRecep != "")
            {
                e.Graphics.DrawString("Comuna: " + oDteDoc.Encabezado.Receptor.CmnaRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }
            if (oDteDoc.Encabezado.Receptor.GiroRecep != "")
            {
                e.Graphics.DrawString("Giro: " + oDteDoc.Encabezado.Receptor.GiroRecep, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
            }

            // Fecha
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SaltoACliente").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Utils.DateFormat(oDteDoc.Encabezado.IdDoc.FchEmis), fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + margen + fontTitulo.Height;

            // Titulo Detalle
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/Salto").InnerText);

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLeft").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);

            stringFormat.Alignment = StringAlignment.Far;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoRight").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            stringFormat.Alignment = StringAlignment.Near;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/TextoLinea2").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontTitulo, Brushes.Black, new Rectangle(left, pos, width, fontTitulo.Height), stringFormat);
            pos = pos + fontTitulo.Height;

            if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
            pos = pos + 2;

            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/SaltoALinea").InnerText);
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Linea/Cod").InnerText);
            bounds = e.PageBounds;

            for (int linea = 0; linea < oDteDoc.Detalle.Count; linea++)
            {
                string Cant_PUnit = oDteDoc.Detalle[linea].QtyItem.ToString(CultureInfo.CurrentUICulture) + "  X  " + oDteDoc.Detalle[linea].PrcItem.ToString("0,0", CultureInfo.CurrentUICulture);
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(Cant_PUnit, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                e.Graphics.DrawString(oDteDoc.Detalle[linea].CdgItem[0].VlrCodigo.ToString(), fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].MontoItem.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawString(oDteDoc.Detalle[linea].NmbItem.ToString(), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                //UDF
                if (oDteDoc.Detalle[linea].UDFs != null)
                    for (int i = 0; i < oDteDoc.Detalle[linea].UDFs.Count; i++)
                        pos = PrintUDF(e, pos, "Linea", fontArial, width, left, "F", oDteDoc.Detalle[linea].UDFs[i], i);
                // End UDF
                pos = pos + fontArial.Height;
                if (PrinterDefXml.SelectSingleNode("/Format/Printer/TituloDetalle/LineaSeparacion").InnerText.ToUpper() != "NO")
                    e.Graphics.DrawLine(Pens.Black, new Point(left, pos), new Point(left + width, pos));
                pos = pos + 2;
            }

            if (oDteDoc.Encabezado.Totales.ImptoReten != null)
                for (int linea = 0; linea < oDteDoc.Encabezado.Totales.ImptoReten.Count; linea++)
                {
                    jj = CodigoImpuesto.IndexOf(oDteDoc.Encabezado.Totales.ImptoReten[linea].TipoImp.Trim());
                    if (jj == -1)
                        jj = CodigoImpuesto.IndexOf("ND");

                    ValorImpuesto[jj] = ValorImpuesto[jj] + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                    ImpuestosAdicionales = ImpuestosAdicionales + oDteDoc.Encabezado.Totales.ImptoReten[linea].MontoImp;
                }

            //Totales
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            //Redondeo
            decimal aDes = 0;
            int aIVA = 0;
            int aNet = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntNeto);
            int aExe = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntExe);
            int aTot = (int)Utils.Round_0(oDteDoc.Encabezado.Totales.MntTotal);
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
            {
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                    aDes = aDes + oDteDoc.DscRcgGlobal[i].ValorDR;
                if (oDteDoc.DscRcgGlobal[i].TpoMov == "R")
                    aDes = aDes - oDteDoc.DscRcgGlobal[i].ValorDR;
            }
            int aSubto = aNet + aExe + (int)Utils.Round_0(aDes);
            aIVA = aTot - aNet - aExe - decimal.ToInt32(ImpuestosAdicionales);

            if ((int)Utils.Round_0(aDes) != 0)
            {
                stringFormat.Alignment = StringAlignment.Near;
                posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Col"].Value.ToString());
                Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/SubTotal").Attributes["Texto"].Value.ToString();
                e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                stringFormat.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(aSubto.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                pos = pos + fontArial.Height;

                decimal aDesVal = 0.0M;
                for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Descuento").Attributes["Col"].Value.ToString());
                    Texto = oDteDoc.DscRcgGlobal[i].GlosaDR;
                    if (oDteDoc.DscRcgGlobal[i].TpoMov == "D")
                        Texto = "( - )" + Texto;
                    else
                        Texto = "( + )" + Texto;
                    aDesVal = oDteDoc.DscRcgGlobal[i].ValorDR;
                    e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(aDesVal.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Neto").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aNet.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            //ImpuestosAdicionales Adicionales
            for (int i = 0; i < ValorImpuesto.Count; i++)
            {
                if (ValorImpuesto[i] > 0)
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    e.Graphics.DrawString(DescripcionImpuesto[i], fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
                    stringFormat.Alignment = StringAlignment.Far;
                    e.Graphics.DrawString(Utils.Round_0(ValorImpuesto[i]).ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
                    pos = pos + fontArial.Height;
                }
            }

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/IVA").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aIVA.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            stringFormat.Alignment = StringAlignment.Near;
            posCol = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Col"].Value.ToString());
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Total").Attributes["Texto"].Value.ToString();
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left + posCol, pos, width, fontArial.Height), stringFormat);
            stringFormat.Alignment = StringAlignment.Far;
            e.Graphics.DrawString(aTot.ToString("0,0", CultureInfo.CurrentUICulture), fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // Monto escrito
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/Totales/Separacion").InnerText);

            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pre").Attributes["Texto"].Value.ToString();
            Texto = Texto + montoEscrito.GetMontoEscrito(oDteDoc.Encabezado.Totales.MntTotal);
            Texto = Texto + PrinterDefXml.SelectSingleNode("/Format/Printer/MontoEscrito/Pos").Attributes["Texto"].Value.ToString();
            SizeF oSize;
            oSize = e.Graphics.MeasureString(Texto, fontArial);
            jj = (int)Math.Ceiling(oSize.Width / width);

            stringFormat.Alignment = StringAlignment.Near;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * jj), stringFormat);
            pos = pos + fontArial.Height * jj;

            // Comentarios UDFs
            if (oDteDoc.Encabezado.UDFs != null)
                for (int i = 0; i < oDteDoc.Encabezado.UDFs.Count; i++)
                    pos = PrintUDF(e, pos, "General", fontArial, width, left, "F", oDteDoc.Encabezado.UDFs[i], i);

            // PDF417
            pos = pos + int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Separacion").InnerText);
            height = int.Parse(PrinterDefXml.SelectSingleNode("/Format/Printer/PDF417/Height").InnerText);
            PrintPDF_Image(sender, e, left, pos, width, height);
            e.Graphics.DrawString("  -  ", fontArial, Brushes.Black, new Rectangle(left, pos, width, height), stringFormat);
            pos = pos + height + 30;

            // Resolucion SII
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion1").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;
            Texto = PrinterDefXml.SelectSingleNode("/Format/Printer/Cabecera/SIIResolucion2").InnerText;
            e.Graphics.DrawString(Texto, fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
            pos = pos + fontArial.Height;

            // recibi conforme
            if (PrintRecibeConforme)
            {
                pos = pos + fontArial.Height;
                stringFormat.Alignment = StringAlignment.Near;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Nombre:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("RUT:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Fecha:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 2;
                e.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(new Rectangle(left, pos, width, fontArial.Height * 2)));
                e.Graphics.DrawString("Firma:", fontTimes, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height * 2), stringFormat);
                pos = pos + fontArial.Height * 3;
            }

            pos = pos + fontArial.Height;
            e.Graphics.DrawString("-", fontArial, Brushes.Black, new Rectangle(left, pos, width, fontArial.Height), stringFormat);
        }
        //private void CortePapel()
        //{
        //    Win32Print w32prn = new Win32Print();

        //    w32prn.SetPrinterName("BIXOLON SRP-350plusII");

        //    w32prn.SetDeviceFont(9.5f, "FontControl", false, false);
        //    w32prn.PrintText("66g");
            
        //    w32prn.EndDoc();
        //}

        private void PrintPDF_Image(object sender, PrintPageEventArgs e, int left, int pos, int width, int height)
        {
            TimbreSII.SetImage_PDF417(TimbreXml, "_Timbre_DTE.jpg");
            Image img = Image.FromFile("_Timbre_DTE.jpg");

            int sourceWidth = img.Width;
            int sourceHeight = img.Height;

            e.Graphics.DrawImage(img,
//                new Rectangle(left, pos + 20, width, height),
                new Rectangle(left, pos + 20, sourceWidth, sourceHeight),
                new Rectangle(0, 0, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);
        }
    }
}
