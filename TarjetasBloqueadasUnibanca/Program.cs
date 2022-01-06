using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarjetasBloqueadasUnibanca
{
    class Program
    {
        public static string fechaarchivo = "";

        static void Main(string[] args)
        {
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\INPUT"))
            {
                try
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\INPUT");
                }
                catch
                {

                }
            }

            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\PROCESADO"))
            {
                try
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\PROCESADO");
                }
                catch
                {

                }
            }
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\OUTPUT"))
            {
                try
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\OUTPUT");
                }
                catch
                {

                }
            }
            if (args.Length == 0)
            {
                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\INPUT", "31312d*.dat", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    Console.WriteLine("Se encontró " + files.Length + " archivo(s) valido(s).\nDesea procesarlo(s)? ([Y]/N)");
                    string key = Console.ReadLine();
                    if (key != "N")
                    {
                        foreach (string file in files)
                        {
                            procesarArchivo(file);
                            File.Move(file, Directory.GetCurrentDirectory() + "\\PROCESADO\\" + Path.GetFileNameWithoutExtension(file) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Path.GetExtension(file));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No se encontró archivos validos en la ruta " + Directory.GetCurrentDirectory() + "\\INPUT");
                }
            }
            else
            {
                Console.WriteLine("Se recibió el siguiente archivo: " + args[0]);
                procesarArchivo(args[0]);
            }
            Console.WriteLine("Fin");
            Console.WriteLine("Presione cualquier tecla para continuar...");
            Console.ReadKey();
        }

        private static void procesarArchivo(string filename)
        {
            int nLineas = 0;
            int cantidadCaracteres = 1125;
            string text = File.ReadAllText(filename);

            string[] lineas = new string[text.Length / cantidadCaracteres];
            DataTable dt;

            while (nLineas * cantidadCaracteres < text.Length)
            {
                try
                {
                    lineas[nLineas] = text.Substring(nLineas * cantidadCaracteres, cantidadCaracteres).Replace(",", " ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                nLineas++;
            }

            dt = separarColumnas(lineas);
            ExportarDataTableCSV(dt, filename);
        }

        private static DataTable separarColumnas(string[] lineas)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Tipo de Registro");
            dt.Columns.Add("Tipo de mensaje");
            dt.Columns.Add("Primer mapa de bits del mensaje");
            dt.Columns.Add("Número de la tarjeta");
            dt.Columns.Add("Fecha y hora de transacción");
            dt.Columns.Add("Trace");
            dt.Columns.Add("Identificador del emisor");
            dt.Columns.Add("Código de respuesta");
            dt.Columns.Add("Reservado");
            dt.Columns.Add("Identificador de tipo de transacción");
            dt.Columns.Add("Compresión de datos");
            dt.Columns.Add("Empaquetado de datos");
            dt.Columns.Add("Tracking");
            dt.Columns.Add("Motivo de bloqueo");
            dt.Columns.Add("Reservado 2");
            dt.Columns.Add("TramaCompleta");

            int[] longitud = new int[dt.Columns.Count];
            longitud[0] = 1;
            longitud[1] = 4;
            longitud[2] = 16;
            longitud[3] = 18;
            longitud[4] = 10;
            longitud[5] = 6;
            longitud[6] = 8;
            longitud[7] = 2;
            longitud[8] = 3;
            longitud[9] = 4;
            longitud[10] = 1;
            longitud[11] = 4;
            longitud[12] = 1;
            longitud[13] = 80;
            longitud[14] = 967;

            int pos = 0;
            int cantidad = 0;
            DateTime fecha = DateTime.Now;
            for (int j = 0; j < lineas.Length; j++)
            {
                pos = 0;
                if (lineas[j].StartsWith("A"))
                {
                    fecha = DateTime.Parse("20" + lineas[j].Substring(28, 2) + "-" + lineas[j].Substring(26, 2) + "-" + lineas[j].Substring(24, 2));
                    fechaarchivo = fecha.ToString("yyyyMMdd");
                }
                else if (lineas[j].StartsWith("B"))
                {
                    DataRow dr = dt.NewRow();
                    int i = 0;
                    for (i = 0; i < dt.Columns.Count - 1; i++)
                    {
                        if (i == 3)
                        {
                            dr[i] = "=\"" + lineas[j].Substring(pos + 2, longitud[i] - 2) + "\"";
                        }
                        else if (i == 4)
                        {
                            dr[i] = "=\"" + lineas[j].Substring(pos + 2, 2) + "/" + lineas[j].Substring(pos, 2) + "/" + fecha.ToString("yyyy") + " " + lineas[j].Substring(pos + 4, 2) + ":" + lineas[j].Substring(pos + 6, 2) + ":" + lineas[j].Substring(pos + 8, 2) + "\"";
                        }
                        else if (i == 6)
                        {
                            dr[i] = "=\"" + lineas[j].Substring(pos + 2, longitud[i] - 2) + "\"";
                        }
                        else if (i == 14)
                        {
                            dr[i] = "";
                        }
                        else
                        {
                            dr[i] = "=\"" + lineas[j].Substring(pos, longitud[i]) + "\"";
                        }
                        pos += longitud[i];
                    }
                    //TramaCompleta
                    dr[i] = lineas[j];
                    dt.Rows.Add(dr);
                    cantidad++;
                }
            }
            return dt;
        }

        public static void ExportarDataTableCSV(DataTable dt, string name)
        {

            string fileName = Directory.GetCurrentDirectory() + "\\OUTPUT\\" + Path.GetFileNameWithoutExtension(name) + "_" + fechaarchivo + ".csv";

            int cols;

            string[] outputCsv = new string[dt.Rows.Count + 1];
            string columnNames = "";
            cols = dt.Columns.Count;
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                columnNames += dt.Columns[i].ColumnName + System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            }
            outputCsv[0] += columnNames;

            //Recorremos el DataTable rellenando la hoja de trabajo
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    if (dt.Rows[i][j] != null)
                    {
                        outputCsv[i + 1] += dt.Rows[i][j].ToString().Replace(",", " ") + System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                    }
                }
            }
            File.WriteAllLines(fileName, outputCsv, Encoding.UTF8);
            Console.WriteLine(" Se generó el .csv " + fileName);
        }

    }
}
