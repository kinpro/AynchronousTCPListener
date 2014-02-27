using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TCPServer
{
    public class Program
    {
        static PerformanceCounter _inMessages, _inBytes, _outMessages, _outBytes, _connected;

        public static void Main(String[] args)
        {
            if (CreatePerformanceCounters())
                return;

            Console.Title = "Server";

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancel = cancellationTokenSource.Token;

            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("Service listening at " + listener.LocalEndpoint.ToString());

            var task = AcceptClientsAsync(listener, cancel);

            Console.ReadKey();
            cancellationTokenSource.Cancel();
            task.Wait();
            Console.WriteLine("end");
            Console.ReadKey(true);
        }

        public static async Task AcceptClientsAsync(TcpListener listener, CancellationToken cancel)
        {
            await Task.Yield();

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var timeoutTask = Task.Delay(2000);
                    var acceptTask = listener.AcceptTcpClientAsync();

                    await Task.WhenAny(timeoutTask, acceptTask);
                    if (!acceptTask.IsCompleted)
                        continue;

                    var client = await acceptTask;
                    HandleClientAsync(client, cancel);
                }
                catch (Exception aex)
                {
                    var ex = aex.GetBaseException();
                    Console.WriteLine("Accepting error: " + ex.Message);
                }
            }
        }

        public static async Task HandleClientAsync(TcpClient client, CancellationToken cancel)
        {
            await Task.Yield();
            _connected.Increment();
            var local = client.Client.LocalEndPoint.ToString();
            Console.WriteLine("Connected " + local);
            try
            {
                var stream = client.GetStream();
                using(var sr = new StreamReader(stream, Encoding.UTF8))
                using(var sw = new StreamWriter(stream, Encoding.UTF8))
                while (!cancel.IsCancellationRequested && client.Connected)
                {
                    //using(var sr = new StreamReader(stream, Encoding.UTF8))
                    //using (var sw = new StreamWriter(stream, Encoding.UTF8))
                    {
                        var msg = await sr.ReadLineAsync();;

                        if (msg == null)
                            continue;

                        _inMessages.Increment();
                        _inBytes.IncrementBy(msg.Length);

                        await sw.WriteLineAsync(msg);
                        await sw.FlushAsync();

                        _outMessages.Increment();
                        _outBytes.IncrementBy(msg.Length);
                    }
                }
            }
            catch (Exception aex)
            {
                var ex = aex.GetBaseException();
                Console.WriteLine("Client error: " + ex.Message);
            }
            finally
            {
                _connected.Decrement();
            }
            Console.WriteLine("Disconnected " + local);
        }

        static String msgIn = "Messages In /sec", byteIn = "Bytes In /sec", msgOut = "Messages Out /sec", byteOut = "Bytes Out /sec", connected = "Connected";
        private static bool CreatePerformanceCounters()
        {
            string categoryName = "TcpListener_Test";

            if (!PerformanceCounterCategory.Exists(categoryName))
            {
                var ccdc = new CounterCreationDataCollection();

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64,
                    CounterName = msgIn
                });

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64,
                    CounterName = byteIn
                });

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64,
                    CounterName = msgOut
                });

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.RateOfCountsPerSecond64,
                    CounterName = byteOut
                });

                ccdc.Add(new CounterCreationData
                {
                    CounterType = PerformanceCounterType.NumberOfItems64,
                    CounterName = connected
                });

                PerformanceCounterCategory.Create(categoryName, "", PerformanceCounterCategoryType.SingleInstance, ccdc);

                Console.WriteLine("Performance counters have been created, please re-run the app");
                return true;
            }
            else
            {
                //PerformanceCounterCategory.Delete(categoryName);
                //Console.WriteLine("Delete");
                //return true;

                _inMessages = new PerformanceCounter(categoryName, msgIn, false);
                _inBytes = new PerformanceCounter(categoryName, byteIn, false);
                _outMessages = new PerformanceCounter(categoryName, msgOut, false);
                _outBytes = new PerformanceCounter(categoryName, byteOut, false);
                _connected = new PerformanceCounter(categoryName, connected, false);
                _connected.RawValue = 0;

                return false;
            }
        }       

    }
}
