using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace IndevProxy
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

                c_to_s.SendBufferSize = 1024 * 1024 * 4; s_to_c.SendBufferSize = 1024 * 1024 * 4;
                c_to_s.ReceiveBufferSize = 1024 * 1024 * 4; s_to_c.ReceiveBufferSize = 1024 * 1024 * 4;

                s_to_c.Connect(server_address);

                c_to_s.SendBufferSize = 1024 * 1024 * 4; s_to_c.SendBufferSize = 1024 * 1024 * 4;
                c_to_s.ReceiveBufferSize = 1024 * 1024 * 4; s_to_c.ReceiveBufferSize = 1024 * 1024 * 4;

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
            PrintStr(data, 0, len);

            // seems to be mismash of Alpha/Beta
            switch (data[0])
            {
                case 0x03:
                    Interpret_Chat(data, len); break;
                /*case 0x0A:
                    Interpret_PlayerFlying(data, len); break;
                case 0x0B:
                    Interpret_PlayerPosition(data, len); break;
                case 0x0C:
                    Interpret_PlayerLook(data, len); break;
                case 0x0D:
                    Interpret_PlayerPositionLook(data, len); break;*/
                case 0x0E:
                    Interpret_PlaceDelete(data, len); break;
                case 0x0F:
                    Interpret_PlaceBlock(data, len); break;
                case 0x32:
                    Interpret_Chunky(data, len); break;
            }
        }

// 0x33 consists of
//  4 bytes unknown (01 00 00 00 for 128, 01 01 00 00 for 256)
//  4 byte width
//  4 byte height
//  4 byte length
//  4 bytes ??? always varies per level sent
// 0x06 consists of
//  4 byte X
//  4 byte Y
//  4 byte Z
// 0x18 consists of
//  ?? but it's like the packet in alpha/beta

        void Interpret_Chunky(byte[] data, int len)
        {
            /*int VAL_A = ReadI32(data, 1);
            int VAL_B = ReadI32(data, 5);
            int VAL_C = ReadI32(data, 9);
            int VAL_D = ReadI32(data, 13);
            Console.WriteLine("# {0} : {1} : {2} : {3}",
                VAL_A, VAL_B, VAL_C, VAL_D);
            Console.WriteLine();

            int skip = 17 + VAL_A + VAL_B;
            PrintHex(data, skip, len - skip);
            PrintStr(data, skip, len - skip);*/

            int chunk1Len = ReadI32(data, 1);
            int chunk2Len = ReadI32(data, 5);
            int unknownAA = ReadI32(data, 9); // ??? always seems to be 100 ???

            //byte[] chunk = ReaderWriterLock(chunkLen);
            Console.WriteLine("# {0} : {1} : {2}",
                chunk1Len, chunk2Len, unknownAA);

            byte[] C1 = DecompressChunk(data, 13);
            byte[] C2 = DecompressChunk(data, 13 + 4 + chunk1Len);

            int S = 0;
            //int skip = 17 + chunk1Len + chunk2Len;
            //PrintHex(data, skip, len - skip); last 4 bytes is just second chunk's GZIP checksum
            //PrintStr(data, skip, len - skip);
        }

        byte[] DecompressChunk(byte[] data, int offset)
        {
            int chunkLen = ReadI32(data, offset);
            Console.WriteLine("#     {0}", chunkLen);

            using (MemoryStream src = new MemoryStream(data, offset + 4, chunkLen))
                using (GZipStream gz = new GZipStream(src, CompressionMode.Decompress))
                    using (MemoryStream dst = new MemoryStream())
            {
                gz.CopyTo(dst);
                return dst.ToArray();
            }
        }

        void Interpret_Chat(byte[] data, int len)
        {
            string value = ReadString(data, 1);
            Console.WriteLine("# {0}",
                value);
        }

        void Interpret_PlayerFlying(byte[] data, int len)
        {
            byte f = data[1];
            Console.WriteLine("# Flying: {0}",
                f);
        }

        void Interpret_PlayerLook(byte[] data, int len)
        {
            float y = ReadF32(data, 1);
            float p = ReadF32(data, 5);
            byte f  = data[9];
            Console.WriteLine("# Yaw: {0}, Pitch: {1}, Flying: {2}",
                y, p, f);
        }

        void Interpret_PlayerPosition(byte[] data, int len)
        {
            float x = ReadF32(data, 1);
            float y = ReadF32(data, 5);
            float s = ReadF32(data, 9);
            float z = ReadF32(data, 13);
            byte f  = data[17];

            Console.WriteLine("# X: {0}, Y: {1}, Stance: {2}, Z: {3}, Flying: {4}",
                x, y, s, z, f);
        }

        void Interpret_PlayerPositionLook(byte[] data, int len)
        {
            float x = ReadF32(data, 1);
            float y = ReadF32(data, 5);
            float s = ReadF32(data, 9);
            float z = ReadF32(data, 13);
            float Y = ReadF32(data, 17);
            float p = ReadF32(data, 21);
            byte f  = data[25];

            Console.WriteLine("# X: {0}, Y: {1}, Stance: {2}, Z: {3}, Yaw: {4}, Pitch: {5}, Flying: {6}",
                x, y, s, z, Y, p, f);
        }

        void Interpret_PlaceDelete(byte[] data, int len)
        {
            byte s = data[2];
            int x = ReadI32(data, 2);
            byte y = data[6];
            int z = ReadI32(data, 7);
            byte f = data[11];

            Console.WriteLine("# X: {0}, Y: {1}, Z: {2} -- S: {3}, F: {4}",
                x, y, z, s, f);
            DumpNbt(data, 12, len - 12); // TODO idk
        }

        void Interpret_PlaceBlock(byte[] data, int len)
        {
            int x  = ReadI32(data, 1);
            byte y = data[5];
            int z  = ReadI32(data, 6);
            byte p = data[10];

            Console.WriteLine("# X: {0}, Y: {1}, Z: {2} (P: {3})",
                x, y, z, p);

            int offset = 0;
            offset += 11; len -= 11;
            short itemID = ReadI16(data, offset);
            if (itemID == -1)
            {
                Console.WriteLine("# Item: None");
                offset += 2; len -= 2;
            }
            else
            {
                byte count = data[offset + 2];
                ushort meta = ReadU16(data, offset + 3);
                Console.WriteLine("# Item: {0}, Count: {1}, Meta: {2}",
                    itemID, count, meta);
                offset += 5; len -= 5;
            }
            DumpNbt(data, offset, len); // TODO idk
        }

        void DumpNbt(byte[] data, int offset, int len)
        {
            // TODO what is this even
            PrintHex(data, offset, len);
        }


        static int ReadStringLength(byte[] buffer, int offset)
        {
            return ReadU16(buffer, offset);
        }

        static string ReadString(byte[] buffer, int offset)
        {
            int len = ReadStringLength(buffer, offset);
            return Encoding.BigEndianUnicode.GetString(buffer, offset + 2, len * 2);
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

        unsafe static float ReadF32(byte[] array, int offset)
        {
            int value = ReadI32(array, offset);
            return *(float*)&value;
        }

        unsafe static double ReadF64(byte[] array, int offset)
        {
            long hi = ReadI32(array, offset + 0) & 0xFFFFFFFFL;
            long lo = ReadI32(array, offset + 4) & 0xFFFFFFFFL;

            long value = (hi << 32) | lo;
            return *(double*)&value;
        }

    }
}