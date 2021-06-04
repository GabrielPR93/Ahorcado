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
        public string[] records;
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
                lock (l)
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
            }
            return mensaje;
        }

        public void guardarRecord(string record)
        {
            if (record != null)
            {
                lock (l)
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

        }
        public void iniciar()
        {
            bool conexion = true;
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, puerto);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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
                Socket sClient = s.Accept();
                Thread hilo = new Thread(hiloCliente);
                hilo.Start(sClient);
            }
        }

        public void hiloCliente(object socket)
        {
            bool conexion = true;
            bool bienvenida = true;
            char caracter;
            string[] mensaje;
            string opcion;
            string opcion2 = "";
            string cadena;
            string cadenaRemplazada = "";
            int vidas = 5;
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
                                            caracter = Convert.ToChar(sr.ReadLine().ToLower());

                                            for (int i = 0; i < caracteresPalabra.Length; i++)
                                            {
                                                if (caracteresPalabra[i] == caracter)
                                                {
                                                    adivinar[i] = caracter;
                                                    aciertos++;
                                                    vidas++;

                                                }

                                            }
                                            foreach (char item in adivinar)
                                            {
                                                sw.Write(item);
                                                sw.Flush();

                                            }

                                        }
                                        catch (ArgumentNullException)
                                        {

                                        }
                                        catch (FormatException)
                                        {

                                        }
                                        catch (NullReferenceException)
                                        {

                                        }


                                        vidas--;
                                    }
                                    if (aciertos == adivinar.Length)
                                    {

                                        sw.WriteLine("\r\n Enhorabuena la acertaste !!");
                                        sw.Flush();

                                    }
                                    else
                                    {
                                        sw.WriteLine("\r\n Fin del juego !! la palabra era {0}",cadena);
                                        sw.Flush();
                                    }
                                  
                                    break;
                                case "sendword":
                                    if (opcion2 != "")
                                    {
                                        sw.WriteLine(guardarPalabra(opcion2));
                                        sw.Flush();

                                    }
                                    else
                                    {
                                        sw.WriteLine("Error, para guardar una palabra inserte: sendword \"palabra\"");
                                        sw.Flush();
                                    }
                                    break;
                                case "getrecords":
                                    break;
                                case "sendrecord":
                                    break;
                                case "closeserver":
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
