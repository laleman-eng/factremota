using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using Logs;

namespace FactRemota
{
    class ProcesarDTE
    {
        DTEDoc oDteDoc;
        EnviarDTE oEnviarDTE = new EnviarDTE();
       
        PrintPDF Imprimir = new PrintPDF();
        string PosError = "";

        public ProcesarDTE(String p_TipoDTE, String p_Local, int p_NumDoc, String p_RUTEmisor, ref int nErr, bool noPrint)
        // Procesa documento entrante
        {
            XmlDocument oXmlTimbre;
            string facturaFile = null;

            // Errores (-400)
            nErr = -420;
            try
            {
                if (globals.Debug)
                    globals.oLog.LogMsg("SetFileNameBase", "A", "I");
                Utils.SetFileNameBase(p_TipoDTE, p_Local, p_NumDoc);
            }
            catch (Exception e)
            {
                throw new Exception("Error en formato de RUT o de archivo. " + e.Message);
            }

            nErr = -421;
            if (File.Exists(globals._FilenameBase + ".ERR"))
                throw new Exception("Documento ya recibido desde POS con Errores - No enviado. Estado ERR");
            nErr = -422;
            if (File.Exists(globals._FilenameBase + ".GEN"))
                throw new Exception("Documento ya recibido desde POS, ha sido procesado y no enviado. Estado GEN");
            nErr = -423;
            if (File.Exists(globals._FilenameBase + ".OK"))
                throw new Exception("Documento ya recibido desde POS, ha sido procesado y enviado. Estado OK");
            nErr = -424;
            if (File.Exists(globals._FilenameBase + "_Timbre_DTE.xml"))
                throw new Exception("Documento ya recibido desde POS. Timbre ya generado");

            try
            {
                PosError = "Error al leer documento enviado por el POS";
                nErr = -401;
                facturaFile = globals._FilenameBase + ".ENV";
                FileIO.GetDataFromFile(facturaFile, ref oDteDoc, p_TipoDTE);
                globals.oLog.LogMsg("Leyendo archivo: " + globals._FilenameBase + ".ENV", "A", "I");
                //facturaFile.Close();

                PosError = "Error en validacion de documento enviado por el POS";
                nErr = -402;
                ValidarFactura(ref oDteDoc);

                PosError = "Error al obtener Folio";
                nErr = -403;
                int folio = Utils.GetFoleoSiguiente(p_TipoDTE, p_Local);

                PosError = "Error al escribir folio en modelo de DTE";
                nErr = -404;
                escribirFolioEnHeader(folio, ref oDteDoc);

                PosError = "Error en generación de timbre SII";
                nErr = -405;

                oXmlTimbre = TimbreSII.EmitirTimbre(oDteDoc.Encabezado.IdDoc.TipoDTE.ToString(),
                                                    oDteDoc.Encabezado.IdDoc.Folio.ToString(),
                                                    oDteDoc.Encabezado.IdDoc.FchEmis,
                                                    oDteDoc.Encabezado.Receptor.RUTRecep,
                                                    oDteDoc.Encabezado.Receptor.RznSocRecep,
                                                    oDteDoc.Encabezado.Totales.MntTotal.ToString(),
                                                    oDteDoc.Detalle[0].NmbItem,
                                                    globals._xmlCAF);

                if (!noPrint)
                {
                    PosError = "Error en impresión de documento";
                    nErr = -406;
                    Imprimir.DoPrint(oDteDoc, oXmlTimbre);
                }

                // cambio de estados
                PosError = "Error al escribir documento.USE";
                nErr = -407;
                FileIO.reescribirFacturaFoliada(oDteDoc); // genera .USE

                PosError = "Error al guardar archivo de timbre SII";
                nErr = -408;
                oXmlTimbre.Save(globals._FilenameBase + "_Timbre_DTE.xml");

                PosError = "Error al marcar folio como usado (.USE)";
                nErr = -409;
                Utils.marcarFoleoUsado(p_TipoDTE, p_Local, folio);
                globals.oLog.LogMsg("marcar folio como usado (.USE): " + folio.ToString(), "A", "I");

                PosError = "Error al generar documento para enviar (.GEN)";
                nErr = -410;

                String docaEnviar;
                if (oDteDoc.Encabezado.IdDoc.TipoDTE == 39 || oDteDoc.Encabezado.IdDoc.TipoDTE == 41)
                    docaEnviar = oEnviarDTE.PrepararBoletaJson(oDteDoc, oXmlTimbre);
                else
                    docaEnviar = oEnviarDTE.PrepararDTEJson(oDteDoc, oXmlTimbre);

                try { File.Delete(globals._FilenameBase + ".ENV"); }
                catch { }

                FileIO.escribirFile(docaEnviar, ".GEN");
                globals.oLog.LogMsg("Documento para enviar generado (.GEN): " + globals._FilenameBase + ".GEN", "A", "I");
                facturaFile = globals._FilenameBase + ".GEN";
                string oData = FileIO.leerFile(facturaFile);

                try
                {
                    PosError = "Error al enviar documento al portal";
                    nErr = -411;

                    if (oEnviarDTE.EnviarDocumento(oDteDoc.Encabezado.IdDoc.TipoDTE, oData))
                    {
                        globals.oLog.LogMsg("Marcando documento como enviado", "A", "I");

                        PosError = "Error al marcar factura como enviada (OK) - Pero la factura fue enviada.";
                        nErr = -412;
                        File.Move(globals._FilenameBase + ".GEN", globals._FilenameBase + ".OK");
                        globals.oLog.LogMsg("marcar documento como enviado (OK): " + globals._FilenameBase + ".GEN", "A", "I");

                        PosError = "Error al marcar foleo como enviado (OK) - Pero la factura fue enviada.";
                        nErr = -413;
                        Utils.marcarFoleoEnviado(p_TipoDTE, p_Local, folio);
                        globals.oLog.LogMsg("marcar folio como enviado (OK): " + folio.ToString(), "A", "I");
                    }
                }
                catch (Exception e)
                {
                    globals.oLog.LogMsg(PosError, "A", "I");
                    globals.oLog.LogMsg(nErr.ToString() + " " + e.Message + " - " + e.StackTrace, "A", "I");
                }
            }
            catch (Exception e)
            {
                globals.oLog.LogMsg("Detalle error siguiente: " + e.Message + " - " + e.StackTrace, "A", "I");
                if (facturaFile != null)
                {
                    //facturaFile.Close();
                    if (File.Exists(globals._FilenameBase + ".ENV"))
                        File.Move(globals._FilenameBase + ".ENV", globals._FilenameBase + ".ERR");
                }
                throw new Exception(nErr.ToString() + " " + PosError + "  ....  " + e.Message + " - " + e.StackTrace);
            }
        }

        public ProcesarDTE(String p_TipoDTE, String p_Local, int p_NumDoc, bool Reimprimir)
        // Reimprime documento indicado
        {
            XmlDocument oXmlTimbre;

            Utils.SetFileNameBase(p_TipoDTE, p_Local, p_NumDoc);
            string facturaFile = globals._FilenameBase + ".USE";

            FileIO.GetDataFromFile(facturaFile, ref oDteDoc, p_TipoDTE);
            globals.oLog.LogMsg("Reenvio - Leyendo archivo: " + globals._FilenameBase + ".USE", "A", "I");
            //facturaFile.Close();

            oXmlTimbre = Utils.CargarXML(globals._FilenameBase + "_Timbre_DTE.xml");
            Imprimir.DoPrint(oDteDoc, oXmlTimbre);
        }

        public ProcesarDTE(String p_TipoDTE, String p_Local, int p_NumDoc)
        // Anula documento indicado
        {
            int oFolio = -1;
            string fOrig = "";
            string fDest = "";

            Utils.SetFileNameBase(p_TipoDTE, p_Local, p_NumDoc);
            if (File.Exists(globals._FilenameBase + ".USE"))
            {
                string facturaFile = globals._FilenameBase + ".USE";
                FileIO.GetDataFromFile(facturaFile, ref oDteDoc, p_TipoDTE);
                globals.oLog.LogMsg("Anulación - Leyendo archivo: " + globals._FilenameBase + ".USE", "A", "I");
                //facturaFile.Close();

                oFolio = oDteDoc.Encabezado.IdDoc.Folio;
                string oFileFoleo = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + p_TipoDTE + "_" + p_Local + "_" + oFolio.ToString();
                if (File.Exists(oFileFoleo + ".USE"))
                {
                    fOrig = oFileFoleo + ".USE";
                    fDest = fOrig.Replace(globals.gpDirFolios, globals.gpDirAnulacion);
                    try { File.Copy(fOrig, fDest); } catch { }
                    try { File.Move(oFileFoleo + ".USE", oFileFoleo + ".FOL"); } catch { }
                }
                if (File.Exists(oFileFoleo + ".OK"))
                {
                    fOrig = oFileFoleo + ".OK";
                    fDest = fOrig.Replace(globals.gpDirFolios, globals.gpDirAnulacion);
                    try { File.Move(fOrig, fDest); } catch { }
                }
                globals.oLog.LogMsg("Anulación traslada folio a DIR:Anulacion: " + oFileFoleo, "A", "I");
            }
            fOrig = globals._FilenameBase + ".ENV";
            fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirAnulacion);
            try { File.Move(fOrig, fDest); }
            catch { }
            fOrig = globals._FilenameBase + ".USE";
            fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirAnulacion);
            try { File.Move(fOrig, fDest); }
            catch { }
            fOrig = globals._FilenameBase + ".GEN";
            fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirAnulacion);
            try { File.Move(fOrig, fDest); }
            catch { }
            fOrig = globals._FilenameBase + ".OK";
            fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirAnulacion);
            try { File.Move(fOrig, fDest); }
            catch { }
            fOrig = globals._FilenameBase + ".ERR";
            fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirAnulacion);
            try { File.Move(fOrig, fDest); }
            catch { }
            fOrig = globals._FilenameBase + "_Timbre_DTE.xml";
            fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirAnulacion);
            try { File.Move(fOrig, fDest); }
            catch { }
            globals.oLog.LogMsg("Anulación traslada documentos a DIR:Anulacion: " + globals._FilenameBase, "A", "I");
        }


        private void escribirFolioEnHeader(int folio, ref DTEDoc DteDoc)
        {
            DteDoc.Encabezado.IdDoc.Folio = folio;
        }

        private bool ValidarFactura(ref DTEDoc oDteDoc)
        {
            bool Ok = false;

            if (oDteDoc.Encabezado.Totales.MntExe > 0.0M && oDteDoc.Encabezado.IdDoc.TipoDTE != 41 && oDteDoc.Encabezado.IdDoc.TipoDTE != 34)  //# no cubierto escenario con lineas afectas y exentas
                throw new Exception("MntExe - debe ser 0.0 ");
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
            {
                if (oDteDoc.DscRcgGlobal[i].TpoValor != "$")
                    throw new Exception("TpoValor - debe ser $ ");
                if ((oDteDoc.DscRcgGlobal[i].TpoMov != "D") && (oDteDoc.DscRcgGlobal[i].TpoMov != "R"))
                    throw new Exception("TipoMov - debe ser D o R ");
            }
            if (oDteDoc.Detalle.Count > 60)
                throw new Exception("Numero de lineas excede máximo permitido (60).");

            for (int i = 0; i < oDteDoc.Detalle.Count; i++)
            {
                if (oDteDoc.Detalle[i].NmbItem.Length < 2)
                    throw new Exception("NmbItem - debe tener algún valor, en linea " + (i + 1).ToString());
                if (oDteDoc.Detalle[i].QtyItem < 0.0M)
                    throw new Exception("QtyItem - debe tener valor mayor a 0, en linea " + (i + 1).ToString());
            }

            Ok = true;
            return Ok;
        }
    }

    class ProcesarFE_NoEnviados
    {
        EnviarDTE oEnviarDTE = new EnviarDTE();
        XmlDocument oXmlTimbre = new XmlDocument();
        String p_Local;
        String p_RUTEmisor;

        public ProcesarFE_NoEnviados(String pLocal, String pRUTEmisor)
        {
            p_Local = pLocal;
            p_RUTEmisor = pRUTEmisor;
        }

        public bool ProcesarDTEs_NoEnviados()
        {
            bool oHayFactura = false;
            bool oHayBoleta = false;
            bool oHayNC = false;
            bool oHayGuia = false;
            bool oHayFacturaR = false;

            try
            {
                CrearRespaldo(p_RUTEmisor);
            }
            catch (Exception e)
            {
                globals.oLog.LogMsg("Respaldo fallo. - " + e.Message, "A", "I");
            }

            globals.oLog.LogMsg("Reenvio de Facturas", "A", "I");
            oHayFactura = ProcesarEnvio("F", p_Local, p_RUTEmisor);

            globals.oLog.LogMsg("Reenvio de Boletas", "A", "I");
            oHayBoleta = ProcesarEnvio("B", p_Local, p_RUTEmisor);

            globals.oLog.LogMsg("Reenvio de NC", "A", "I");
            oHayBoleta = ProcesarEnvio("NC", p_Local, p_RUTEmisor);

            globals.oLog.LogMsg("Reenvio de Guias de despacho", "A", "I");
            oHayBoleta = ProcesarEnvio("GD", p_Local, p_RUTEmisor);

            globals.oLog.LogMsg("Reenvio de Facturas reserva", "A", "I");
            oHayBoleta = ProcesarEnvio("FR", p_Local, p_RUTEmisor);

            return oHayFactura || oHayBoleta || oHayNC || oHayGuia || oHayFacturaR; 
        }

        public bool ProcesarEnvio(String p_TipoDTE, String p_Local, String p_RUTEmisor)
        {
            DTEDoc oDteDoc = new DTEDoc();

            try
            {
                string oName = Utils.GetFileNameBaseGeneric(p_TipoDTE, p_Local);
                string[] ficheros = Directory.GetFiles(globals.gpDirDocs, oName + "*.GEN");

                if (ficheros.Length <= 0)
                {
                    globals.oLog.LogMsg("No se encontraron documentos a reenviar", "A", "I");
                    return false;
                }

                foreach (string f in ficheros)
                {
                    string oDocNumber = f.Substring(f.LastIndexOf("_") + 1, f.Length - 4 - (f.LastIndexOf("_") + 1));
                    globals.gpInternalDocNum = Int32.Parse(oDocNumber);
                    string fUse = f.Remove(f.Length - 4, 4) + ".USE";
                    string facturaFile = f.Remove(f.Length - 4, 4) + ".GEN";
                    try
                    {
                        string oData = FileIO.leerFile(facturaFile);
                        FileIO.GetDataFromFile(fUse, ref oDteDoc, p_TipoDTE);
                        //facturaFile.Close();

                        if (oEnviarDTE.EnviarDocumento(oDteDoc.Encabezado.IdDoc.TipoDTE, oData ))
                        {
                            globals.oLog.LogMsg("Marcando documento como enviado", "A", "I");

                            File.Move(f.Remove(f.Length - 4, 4) + ".GEN", f.Remove(f.Length - 4, 4) + ".OK");
                            globals.oLog.LogMsg("marcar documento como enviado (OK): " + globals._FilenameBase + ".GEN", "A", "I");

                            Utils.marcarFoleoEnviado(p_TipoDTE, p_Local, oDteDoc.Encabezado.IdDoc.Folio);
                            globals.oLog.LogMsg("marcar folio como enviado (OK): " + oDteDoc.Encabezado.IdDoc.Folio.ToString(), "A", "I");
                        }
                    }
                    catch (Exception e)
                    {
                        globals.oLog.LogMsg("Error al enviar " + fUse + "  -  " + e.Message, "A", "I");
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private void CrearRespaldo( String p_RUTEmisor)
        {
            globals.oLog.LogMsg("Comienza respaldo.", "A", "I");

            string[] ficheros = Directory.GetFiles(globals.gpDirDocs, globals.gpRUTEmisor + "*.OK");
            //String s = globals.gpDirDocs + "\\" + globals.gpRUTEmisor + "_" + p_TipoDTE + "_" + p_Local + "_";
            string fOrig = "";
            string fDest = "";

            // resta 1 dias
            DateTime fecha = DateTime.Now.AddDays(-1);

            foreach (string f in ficheros)
            {
                    string fUse = f.Remove(f.Length - 3, 3);

                    fOrig = fUse + ".OK";
                    if (!File.Exists(fOrig))
                        continue;
                    fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirRespaldo);
                    File.Move(fOrig, fDest);

                    fOrig = fUse + ".USE";
                    fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirRespaldo);
                    File.Move(fOrig, fDest);

                    fOrig = fUse + "_Timbre_DTE.xml";
                    fDest = fOrig.Replace(globals.gpDirDocs, globals.gpDirRespaldo);
                    File.Move(fOrig, fDest);
            }

            string[] foleos = Directory.GetFiles(globals.gpDirFolios, "*.OK");
            foreach (string f in foleos)
            {
                fDest = f.Replace(globals.gpDirFolios, globals.gpDirRespaldo);
                File.Move(f, fDest);
            }
        }
    }
}
