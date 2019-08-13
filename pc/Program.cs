using System;
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
        public byte[] buffer;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 img_size;
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
                String name = Encoding.ASCII.GetString(head.buffer).TrimEnd((Char)0);
                //Console.WriteLine("RECV: {0}: {1} {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(head.buffer, 0, bytes));

                client.SetPresence(new RichPresence()
                {
                    Details = name,
                    State = ":shrek:",
                });

            }, state);



            Console.ReadLine();

            client.Dispose();
        }
    }
}
