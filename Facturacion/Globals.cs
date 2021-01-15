using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Logs;

namespace FactRemota
{
    sealed class globals
    {
        public static Mutex gpMutex;
        public static string gpRUTEmisor;
        public static string gpLocal;
        public static string gpDirDocs;
        public static string gpDirFolios;
        public static string gpDirRespaldo;
        public static string gpDirAnulacion;
        public static string gpLogFile;
        public static string gpUser;
        public static string gpPW;
        public static string gpURLEnvioDoc;
        public static string gpURLEnvioBol;
        public static string gpURLCAFSucursal;
        public static string gpURLCAFRangoDTE;
        public static string gpURLCAFRangoBoleta;
        public static Logs.Logger oLog;
        public static XmlDocument _xmlCAF;
        public static string _FilenameBase;
        public static int gpInternalDocNum = -1;
        public static bool Debug = false;
        public static Int32 Timeout = 45000;
        public static Random retry = new Random();

        public globals()
        {
            oLog = new Logs.Logger();
            _xmlCAF = new XmlDocument();
        }

        public static void init_DTEDoc(ref DTEDoc dte)
        {
            dte.Encabezado.IdDoc.TipoDTE = 0;
            dte.Encabezado.IdDoc.Folio = 0;
            dte.Encabezado.IdDoc.FchEmis = "";
            dte.Encabezado.IdDoc.MedioPago = "";
            dte.Encabezado.IdDoc.FchVenc = "";

            dte.Encabezado.Emisor.RUTEmisor = "";
            dte.Encabezado.Emisor.RznSoc = "";
            dte.Encabezado.Emisor.GiroEmis = "";
            dte.Encabezado.Emisor.CdgSIISucur = 0;
            dte.Encabezado.Emisor.DirOrigen = "";
            dte.Encabezado.Emisor.CmnaOrigen = "";
            dte.Encabezado.Emisor.CiudadOrigen = "";
            dte.Encabezado.Emisor.CdgVendedor = "";

            dte.Encabezado.Receptor.RUTRecep = "";
            dte.Encabezado.Receptor.RznSocRecep = "";
            dte.Encabezado.Receptor.GiroRecep = "";
            dte.Encabezado.Receptor.CorreoRecep = "";
            dte.Encabezado.Receptor.DirRecep = "";
            dte.Encabezado.Receptor.CmnaRecep = "";
            dte.Encabezado.Receptor.CiudadRecep = "";

            dte.Encabezado.Totales.MntNeto = 0.0M;
            dte.Encabezado.Totales.MntExe = 0.0M;
            dte.Encabezado.Totales.TasaIVA = 0.0M;
            dte.Encabezado.Totales.IVA = 0.0M;
            dte.Encabezado.Totales.ImptoReten.Clear();
            dte.Encabezado.Totales.MntTotal = 0.0M;

            dte.Encabezado.UDFs.Clear();

            dte.Detalle.Clear();
            dte.DscRcgGlobal.Clear();
            dte.Referencia.Clear();
        }

        public static void init_ImptoReten(ref oImpuestosRetenidos oImp)
        {
            oImp.TipoImp = "";
            oImp.TasaImp = 0;
            oImp.MontoImp = 0;
        }

        public static void init_UDFs(ref oUDFs oUDF)
        {
            oUDF.Codigo = "";
            oUDF.Descripcion = "";
            oUDF.Valor = 0;
        }

        public static void init_Detalle(ref oDetalle oDet)
        {
            oDet.NroLinDet = 0;
            oDet.CdgItem.Clear();
            oDet.IndExe = 0;
            oDet.NmbItem = "";
            oDet.DscItem = "";
            oDet.QtyItem = 0;
            oDet.UnmdItem = "";
            oDet.FchElabor = "";
            oDet.FchVencim = "";
            oDet.PrcItem = 0;
            oDet.DescuentoPct = 0;
            oDet.DescuentoMonto = 0;
            oDet.RecargoPct = 0;
            oDet.RecargoMonto = 0;
            oDet.CodImpAdic = "";
            oDet.MontoItem = 0;
            oDet.UDFs.Clear();
        }

        public static void init_DscRcgGlobal(ref oDscRcgGlobal oDscRcgGlobal)
        {
            oDscRcgGlobal.NroLinDR = 1;
            oDscRcgGlobal.TpoMov = "";
            oDscRcgGlobal.GlosaDR = "";
            oDscRcgGlobal.TpoValor = "$";
            oDscRcgGlobal.ValorDR = 0.0M;
        }

        public static void init_Referencia(ref oReferencia oRef)
        {
            oRef.NroLinRef = 0;
            oRef.TpoDocRef = "";
            oRef.IndGlobal = 0;
            oRef.FolioRef = "";
            oRef.FchRef = "";
        }
    }

    public class DTEDoc
    {
        public oEncabezado Encabezado { get; set; }
        public List<oDetalle> Detalle { get; set; }
        public List<oDscRcgGlobal> DscRcgGlobal { get; set; }
        public List<oReferencia> Referencia { get; set; }
    }

    public class oEncabezado
    {
        public oIdDoc IdDoc { get; set; }           
        public oEmisor Emisor { get; set; }
        public oReceptor Receptor { get; set; }
        public oTotales Totales { get; set; }
        public List<oUDFs> UDFs { get; set; }
    }

    public class oIdDoc
    {
        public int    TipoDTE   { get; set; }         //  3
        public int    Folio     { get; set; }         //  10
        public string FchEmis   { get; set; }         //  10
        public string MedioPago { get; set; }       //  2
        public string FchVenc   { get; set; }         //  10
        public int    IndTraslado  { get; set; }   
    }

    public class oEmisor
    {
        public string RUTEmisor { get; set; }       //  10
        public string RznSoc { get; set; }          //  100
        public string GiroEmis { get; set; }        //  80
        public int    CdgSIISucur { get; set; }     //  9
        public string DirOrigen { get; set; }       //  60
        public string CmnaOrigen { get; set; }      //  20
        public string CiudadOrigen { get; set; }    //  20
        public string CdgVendedor { get; set; }     //  60    
    }

    public class oReceptor
    {
        public string RUTRecep { get; set; }        //  10
        public string RznSocRecep { get; set; }     //  100
        public string GiroRecep { get; set; }       //  40
        public string CorreoRecep { get; set; }     //  80
        public string DirRecep { get; set; }        //  70
        public string CmnaRecep { get; set; }       //  20
        public string CiudadRecep { get; set; }     //  20
    }

    public class oTotales
    {
        public decimal MntNeto { get; set; }         //  18
        public decimal MntExe { get; set; }          //  18
        public decimal TasaIVA { get; set; }         //  5 (3,2)
        public decimal IVA { get; set; }             //  18
        public List<oImpuestosRetenidos> ImptoReten { get; set; }
        public decimal MntTotal { get; set; }        //  18
    }

    public class oImpuestosRetenidos
    {
        public string  TipoImp { get; set; }         //   3
        public decimal TasaImp { get; set; }         //   5 (3,2)
        public decimal MontoImp { get; set; }        //   18
    }

    public class oUDFs
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Valor { get; set; }
    }

    public class oExtraDTE
    {
        public int FolioInterno { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Extra3 { get; set; }
        public string Extra4 { get; set; }
        public string Extra5 { get; set; }
        public string Extra6 { get; set; }
        public string Extra7 { get; set; }
        public string Extra8 { get; set; }
        public string Extra9 { get; set; }
        public string Extra10 { get; set; }
    }

    public class oDetalle
    {
        public int     NroLinDet { get; set; }       //  4
        public List<oCdgItem> CdgItem { get; set; }
        public int     IndExe { get; set; }          //  1
        public string  NmbItem { get; set; }         //  80
        public string  DscItem { get; set; }         //  1000
        public decimal QtyItem { get; set; }         //  18 (12,6)
        public string  UnmdItem { get; set; }        //  4
        public string  FchElabor { get; set; }         //  10
        public string  FchVencim { get; set; }         //  10
        public decimal PrcItem { get; set; }         //  18 (12,6)
        public decimal DescuentoPct { get; set; }    //  5 (3,2)
        public decimal DescuentoMonto { get; set; }  //  18
        public decimal RecargoPct { get; set; }      //  5 (3,2)
        public decimal RecargoMonto { get; set; }    //  18
        public string  CodImpAdic { get; set; }      //  6
        public decimal MontoItem { get; set; }       //  18
        public List<oUDFs> UDFs { get; set; }
    }

    public class oCdgItem
    {
        public string TpoCodigo { get; set; }       //  10
        public string VlrCodigo { get; set; }       //  35
    }

    public class oDscRcgGlobal
    {
        public int     NroLinDR { get; set; }        //   2
        public string  TpoMov { get; set; }         //   1
        public string  GlosaDR { get; set; }         //   45
        public string  TpoValor { get; set; }        //   1
        public decimal ValorDR { get; set; }         //   18
    }

    public class oReferencia
    {
        public int     NroLinRef { get; set; }       //   2
        public string  TpoDocRef { get; set; }       //   3
        public int     IndGlobal { get; set; }       //   1
        public string  FolioRef { get; set; }        //   18
        public string  FchRef { get; set; }          //   10
        public int     CodRef { get; set; }          //   1
        public string  RazonRef { get; set; }        //   90    
    }

    // ************** DTE SII ******************

    public class DTEDocBaseSII
    {
        public DTEDocSII Documento { get; set; }
    }

    public class DTEDocSII
    {
        public oEncabezadoSII Encabezado { get; set; }
        public List<oDetalleSII> Detalle { get; set; }
        public List<oDscRcgGlobal> DscRcgGlobal { get; set; }
        public List<oReferencia> Referencia { get; set; }
        public oExtraDTE Extra { get; set; }
        public string TED { get; set; }
    }


    public class oEncabezadoSII
    {
        public oIdDoc IdDoc { get; set; }
        public oEmisor Emisor { get; set; }
        public oReceptor Receptor { get; set; }
        public oTotales Totales { get; set; }
    }

    public class oDetalleSII
    {
        public int NroLinDet { get; set; }       //  4
        public List<oCdgItem> CdgItem { get; set; }
        public int IndExe { get; set; }          //  1
        public string NmbItem { get; set; }         //  80
        public string DscItem { get; set; }         //  1000
        public decimal QtyItem { get; set; }         //  18 (12,6)
        public string UnmdItem { get; set; }        //  4
        public string FchElabor { get; set; }         //  10
        public string FchVencim { get; set; }         //  10
        public decimal PrcItem { get; set; }         //  18 (12,6)
        public decimal DescuentoPct { get; set; }    //  5 (3,2)
        public decimal DescuentoMonto { get; set; }  //  18
        public decimal RecargoPct { get; set; }      //  5 (3,2)
        public decimal RecargoMonto { get; set; }    //  18
        public string CodImpAdic { get; set; }      //  6
        public decimal MontoItem { get; set; }       //  18
    }

    // ************** DTE - Boleta SII ******************

    public class DTEBoletaDocBaseSII
    {
        public DTEBoletaDocSII Documento { get; set; }
    }

    public class DTEBoletaDocSII
    {
        public oEncabezadoBoletaSII Encabezado { get; set; }
        public List<oDetalleBoletaSII> Detalle { get; set; }
        public List<oDscRcgGlobal> DscRcgGlobal { get; set; }
        public List<oReferenciaBoleta> Referencia { get; set; }
        public oExtraBoleta Extra { get; set; }
        public string TED { get; set; }
    }


    public class oEncabezadoBoletaSII
    {
        public oIdDocBoleta IdDoc { get; set; }
        public oEmisorBoleta Emisor { get; set; }
        public oReceptorBoleta Receptor { get; set; }
        public oTotalesBoleta Totales { get; set; }
    }

    public class oIdDocBoleta
    {
        public string TipoDTE { get; set; }         //  3
        public int Folio { get; set; }           //  10
        public string FchEmis { get; set; }      //  10
        public string FchVenc { get; set; }         //  10
    }

    public class oEmisorBoleta
    {
        public string RUTEmisor { get; set; }       //  10
        public string RznSocEmisor { get; set; }          //  100
        public string GiroEmisor { get; set; }        //  80
        public int CdgSIISucur { get; set; }     //  9
        public string DirOrigen { get; set; }       //  60
        public string CmnaOrigen { get; set; }      //  20
        public string CiudadOrigen { get; set; }    //  20
    }

    public class oReceptorBoleta
    {
        public string RUTRecep { get; set; }        //  10
        public string RznSocRecep { get; set; }     //  100
        public string DirRecep { get; set; }        //  70
        public string CmnaRecep { get; set; }       //  20
        public string CiudadRecep { get; set; }     //  20
    }

    public class oTotalesBoleta
    {
        public decimal MntNeto { get; set; }         //  18
        public decimal MntExe { get; set; }          //  18
        public decimal IVA { get; set; }             //  18
        public decimal MntTotal { get; set; }        //  18
    }

    public class oDetalleBoletaSII
    {
        public int NroLinDet { get; set; }       //  4
        public List<oCdgItem> CdgItem { get; set; }
        public int IndExe { get; set; }          //  1
        public string NmbItem { get; set; }         //  80
        public string DscItem { get; set; }         //  1000
        public decimal QtyItem { get; set; }         //  18 (12,6)
        public string UnmdItem { get; set; }        //  4
        public decimal PrcItem { get; set; }         //  18 (12,6)
        public decimal DescuentoPct { get; set; }    //  5 (3,2)
        public decimal DescuentoMonto { get; set; }  //  18
        public decimal RecargoPct { get; set; }      //  5 (3,2)
        public decimal RecargoMonto { get; set; }    //  18
        public decimal MontoItem { get; set; }       //  18
    }

    public class oReferenciaBoleta
    {
        public int NroLinRef { get; set; }       //   2
        public int CodRef { get; set; }          //   1
        public string RazonRef { get; set; }     // 90
        public string CodVndor { get; set; }    // 8
        public string CodCaja { get; set; }    // 8
    }

    public class oExtraBoleta
    {
        public string Email { get; set; }        
        public int FolioInterno { get; set; }
        public string Extra1 { get; set; }
        public string Extra2 { get; set; }
        public string Extra3 { get; set; }
        public string Extra4 { get; set; }
        public string Extra5 { get; set; }
        public string Extra6 { get; set; }
        public string Extra7 { get; set; }
        public string Extra8 { get; set; }
        public string Extra9 { get; set; }
        public string Extra10 { get; set; }
    }

    // ************** Parametros CAF ******************

    public class CAFParamsJson
    {
        public string P0 { get; set; }
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
        public string P4 { get; set; }
        public string Rut { get; set; }
    }

    public class CAFDesdeHasta
    {
        public int Desde { get; set; }
        public int Hasta { get; set; }
        public int Folio { get; set; }
        public int Aux   { get; set; }
        public string CafStr { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
    }

    public class nroFolio
    {
        public int folio { get; set; }
    }

    public class ErrorMsgSendDTE
    {
        public string Status { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public int Folio { get; set; }
        public int TrackId { get; set; }
    }

}

