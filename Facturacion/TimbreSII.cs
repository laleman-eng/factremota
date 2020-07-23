using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Security.Cryptography;
using IDAutomation.Windows.Forms.PDF417Barcode;

namespace FactRemota
{
    public static class TimbreSII
    {
        public static void SetImage_PDF417(XmlDocument Timbre, string pFileName)
        {
            IDAutomation.Windows.Forms.PDF417Barcode.PDF417Barcode PDF417Barcode = new IDAutomation.Windows.Forms.PDF417Barcode.PDF417Barcode();
            Bitmap imagePdf417 = new Bitmap(350, 150);

            PDF417Barcode.DataToEncode = Timbre.InnerXml;
            PDF417Barcode.PDFMode = IDAutomation.Windows.Forms.PDF417Barcode.PDF417Barcode.PDF417Modes.Binary;
            PDF417Barcode.Resolution = IDAutomation.Windows.Forms.PDF417Barcode.PDF417Barcode.Resolutions.Custom;
            PDF417Barcode.Height = 150;
            PDF417Barcode.Width = 315;

            // Laser
            //PDF417Barcode.PDFColumns = 16; 
            //PDF417Barcode.ResolutionCustomDPI = 600;

            // Bixolon
            PDF417Barcode.PDFColumns = 12; 
            PDF417Barcode.ResolutionCustomDPI = 90;

            PDF417Barcode.XtoYRatio = 4;
            PDF417Barcode.PDFErrorCorrectionLevel = 5;
            PDF417Barcode.RefreshImage();
            PDF417Barcode.SaveImageAs(pFileName , System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        static byte[] PEMaBytes(string data)
        {
            Regex rex = new Regex(@"-----BEGIN RSA PRIVATE KEY-----\s+((?<datos>\S+)\s+)+-----END RSA PRIVATE KEY-----");
            Match m = rex.Match(data);
            if (!m.Success)
                throw new Exception("El elemento de firma no corresponde al formato de clave RSA esperado.");
            StringBuilder datos = new StringBuilder();
            foreach (Capture c in m.Groups["datos"].Captures)
            {
                datos.Append(c);
            }
            return Convert.FromBase64String(datos.ToString());
        }

        static byte[] DERExtractIntegerAsByteArrayV2(byte[] inputBytes, ref int startOfInteger)
        {
            int index = startOfInteger;
            // Make sure we're looking at an integer
            if (inputBytes[index] != 0x02) throw new ArgumentException();
            index++;
            int tempLength = inputBytes[index];
            index++;
            // CryptoAPI can't deal with leading zero bytes, so strip them off if we're looking 
            // at a multi-byte quantity

            byte[] retbytes = new byte[tempLength];
            Array.Copy(inputBytes, index, retbytes, 0, tempLength);
            startOfInteger = index + tempLength;
            return retbytes;
        }

        static RSAParameters ParsePEMPrivateKey(byte[] asnBytes, byte[] checkModulus, int flag)
        {
            byte[] modulus;
            byte[] exponent;
            byte[] d;
            byte[] p;
            byte[] q;
            byte[] dp;
            byte[] dq;
            byte[] coeff;

            // First check that the first byte of the array is 0x30, which is the start of
            // a sequence.  If it isn't throw an exception
            if (asnBytes[0] != 0x30)
            {
                throw new ArgumentException();
            }
            // check the second byte; top 4 bits should be 8
            if ((asnBytes[1] & 0xf0) != 0x80)
            {
                throw new ArgumentException();
            }
            // OK, get the length of the SEQUENCE
            int sequenceLengthLength = (int)(asnBytes[1] & 0x0f);
            int sequenceLength;
            if (sequenceLengthLength == 1) sequenceLength = asnBytes[2]; else sequenceLength = (asnBytes[2] * 256) + asnBytes[3];
            int index = 2 + sequenceLengthLength;
            // Now start processing integers. 
            byte[] zero = DERExtractIntegerAsByteArray(asnBytes, ref index);
            modulus = DERExtractIntegerAsByteArray(asnBytes, ref index);
            //Debug.Assert(modulus.Length == checkModulus.Length);
            int innerindex = 0;
            foreach (byte b in modulus)
            {
                //Debug.Assert(b == checkModulus[innerindex]);
                innerindex++;
            }
            exponent = DERExtractIntegerAsByteArray(asnBytes, ref index);
            d = DERExtractIntegerAsByteArray(asnBytes, ref index);
            p = DERExtractIntegerAsByteArray(asnBytes, ref index);
            q = DERExtractIntegerAsByteArray(asnBytes, ref index);
            dp = DERExtractIntegerAsByteArray(asnBytes, ref index);
            dq = DERExtractIntegerAsByteArray(asnBytes, ref index);
            if (flag == 0)
            {
                coeff = DERExtractIntegerAsByteArray(asnBytes, ref index);
            }
            else
            {
                coeff = DERExtractIntegerAsByteArrayV2(asnBytes, ref index);
            }

            RSAParameters rsa = new RSAParameters();
            rsa.Modulus = modulus;
            rsa.Exponent = exponent;
            rsa.D = d;
            rsa.P = p;
            rsa.Q = q;
            rsa.DP = dp;
            rsa.DQ = dq;
            rsa.InverseQ = coeff;
            return rsa;
        }

        static byte[] DERExtractIntegerAsByteArray(byte[] inputBytes, ref int startOfInteger)
        {
            int index = startOfInteger;
            // Make sure we're looking at an integer
            if (inputBytes[index] != 0x02) throw new ArgumentException();
            index++;
            int tempLength = inputBytes[index];
            index++;
            // CryptoAPI can't deal with leading zero bytes, so strip them off if we're looking 
            // at a multi-byte quantity
            if (tempLength > 1)
            {
                while (inputBytes[index] == 0x00)
                {
                    index++;
                    tempLength--;
                }
            }

            byte[] retbytes = new byte[tempLength];
            Array.Copy(inputBytes, index, retbytes, 0, tempLength);
            startOfInteger = index + tempLength;
            return retbytes;
        }

        public static XmlDocument TimbrarDTE(XmlDocument Datos, XmlDocument AutorizacionSII, int flag)
        {
            XmlDocument res = new XmlDocument();
            res.PreserveWhitespace = true;

            // 1 obtener la clave para la firma -- 1b cargar /AUTORIZACION/RSASK
            // Build an RSAParameters structure from the byte array
            // antes de calcular los parámetros, pasar el módulo que debería ser igual a la clave pública
            // Create a new RSACSP object, attached to a random, transient key container
            // 3a obtener el contexto de encripción

            XmlElement rsask = (XmlElement)AutorizacionSII.SelectSingleNode("/AUTORIZACION/RSASK");
            if (null == rsask)
                throw new Exception("La firma autorizadora no se encuentra en el documento.\nAsegúrese que se trata del documento entregado por SII.");

            byte[] inputBytes = PEMaBytes(rsask.InnerText);
            XmlElement elModulus = (XmlElement)AutorizacionSII.SelectSingleNode("/AUTORIZACION/CAF/DA/RSAPK/M");
            if (null == elModulus)
                throw new ArgumentException("No se encuentra la clave pública.", "AutorizaciónSII");
            byte[] modulus = Convert.FromBase64String(elModulus.InnerText);
            RSAParameters rsa = ParsePEMPrivateKey(inputBytes, modulus, flag);

            RSACryptoServiceProvider.UseMachineKeyStore = true;
            RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider();
            // Import the parameters into the key container
            // 3b poner la clave en el hash
            rsaCSP.ImportParameters(rsa);
            // 3 crear la firma
            System.Text.Encoding e1 = System.Text.Encoding.GetEncoding("ISO-8859-1");
            string datosAfirmar = Datos.OuterXml; // los datos a firmar, como string

            int nLength = e1.GetByteCount(datosAfirmar); // preparar un arreglo de bytes para tener los datos
            byte[] bytesAfirmar = new byte[nLength];
            e1.GetBytes(datosAfirmar, 0, datosAfirmar.Length, bytesAfirmar, 0); // obtener los datos como bytes

            // Obtener el hash SHA1
            SHA1 shalgo = new SHA1Managed();
            // Encrypt (second arg false says use PKCS#1 padding, not OAEP)
            byte[] firma = rsaCSP.SignData(bytesAfirmar, shalgo); // Encrypt(hashAcifrar, false); // firmar con RSA


            // 4 crear el documento de salida	
            XmlElement elTED = res.CreateElement("TED");

            elTED.SetAttribute("version", "1.0");
            // 5 pegar los datos
            elTED.InnerXml = Datos.DocumentElement.OuterXml;
            // 6 pegar la firma
            XmlElement elFRMT = res.CreateElement("FRMT");
            elFRMT.SetAttribute("algoritmo", "SHA1withRSA");
            elFRMT.InnerText = Convert.ToBase64String(firma);
            elTED.AppendChild(elFRMT);
            res.AppendChild(elTED);
            rsaCSP.Clear();//cambio MAXIMISE -> libera la llave del store
            return res;
        }

        public static XmlDocument EmitirTimbre(string pTipoDTE, string pFoleo, string pFechaEmision, string pRUTReceptor, string pRazonReceptor, string pMontoTotal, string pDescItem, XmlDocument pCAF)
        {
            XmlDocument xmlTimbre = new XmlDocument();
            XmlNode _xmlNode;
            XmlNode auxNode;

            xmlTimbre.LoadXml("<DD></DD>");
            _xmlNode = xmlTimbre.SelectSingleNode("/DD");

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "RE", "");
            auxNode.InnerText = globals.gpRUTEmisor;
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "TD", "");
            auxNode.InnerText = pTipoDTE;
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "F", "");
            auxNode.InnerText = pFoleo;
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "FE", "");
            auxNode.InnerText = pFechaEmision; 
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "RR", "");
            auxNode.InnerText = pRUTReceptor;
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "RSR", "");
            auxNode.InnerText = Utils.stringTruncate(Utils.ToIso8859SinEspeciales(pRazonReceptor), 40);
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "MNT", "");
            auxNode.InnerText = pMontoTotal;
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "IT1", "");
            auxNode.InnerText = Utils.stringTruncate(Utils.ToIso8859SinEspeciales(pDescItem), 40);
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "CAF", "");
            auxNode = xmlTimbre.ImportNode(pCAF.SelectSingleNode("/AUTORIZACION/CAF"), true);
            _xmlNode.AppendChild(auxNode);

            auxNode = xmlTimbre.CreateNode(XmlNodeType.Element, "TSTED", "");
            auxNode.InnerText = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            _xmlNode.AppendChild(auxNode);

            xmlTimbre = TimbreSII.TimbrarDTE(xmlTimbre, globals._xmlCAF, 0);

            return xmlTimbre;
        }
    }
}
