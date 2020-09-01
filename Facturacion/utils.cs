using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Globalization;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace FactRemota
{
    public static class Utils
    {
        public static bool ValidaRUT(string RUT)
        {
            if (RUT.Length < 2)
                return false;

            int acum = 0;
            int i;
            int j = 2;
            int digit;

            string dv = RUT.Substring(RUT.Length - 1, 1);

            for (i = RUT.Length - 3; i >= 0; i--)
            {
                if (!int.TryParse(RUT.Substring(i, 1), out digit))
                    return false;

                acum = acum + digit * j;
                j++;
                if (j > 7)
                    j = 2;
            }

            acum = 11 - acum % 11;
            if (acum.ToString().ToUpper() == dv.ToUpper())
                return true;
            else
                return false;
        }

        public static string GetRut(string RUT)
        {
            return RUT.Replace(".", "").Replace(",", "");
        }

        public static string readParams(string FileName_RUT)
        {
            StreamReader streamFile;
            String oString;
            int i;

            try
            {
                streamFile = File.OpenText(FileName_RUT + ".setup");

                i = 0;
                while ((oString = streamFile.ReadLine()) != null)
                {
                    i++;
                    switch (i)
                    {
                        case 1: globals.gpRUTEmisor = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 2: globals.gpLocal = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 3: globals.gpDirDocs = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 4: globals.gpDirFolios = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 5: globals.gpLogFile = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 6: globals.gpUser = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 7: globals.gpPW = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 8: globals.gpURLEnvioDoc = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 9: globals.gpURLEnvioBol = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 10: globals.gpURLCAFSucursal = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 11: globals.gpURLCAFRangoDTE = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                        case 12: globals.gpURLCAFRangoBoleta = Crypt.Decrypt(oString, "FElec_2014_..09");
                            break;
                    }
                }
                streamFile.Close();
                globals.gpDirRespaldo = Directory.GetCurrentDirectory() + "\\Respaldo";
                globals.gpDirAnulacion = Directory.GetCurrentDirectory() + "\\Anulacion";
                return ""; // "" => sin error
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }

        public static string SaveParams(String FileName)
        {
            StreamWriter streamFile;

            try
            {
                streamFile = File.CreateText(FileName + ".setup");

                streamFile.WriteLine(Crypt.Encrypt(globals.gpRUTEmisor, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpLocal, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpDirDocs, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpDirFolios, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpLogFile, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpUser, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpPW, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpURLEnvioDoc, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpURLEnvioBol, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpURLCAFSucursal, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpURLCAFRangoDTE, "FElec_2014_..09"));
                streamFile.WriteLine(Crypt.Encrypt(globals.gpURLCAFRangoBoleta, "FElec_2014_..09"));

                streamFile.Close();
                return ""; // "" => sin error
            }
            catch (Exception e)
            {
                globals.oLog.LogMsg("Error al guardar parametros - " + e.Message + " - " + e.StackTrace, "A", "I");
                return "Error: " + e.Message;
            }
        }

        public static XmlDocument CargarXML(string pFileName)
        {
            try
            {
                XmlDocument oXml = new XmlDocument();
                oXml.PreserveWhitespace = false;
                oXml.Load(pFileName);
                return oXml;
            }
            catch
            {
                throw new Exception("No se pudo abrir archivo XML: " + pFileName);
            }
        }

        public static string ValidarURLs(string URLEnvio, string URLEnvioBol, string URLCAFSuc, string URLRngBol, string URLRngDTE)
        {
            string sURL = "";
            try
            {
                sURL = "URL Envio documentos " + URLEnvio + " , no responde revisar.";
                sendRequest(URLEnvio, "", "text/json");
                sURL = "URL Envio boletas " + URLEnvioBol + " , no responde revisar.";
                sendRequest(URLEnvioBol, "", "text/json");
                sURL = "URL CAF de sucursal " + URLCAFSuc + " , no responde revisar.";
                sendRequest(URLCAFSuc, "", "text/json");
                sURL = "URL rango boletas " + URLRngBol + " , no responde revisar.";
                sendRequest(URLRngBol, "", "text/json");
                sURL = "URL rango DTE " + URLRngDTE + " , no responde revisar.";
                sendRequest(URLRngDTE, "", "text/json");
                return "";
            }
            catch (Exception e)
            {
                globals.oLog.LogMsg("Error al validar URLs - " + e.Message + " - " + e.StackTrace, "A", "I");
                return sURL + " - " + e.Message;
            }
        }

        public static string sendRequest(string URL, string postData, string contentType)
        {
            string sErr;

            try
            {
                if (globals.Debug)
                    globals.oLog.LogMsg("sendRequest start", "A", "I");

                WebRequest request = WebRequest.Create(URL);
                request.Timeout = globals.Timeout;
                //request.Credentials = new NetworkCredential(globals.gpUser, globals.gpPW);
                //request.Credentials = CredentialCache.DefaultNetworkCredentials;
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(globals.gpUser + ":" + globals.gpPW)); 
                request.Method = "POST";

                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = contentType;
                request.ContentLength = byteArray.Length;
                if (globals.Debug)
                    globals.oLog.LogMsg("sendRequest initStream", "A", "I");
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(byteArray, 0, byteArray.Length);
                requestStream.Close();

                if (globals.Debug)
                    globals.oLog.LogMsg("GetResponse start", "A", "I");
                WebResponse response = request.GetResponse();
                if (globals.Debug)
                    globals.oLog.LogMsg("GetResponse end", "A", "I");

                if (((HttpWebResponse)(response)).StatusCode != HttpStatusCode.OK)
                {
                    sErr = "Error servidor portal " + ((HttpWebResponse)(response)).StatusDescription + " - " + ((HttpWebResponse)(response)).StatusCode;
                    throw new Exception(sErr);
                }

                if (globals.Debug)
                    globals.oLog.LogMsg("GetResponseStream start", "A", "I");
                Stream responseStream = response.GetResponseStream();
                if (globals.Debug)
                    globals.oLog.LogMsg("GetResponseStream done", "A", "I");
                StreamReader reader = new StreamReader(responseStream);
                string responseFromServer = reader.ReadToEnd();
                if (globals.Debug)
                    globals.oLog.LogMsg("GetResponseStream read", "A", "I");
                reader.Close();
                responseStream.Close();
                response.Close();
                if (globals.Debug)
                    globals.oLog.LogMsg("GetResponseStream closed", "A", "I");

                return responseFromServer;
            }
            catch (Exception e)
            {
                globals.oLog.LogMsg("Error sendRequest " + e.Message, "A", "I");
                throw new Exception("Error sendRequest " + e.Message);
            }
        }

        // Codificacione (Encoding)        
        public static string ToAsciiString(string s)
        {
            //Encoding s737 = Encoding.GetEncoding(737); 
            Encoding ascii = Encoding.ASCII;
            Encoding unicode = Encoding.Unicode;

            // Convert the string into a byte array.
            byte[] unicodeBytes = unicode.GetBytes(s);
            byte[] asciiBytes = Encoding.Convert(unicode, ascii, unicodeBytes);

            // Convert the new byte[] into a char[] and then into a string. 
            char[] asciiChars = new char[ascii.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            ascii.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            return new string(asciiChars);
        }

        public static string ToIso8859String(string s)
        {
            Encoding iso8859 = Encoding.GetEncoding(28591);
            Encoding unicode = Encoding.Unicode;

            // Convert the string into a byte array.
            byte[] unicodeBytes = unicode.GetBytes(s);
            byte[] iso8859Bytes = Encoding.Convert(unicode, iso8859, unicodeBytes);

            // Convert the new byte[] into a char[] and then into a string. 
            char[] asciiChars = new char[iso8859.GetCharCount(iso8859Bytes, 0, iso8859Bytes.Length)];
            iso8859.GetChars(iso8859Bytes, 0, iso8859Bytes.Length, asciiChars, 0);
            return new string(asciiChars);
        }

        public static string ToIso8859SinEspeciales(string s)
        {
            s = s.Replace("&", "");  //"&amp;
            s = s.Replace("<", "");  //"&lt;
            s = s.Replace(">", "");  //"&gt;
            s = s.Replace("\"", ""); //"&quot;
            s = s.Replace("'", "");  //"&apos;
            return s;
        }

        public static string stringTruncate(string s, int largomax)
        {
            return s.Substring(0, (s.Length > largomax) ? largomax : s.Length).Trim();
        }

        // Validaciones en entrada de datos fHeader y fDetalle
        public static string stringValidate(string campo, string s, int largomax)
        {
            s = s.Trim();
            if (s.Length > largomax)
                throw new Exception(campo + " - Largo de string excede el máximo permitido.");

            return s;
        }

        public static int TipoDTEValidate(string campo, string Tipo, string ValorLeido)
        {
            ValorLeido = ValorLeido.Trim();
            if ((Tipo == "F") && (ValorLeido != "33"))
                throw new Exception(campo + " - Factura leida no coincide con tipo DTE indicado en archivo enviado.");
            else if ((Tipo == "FR") && (ValorLeido != "33"))
                throw new Exception(campo + " - Factura reserva leida no coincide con tipo DTE indicado en archivo enviado.");
            else if ((Tipo == "B") && (ValorLeido != "39"))
                throw new Exception(campo + " - Boleta leida no coincide con tipo DTE indicado en archivo enviado.");
            else if ((Tipo == "NC") && (ValorLeido != "61"))
                throw new Exception(campo + " - Nota de crédito leida no coincide con tipo DTE indicado en archivo enviado.");
            else if ((Tipo == "GD") && (ValorLeido != "52"))
                throw new Exception(campo + " - Guía de despacho leida no coincide con tipo DTE indicado en archivo enviado.");
            else if (((Tipo == "F") && (ValorLeido == "33")) || ((Tipo == "NC") && (ValorLeido == "61")) || ((Tipo == "B") && (ValorLeido == "39")) || ((Tipo == "FR") && (ValorLeido == "33")) || ((Tipo == "GD") && (ValorLeido == "52")) )
                return int.Parse(ValorLeido);
            else
                throw new Exception(campo + " - Tipo de documento no soportado. Debe ser F, FR, B, NC o GD.");
        }

        public static int intValidate(string campo, string s)
        {
            int i;

            try 
            {
                i = (int)float.Parse(s, CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new Exception(campo + " - Numero invalido.");
            }

            return i;
        }

        public static string DateValidate(string campo, string s)
        {
            string oDate = "";
            DateTime fec, des, has;

            try
            {// YYYY-MM-DD
                int d = int.Parse(s.Substring(8, 2));
                int m = int.Parse(s.Substring(5, 2));
                int y = int.Parse(s.Substring(0, 4));
                fec = new DateTime(y, m, d);
                des = new DateTime(2018, 1, 1);
                has = new DateTime(2050, 12, 31);
            }
            catch
            {
                throw new Exception(campo + " - Fecha invalido, formato: AAAA-MM-DD.");
            }

            if ((fec < des) || (fec > has))
                throw new Exception(campo + " - Fecha rango invalido.");

            oDate = s.Substring(0, 4) + "-" + s.Substring(5, 2) + "-" + s.Substring(8, 2);
            return oDate;
        }

        public static string RUTValidate(string campo, string TipoDTE, bool ValidaSiempre, string s)
        {
            string sRUT;

            sRUT = s.Replace(".", "").Replace(",", "");

            if ((sRUT == "66666666-6") && (TipoDTE == "39") && (!ValidaSiempre))
                return sRUT;

            if (!ValidaRUT(sRUT))
                throw new Exception(campo + " - RUT invalido.");

            return sRUT;
        }
        
        public static string DateFormat(string f)
        {
            int i;
            string s;

            i = int.Parse(f.Substring(8, 2));
            s = i.ToString();
            i = int.Parse(f.Substring(5, 2));
            switch (i)
            {
                case 1: s = s + " de Enero de "; break;
                case 2: s = s + " de Febrero de "; break;
                case 3: s = s + " de Marzo de "; break;
                case 4: s = s + " de Abril de "; break;
                case 5: s = s + " de Mayo de "; break;
                case 6: s = s + " de Junio de "; break;
                case 7: s = s + " de Julio de "; break;
                case 8: s = s + " de Agosto de "; break;
                case 9: s = s + " de Septiembre de "; break;
                case 10: s = s + " de Octubre de "; break;
                case 11: s = s + " de Noviembre de "; break;
                case 12: s = s + " de Diciembre de "; break;
            }
            s = s + f.Substring(0, 4);
            return s;
        }

        public static decimal floatValidate(string campo, string s)
        {
            decimal f;

            try
            {
                f = decimal.Parse(s.Replace(",", "."), CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new Exception(campo + " - Numero decimal invalido.");
            }

            return f;
        }

        public static decimal floatValidate(string campo, string s, int n)
        {
            decimal f = floatValidate(campo, s);
            return Math.Round(f, n, MidpointRounding.AwayFromZero);
        }

        public static decimal floatRound(decimal f, int n)
        {
            return Math.Round(f, n, MidpointRounding.AwayFromZero);
        }

        public static string decimalToString(decimal f)
        {
            return f.ToString(CultureInfo.InvariantCulture);
        }

        // Obtener variables usadas para leer factura recibida
        public static string getFechaFormateada()
        {
            return DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0');
        }

        public static void SetFileNameBase(String p_TipoDoc, String p_Local, int p_NumDoc)
        {
            XmlDocument oXmlParams = Utils.CargarXML("FormatoFactura.xml");
            string vRutEmisor = "";

            string SeparadorRUT = oXmlParams.SelectSingleNode("/Format/FormatoArchivoEnviadoPOS/RUT").Attributes["Separador"].Value.ToString();
            if (SeparadorRUT == null)
                SeparadorRUT = "";
            if ((SeparadorRUT != ".") && (SeparadorRUT != ",") && (SeparadorRUT != ""))
                throw new Exception("Separador de archivo enviado invalido");

            if (SeparadorRUT.Length == 0)
                vRutEmisor = globals.gpRUTEmisor;
            else
                vRutEmisor = globals.gpRUTEmisor.Substring(0, globals.gpRUTEmisor.Length - 8) + SeparadorRUT + globals.gpRUTEmisor.Substring(globals.gpRUTEmisor.Length - 8, 3) + SeparadorRUT + globals.gpRUTEmisor.Substring(globals.gpRUTEmisor.Length - 5, 5);

            string Fecha = getFechaFormateada();
            globals._FilenameBase = globals.gpDirDocs + "\\" + vRutEmisor + "_" + p_TipoDoc + "_" + p_Local + "_" + Fecha + "_" + p_NumDoc.ToString();
        }

        public static string GetFileNameBaseGeneric(String p_TipoDoc, String p_Local)
        {
            XmlDocument oXmlParams = Utils.CargarXML("FormatoFactura.xml");
            string vRutEmisor = "";

            string SeparadorRUT = oXmlParams.SelectSingleNode("/Format/FormatoArchivoEnviadoPOS/RUT").Attributes["Separador"].Value.ToString();
            if (SeparadorRUT == null)
                SeparadorRUT = "";
            if ((SeparadorRUT != ".") && (SeparadorRUT != ",") && (SeparadorRUT != ""))
                throw new Exception("Separador de archivo enviado invalido");

            if (SeparadorRUT.Length == 0)
                vRutEmisor = globals.gpRUTEmisor;
            else
                vRutEmisor = globals.gpRUTEmisor.Substring(0, globals.gpRUTEmisor.Length - 8) + SeparadorRUT + globals.gpRUTEmisor.Substring(globals.gpRUTEmisor.Length - 8, 3) + SeparadorRUT + globals.gpRUTEmisor.Substring(globals.gpRUTEmisor.Length - 5, 5);

            return  vRutEmisor + "_" + p_TipoDoc + "_" + p_Local + "_";
        }

        // Obtener y asignar Folios
        public static void BorrrarFoleosNoUsados(String p_TipoDoc, String p_Local)
        {
            string oTipoDoc = p_TipoDoc;
            if (oTipoDoc == "FR")
                oTipoDoc = "F";

            string[] ficheros = Directory.GetFiles(globals.gpDirFolios, globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_*.FOL");
            foreach (string f in ficheros)
            {
                File.Delete(f);
            }
        }

        public static List<int> GetFoliosAsignados(String p_TipoDoc, String p_Local)
        {
            List<nroFolio> folios = new List<nroFolio>();
            List<int> foliosNoEnviados = new List<int>();
            List<CAFDesdeHasta> ListCafDesdeHasta;
            string oTipoDTE;
            string oTipoDoc;

            oTipoDoc = p_TipoDoc;
            if (oTipoDoc == "FR")
                oTipoDoc = "F";

            string[] ficheros = Directory.GetFiles(globals.gpDirFolios, globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_*.USE");
            string s = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_";
            int desde = s.Length;
            int fol;

            if (p_TipoDoc == "F")
                oTipoDTE = "33";
            else if (p_TipoDoc == "FR")
                oTipoDTE = "33";
            else if (p_TipoDoc == "B")
                oTipoDTE = "39";
            else if (p_TipoDoc == "NC")
                oTipoDTE = "61";
            else if (p_TipoDoc == "GD")
                oTipoDTE = "52";
            else if (p_TipoDoc == "FE")
                oTipoDTE = "34";
            else if (p_TipoDoc == "BE")
                oTipoDTE = "41";
            else
                throw new Exception("Tipo documento (DTE) invalido: " + p_TipoDoc + " - Valores permitidos: Factura F, Factura reserva FR, Boleta B, Nota de Credito NC  y Guía de despacho GD");


            foreach (string f in ficheros)
            {
                fol = int.Parse(f.Substring(desde, f.Length - desde - (5 - 1)));
                foliosNoEnviados.Add(fol);
            }

            CAFParamsJson oCAFParamsJson = new CAFParamsJson();
            oCAFParamsJson.P0 = p_Local;
            oCAFParamsJson.P1 = oTipoDTE;
            oCAFParamsJson.P2 = "";
            oCAFParamsJson.P3 = "";
            oCAFParamsJson.P4 = "";
            oCAFParamsJson.Rut = globals.gpRUTEmisor.Replace("-","");
            string postData = JsonConvert.SerializeObject(oCAFParamsJson);

            string CAFstr = Utils.sendRequest(globals.gpURLCAFSucursal, postData, "text/json");

            if (false) //Cambiara true si es base64
            {
                var base64EncodedBytes = System.Convert.FromBase64String(CAFstr);
                CAFstr = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }

            ListCafDesdeHasta = JsonConvert.DeserializeObject<List<CAFDesdeHasta>>(CAFstr);

            // Params {0} cantidad de foleos = Hasta - Desde + 1
            // Params {1} foleo inicial - 1 = Desde - 1
            // Params {2} Tipo de documento
            // Params {3} foleo inicial = Desde 
            // Params {4} foleo Final   = Hasta 

            List<int> auxList = new List<int>();
            string foliosStr;
            for (int i = 0; i < ListCafDesdeHasta.Count; i++)
            {
                oCAFParamsJson.P0 = (ListCafDesdeHasta[i].Hasta - ListCafDesdeHasta[i].Desde + 1).ToString();
                oCAFParamsJson.P1 = (ListCafDesdeHasta[i].Desde - 1).ToString();
                oCAFParamsJson.P2 = oTipoDTE;
                oCAFParamsJson.P3 = (ListCafDesdeHasta[i].Desde).ToString();
                oCAFParamsJson.P4 = (ListCafDesdeHasta[i].Hasta).ToString();
                oCAFParamsJson.Rut = globals.gpRUTEmisor.Replace("-","");
                postData = JsonConvert.SerializeObject(oCAFParamsJson);

                if (oTipoDTE == "39" || oTipoDTE == "41")
                    foliosStr = Utils.sendRequest(globals.gpURLCAFRangoBoleta, postData, "text/json");
                else
                    foliosStr = Utils.sendRequest(globals.gpURLCAFRangoDTE, postData, "text/json");

                folios = JsonConvert.DeserializeObject<List<nroFolio>>(foliosStr);

                if (folios.Count > 0)
                {
                    XmlDocument CAFXmlDoc = new XmlDocument();
                    CAFXmlDoc.LoadXml(ListCafDesdeHasta[i].CafStr);
                    int D = int.Parse(CAFXmlDoc.SelectSingleNode("/AUTORIZACION/CAF/DA/RNG/D").InnerText);
                    int H = int.Parse(CAFXmlDoc.SelectSingleNode("/AUTORIZACION/CAF/DA/RNG/H").InnerText);

                    string CAFFileName = globals.gpDirFolios + "\\_CAF_" + oTipoDoc + "_" + D.ToString() + "_" + H.ToString() + ".xml";
                    if (!File.Exists(CAFFileName))
                        CAFXmlDoc.Save(CAFFileName);

                    for (int k = 0; k < folios.Count; k++)
                        auxList.Add(folios[k].folio);
                }
            }

            int oIndex;
            for (int i = 0; i < foliosNoEnviados.Count; i++)
            {
                oIndex = auxList.IndexOf(foliosNoEnviados[i]);
                if (oIndex >= 0)
                    auxList.RemoveAt(oIndex);
            }

            return auxList;
        }

        public static int GetFoleoSiguiente(String p_TipoDoc, String p_Local)
        {
            String oTipoDoc = p_TipoDoc;
            if (oTipoDoc == "FR")
                oTipoDoc = "F";

            string[] ficheros = Directory.
                GetFiles(globals.gpDirFolios, globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_*.FOL");
            string s = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_";
            int desde = s.Length;
            int fol = int.MaxValue;
            int aux, ini, fin, hasta;

            if (ficheros.Length <= 0)
                throw new Exception("No hay folios disponibles");

            foreach (string f in ficheros)
            {
                // numero de folio
                aux = int.Parse(f.Substring(desde, f.Length - desde - (5 - 1)));
                if (aux < fol)
                    fol = aux;
            }

            // Buscar CAF
            ficheros = Directory.GetFiles(globals.gpDirFolios, "_CAF_" + oTipoDoc + "_*.xml");
            if (ficheros.Length <= 0)
                throw new Exception("No hay rangos CAF disponibles");

            foreach (string f in ficheros)
            {
                ini = f.IndexOf("\\_CAF_" + oTipoDoc + "_");

                if ((oTipoDoc == "NC") || (oTipoDoc == "GD") || (oTipoDoc == "BE") || (oTipoDoc == "FE"))
                    ini = f.IndexOf('_', ini) + 8;
                else
                    ini = f.IndexOf('_', ini) + 7;
                
                fin = f.LastIndexOf('_');
                desde = int.Parse(f.Substring(ini, fin - ini));
                aux = f.Length - 5;
                hasta = int.Parse(f.Substring(fin + 1, aux - fin));
                if ((desde <= fol) && (fol <= hasta))
                {
                    globals._xmlCAF = Utils.CargarXML(f);
                    break;
                }
            }

            if (ficheros.Length <= 0)
                throw new Exception("Foleos no coinciden con rango CAF");

            //s = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + p_TipoDoc + "_" + p_Local + "_";
            //string CAFFileName = globals.gpDirFolios + "\\_CAF_" + D.ToString() + "_" + H.ToString();

            return fol;
        }

        public static void EscribirFoleos(List<int> foleos, String p_TipoDoc, String p_Local)
        {
            string s;
            string oTipoDoc = p_TipoDoc;
            if (oTipoDoc == "FR")
                oTipoDoc = "F";

            foreach (int fol in foleos)
            {
                s = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_" + fol.ToString() + ".FOL";
                File.Create(s);
            }
        }

        // Bloquear, modificar y cambiar extensiones en uso de archivos
        public static void marcarFoleoUsado(String p_TipoDoc, String p_Local, int foleo)
        {
            string oTipoDoc = p_TipoDoc;
            if (oTipoDoc == "FR")
                oTipoDoc = "F";

            string s1 = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_" + foleo.ToString() + ".FOL";
            string s2 = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_" + foleo.ToString() + ".USE";
            File.Move(s1, s2);
        }

        public static void marcarFoleoEnviado(String p_TipoDoc, String p_Local, int foleo)
        {
            string oTipoDoc = p_TipoDoc;
            if (oTipoDoc == "FR")
                oTipoDoc = "F";

            string s1 = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_" + foleo.ToString() + ".USE";
            string s2 = globals.gpDirFolios + "\\" + globals.gpRUTEmisor + "_" + oTipoDoc + "_" + p_Local + "_" + foleo.ToString() + ".OK";
            File.Move(s1, s2);
        }

        public static decimal Round_4(decimal f)
        {
            return Math.Round(f, 4, MidpointRounding.AwayFromZero);
        }

        public static decimal Round_2(decimal f)
        {
            return Math.Round(f, 4, MidpointRounding.AwayFromZero);
        }

        public static decimal Round_0(decimal f)
        {
            return Math.Round(f, 0, MidpointRounding.AwayFromZero);
        }

        public static void SetDebugModeAndTimeout()
        {
            XmlDocument PrinterDefXml = null;

            try 
            { 
                PrinterDefXml = Utils.CargarXML("FormatoFactura.xml");
                string sDebug = PrinterDefXml.SelectSingleNode("/Format/Debug").Attributes["Debug"].Value.ToString();
                if (sDebug.ToUpper() == "TRUE")
                    globals.Debug = true;
            }
            catch 
            {
                globals.Debug = false;
            }

            try
            {
                string sTimeout = PrinterDefXml.SelectSingleNode("/Format/Timeout").Attributes["Timeout"].Value.ToString();
                Int32 timeout = Int32.Parse(sTimeout);
                if (timeout < 3000)
                    timeout = 3000;
                globals.Timeout = timeout;
            }
            catch
            {
                globals.Timeout = 30000;
            }
        }
    }
}