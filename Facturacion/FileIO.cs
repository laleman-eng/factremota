using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace FactRemota
{
    public static class FileIO
    {
        public static void GetDataFromFile(string oFile, ref DTEDoc oDteDoc, String p_TipoDTE)
        { // Formato nombre del archivo - Archivo <RUT Emisor>_<Tipo documento>_<Local>_Fecha_<Numero documento>.ENV

            string jsonstr = File.ReadAllText(oFile);
//            string AUX = jsonstr.Substring(jsonstr.IndexOf("CdgSIISucur"));
//            AUX = AUX.Substring(0,AUX.IndexOf(",")+1);
//            AUX = AUX.Substring("CdgSIISucur".
            oDteDoc = JsonConvert.DeserializeObject<DTEDoc>(jsonstr);
            if (((p_TipoDTE == "F") || (p_TipoDTE == "FR")) && (oDteDoc.Encabezado.IdDoc.TipoDTE != 33))
                throw new Exception("Factura enviada y tipo DTE distinto a 33");
            else if ((p_TipoDTE == "B") && (oDteDoc.Encabezado.IdDoc.TipoDTE != 39))
                throw new Exception("Boleta enviada y tipo DTE distinto a 39");
            else if ((p_TipoDTE == "NC") && (oDteDoc.Encabezado.IdDoc.TipoDTE != 61))
                throw new Exception("NC enviada y tipo DTE distinto a 61");
            else if ((p_TipoDTE == "GD") && (oDteDoc.Encabezado.IdDoc.TipoDTE != 52))
                throw new Exception("Guía de despacho enviada y tipo DTE distinto a 52");
            else if ((p_TipoDTE == "FE") && (oDteDoc.Encabezado.IdDoc.TipoDTE != 34))
                throw new Exception("Factura Exenta enviada y tipo DTE distinto a 34 ");
            else if ((p_TipoDTE == "BE") && (oDteDoc.Encabezado.IdDoc.TipoDTE != 41))
                throw new Exception("Boleta Exenta enviada y tipo DTE distinto a 41 ");
            // Corregir Fechas y RUT
            if (oDteDoc.Encabezado.IdDoc.FchEmis.Length > 10)
                oDteDoc.Encabezado.IdDoc.FchEmis = oDteDoc.Encabezado.IdDoc.FchEmis.Substring(0, 10);
            if (oDteDoc.Encabezado.IdDoc.FchVenc.Length > 10)
                oDteDoc.Encabezado.IdDoc.FchVenc = oDteDoc.Encabezado.IdDoc.FchVenc.Substring(0, 10);
            for (int i=0; i < oDteDoc.Detalle.Count; i++)
            {
                if (oDteDoc.Detalle[i].FchElabor.Length > 10)
                    oDteDoc.Detalle[i].FchElabor = oDteDoc.Detalle[i].FchElabor.Substring(0, 10);
                if (oDteDoc.Detalle[i].FchVencim.Length > 10)
                    oDteDoc.Detalle[i].FchVencim = oDteDoc.Detalle[i].FchVencim.Substring(0, 10);
            }
            for (int i = 0; i < oDteDoc.Referencia.Count; i++)
            {
                if (oDteDoc.Referencia[i].FchRef.Length > 10)
                    oDteDoc.Referencia[i].FchRef = oDteDoc.Referencia[i].FchRef.Substring(0, 10);
            }

            oDteDoc.Encabezado.Emisor.RUTEmisor = Utils.GetRut(oDteDoc.Encabezado.Emisor.RUTEmisor).TrimStart('0').Trim();
            oDteDoc.Encabezado.Receptor.RUTRecep = Utils.GetRut(oDteDoc.Encabezado.Receptor.RUTRecep).TrimStart('0').Trim();

        }

        public static void reescribirFacturaFoliada(DTEDoc oDteDoc)
        {

            try { File.Delete(globals._FilenameBase + ".USE"); }
            catch { }

            string oData = JsonConvert.SerializeObject(oDteDoc); 
            
            StreamWriter facturaFile = new StreamWriter(globals._FilenameBase + ".USE", false, Encoding.Default);

            facturaFile.WriteLine(oData);
            facturaFile.Close();
            facturaFile.Dispose();
        }

        public static void escribirFile(string oData, string extension)
        {
            StreamWriter facturaFile = new StreamWriter(globals._FilenameBase + extension, false, Encoding.Default);

            facturaFile.WriteLine(oData);
            facturaFile.Close();
            facturaFile.Dispose();
        }

        public static string leerFile(string oFile)
        {
            string contenido = File.ReadAllText(oFile);
            return contenido;
        }
    }
}
