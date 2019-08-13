using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using DiscordRPC;

namespace pc
{

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct pkg_header
    {
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 magic;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] name;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 img_size;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct pkg_img
    {
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 magic;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 index;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 used_size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32768)]
        public byte[] buffer;
    }


    class Program
    {

        static int discordPipe = -1;
        static Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


        static readonly int bufSize = 65507;
        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        static State state = new State();

        static EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        static AsyncCallback recv = null;

        static byte[] image = null;
        static int num_chunks_left = 0;
        static String name;


        static void Main(string[] args)
        {
            var client = new DiscordRpcClient("610748528528195584", pipe: discordPipe)
            {
            };
            client.Initialize();

            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            sock.EnableBroadcast = true;
            sock.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 51966));

            sock.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = sock.EndReceiveFrom(ar, ref epFrom);
                sock.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);

                pkg_header head = StructConverter.ByteArrayToStructure<pkg_header>(so.buffer);
                pkg_img img = StructConverter.ByteArrayToStructure<pkg_img>(so.buffer);


                if (head.magic == 0xffaadd23) // Header
                {
                    name = Encoding.ASCII.GetString(head.name).TrimEnd((Char)0);
                    Console.WriteLine("Got header for " + name);
                    image = new byte[head.img_size];
                    num_chunks_left = (int)Math.Ceiling((float)head.img_size / 32768);

                }
                else if (img.magic == 0xaabbdd32) // Image Chunk
                {
                    if (num_chunks_left == 0)
                    {
                        // Got image chunk before header, we don't want that
                        return;
                    }
                    for (int i = 0; i < img.used_size; i++)
                    {
                        image[32768 * img.index + i] = img.buffer[i];
                    }
                    num_chunks_left--;
                    if (num_chunks_left == 0)
                    {
                        Console.WriteLine("hey, got image complete :)");
                        client.SetPresence(new RichPresence()
                        {
                            Details = name,
                            State = ":shrek:",
                        });
                        var fileStream = new FileStream("out.img", FileMode.Create);
                        fileStream.Write(image, 0, image.Length);
                        fileStream.Close();
                    }
                }






            }, state);



            Console.ReadLine();

            client.Dispose();
        }
    }
}
