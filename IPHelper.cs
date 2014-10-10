using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace IpHlpApidotnet
{
    public class IPHelper
    {
        /*
         * Tcp Struct
         * */
        public IpHlpApidotnet.MIB_TCPTABLE TcpConnections;
        public IpHlpApidotnet.MIB_TCPSTATS TcpStats;
        public IpHlpApidotnet.MIB_EXTCPTABLE TcpExConnections;
        //public IpHlpApidotnet.MIB_TCPTABLE_OWNER_PID TcpExAllConnections;

        /*
         * Udp Struct
         * */
        public IpHlpApidotnet.MIB_UDPSTATS UdpStats;
        public IpHlpApidotnet.MIB_UDPTABLE UdpConnections;
        public IpHlpApidotnet.MIB_EXUDPTABLE UdpExConnections;
        //public IpHlpApidotnet.MIB_UDPTABLE_OWNER_PID UdpExAllConnections;

        public TCPUDPConnections Connections; 

        public IPHelper()
        {

        }

        #region Tcp Functions

        public void GetTcpStats()
        {
            TcpStats = new MIB_TCPSTATS();
            IPHlpAPI32Wrapper.GetTcpStatistics(ref TcpStats);
        }

        public void GetExTcpConnections()
        {
            const int AF_INET = 2;    // IP_v4
            // the size of the MIB_EXTCPROW struct =  6*DWORD
            int rowsize = 24;
            int BufferSize = 100000;
            // allocate a dumb memory space in order to retrieve  nb of connection
            IntPtr lpTable = Marshal.AllocHGlobal(BufferSize);
            //getting infos
            int res = IPHlpAPI32Wrapper.AllocateAndGetTcpExTableFromStack(ref lpTable, true, IPHlpAPI32Wrapper.GetProcessHeap(), 0, 2);
            if (res != Utils.NO_ERROR)
            {
                Debug.WriteLine("Error : " + IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res) + " " + res);
                return; // Error. You should handle it
            }
            int CurrentIndex = 0;
            //get the number of entries in the table
            int NumEntries = (int)Marshal.ReadIntPtr(lpTable);
            lpTable = IntPtr.Zero;
            // free allocated space in memory
            Marshal.FreeHGlobal(lpTable);
            ///////////////////
            // calculate the real buffer size nb of entrie * size of the struct for each entrie(24) + the dwNumEntries
            BufferSize = (NumEntries * rowsize) + 4;
            // make the struct to hold the resullts
            TcpExConnections = new IpHlpApidotnet.MIB_EXTCPTABLE();
            // Allocate memory
            lpTable = Marshal.AllocHGlobal(BufferSize);
            res = IPHlpAPI32Wrapper.AllocateAndGetTcpExTableFromStack(ref lpTable, true, IPHlpAPI32Wrapper.GetProcessHeap(), 0, 2);
            if (res != Utils.NO_ERROR)
            {
                Debug.WriteLine("Error : " + IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res) + " " + res);
                return; // Error. You should handle it
            }
            // New pointer of iterating throught the data
            IntPtr current = lpTable;
            CurrentIndex = 0;
            // get the (again) the number of entries
            NumEntries = (int)Marshal.ReadIntPtr(current);
            TcpExConnections.dwNumEntries = NumEntries;
            // Make the array of entries
            TcpExConnections.table = new MIB_EXTCPROW[NumEntries];
            // iterate the pointer of 4 (the size of the DWORD dwNumEntries)
            CurrentIndex += 4;
            current = (IntPtr)((int)current + CurrentIndex);
            // for each entries
            for (int i = 0; i < NumEntries; i++)
            {

                // The state of the connection (in string)
                TcpExConnections.table[i].StrgState = Utils.StateToStr((int)Marshal.ReadIntPtr(current));
                // The state of the connection (in ID)
                TcpExConnections.table[i].iState = (int)Marshal.ReadIntPtr(current);
                // iterate the pointer of 4
                current = (IntPtr)((int)current + 4);
                // get the local address of the connection
                UInt32 localAddr = (UInt32)Marshal.ReadIntPtr(current);
                // iterate the pointer of 4
                current = (IntPtr)((int)current + 4);
                // get the local port of the connection
                UInt32 localPort = (UInt32)Marshal.ReadIntPtr(current);
                // iterate the pointer of 4
                current = (IntPtr)((int)current + 4);
                // Store the local endpoint in the struct and convertthe port in decimal (ie convert_Port())
                TcpExConnections.table[i].Local = new IPEndPoint(localAddr, (int)Utils.ConvertPort(localPort));
                // get the remote address of the connection
                UInt32 RemoteAddr = (UInt32)Marshal.ReadIntPtr(current);
                // iterate the pointer of 4
                current = (IntPtr)((int)current + 4);
                UInt32 RemotePort = 0;
                // if the remote address = 0 (0.0.0.0) the remote port is always 0
                // else get the remote port
                if (RemoteAddr != 0)
                {
                    RemotePort = (UInt32)Marshal.ReadIntPtr(current);
                    RemotePort = Utils.ConvertPort(RemotePort);
                }
                current = (IntPtr)((int)current + 4);
                // store the remote endpoint in the struct  and convertthe port in decimal (ie convert_Port())
                TcpExConnections.table[i].Remote = new IPEndPoint(RemoteAddr, (int)RemotePort);
                // store the process ID
                TcpExConnections.table[i].dwProcessId = (int)Marshal.ReadIntPtr(current);
                // Store and get the process name in the struct
                TcpExConnections.table[i].ProcessName = Utils.GetProcessNameByPID(TcpExConnections.table[i].dwProcessId);
                current = (IntPtr)((int)current + 4);

            }
            // free the buffer
            Marshal.FreeHGlobal(lpTable);
            // re init the pointer
            current = IntPtr.Zero;
        }

        public TcpConnectionInformation[] GetTcpConnectionsNative()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            return properties.GetActiveTcpConnections();
        }

        public IPEndPoint[] GetUdpListeners()
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
        }

        public IPEndPoint[] GetTcpListeners()
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        }

        public void GetTcpConnections()
        {
            byte[] buffer = new byte[20000]; // Start with 20.000 bytes left for information about tcp table
            int pdwSize = 20000;
            int res = IPHlpAPI32Wrapper.GetTcpTable(buffer, out pdwSize, true);
            if (res != Utils.NO_ERROR)
            {
                buffer = new byte[pdwSize];
                res = IPHlpAPI32Wrapper.GetTcpTable(buffer, out pdwSize, true);
                if (res != 0)
                    return;     // Error. You should handle it
            }

            TcpConnections = new IpHlpApidotnet.MIB_TCPTABLE();

            int nOffset = 0;
            // number of entry in the
            TcpConnections.dwNumEntries = Convert.ToInt32(buffer[nOffset]);
            nOffset += 4;
            TcpConnections.table = new MIB_TCPROW[TcpConnections.dwNumEntries];

            for (int i = 0; i < TcpConnections.dwNumEntries; i++)
            {
                // state
                int st = Convert.ToInt32(buffer[nOffset]);
                // state in string
                TcpConnections.table[i].StrgState = Utils.StateToStr(st);
                // state  by ID
                TcpConnections.table[i].iState = st;
                nOffset += 4;
                // local address
                TcpConnections.table[i].Local = Utils.BufferToIPEndPoint(buffer, ref nOffset, false);
                // remote address
                TcpConnections.table[i].Remote = Utils.BufferToIPEndPoint(buffer, ref nOffset, true);
            }
        }

        #endregion

        #region Udp Functions

        public void GetUdpStats()
        {

            UdpStats = new MIB_UDPSTATS();
            IPHlpAPI32Wrapper.GetUdpStatistics(ref UdpStats);
        }

        public void GetUdpConnections()
        {
            byte[] buffer = new byte[20000]; // Start with 20.000 bytes left for information about tcp table
            int pdwSize = 20000;
            int res = IPHlpAPI32Wrapper.GetUdpTable(buffer, out pdwSize, true);
            if (res != Utils.NO_ERROR)
            {
                buffer = new byte[pdwSize];
                res = IPHlpAPI32Wrapper.GetUdpTable(buffer, out pdwSize, true);
                if (res != Utils.NO_ERROR)
                    return;     // Error. You should handle it
            }

            UdpConnections = new IpHlpApidotnet.MIB_UDPTABLE();

            int nOffset = 0;
            // number of entry in the
            UdpConnections.dwNumEntries = Convert.ToInt32(buffer[nOffset]);
            nOffset += 4;
            UdpConnections.table = new MIB_UDPROW[UdpConnections.dwNumEntries];
            for (int i = 0; i < UdpConnections.dwNumEntries; i++)
            {
                UdpConnections.table[i].Local = Utils.BufferToIPEndPoint(buffer, ref nOffset, false);//new IPEndPoint(IPAddress.Parse(LocalAdrr), LocalPort);
            }
        }

        public void GetExUdpConnections()
        {
            // the size of the MIB_EXTCPROW struct =  4*DWORD
            int rowsize = 12;
            int BufferSize = 100000;
            // allocate a dumb memory space in order to retrieve  nb of connection
            IntPtr lpTable = Marshal.AllocHGlobal(BufferSize);
            //getting infos
            int res = IPHlpAPI32Wrapper.AllocateAndGetUdpExTableFromStack(ref lpTable, true, IPHlpAPI32Wrapper.GetProcessHeap(), 0, 2);
            if (res != Utils.NO_ERROR)
            {
                Debug.WriteLine("Error : " + IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res) + " " + res);
                return; // Error. You should handle it
            }
            int CurrentIndex = 0;
            //get the number of entries in the table
            int NumEntries = (int)Marshal.ReadIntPtr(lpTable);
            lpTable = IntPtr.Zero;
            // free allocated space in memory
            Marshal.FreeHGlobal(lpTable);

            ///////////////////
            // calculate the real buffer size nb of entrie * size of the struct for each entrie(24) + the dwNumEntries
            BufferSize = (NumEntries * rowsize) + 4;
            // make the struct to hold the resullts
            UdpExConnections = new IpHlpApidotnet.MIB_EXUDPTABLE();
            // Allocate memory
            lpTable = Marshal.AllocHGlobal(BufferSize);
            res = IPHlpAPI32Wrapper.AllocateAndGetUdpExTableFromStack(ref lpTable, true, IPHlpAPI32Wrapper.GetProcessHeap(), 0, 2);
            if (res != Utils.NO_ERROR)
            {
                Debug.WriteLine("Error : " + IPHlpAPI32Wrapper.GetAPIErrorMessageDescription(res) + " " + res);
                return; // Error. You should handle it
            }
            // New pointer of iterating throught the data
            IntPtr current = lpTable;
            CurrentIndex = 0;
            // get the (again) the number of entries
            NumEntries = (int)Marshal.ReadIntPtr(current);
            UdpExConnections.dwNumEntries = NumEntries;
            // Make the array of entries
            UdpExConnections.table = new MIB_EXUDPROW[NumEntries];
            // iterate the pointer of 4 (the size of the DWORD dwNumEntries)
            CurrentIndex += 4;
            current = (IntPtr)((int)current + CurrentIndex);
            // for each entries
            for (int i = 0; i < NumEntries; i++)
            {
                // get the local address of the connection
                UInt32 localAddr = (UInt32)Marshal.ReadIntPtr(current);
                // iterate the pointer of 4
                current = (IntPtr)((int)current + 4);
                // get the local port of the connection
                UInt32 localPort = (UInt32)Marshal.ReadIntPtr(current);
                // iterate the pointer of 4
                current = (IntPtr)((int)current + 4);
                // Store the local endpoint in the struct and convertthe port in decimal (ie convert_Port())
                UdpExConnections.table[i].Local = new IPEndPoint(localAddr, Utils.ConvertPort(localPort));
                // store the process ID
                UdpExConnections.table[i].dwProcessId = (int)Marshal.ReadIntPtr(current);
                // Store and get the process name in the struct
                UdpExConnections.table[i].ProcessName = Utils.GetProcessNameByPID(UdpExConnections.table[i].dwProcessId);
                current = (IntPtr)((int)current + 4);

            }
            // free the buffer
            Marshal.FreeHGlobal(lpTable);
            // re init the pointer
            current = IntPtr.Zero;
        }

        #endregion
    }
}