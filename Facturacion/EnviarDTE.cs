using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace FactRemota
{
    class EnviarDTE
    {
        public bool EnviarDocumento(Int32 oTipoDTE, string docEnviarjson)
        {
            string respuesta;

            if (oTipoDTE == 39 || oTipoDTE == 41)
                respuesta = Utils.sendRequest(globals.gpURLEnvioBol, docEnviarjson, "application/json");
            else
                respuesta = Utils.sendRequest(globals.gpURLEnvioDoc, docEnviarjson, "application/json");

            ErrorMsgSendDTE errMsg = new ErrorMsgSendDTE();
            errMsg = JsonConvert.DeserializeObject<ErrorMsgSendDTE>(respuesta);

            if (errMsg.Status == "ERROR")
                throw new Exception(errMsg.Codigo + " - " + errMsg.Descripcion);

            return true;
        }

        public string PrepararDTEJson(DTEDoc oDteDoc, XmlDocument XmlTimbre)
        {
            globals.oLog.LogMsg(DateTime.Now.ToString() + " - Preparacion Documento a enviar", "A", "I");
            DTEDocBaseSII oDoc = new DTEDocBaseSII();

            oDoc.Documento = new DTEDocSII();

            oDoc.Documento.Encabezado = new oEncabezadoSII();
            oDoc.Documento.Encabezado.IdDoc = new oIdDoc();
            oDoc.Documento.Encabezado.IdDoc.TipoDTE   = oDteDoc.Encabezado.IdDoc.TipoDTE;
            oDoc.Documento.Encabezado.IdDoc.Folio     = oDteDoc.Encabezado.IdDoc.Folio;
            oDoc.Documento.Encabezado.IdDoc.FchEmis   = oDteDoc.Encabezado.IdDoc.FchEmis;
            oDoc.Documento.Encabezado.IdDoc.MedioPago = oDteDoc.Encabezado.IdDoc.MedioPago;
            oDoc.Documento.Encabezado.IdDoc.FchVenc   = oDteDoc.Encabezado.IdDoc.FchVenc;

            oDoc.Documento.Encabezado.Emisor = new oEmisor();
            oDoc.Documento.Encabezado.Emisor.RUTEmisor    = oDteDoc.Encabezado.Emisor.RUTEmisor; 
            oDoc.Documento.Encabezado.Emisor.RznSoc       = oDteDoc.Encabezado.Emisor.RznSoc; 
            oDoc.Documento.Encabezado.Emisor.GiroEmis     = oDteDoc.Encabezado.Emisor.GiroEmis; 
            oDoc.Documento.Encabezado.Emisor.CdgSIISucur  = oDteDoc.Encabezado.Emisor.CdgSIISucur; 
            oDoc.Documento.Encabezado.Emisor.DirOrigen    = oDteDoc.Encabezado.Emisor.DirOrigen; 
            oDoc.Documento.Encabezado.Emisor.CmnaOrigen   = oDteDoc.Encabezado.Emisor.CmnaOrigen;
            oDoc.Documento.Encabezado.Emisor.CiudadOrigen = oDteDoc.Encabezado.Emisor.CiudadOrigen;
            oDoc.Documento.Encabezado.Emisor.CdgVendedor  = oDteDoc.Encabezado.Emisor.CdgVendedor; 
            
            oDoc.Documento.Encabezado.Receptor = new oReceptor();
            oDoc.Documento.Encabezado.Receptor.RUTRecep    = oDteDoc.Encabezado.Receptor.RUTRecep; 
            oDoc.Documento.Encabezado.Receptor.RznSocRecep = oDteDoc.Encabezado.Receptor.RznSocRecep;
            oDoc.Documento.Encabezado.Receptor.GiroRecep   = oDteDoc.Encabezado.Receptor.GiroRecep;
            oDoc.Documento.Encabezado.Receptor.CorreoRecep = oDteDoc.Encabezado.Receptor.CorreoRecep;
            oDoc.Documento.Encabezado.Receptor.DirRecep    = oDteDoc.Encabezado.Receptor.DirRecep; 
            oDoc.Documento.Encabezado.Receptor.CmnaRecep   = oDteDoc.Encabezado.Receptor.CmnaRecep;
            oDoc.Documento.Encabezado.Receptor.CiudadRecep = oDteDoc.Encabezado.Receptor.CiudadRecep;

            oDoc.Documento.Encabezado.Totales = new oTotales();
            oDoc.Documento.Encabezado.Totales.MntNeto   = oDteDoc.Encabezado.Totales.MntNeto;
            oDoc.Documento.Encabezado.Totales.MntExe    = oDteDoc.Encabezado.Totales.MntExe;
            oDoc.Documento.Encabezado.Totales.TasaIVA   = oDteDoc.Encabezado.Totales.TasaIVA;
            oDoc.Documento.Encabezado.Totales.IVA       = oDteDoc.Encabezado.Totales.IVA; 
            oDoc.Documento.Encabezado.Totales.MntTotal  = oDteDoc.Encabezado.Totales.MntTotal;
            oDoc.Documento.Encabezado.Totales.ImptoReten = new List<oImpuestosRetenidos>();
            oImpuestosRetenidos oImpRet;
            for (int i = 0; i < oDteDoc.Encabezado.Totales.ImptoReten.Count; i++)
            {
                oImpRet = new oImpuestosRetenidos();
                oImpRet.TipoImp  = oDteDoc.Encabezado.Totales.ImptoReten[i].TipoImp;
                oImpRet.TasaImp  = oDteDoc.Encabezado.Totales.ImptoReten[i].TasaImp;
                oImpRet.MontoImp = oDteDoc.Encabezado.Totales.ImptoReten[i].MontoImp;
                oDoc.Documento.Encabezado.Totales.ImptoReten.Add(oImpRet);
            }

            oDoc.Documento.Detalle = new List<oDetalleSII>();
            oDetalleSII Linea;
            oCdgItem Cdg;
            for (int i = 0; i < oDteDoc.Detalle.Count; i++ )
            {
                Linea = new oDetalleSII();
                Linea.NroLinDet = oDteDoc.Detalle[i].NroLinDet;
                Linea.CdgItem = new List<oCdgItem>();
                for (int j = 0; j < oDteDoc.Detalle[i].CdgItem.Count; j++)
                {
                    Cdg = new oCdgItem();
                    Cdg.TpoCodigo = oDteDoc.Detalle[i].CdgItem[j].TpoCodigo;
                    Cdg.VlrCodigo = oDteDoc.Detalle[i].CdgItem[j].VlrCodigo;
                    Linea.CdgItem.Add(Cdg);
                }
                Linea.IndExe = oDteDoc.Detalle[i].IndExe;
                Linea.NmbItem = oDteDoc.Detalle[i].NmbItem;
                Linea.DscItem = oDteDoc.Detalle[i].DscItem;
                Linea.QtyItem = oDteDoc.Detalle[i].QtyItem;
                Linea.UnmdItem = oDteDoc.Detalle[i].UnmdItem;
                Linea.FchElabor = oDteDoc.Detalle[i].FchElabor;
                Linea.FchVencim = oDteDoc.Detalle[i].FchVencim;
                Linea.PrcItem = oDteDoc.Detalle[i].PrcItem;
                Linea.DescuentoPct = oDteDoc.Detalle[i].DescuentoPct;
                Linea.DescuentoMonto = oDteDoc.Detalle[i].DescuentoMonto;
                Linea.RecargoPct = oDteDoc.Detalle[i].RecargoPct;
                Linea.RecargoMonto = oDteDoc.Detalle[i].RecargoMonto;
                Linea.CodImpAdic = oDteDoc.Detalle[i].CodImpAdic;
                Linea.MontoItem = oDteDoc.Detalle[i].MontoItem;
                oDoc.Documento.Detalle.Add(Linea);
            }

            oDoc.Documento.DscRcgGlobal = new List<oDscRcgGlobal>();
            oDscRcgGlobal Dscto;
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++ )
            {
                Dscto = new oDscRcgGlobal();
                Dscto.NroLinDR = oDteDoc.DscRcgGlobal[i].NroLinDR;
                Dscto.TpoMov   = oDteDoc.DscRcgGlobal[i].TpoMov;
                Dscto.GlosaDR  = oDteDoc.DscRcgGlobal[i].GlosaDR;
                Dscto.TpoValor = oDteDoc.DscRcgGlobal[i].TpoValor;
                Dscto.ValorDR  = oDteDoc.DscRcgGlobal[i].ValorDR;
                oDoc.Documento.DscRcgGlobal.Add(Dscto);
            }
                
            oDoc.Documento.Referencia = new List<oReferencia>(); 
            oReferencia oRef;
            for (int i = 0; i < oDteDoc.Referencia.Count; i++ )
            {
                oRef = new oReferencia();
                oRef.NroLinRef = oDteDoc.Referencia[i].NroLinRef;
                oRef.TpoDocRef = oDteDoc.Referencia[i].TpoDocRef;
                oRef.IndGlobal = oDteDoc.Referencia[i].IndGlobal;
                oRef.FolioRef  = oDteDoc.Referencia[i].FolioRef;
                oRef.FchRef    = oDteDoc.Referencia[i].FchRef;
                oRef.CodRef    = oDteDoc.Referencia[i].CodRef;
                oRef.RazonRef = oDteDoc.Referencia[i].RazonRef;
                oDoc.Documento.Referencia.Add(oRef);
            }

            oDoc.Documento.Extra = new oExtraDTE();
            oDoc.Documento.Extra.FolioInterno = globals.gpInternalDocNum;
            oDoc.Documento.Extra.Extra1 = globals.gpLocal;
            oDoc.Documento.Extra.Extra2 = "";
            oDoc.Documento.Extra.Extra3 = "";
            oDoc.Documento.Extra.Extra4 = "";
            oDoc.Documento.Extra.Extra5 = "";
            oDoc.Documento.Extra.Extra6 = "";
            oDoc.Documento.Extra.Extra7 = "";
            oDoc.Documento.Extra.Extra8 = "";
            oDoc.Documento.Extra.Extra9 = "";
            oDoc.Documento.Extra.Extra10 = "";

            byte[] data = Encoding.UTF8.GetBytes(XmlTimbre.OuterXml);
            oDoc.Documento.TED = System.Convert.ToBase64String(data);

            globals.oLog.LogMsg(DateTime.Now.ToString() + " - Json finalizado", "A", "I");
            return JsonConvert.SerializeObject(oDoc);
        }

        public string PrepararBoletaJson(DTEDoc oDteDoc, XmlDocument XmlTimbre)
        {
            globals.oLog.LogMsg(DateTime.Now.ToString() + " - Preparacion Documento a enviar", "A", "I");
            DTEBoletaDocBaseSII oDoc = new DTEBoletaDocBaseSII();

            oDoc.Documento = new DTEBoletaDocSII();

            oDoc.Documento.Encabezado = new oEncabezadoBoletaSII();
            oDoc.Documento.Encabezado.IdDoc = new oIdDocBoleta();
            oDoc.Documento.Encabezado.IdDoc.TipoDTE = oDteDoc.Encabezado.IdDoc.TipoDTE.ToString();
            oDoc.Documento.Encabezado.IdDoc.Folio = oDteDoc.Encabezado.IdDoc.Folio;
            oDoc.Documento.Encabezado.IdDoc.FchEmis = oDteDoc.Encabezado.IdDoc.FchEmis;
            oDoc.Documento.Encabezado.IdDoc.FchVenc = oDteDoc.Encabezado.IdDoc.FchVenc;

            oDoc.Documento.Encabezado.Emisor = new oEmisorBoleta();
            oDoc.Documento.Encabezado.Emisor.RUTEmisor = oDteDoc.Encabezado.Emisor.RUTEmisor;
            oDoc.Documento.Encabezado.Emisor.GiroEmisor = oDteDoc.Encabezado.Emisor.GiroEmis;
            oDoc.Documento.Encabezado.Emisor.CdgSIISucur = oDteDoc.Encabezado.Emisor.CdgSIISucur;
            oDoc.Documento.Encabezado.Emisor.DirOrigen = oDteDoc.Encabezado.Emisor.DirOrigen;
            oDoc.Documento.Encabezado.Emisor.CmnaOrigen = oDteDoc.Encabezado.Emisor.CmnaOrigen;
            oDoc.Documento.Encabezado.Emisor.CiudadOrigen = oDteDoc.Encabezado.Emisor.CiudadOrigen;

            oDoc.Documento.Encabezado.Receptor = new oReceptorBoleta();
            oDoc.Documento.Encabezado.Receptor.RUTRecep = oDteDoc.Encabezado.Receptor.RUTRecep;
            oDoc.Documento.Encabezado.Receptor.RznSocRecep = oDteDoc.Encabezado.Receptor.RznSocRecep;
            oDoc.Documento.Encabezado.Receptor.DirRecep = oDteDoc.Encabezado.Receptor.DirRecep;
            oDoc.Documento.Encabezado.Receptor.CmnaRecep = oDteDoc.Encabezado.Receptor.CmnaRecep;
            oDoc.Documento.Encabezado.Receptor.CiudadRecep = oDteDoc.Encabezado.Receptor.CiudadRecep;

            oDoc.Documento.Encabezado.Totales = new oTotalesBoleta();
            oDoc.Documento.Encabezado.Totales.MntNeto = oDteDoc.Encabezado.Totales.MntNeto;
            oDoc.Documento.Encabezado.Totales.MntExe = oDteDoc.Encabezado.Totales.MntExe;
            oDoc.Documento.Encabezado.Totales.IVA = oDteDoc.Encabezado.Totales.IVA;
            oDoc.Documento.Encabezado.Totales.MntTotal = oDteDoc.Encabezado.Totales.MntTotal;

            oDoc.Documento.Detalle = new List<oDetalleBoletaSII>();
            oDetalleBoletaSII Linea;
            oCdgItem Cdg;
            for (int i = 0; i < oDteDoc.Detalle.Count; i++)
            {
                Linea = new oDetalleBoletaSII();
                Linea.NroLinDet = oDteDoc.Detalle[i].NroLinDet;
                Linea.CdgItem = new List<oCdgItem>();
                for (int j = 0; j < oDteDoc.Detalle[i].CdgItem.Count; j++)
                {
                    Cdg = new oCdgItem();
                    Cdg.TpoCodigo = oDteDoc.Detalle[i].CdgItem[j].TpoCodigo;
                    Cdg.VlrCodigo = oDteDoc.Detalle[i].CdgItem[j].VlrCodigo;
                    Linea.CdgItem.Add(Cdg);
                }
                Linea.IndExe = oDteDoc.Detalle[i].IndExe;
                Linea.NmbItem = oDteDoc.Detalle[i].NmbItem;
                Linea.DscItem = oDteDoc.Detalle[i].DscItem;
                Linea.QtyItem = oDteDoc.Detalle[i].QtyItem;
                Linea.UnmdItem = oDteDoc.Detalle[i].UnmdItem;
                Linea.PrcItem = oDteDoc.Detalle[i].PrcItem;
                Linea.DescuentoPct = oDteDoc.Detalle[i].DescuentoPct;
                Linea.DescuentoMonto = oDteDoc.Detalle[i].DescuentoMonto;
                Linea.RecargoPct = oDteDoc.Detalle[i].RecargoPct;
                Linea.RecargoMonto = oDteDoc.Detalle[i].RecargoMonto;
                Linea.MontoItem = oDteDoc.Detalle[i].MontoItem;
                oDoc.Documento.Detalle.Add(Linea);
            }

            oDoc.Documento.DscRcgGlobal = new List<oDscRcgGlobal>();
            oDscRcgGlobal Dscto;
            for (int i = 0; i < oDteDoc.DscRcgGlobal.Count; i++)
            {
                Dscto = new oDscRcgGlobal();
                Dscto.NroLinDR = oDteDoc.DscRcgGlobal[i].NroLinDR;
                Dscto.TpoMov = oDteDoc.DscRcgGlobal[i].TpoMov;
                Dscto.GlosaDR = oDteDoc.DscRcgGlobal[i].GlosaDR;
                Dscto.TpoValor = oDteDoc.DscRcgGlobal[i].TpoValor;
                Dscto.ValorDR = oDteDoc.DscRcgGlobal[i].ValorDR;
                oDoc.Documento.DscRcgGlobal.Add(Dscto);
            }

            oDoc.Documento.Referencia = new List<oReferenciaBoleta>();
            oReferenciaBoleta oRef;
            for (int i = 0; i < oDteDoc.Referencia.Count; i++)
            {
                oRef = new oReferenciaBoleta();
                oRef.NroLinRef = oDteDoc.Referencia[i].NroLinRef;
                oRef.CodRef = oDteDoc.Referencia[i].CodRef;
                oRef.RazonRef = oDteDoc.Referencia[i].RazonRef;
                if (oDteDoc.Encabezado.Emisor.CdgVendedor == null)
                    oRef.CodVndor = "";
                else
                    oRef.CodVndor = oDteDoc.Encabezado.Emisor.CdgVendedor.Substring(0, (oDteDoc.Encabezado.Emisor.CdgVendedor.Length > 8) ? 8 : oDteDoc.Encabezado.Emisor.CdgVendedor.Length).Trim();
                oDoc.Documento.Referencia.Add(oRef);
            }

            oDoc.Documento.Extra = new oExtraBoleta();
            oDoc.Documento.Extra.Email = oDteDoc.Encabezado.Receptor.CorreoRecep;
            oDoc.Documento.Extra.FolioInterno = globals.gpInternalDocNum;
            oDoc.Documento.Extra.Extra1 = globals.gpLocal;
            oDoc.Documento.Extra.Extra2 = "";
            oDoc.Documento.Extra.Extra3 = "";
            oDoc.Documento.Extra.Extra4 = "";
            oDoc.Documento.Extra.Extra5 = "";
            oDoc.Documento.Extra.Extra6 = "";
            oDoc.Documento.Extra.Extra7 = "";
            oDoc.Documento.Extra.Extra8 = "";
            oDoc.Documento.Extra.Extra9 = "";
            oDoc.Documento.Extra.Extra10 = "";

            byte[] data = Encoding.UTF8.GetBytes( XmlTimbre.OuterXml );
            oDoc.Documento.TED = System.Convert.ToBase64String( data);

            globals.oLog.LogMsg(DateTime.Now.ToString() + " - Json finalizado", "A", "I");
            return JsonConvert.SerializeObject(oDoc);
        }
    }
}