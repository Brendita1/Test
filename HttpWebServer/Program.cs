using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.Windows.Forms;

namespace HttpWebServer
{
    class Program
    {


        public static HttpListener listener;
        public static string url = "http://+:8000/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +

            "    <form method=\"post\" action=\"shutdown\" > " +
            "      <p>User</p>" +
            "      <input name=\"user\"></input>" +
            "      <p>Password</p>" +
            "      <input name=\"pass\" type=\"password\"></input>" +
            "      <p>Press to envie</p>" +
            "     <button type=\"submit\">Send</button>" +
            "    </form>" +
            "  </body>" +
            "</html>";

        public static void connected()
        {
            // La siguiente linea es la que provee la conexión entre C# y MySQL.
            // Debes cambiar el nombre de usuario, contrasenia y nombre de base de datos
            // de acuerdo a tus datos
            // Puedes ignorar la opción de base de datos (database) si quieres acceder a todas
            // 127.0.0.1 es de localhost y el puerto predeterminado para conectar
            string connectionString = "datasource=127.0.0.1;port=3306;username=root;password=;database=prueba;";
            // Tu consulta en SQL
            string query = "SELECT * FROM users";

            // Prepara la conexión
            MySqlConnection databaseConnection = new MySqlConnection(connectionString);
            MySqlCommand commandDatabase = new MySqlCommand(query, databaseConnection);
            commandDatabase.CommandTimeout = 60;
            MySqlDataReader reader;

            // A consultar !
            try
            {
                // Abre la base de datos
                databaseConnection.Open();

                // Ejecuta la consultas
                reader = commandDatabase.ExecuteReader();

                // Hasta el momento todo bien, es decir datos obtenidos

                // IMPORTANTE :#
                // Si tu consulta retorna un resultado, usa el siguiente proceso para obtener datos

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        // En nuestra base de datos, el array contiene:  ID 0, FIRST_NAME 1,LAST_NAME 2, ADDRESS 3
                        // Hacer algo con cada fila obtenida
                        string[] row = { reader.GetString(0), reader.GetString(1), reader.GetString(2) };
                        Console.WriteLine("Conexion realizada a MYSQL");
                    }
                }
                else
                {
                    Console.WriteLine("No se encontraron datos.");
                }

                // Cerrar la conexión
                databaseConnection.Close();
            }
            catch (Exception ex)
            {
                // Mostrar cualquier excepción
                MessageBox.Show(ex.Message);
            }



        }

        public static async Task HandleIncomingConnections()
        {

            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                {

                    System.IO.Stream body = req.InputStream;
                    System.Text.Encoding encoding = req.ContentEncoding;
                    System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
                    if (req.ContentType != null)
                    {
                        Console.WriteLine("Client data content type {0}", req.ContentType);
                    }
                    Console.WriteLine("Client data content length {0}", req.ContentLength64);
                    Console.WriteLine("Shutdown requested");
                    string s = reader.ReadToEnd();
                    string[] subs = s.Split('='); //subs contiene el password en la posicion 2
                    string[] subs2 = subs[1].Split('&'); // Subs2 contiene al usuario en la posicion 0
                   
                    Console.WriteLine("Password:"+subs[2]);
                    Console.WriteLine("User:"+subs2[0]);
                    Console.WriteLine("End of client data:");
                    connected(); 
                    //runServer = false;

                }

                // Make sure we don't increment the page views counter if `favicon.ico` is requested
                if (req.Url.AbsolutePath != "/favicon.ico")
                    pageViews += 1;

                // Write the response info
                
                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }
  

        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Esperando respuesta de el servidor con el puerto: {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
           
            // Close the listener
            listener.Close();
        }
       
    }
}
