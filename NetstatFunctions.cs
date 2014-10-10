using System;
using System.Collections.Generic;
using System.Text;

namespace IpHlpApidotnet
{
    public class NetStatFunctions
    {
        public static List<TCPUDPConnection> GetTcpConnections()
        {
            int AF_INET = 2;    // IP_v4
            int buffSize = 20000;
            byte[] buffer = new byte[buffSize];
            int res = IPHlpAPI32Wrapper.GetExtendedTcpTable(buffer, out buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            if (res != Utils.NO_ERROR) //If there is no enouth memory to execute function
            {
                buffer = new byte[buffSize];
                res = IPHlpAPI32Wrapper.GetExtendedTcpTable(buffer, out buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (res != Utils.NO_ERROR)
                {
                    throw new Exception("Couldn't call win API, because we couldn't build a large enough buffer");
                }
            }
            var ourReturn = new List<TCPUDPConnection>();
            int nOffset = 0;
            // number of entry in the
            int NumEntries = Convert.ToInt32(buffer[nOffset]);
            Console.WriteLine("there are {0} entries", NumEntries);
            nOffset += 4;
            for (int i = 0; i < NumEntries; i++)
            {
                // state
                int st = Convert.ToInt32(buffer[nOffset]);
                var row = new TCPUDPConnection(null);
                row.iState = st;
                nOffset += 4;
                row.Protocol = Protocol.TCP;
                row.Local = Utils.BufferToIPEndPoint(buffer, ref nOffset, false);
                row.Remote = Utils.BufferToIPEndPoint(buffer, ref nOffset, true);
                row.PID = Utils.BufferToInt(buffer, ref nOffset);
                ourReturn.Add(row);
            }
            return ourReturn;
        }
    }
}
