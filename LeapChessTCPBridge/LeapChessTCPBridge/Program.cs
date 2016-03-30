using System;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Contexts;

namespace LeapChessTCPBridge
{
    class Program
    {
        static String ReadOutput(StreamReader reader, bool print = true)
        {
            StringBuilder str = new StringBuilder();
            reader.DiscardBufferedData();
            while (reader.Peek() != -1)
            {
                str.Append((char)reader.Read());
            }
            str.Append("\n");

            if(print)
                Console.WriteLine("engine << " + str.ToString());
            
            return str.ToString().Trim();
        }
        
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Engine not specified!");
                System.Environment.Exit(1);
            }

            Console.WriteLine("Starting bridge: "  + args[0]);
            

            //Starting the chess engine process
            Process engine = new Process();
            engine.StartInfo.FileName = args[0];
            engine.StartInfo.UseShellExecute = false;
            engine.StartInfo.RedirectStandardInput = true;
            engine.StartInfo.RedirectStandardOutput = true;

            if(engine.Start())
                Console.WriteLine("Engine started");


            StreamWriter myStreamWriter = engine.StandardInput;
            StreamReader myStreamReader = engine.StandardOutput;
            
            StringBuilder output = new StringBuilder();

            String o = ReadOutput(myStreamReader);
                
            //init Stockfish
            Console.WriteLine("Engine init");

            myStreamWriter.WriteLine("uci");
            o = ReadOutput(myStreamReader);

            myStreamWriter.WriteLine("isready");
            o = ReadOutput(myStreamReader);


            if (o != "readyok")
            {
                Console.WriteLine("Failed initializing StockFish. Output: " + output);
            }
            else
                Console.WriteLine("StockFish ready");


            // Create a TCPListener to accept client connections
            TcpListener server = new TcpListener(IPAddress.Any, 4242);
            server.Start();
            // Buffer for reading data
            Byte[] bytes = new Byte[256];
            String data = null;

            Console.Write("Waiting for a connection... ");
            // Perform a blocking call to accept requests.
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Connected!");
            NetworkStream stream = client.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            // leapchess ->  read from tcp -> send to engine -> read engine output -> send to tcp -> leapchess
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("leapchess>> {0}", data);

                    //send to engine
                    myStreamWriter.Write(data + "\n");

                    //Hint requested
                    if(data == "go")
                    {
                        Console.WriteLine("Hint requested...");//: {0}", o);
                        System.Threading.Thread.Sleep(100);
                        //read engine output
                        o = ReadOutput(myStreamReader);

                        //TODO: it works, but is veeeery ugly
                        var result = Regex.Split(o.Trim(), "\n\r|\r|\n");//, StringSplitOptions.RemoveEmptyEntries);
                        var bestmove = result[result.Length - 1].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); //TODO: what if there isn't bestmove?

                        Console.WriteLine("best move>>" + bestmove[1]);
                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(bestmove[1]);

                        // Send engine output to tcp
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent");//: {0}", o);
                    }
                    else if (data != "ucinewgame")
                    {
                        //start thinking
                        myStreamWriter.Write("go\n");
                        System.Threading.Thread.Sleep(100);
                        //read engine output
                        o = ReadOutput(myStreamReader);

                        //TODO: it works, but is veeeery ugly
                        var result = Regex.Split(o.Trim(), "\n\r|\r|\n");//, StringSplitOptions.RemoveEmptyEntries);
                        var bestmove = result[result.Length - 1].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries); //TODO: what if there isn't bestmove?

                        Console.WriteLine("best move>>" + bestmove[1]);
                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(bestmove[1]);

                        // Send engine output to tcp
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent");//: {0}", o);

                        /*
                        //Send to engine the bestmove
                        System.Threading.Thread.Sleep(100);
                        myStreamWriter.Write(data + " {0}\n", bestmove[1]);
                        myStreamWriter.Write("d\n");
                        System.Threading.Thread.Sleep(100);
                        //read engine output
                        Console.WriteLine("Updated status: {0} {1} d ", data, bestmove[1]);
                        o = ReadOutput(myStreamReader);
                        */
                    }
                    else
                        Console.WriteLine("************* Starting new game *************");

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught ({0}), maybe client disconected.", e.ToString());
                myStreamWriter.Write("quit\n");
                // start new process
                System.Diagnostics.Process.Start(
                     Environment.GetCommandLineArgs()[0],
                     Environment.GetCommandLineArgs()[1]);

                // close current process
                Environment.Exit(0);
            }
            // End the input/output stream to the chess engine.
            myStreamWriter.Close();
            myStreamReader.Close();

            // Wait for the engine process.
            engine.WaitForExit();
            engine.Close();
        }
    }
}
