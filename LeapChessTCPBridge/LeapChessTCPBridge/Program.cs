using System;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace LeapChessTCPBridge
{
    class Program
    {
        static String ReadOutput(StreamReader reader)
        {
            StringBuilder str = new StringBuilder();
            reader.DiscardBufferedData();
            while (reader.Peek() != -1)
            {
                str.Append((char)reader.Read());
            }
            str.Append("\n");

            Console.WriteLine("<< " + str.ToString());
            return str.ToString();
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("Starting bridge: "  + args[0]);
            if (args.Length == 0)
            {
                Console.WriteLine("Engine not specified!");
                System.Environment.Exit(1);
            }



            // Start the Sort.exe process with redirected input.
            // Use the sort command to sort the input text.
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

            while (myStreamReader.Peek() != -1){ output.Append((char)myStreamReader.Read());}
            Console.WriteLine(output);
            output.Clear();

           
            
                
            //init Stockfish
            Console.WriteLine("Engine init");

            myStreamWriter.WriteLine("uci");
            String o = ReadOutput(myStreamReader);

            myStreamWriter.WriteLine("isready");
            o = ReadOutput(myStreamReader);


            /*
            myStreamReader.DiscardBufferedData();
            myStreamWriter.Write("uci\n");
            while (myStreamReader.Peek() == -1) { }
            while (myStreamReader.Peek() != -1) { output.Append((char)myStreamReader.Read()); }
            Console.WriteLine(output);
            output.Clear();

            Console.WriteLine("Engine boh2");
            myStreamReader.DiscardBufferedData();
            myStreamWriter.WriteLine("isready");
            while (myStreamReader.Peek() != -1) { output.Append((char)myStreamReader.Read()); }
            Console.WriteLine(output);
            //output.Clear();
            */
            if (o.Trim() != "readyok")
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
            // You could also user server.AcceptSocket() here.
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Connected!");
            NetworkStream stream = client.GetStream();

            int i;

            // Loop to receive all the data sent by the client.
            // leapchess ->  read from tcp -> send to engine -> read engine output -> send to tcp -> leapchess
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                Console.WriteLine("Received: {0}", data);

                //send to engine
                myStreamWriter.WriteLine(data);

                //read engine output
                o = ReadOutput(myStreamReader);
                Console.WriteLine(">>" + o);                


                byte[] msg = System.Text.Encoding.ASCII.GetBytes(o.Trim());

                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                Console.WriteLine("Sent: {0}", o);
            }






            /*
             // Prompt the user for input text lines to sort. 
             // Write each line to the StandardInput stream of
             // the sort command.
             String inputText;
             int numLines = 0;
             do 
             {
                Console.WriteLine("Enter a line of text (or press the Enter key to stop):");

                inputText = Console.ReadLine();
                if (inputText.Length > 0)
                {
                   numLines ++;
                   myStreamWriter.WriteLine(inputText);
                }
             } while (inputText.Length != 0);


             // Write a report header to the console.
             if (numLines > 0)
             {
                Console.WriteLine(" {0} sorted text line(s) ", numLines);
                Console.WriteLine("------------------------");
             }
             else 
             {
                Console.WriteLine(" No input was sorted");
             }
                */
            // End the input stream to the sort command.
            // When the stream closes, the sort command
            // writes the sorted text lines to the 
            // console.
            myStreamWriter.Close();
            myStreamReader.Close();


            // Wait for the sort process to write the sorted text lines.
            engine.WaitForExit();
            engine.Close();

        }
    }
}
