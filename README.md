AynchronousTCPListener
======================

Example of an asynchronous TCP server and clients using async/await.

The server populates these performance counters:

Category: TcpListener_Test
 * Messages In /sec
 * Messages Out /sec
 * Bytes In /sec
 * Bytes Out /sec
 * Connected
 
The first time you run the app will set the counters and exit.
 
It is normal that at connection you can see exceptions on screen, since that means that the server was busy accepting clients. The important thing is to check up in the "Connected" performance counter that all clients have eventually connected.
