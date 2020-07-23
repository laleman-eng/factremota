using System;

namespace FactRemota
{
    public class montoEscrito
    {

        static Array arUnidad, arQuinces, arDecena, arCentena;
        static decimal Numero;
        static string Monto;
        static long I, J, K, A, B, C, D, E, F, G, H, SinDecimales;

        public static string GetMontoEscrito(decimal n)
        {

            Numero = n;

            if (Numero > 999999999.99M)
                return "El valor es mayor que 999999999.99 - No puedo procesar";

            arUnidad = Array.CreateInstance(typeof(string), 10);
            arUnidad.SetValue("Un ", 1);
            arUnidad.SetValue("Dos ", 2);
            arUnidad.SetValue("Tres ", 3);
            arUnidad.SetValue("Cuatro ", 4);
            arUnidad.SetValue("Cinco ", 5);
            arUnidad.SetValue("Seis ", 6);
            arUnidad.SetValue("Siete ", 7);
            arUnidad.SetValue("Ocho ", 8);
            arUnidad.SetValue("Nueve ", 9);

            arQuinces = Array.CreateInstance(typeof(string), 6);
            arQuinces.SetValue("Once ", 1);
            arQuinces.SetValue("Doce ", 2);
            arQuinces.SetValue("Trece ", 3);
            arQuinces.SetValue("Catorce ", 4);
            arQuinces.SetValue("Quince ", 5);

            arDecena = Array.CreateInstance(typeof(string), 10);
            arDecena.SetValue("Dieci", 1);
            arDecena.SetValue("Veinti", 2);
            arDecena.SetValue("Treinta ", 3);
            arDecena.SetValue("Curenta ", 4);
            arDecena.SetValue("Cincuenta ", 5);
            arDecena.SetValue("Sesenta ", 6);
            arDecena.SetValue("Setenta ", 7);
            arDecena.SetValue("Ochenta ", 8);
            arDecena.SetValue("Noventa ", 9);

            arCentena = Array.CreateInstance(typeof(string), 10);
            arCentena.SetValue("Cien ", 1);
            arCentena.SetValue("Docientos ", 2);
            arCentena.SetValue("Trecientos ", 3);
            arCentena.SetValue("Cuatrocientos ", 4);
            arCentena.SetValue("Quinientos ", 5);
            arCentena.SetValue("Seiscientos ", 6);
            arCentena.SetValue("Setecientos ", 7);
            arCentena.SetValue("Ochocientos ", 8);
            arCentena.SetValue("Novecientos ", 9); // Separo el Nro. en enteros //

            // numero :  IJK.ABC.DEF,HG
            SinDecimales = Convert.ToInt64(Numero * 100);
            I = (SinDecimales / 10000000000);
            SinDecimales = SinDecimales - (I * 10000000000);
            J = (SinDecimales / 1000000000);
            SinDecimales = SinDecimales - (J * 1000000000);
            K = (SinDecimales / 100000000);
            SinDecimales = SinDecimales - (K * 100000000);
            A = (SinDecimales / 10000000);
            SinDecimales = SinDecimales - (A * 10000000);
            B = (SinDecimales / 1000000);
            SinDecimales = SinDecimales - (B * 1000000);
            C = (SinDecimales / 100000);
            SinDecimales = SinDecimales - (C * 100000);
            D = (SinDecimales / 10000);
            SinDecimales = SinDecimales - (D * 10000);
            E = (SinDecimales / 1000);
            SinDecimales = SinDecimales - (E * 1000);
            F = (SinDecimales / 100);
            SinDecimales = SinDecimales - (F * 100);
            G = (SinDecimales / 10);
            SinDecimales = SinDecimales - (G * 10);
            H = SinDecimales;

            Monto = "";

            if (I != 0 || J != 0 || K != 0)
            {
                Monto = Monto + Miles(I, J, K);
                if (I == 0 && J == 0 && K == 1)
                {
                    Monto = Monto + "Millon ";
                }
                else
                {
                    Monto = Monto + "Millones ";
                }
            }

            if (A != 0 || B != 0 || C != 0)
            {
                Monto = Monto + Miles(A, B, C);
                Monto = Monto + "Mil ";
            }

            if (D != 0 || E != 0 || F != 0)
            {
                Monto = Monto + Miles(D, E, F);
            }

            if (G != 0 || H != 0)
            {
                Monto = Monto + "con ";
                Monto = Monto + Miles(0, G, H);
                if (G == 0 && H == 1)
                {
                    Monto = Monto + "Centavo.";
                }
                else
                {
                    Monto = Monto + "Centavos.";
                }
            }

            return Monto;
        }

        static string Miles(long CE, long DE, long UN)
        {
            string Monto = "";

            if (CE != 0)
            {
                Monto = Monto + arCentena.GetValue(CE);
                if ((DE != 0 || UN != 0) && CE == 1)
                    Monto = Monto.Trim() + "to ";
            }


            if (DE == 1)
            {
                if (UN == 0)
                    Monto = Monto + "Diez ";
                else if (UN <= 5)
                    Monto = Monto + arQuinces.GetValue(UN);
                else
                    Monto = Monto + arDecena.GetValue(DE) + ((string)(arUnidad.GetValue(UN))).ToLower();
            }
            else if (DE == 2)
            {
                if (UN == 0)
                    Monto = Monto + "Veinte ";
                else
                    Monto = Monto + arDecena.GetValue(2) + arUnidad.GetValue(UN);
            }
            if (DE > 2)
            {
                Monto = Monto + arDecena.GetValue(DE);
                if (UN > 0)
                    Monto = Monto + "y " + arUnidad.GetValue(UN);
            }
            else if ((DE == 0) && (UN != 0))
            {
                Monto = Monto + arUnidad.GetValue(UN);
            }

            return Monto;
        }
    }
}