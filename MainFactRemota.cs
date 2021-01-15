using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Data.SqlClient;

namespace FactRemota
{
    class MainFactRemota
    {
        static void Main(string[] args)
        {
            globals Glob = new globals();
            globals.gpLogFile = "Log_FE.log";
            globals.Debug = true;

            try
            {
                globals.gpMutex = Mutex.OpenExisting("SINGLEINSTANCE");
                if (globals.gpMutex != null)
                {
                    Console.WriteLine("Error : Only 1 instance of this application can run at a time");
                    globals.oLog.LogMsg("-100 " + "Error : Only 1 instance of this application can run at a time", "A", "I");
                    Environment.Exit(-100);
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                globals.gpMutex = new Mutex(true, "SINGLEINSTANCE");
            }

            DoIt oRun = new DoIt();
            oRun.verificarParametros(args);
        }
    }

    class DoIt
    {
        const string version = "2.30.14";

        //string _TipoDoc = "";
        //int _NumDoc = -1;
        //string _RUTEmisor = "14147842-7";
        //string _Local = "sucursal1";

        string _TipoDoc = "";
        int _NumDoc = -1;
        string _RUTEmisor = "";
        string _Local = "";

        public void verificarParametros(string[] args)
        {
            //-d F/B/NC => Tipo documento - requerido
            //-n ##### => numero documento - requerido
            //-R sssss => RUT del emisor - requerido
            //-S Setup
            //-h help
            //-Pr Reimprimir

            // Si hay errores termina.

            try
            {
                bool DoSetup = false;
                bool DoHelp = false;
                bool DoReadSetup = false;
                bool DoProcesarNoEnviados = false;
                bool DoReimprimir = false;
                bool DoObtenerFoleos = false;
                bool DoAnularTransaccion = false;
                bool NoImprimir = false;

                Utils.SetDebugModeAndTimeout();

                for (int i = 0; i <= args.Count() - 1; i++)
                {
                    switch (args[i].ToUpper())
                    {
                        case "-D":
                            if (i + 1 <= args.Count() - 1)
                                i++;
                            if ((args[i] == "F") || (args[i] == "B") || (args[i] == "NC") || (args[i] == "FR") || (args[i] == "GD") || (args[i] == "FE") || (args[i] == "BE")) 
                                _TipoDoc = args[i];
                            else
                                throw new Exception("Parametro invalido: -d " + args[i] + " - Valores permitidos  Factura F, Factura de reserva FR, Boleta B, Guia de despacho GD, Nota de Credito NC, Factura Exenta FE, Boleta Exenta BE ");
                            break;
                        case "-L":
                            if (i + 1 <= args.Count() - 1)
                                i++;
                            _Local = args[i];
                            break;
                        case "-N":
                            if (i + 1 <= args.Count() - 1)
                                i++;
                            if (!int.TryParse(args[i], out _NumDoc))
                                throw new Exception("Parametro invalido: -n " + args[i] + " - debe ser un numero entero sin puntos ni comas.");
                            break;
                        case "-R":
                            if (i + 1 <= args.Count() - 1)
                                i++;
                            if (!Utils.ValidaRUT(Utils.GetRut(args[i])))
                                throw new Exception("Parametro invalido: -R " + args[i] + " - Debe ingresar un RUT valido, formatos validos: 99.999.999-D o 99999999-D ");
                            _RUTEmisor = Utils.GetRut(args[i]);

                            // Lectura de parametros
                            string s = Utils.readParams(_RUTEmisor);
                            if (s != "")
                                throw new Exception("Parametro invalido: -R - Error en lectura de parametros: " + s);
                            break;
                        case "-S": DoSetup = true;
                            break;
                        case "-H": DoHelp = true;
                            break;
                        case "-RS_": DoReadSetup = true; // uso en conjunto con -R 99999999-D
                            break;
                        case "-REEN": DoProcesarNoEnviados = true;
                            break;
                        case "-PRINT": DoReimprimir = true;
                            break;
                        case "-ANULAR": DoAnularTransaccion = true;
                            break;
                        case "-OF": DoObtenerFoleos = true;
                            break;
                        case "-NOPRINT": NoImprimir = true;
                            break;
                        default:
                            break;
                    }
                }

                if (DoHelp)
                {
                    showHelp();
                    Environment.Exit(0);
                }
                else if (DoSetup)
                {
                    doSetup();
                }
                else if (DoReadSetup)
                {
                    doReadSetup();
                }
                else if (DoReimprimir)
                {
                    ValidarParametros("PRINT");
                    ReimprimirDocumento(version);
                }
                else if (DoAnularTransaccion)
                {
                    ValidarParametros("ANULAR");
                    AnularTransaccion(version);
                }
                else if (DoProcesarNoEnviados)
                {
                    ValidarParametros("REEN");
                    procesarNoEnviados(version);
                }
                else if (DoObtenerFoleos)
                {
                    ValidarParametros("OF");
                    obtenerFoleos(version);
                }
                else
                {
                    ValidarParametros("");
                    ProcesarDocumento(version, NoImprimir);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                globals.oLog.LogMsg("-200 " + e.Message, "A", "I");
                Environment.Exit(-200);
            }
        }

        void showHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("Versión : " + version);
            Console.WriteLine("");
            Console.WriteLine(" Tipos de documento: Factura F, Factura de reserva FR, Guia de despacho GD, para Boleta B , para Nota de Credito NC, Factura Exenta FE, Boleta Exenta BE ");
            Console.WriteLine("");
            Console.WriteLine(" Uso normal :");
            Console.WriteLine("   FactRemota.exe -d <Tipo documento> -n <numero documento> -R <Rut del emisor> -L <Local> ");
            Console.WriteLine("");
            Console.WriteLine(" reimprimir documento :");
            Console.WriteLine("   FactRemota.exe -d <Tipo documento> -n <numero documento> -R <Rut del emisor> -L <Local> -PRINT ");
            Console.WriteLine("");
            Console.WriteLine(" anular documento :");
            Console.WriteLine("   FactRemota.exe -d <Tipo documento> -n <numero documento> -R <Rut del emisor> -L <Local> -ANULAR ");
            Console.WriteLine("");
            Console.WriteLine(" Inicializacion :");
            Console.WriteLine("   FactRemota.exe -S ");
            Console.WriteLine("");
            Console.WriteLine(" Listar parametros :");
            Console.WriteLine("   FactRemota.exe -RS_ -R <Rut del emisor>");
            Console.WriteLine("");
            Console.WriteLine(" Obtener folios :");
            Console.WriteLine("   FactRemota.exe -OF -d <Tipo documento> -R <Rut del emisor> -L <Local> ");
            Console.WriteLine("");
            Console.WriteLine(" Procesar documentos no enviados :");
            Console.WriteLine("   FactRemota.exe -REEN -d <Tipo documento> -R <Rut del emisor> -L <Local> ");
            Console.WriteLine("");
            Console.WriteLine(" Opcion -NOPRINT :");
            Console.WriteLine("   No se imprime el documento, este comando se aplica a la generción de documentos. ");
            Console.WriteLine("");
            Console.WriteLine(" Ayuda :");
            Console.WriteLine("   FactRemota.exe -h ");
            Console.WriteLine("");
            Console.WriteLine("Versión : " + version);
            Console.ReadLine();
        }

        bool doSetup()
        {
            string oStr;
            string path;
            string oUsr;
            string oPw;
            string URLEnvio;
            string URLEnvioBol;
            string URLRngDTE;
            string URLRngBol;
            string URLCAFSuc;
            string sErr = "";

            Console.WriteLine("Proceso de inicialización de la aplicación.");
            //RUT
            do
            {
                Console.Write("RUT del emisor < 99999999-D > : ");
                oStr = Console.ReadLine();
            }
            while (!Utils.ValidaRUT(oStr));
            globals.gpRUTEmisor = oStr;

            Utils.readParams(globals.gpRUTEmisor);

            //Local
            oStr = "N";
            Console.Write("Nombre de caja  : " + globals.gpLocal + "  ¿ Modificar S/N ?");
            oStr = Console.ReadLine();
            if (oStr.Trim().ToUpper() == "S")
                oStr = Console.ReadLine();
            else
                oStr = globals.gpLocal;
            globals.gpLocal = oStr;


            // Docs
            Console.WriteLine(" ");
            path = Directory.GetCurrentDirectory();
            Console.WriteLine("Directorio de documentos : Docs_FE");
            globals.gpDirDocs = path + "\\Docs_FE";

            //Folios
            path = Directory.GetCurrentDirectory();
            Console.WriteLine("Directorio de folios : Foleos_FE");
            globals.gpDirFolios = path + "\\Foleos_FE";

            //Respaldos
            path = Directory.GetCurrentDirectory();
            Console.WriteLine("Directorio de respaldos : Respaldo");
            globals.gpDirRespaldo = path + "\\Respaldo";

            //Anulaciones
            path = Directory.GetCurrentDirectory();
            Console.WriteLine("Directorio de anulaciones : Anulacion");
            globals.gpDirAnulacion = path + "\\Anulacion";

            //Logs
            Console.WriteLine("Archivos de log : Log_FE.log :");
            globals.gpLogFile = "Log_FE.log";
            Console.WriteLine(" ");


            //Servidor SQLServer
            do
            {
                oStr = "N";
                Console.Write("Ingresar usuario portal : " + "  ¿ Modificar S/N ?");
                oStr = Console.ReadLine();
                if (oStr.Trim().ToUpper() == "S")
                    oUsr = Console.ReadLine();
                else
                    oUsr = globals.gpUser;

                oStr = "N";
                Console.Write("Ingresar password usuario remoto : " + "  ¿ Modificar S/N ?");
                oStr = Console.ReadLine();
                if (oStr.Trim().ToUpper() == "S")
                    oPw = Console.ReadLine();
                else
                    oPw = globals.gpPW;

                oStr = "N";
                Console.Write("URL Envio de documentos (DTE/Generar): " + globals.gpURLEnvioDoc + "  ¿ Modificar S/N ?");
                oStr = Console.ReadLine();
                if (oStr.Trim().ToUpper() == "S")
                    URLEnvio = Console.ReadLine();
                else
                    URLEnvio = globals.gpURLEnvioDoc;

                oStr = "N";
                Console.Write("URL Envio de Boletas (Boleta/Generar): " + globals.gpURLEnvioBol + "  ¿ Modificar S/N ?");
                oStr = Console.ReadLine();
                if (oStr.Trim().ToUpper() == "S")
                    URLEnvioBol = Console.ReadLine();
                else
                    URLEnvioBol = globals.gpURLEnvioBol;

                oStr = "N";
                Console.Write("URL CAF Sucursal: " + globals.gpURLCAFSucursal + "  ¿ Modificar S/N ?");
                oStr = Console.ReadLine();
                if (oStr.Trim().ToUpper() == "S")
                    URLCAFSuc = Console.ReadLine();
                else
                    URLCAFSuc = globals.gpURLCAFSucursal;

                oStr = "N";
                Console.Write("URL CAF Rango Boletas: " + globals.gpURLCAFRangoBoleta + "  ¿ Modificar S/N ?");
                oStr = Console.ReadLine();
                if (oStr.Trim().ToUpper() == "S")
                    URLRngBol = Console.ReadLine();
                else
                    URLRngBol = globals.gpURLCAFRangoBoleta;

                oStr = "N";
                Console.Write("URL CAF Rango DTE: " + globals.gpURLCAFRangoDTE + "  ¿ Modificar S/N ?");
                oStr = Console.ReadLine();
                if (oStr.Trim().ToUpper() == "S")
                    URLRngDTE = Console.ReadLine();
                else
                    URLRngDTE = globals.gpURLCAFRangoDTE;

                globals.gpUser = oUsr;
                globals.gpPW = oPw;
                sErr = Utils.ValidarURLs(URLEnvio, URLEnvioBol, URLCAFSuc, URLRngBol, URLRngDTE);
                if ("" != sErr)
                {
                    Console.Write("\r\n Error al validar URLs : " + sErr);
                    oStr = Console.ReadLine();
                }
            }
            while ("" != sErr);

            globals.gpURLEnvioDoc = URLEnvio;
            globals.gpURLEnvioBol = URLEnvioBol;
            globals.gpURLCAFSucursal = URLCAFSuc;
            globals.gpURLCAFRangoBoleta = URLRngBol;
            globals.gpURLCAFRangoDTE = URLRngDTE;
            
            sErr = Utils.SaveParams(globals.gpRUTEmisor);
            if ( "" != sErr )
            {
                Console.Write("\r\n Error al guardar parametros : " + sErr);
                oStr = Console.ReadLine();
            }

            return false;
        }

        bool doReadSetup()
        {
            Console.WriteLine("Emisor        : " + globals.gpRUTEmisor);
            Console.WriteLine("Local         : " + globals.gpLocal);
            Console.WriteLine("Usuario       : " + globals.gpUser);
            Console.WriteLine("Archivo Log   : " + globals.gpLogFile);
            Console.WriteLine(globals.gpDirDocs);
            Console.WriteLine(globals.gpDirFolios);
            Console.WriteLine(globals.gpDirRespaldo);
            Console.WriteLine(globals.gpDirAnulacion);
            Console.WriteLine(globals.gpURLEnvioDoc);
            Console.WriteLine(globals.gpURLEnvioBol);
            Console.WriteLine(globals.gpURLCAFSucursal);
            Console.WriteLine(globals.gpURLCAFRangoDTE);
            Console.WriteLine(globals.gpURLCAFRangoBoleta);

            Console.ReadLine();
            return false;
        }

        public void ValidarParametros(string TipoValidacion)
        {
            try
            {
                if (_RUTEmisor != globals.gpRUTEmisor)
                    throw new Exception("E300 - RUT enviado no correspode al del emisor - " + _RUTEmisor);
                if (_Local != globals.gpLocal)
                    throw new Exception("E301 - Local enviado no correspode al asignado a caja - " + _Local);
                if ((_TipoDoc != "F") && (_TipoDoc != "B") && (_TipoDoc != "NC") && (_TipoDoc != "GD") && (_TipoDoc != "FR") && (_TipoDoc != "BE") && (_TipoDoc != "FE"))
                    throw new Exception("E302 - Tipo de documento : " + _TipoDoc + " invalido");
                if ((_NumDoc < 0) && (TipoValidacion != "OF") && (TipoValidacion != "REEN"))
                    throw new Exception("E303 - Numero de documento : " + _NumDoc.ToString() + " invalido");
                if (!Directory.Exists(globals.gpDirDocs))
                    Directory.CreateDirectory(globals.gpDirDocs);
                if (!Directory.Exists(globals.gpDirFolios))
                    Directory.CreateDirectory(globals.gpDirFolios);
                if (!Directory.Exists(globals.gpDirRespaldo))
                    Directory.CreateDirectory(globals.gpDirRespaldo);
                if (!Directory.Exists(globals.gpDirAnulacion))
                    Directory.CreateDirectory(globals.gpDirAnulacion);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Termino ejecución V " + version);
                globals.oLog.LogMsg("  ", "A", "I");
                globals.oLog.LogMsg("-300 " + e.Message, "A", "I");
                globals.oLog.LogMsg("Termino ejecución V " + version, "A", "I");
                Environment.Exit(-300);
            }
        }

        public void ProcesarDocumento(string v, bool noPrint)
        {
            int nErr = 400;
            try
            {
                globals.oLog.LogMsg("Versión " + v + " Comienza proceso", "A", "I");
                globals.gpInternalDocNum = _NumDoc;
                new ProcesarDTE(_TipoDoc, _Local, _NumDoc, _RUTEmisor, ref nErr, noPrint);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                globals.oLog.LogMsg("  ", "A", "I");
                globals.oLog.LogMsg(e.Message, "A", "I");
                Environment.Exit(nErr); //No se pudo imprimir
            }
        }

        public void obtenerFoleos(string v)
        {
            try
            {
                List<int> folios;

                globals.oLog.LogMsg("Versión " + v + " Obtención de folios - inicio " + _TipoDoc, "A", "I");
                folios = Utils.GetFoliosAsignados(_TipoDoc, _Local);

                if (folios.Count > 0)
                {
                    Utils.BorrrarFoleosNoUsados(_TipoDoc, _Local);
                    Utils.EscribirFoleos(folios, _TipoDoc, _Local);
                }
                else
                {
                    globals.oLog.LogMsg("No se recuperaron folios, se mantienen los actuales. ", "A", "I");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                globals.oLog.LogMsg("  ", "A", "I");
                globals.oLog.LogMsg("-500 " + e.Message, "A", "I");
                Environment.Exit(-500);
            }
        }

        public void ReimprimirDocumento(string v)
        {
            try
            {
                globals.oLog.LogMsg("Versión " + v + " Comienza reimpresion", "A", "I");
                globals.gpInternalDocNum = _NumDoc;
                new ProcesarDTE(_TipoDoc, _Local, _NumDoc, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                globals.oLog.LogMsg("  ", "A", "I");
                globals.oLog.LogMsg("-700 " + e.Message + "  -  " + e.StackTrace, "A", "I");
                Environment.Exit(-700);
            }
        }

        public void AnularTransaccion(string v)
        {
            try
            {
                globals.oLog.LogMsg("Versión " + v + " Comienza anulación", "A", "I");
                globals.gpInternalDocNum = _NumDoc;
                new ProcesarDTE(_TipoDoc, _Local, _NumDoc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                globals.oLog.LogMsg("  ", "A", "I");
                globals.oLog.LogMsg("-900 " + e.Message + "  -  " + e.StackTrace, "A", "I");
                Environment.Exit(-900);
            }
        }

        void procesarNoEnviados(string v)
        {
            try
            {
                globals.oLog.LogMsg("Versión " + v + " Comienza Reenvio de documentos no procesados", "A", "I");
                ProcesarFE_NoEnviados aux = new ProcesarFE_NoEnviados(_Local, _RUTEmisor);
                if (aux.ProcesarDTEs_NoEnviados())
                    globals.oLog.LogMsg("No hay documentos para enviar", "A", "I");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                globals.oLog.LogMsg("  ", "A", "I");
                globals.oLog.LogMsg("-800 " + e.Message + "  -  " + e.StackTrace, "A", "I");
                Environment.Exit(-800);
            }
        }
    }
}
