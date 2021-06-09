using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ahorcado
{
    class Juego
    {
        public string clave = "clave";
        public Random r = new Random();
        public static readonly object l = new object();
        public int puerto = 31416;
        public string[] todasPalabras;
        private Socket s;
        public string Leepalabra()
        {
            string linea;
            int num = 0;
            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("USERPROFILE") + "/palabras.txt"))
                {
                    while ((linea = sr.ReadLine()) != null)
                    {
                        if (linea.IndexOf(',') != -1)
                        {
                            todasPalabras = linea.Split(',');

                        }
                    }
                }
                num = r.Next(0, todasPalabras.Count());
            }
            catch (IOException e)
            {

                Console.WriteLine(e.Message);
            }

            return todasPalabras[num];
        }

        public string guardarPalabra(string palabra)
        {
            string mensaje = "";

            if (palabra != null)
            {
                Leepalabra();//solo para guardar todas las palabras
                if (!todasPalabras.Contains(palabra))
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(Environment.GetEnvironmentVariable("USERPROFILE") + "/palabras.txt", true))
                        {
                            sw.Write("," + palabra);
                            mensaje = "Palabra guardada correctamente";
                        }
                    }
                    catch (IOException e)
                    {

                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    mensaje = "El archivo ya contiene esa palabra";
                }

            }
            return mensaje;
        }

        public void guardarRecord(string record)
        {
            if (record != null)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(Environment.GetEnvironmentVariable("USERPROFILE") + "/records.txt", true))
                    {
                        sw.WriteLine(record);

                    }
                }
                catch (IOException e)
                {

                    Console.WriteLine(e.Message);
                }
            }
        }

        public string leeRecords()
        {
            string linea;
            string cadena = "";
            try
            {
                using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("USERPROFILE") + "/records.txt"))
                {
                    while ((linea = sr.ReadLine()) != null)
                    {
                        cadena += linea + "\r\n";
                    }
                }
            }
            catch (IOException e)
            {

                Console.WriteLine(e.Message);
            }

            return cadena;
        }
        public void iniciar()
        {
            bool conexion = true;
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, puerto);
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                s.Bind(ie);
                s.Listen(5);
                Console.WriteLine("Conectado en puerto {0}", ie.Port);
            }
            catch (SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
            {
                conexion = false;
                Console.WriteLine("puerto ocupado");
            }

            while (conexion)
            {
                lock (l)
                {
                    if (conexion)
                    {
                        try
                        {
                            Socket sClient = s.Accept();
                            Thread hilo = new Thread(hiloCliente);
                            hilo.Start(sClient);
                            hilo.IsBackground = true;

                        }
                        catch (SocketException)
                        {
                            conexion = false;

                        }
                    }
                }

            }
        }

        public void hiloCliente(object socket)
        {
            bool conexion = true;
            bool bienvenida = true;
            char caracter;
            string caracterInsertado;
            string[] mensaje;
            string opcion;
            string opcion2 = "";
            string cadena;
            Socket sCliente = (Socket)socket;
            IPEndPoint ieCliente = (IPEndPoint)sCliente.RemoteEndPoint;
            Console.WriteLine("Conectado cliente {0} en puerto {1}", ieCliente.Address, ieCliente.Port);

            while (conexion)
            {

                try
                {
                    using (NetworkStream ns = new NetworkStream(sCliente))
                    using (StreamReader sr = new StreamReader(ns))
                    using (StreamWriter sw = new StreamWriter(ns))
                    {
                        if (bienvenida)
                        {
                            sw.WriteLine("---------- Bienvenido al Ahorcado ------------\n");
                            sw.Flush();
                            bienvenida = false;
                        }
                        sw.WriteLine("Introduce una opcion");
                        sw.Flush();

                        opcion = sr.ReadLine();                           // getword: El servidor envía una palabra al cliente.
                        if (opcion != null)                              // sendword palabra: El cliente le envía una palabra nueva al servidor
                        {                                               // getrecords: El servidor le envía la lista de records al cliente
                                                                       // sendrecord record: El cliente le envía un nuevo récord al servidor.
                                                                      // closeserver clave: cierra el servidor si se dispone de la clave adecuada.
                            if (opcion.IndexOf(' ') != -1)
                            {
                                mensaje = opcion.Split(' ');
                                opcion = mensaje[0];
                                opcion2 = mensaje[1];
                            }

                            switch (opcion.ToLower())
                            {
                                case "getword":
                                    int aciertos = 0;
                                    int vidas = 5;
                                    bool flagPintar = true;
                                    cadena = Leepalabra().ToLower();
                                    char[] caracteresPalabra = new char[cadena.Length];
                                    char[] adivinar = new char[cadena.Length];

                                    while (vidas != 0 && aciertos != adivinar.Length)
                                    {
                                        if (flagPintar)//Rellenamos arrays y dibujamos los *
                                        {
                                            for (int i = 0; i < cadena.Length; i++)
                                            {
                                                caracteresPalabra[i] = cadena[i];
                                                adivinar[i] = '*';
                                            }
                                            foreach (char item in adivinar)
                                            {
                                                sw.Write(item);
                                                sw.Flush();

                                            }
                                            flagPintar = false;
                                        }


                                        sw.WriteLine(" Inserta caracter - {0} intentos restantes", vidas);
                                        sw.Flush();
                                        try
                                        {
                                            caracterInsertado = sr.ReadLine();

                                            if (caracterInsertado != null)
                                            {
                                                caracter = Convert.ToChar(caracterInsertado.ToLower());

                                                if (caracteresPalabra.Contains(caracter))
                                                {
                                                    for (int i = 0; i < caracteresPalabra.Length; i++)
                                                    {
                                                        if (caracteresPalabra[i] == caracter)
                                                        {
                                                            adivinar[i] = caracter;
                                                            aciertos++;


                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    vidas--;
                                                }
                                                foreach (char item in adivinar)
                                                {
                                                    sw.Write(item);
                                                    sw.Flush();

                                                }
                                            }
                                        }
                                        catch (ArgumentNullException e)
                                        {
                                            Console.WriteLine(e.Message);
                                        }
                                        catch (FormatException e)
                                        {
                                            Console.WriteLine(e.Message);
                                        }

                                    }
                                    if (aciertos == adivinar.Length)
                                    {

                                        sw.WriteLine("\r\n Enhorabuena la acertaste !!");
                                        sw.WriteLine("\r\n Introduce 3 iniciales (nombre) para guardar record");
                                        sw.Flush();

                                        string nombre = sr.ReadLine();
                                        if (nombre != null)
                                        {
                                            try
                                            {
                                                guardarRecord(String.Format("{0} aciertos {1}", nombre, aciertos));
                                                sw.WriteLine("\r\n Record guardado correctamente");
                                                sw.Flush();
                                            }
                                            catch (FormatException e)
                                            {
                                                Console.WriteLine(e.Message);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        sw.WriteLine("\r\n Fin del juego !! la palabra era {0}", cadena);
                                        sw.Flush();
                                    }

                                    break;
                                case "sendword":
                                    if (opcion2 != "")
                                    {
                                        sw.WriteLine(guardarPalabra(opcion2));
                                        sw.Flush();
                                        opcion2 = "";
                                    }
                                    else
                                    {
                                        sw.WriteLine("Error, para guardar una palabra inserte: sendword \"palabra\"");
                                        sw.Flush();
                                    }
                                    break;
                                case "getrecords":

                                    sw.WriteLine(leeRecords());
                                    sw.Flush();

                                    break;
                                case "sendrecord":
                                    string iniciales;
                                    string puntuaciones;
                                    if (opcion2 != "")
                                    {
                                        try
                                        {
                                            iniciales = opcion2.Substring(0, 3);
                                            puntuaciones = opcion2.Substring(3);

                                            guardarRecord(String.Format("{0} aciertos {1}", iniciales, puntuaciones));
                                            sw.WriteLine("Record guardado correctamente");
                                            sw.Flush();

                                        }
                                        catch (FormatException e)
                                        {
                                            Console.WriteLine(e.Message);
                                        }
                                        catch (ArgumentOutOfRangeException e)
                                        {
                                            Console.WriteLine(e.Message);
                                        }

                                    }
                                    else
                                    {
                                        sw.WriteLine("Error, para guardar un record inserte 3 iniciales seguido de la puntuacion. Ejemplo: sendrecord \"esp15\"");
                                        sw.Flush();
                                    }
                                    break;
                                case "closeserver":
                                    if (opcion2 != "")
                                    {
                                        if (opcion2 == clave)
                                        {
                                            s.Close();
                                            lock (l)
                                            {
                                                conexion = false;
                                            }
                                        }
                                        else
                                        {
                                            sw.WriteLine("-- Clave incorrecta --");
                                            sw.Flush();
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }

                    }
                }
                catch (IOException e)
                {
                    conexion = false;
                    Console.WriteLine(e.Message);
                }

            }
        }
    }
}
