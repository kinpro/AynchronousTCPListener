using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPClient
{
    public class Program
    {
        static IPEndPoint _endPoint;
        static TimeSpan _delay;

        public static void Main(String[] args)
        {            
            IPAddress address;
            Int32 port;
            Int32 amountOfClients;
            Int32 millisecondDelay;

            if(args.Length < 4 || !IPAddress.TryParse(args[0],out address) || !Int32.TryParse(args[1], out port) || !Int32.TryParse(args[2],out amountOfClients) || !Int32.TryParse(args[3], out millisecondDelay))
            {
                Console.WriteLine("parameters: [IPAddress] [Port] [Number of clients] [Delay between messages (ms)]");
                return;
            }

            Thread.Sleep(1000); // give the server time to start up.
            Console.Title = "Client";

            _endPoint = new IPEndPoint(address, port);
            _delay = TimeSpan.FromMilliseconds(millisecondDelay);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancel = cancellationTokenSource.Token;
            Random ran = new Random(DateTime.Now.Millisecond);

            List<Task> clientList = new List<Task>();
            for (int i = 0; i < amountOfClients; i++)
            {
                clientList.Add(CreateClientAsync(cancel, ran, amountOfClients));
            }
                                    
            Console.ReadKey();
            cancellationTokenSource.Cancel();
            Task.WaitAll(clientList.ToArray());
            Console.WriteLine("end");
            Console.ReadKey(true);
        }

        private static async Task CreateClientAsync(CancellationToken cancel, Random random, Int32 amountOfClients)
        {
            await Task.Yield();

            while (!cancel.IsCancellationRequested)
            {
                Console.WriteLine("Connecting " + _endPoint.ToString());
                await Task.Delay(random.Next(10, amountOfClients));

                StreamReader sr = null;
                StreamWriter sw = null;
                try
                {
                    TcpClient client = new TcpClient();
                    await client.ConnectAsync(_endPoint.Address, _endPoint.Port);
                    var stream = client.GetStream();
                    sr = new StreamReader(stream, Encoding.UTF8);
                    sw = new StreamWriter(stream, Encoding.UTF8);
                    while (client.Connected && !cancel.IsCancellationRequested)
                    {
                        await sw.WriteLineAsync("- There's something very important I forgot to tell you. - What? - Don't cross the streams. - Why? - It would be bad. - I'm fuzzy on the whole good/bad thing. What do you mean, 'bad'? - Try to imagine all life as you know it stopping instantaneously and every molecule in your body exploding at the speed of light. - Total protonic reversal. - Right. That's bad. Okay. All right. Important safety tip. Thanks, Egon.");
                        await sw.FlushAsync();
                        var msg = await sr.ReadLineAsync();
                        //Console.WriteLine("Server says " + msg);
                        await Task.Delay(_delay);
                    }
                }
                catch (Exception aex)
                {
                    var ex = aex.GetBaseException();
                    Console.WriteLine("Client error: " + ex.Message);
                }
                finally
                {
                    if (sr != null)
                        sr.Dispose();
                    if (sw != null)
                        sw.Dispose();
                }
                Console.WriteLine("Disconnecting " + _endPoint.ToString());
            }
        }
    }
}
