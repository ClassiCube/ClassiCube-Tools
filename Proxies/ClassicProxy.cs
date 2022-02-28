using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using System.IO.Compression;

// pretty terrible and incomplete proxy that can be used to assist when 
//  debugging protocol communication between a client and a server
// change server_address if the target server isn't running at 127.0.0.1:25565
//  e.g. server_address = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 25565);
namespace ClassicProxy
{
    class Program
    {
        static IPEndPoint server_address = new IPEndPoint(IPAddress.Loopback, 25565);
        static IPEndPoint proxy_address  = new IPEndPoint(IPAddress.Any, 25566);
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(proxy_address);
            server.Start();
            Console.WriteLine("Listening on port {0}", proxy_address.Port);

            for (; ;)
            {
                try
                {
                    Socket client = server.AcceptSocket();
                    Console.WriteLine("Got connection from {0}", client.LocalEndPoint);
                    new Thread(RunProxy).Start(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("!!! Listener error");
                    Console.WriteLine(ex);
                }
            }
        }

        static void RunProxy(object state)
        {
            try
            {
                Socket c_to_s = (Socket)state;
                Socket s_to_c = new Socket(SocketType.Stream, ProtocolType.Tcp);

                c_to_s.SendBufferSize = 1024 * 1024 * 8; s_to_c.SendBufferSize = 1024 * 1024 * 8;
                c_to_s.ReceiveBufferSize = 1024 * 1024 * 8; s_to_c.ReceiveBufferSize = 1024 * 1024 * 8;

                s_to_c.Connect(server_address);

                c_to_s.SendBufferSize = 1024 * 1024 * 8; s_to_c.SendBufferSize = 1024 * 1024 * 8;
                c_to_s.ReceiveBufferSize = 1024 * 1024 * 8; s_to_c.ReceiveBufferSize = 1024 * 1024 * 8;

                RunProxyCore(c_to_s, s_to_c);
            }
            catch (Exception ex)
            {
                Console.WriteLine("!!! Proxy error");
                Console.WriteLine(ex);
            }
        }

        static void RunProxyCore(Socket c_to_s, Socket s_to_c)
        {
            // client_to_server = socket with data from client -> forwarded onto proxy_to_server socket
            // server_to_client = socket with data from server -> forwarded onto proxy_to_client socket
            Inteprereter interp_c_to_s = new Inteprereter(); interp_c_to_s.c_to_s = true;
            Inteprereter interp_s_to_c = new Inteprereter(); interp_s_to_c.c_to_s = false;

            for (; ;)
            {
                if (c_to_s.Available > 0)
                {
                    byte[] data = new byte[c_to_s.Available];
                    int len = c_to_s.Receive(data);

                    interp_c_to_s.Intepret(data, len);
                    s_to_c.Send(data, len, 0);
                }

                if (s_to_c.Available > 0)
                {
                    byte[] data = new byte[s_to_c.Available];
                    int len = s_to_c.Receive(data);

                    interp_s_to_c.Intepret(data, len);
                    c_to_s.Send(data, len, 0);
                }

                Thread.Sleep(10);
            }
        }
    }

    class Inteprereter
    {
        public bool c_to_s;

        void PrintHex(byte[] data, int offset, int len)
        {
            StringBuilder str = new StringBuilder(len * 3 + 3);
            str.Append("   ");

            for (int i = offset; i < offset + len; i++)
            {
                str.AppendFormat("{0:X2} ", data[i]);
            }
            Console.WriteLine(str.ToString());
        }

        void PrintStr(byte[] data, int offset, int len)
        {
            StringBuilder str = new StringBuilder(len + 3);
            str.Append("   ");

            for (int i = 0; i < len; i++)
            {
                byte B = (byte)data[i];
                str.Append((B >= 32 && B < 127) ? (char)B : '.');
            }
            Console.WriteLine(str.ToString());
        }

        public void Intepret(byte[] data, int len)
        {
            if (len == 0) return; // ????
            Console.WriteLine("{0} [{1:X2} - {2} bytes]",
                c_to_s ? "C -> S" : "S -> C", data[0], len);

            PrintHex(data, 0, len);
            //PrintStr(data, 0, len);

            switch (data[0])
            {
                case 0x00:
                    Interpret_Identification(data, len); break;
                case 0x05:
                    Interpret_SetBlockClient(data, len); break;
                case 0x06:
                    Interpret_SetBlockServer(data, len); break;
                case 0x0D:
                    Interpret_Message(data, len); break;
                case 0x0E:
                    Interpret_Disconnect(data, len); break;
                case 0x0F:
                    Interpret_UserType(data, len); break;
            }
        }

        void Interpret_Identification(byte[] data, int len)
        {
            byte version = data[1];
            string name  = ReadString(data, 2);
            string motd  = ReadString(data, 66);
            byte userType = data[130];
            Console.WriteLine("# (version {0}, mode {1}) = {2}, {3}",
                version, userType, name, motd);
        }

        void Interpret_SetBlockClient(byte[] data, int len)
        {
            int X = ReadU16(data, 1);
            int Y = ReadU16(data, 3);
            int Z = ReadU16(data, 5);
            byte M = data[7];
            byte B = data[8];

            Console.WriteLine("# {0}, {1}, {2} = {3} (mode {4})",
                X, Y, Z, B, M);
        }

        void Interpret_SetBlockServer(byte[] data, int len)
        {
            int X = ReadU16(data, 1);
            int Y = ReadU16(data, 3);
            int Z = ReadU16(data, 5);
            byte B = data[7];

            Console.WriteLine("# {0}, {1}, {2} = {3}",
                X, Y, Z, B);
        }
        void Interpret_Message(byte[] data, int len)
        {
            byte type = data[1];
            string value = ReadString(data, 2);
            Console.WriteLine("# {0}, {1}",
                type, value);
        }

        void Interpret_Disconnect(byte[] data, int len)
        {
            string value = ReadString(data, 1);
            Console.WriteLine("# {0}",
                value);
        }

        void Interpret_UserType(byte[] data, int len)
        {
            byte M = data[7];
            Console.WriteLine("# Mode: {0}",
                M);
        }

        unsafe static string ReadString(byte[] data, int offset)
        {
            int length = 0;
            char* characters = stackalloc char[64];
            for (int i = 64 - 1; i >= 0; i--)
            {
                byte code = data[i + offset];
                if (code == 0) code = 0x20; // NULL to space

                if (length == 0 && code != 0x20) { length = i + 1; }
                characters[i] = ((char)code);
            }
            return new String(characters, 0, length);
        }

        static ushort ReadU16(byte[] array, int offset)
        {
            return (ushort)(array[offset] << 8 | array[offset + 1]);
        }

        static short ReadI16(byte[] array, int offset)
        {
            return (short)(array[offset] << 8 | array[offset + 1]);
        }

        static int ReadI32(byte[] array, int offset)
        {
            return array[offset] << 24 | array[offset + 1] << 16
                | array[offset + 2] << 8 | array[offset + 3];
        }

    }
}
