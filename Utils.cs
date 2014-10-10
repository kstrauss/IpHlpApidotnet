using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace IpHlpApidotnet
{
    public static class Utils
    {
        public const int NO_ERROR = 0;
        public const int MIB_TCP_STATE_CLOSED = 1;
        public const int MIB_TCP_STATE_LISTEN = 2;
        public const int MIB_TCP_STATE_SYN_SENT = 3;
        public const int MIB_TCP_STATE_SYN_RCVD = 4;
        public const int MIB_TCP_STATE_ESTAB = 5;
        public const int MIB_TCP_STATE_FIN_WAIT1 = 6;
        public const int MIB_TCP_STATE_FIN_WAIT2 = 7;
        public const int MIB_TCP_STATE_CLOSE_WAIT = 8;
        public const int MIB_TCP_STATE_CLOSING = 9;
        public const int MIB_TCP_STATE_LAST_ACK = 10;
        public const int MIB_TCP_STATE_TIME_WAIT = 11;
        public const int MIB_TCP_STATE_DELETE_TCB = 12;

        #region helper function

        public static UInt16 ConvertPort(UInt32 dwPort)
        {
            byte[] b = new Byte[2];
            // high weight byte
            b[0] = byte.Parse((dwPort >> 8).ToString());
            // low weight byte
            b[1] = byte.Parse((dwPort & 0xFF).ToString());
            return BitConverter.ToUInt16(b, 0);
        }

        public static int BufferToInt(byte[] buffer, ref int nOffset)
        {
            int res = (((int)buffer[nOffset])) + (((int)buffer[nOffset + 1]) << 8) +
                      (((int)buffer[nOffset + 2]) << 16) + (((int)buffer[nOffset + 3]) << 24);
            nOffset += 4;
            return res;
        }

        public static string StateToStr(int state)
        {
            string strg_state = "";
            switch (state)
            {
                case MIB_TCP_STATE_CLOSED: strg_state = "CLOSED"; break;
                case MIB_TCP_STATE_LISTEN: strg_state = "LISTEN"; break;
                case MIB_TCP_STATE_SYN_SENT: strg_state = "SYN_SENT"; break;
                case MIB_TCP_STATE_SYN_RCVD: strg_state = "SYN_RCVD"; break;
                case MIB_TCP_STATE_ESTAB: strg_state = "ESTAB"; break;
                case MIB_TCP_STATE_FIN_WAIT1: strg_state = "FIN_WAIT1"; break;
                case MIB_TCP_STATE_FIN_WAIT2: strg_state = "FIN_WAIT2"; break;
                case MIB_TCP_STATE_CLOSE_WAIT: strg_state = "CLOSE_WAIT"; break;
                case MIB_TCP_STATE_CLOSING: strg_state = "CLOSING"; break;
                case MIB_TCP_STATE_LAST_ACK: strg_state = "LAST_ACK"; break;
                case MIB_TCP_STATE_TIME_WAIT: strg_state = "TIME_WAIT"; break;
                case MIB_TCP_STATE_DELETE_TCB: strg_state = "DELETE_TCB"; break;
            }
            return strg_state;
        }

        public static IPEndPoint BufferToIPEndPoint(byte[] buffer, ref int nOffset, bool IsRemote)
        {
            //address
            Int64 m_Address = ((((buffer[nOffset + 3] << 0x18) | (buffer[nOffset + 2] << 0x10)) | (buffer[nOffset + 1] << 8)) | buffer[nOffset]) & ((long)0xffffffff);
            nOffset += 4;
            int m_Port = 0;
            m_Port = (IsRemote && (m_Address == 0))? 0 : 
                (((int)buffer[nOffset]) << 8) + (((int)buffer[nOffset + 1])) + (((int)buffer[nOffset + 2]) << 24) + (((int)buffer[nOffset + 3]) << 16);
            nOffset += 4;

            // store the remote endpoint
            IPEndPoint temp = new IPEndPoint(m_Address, m_Port);
            if (temp == null)
                Debug.WriteLine("Parsed address is null. Addr=" + m_Address.ToString() + " Port=" + m_Port + " IsRemote=" + IsRemote.ToString());
            return temp;
        }

        public static string GetHostName(IPEndPoint HostAddress)
        {
            try
            {
                if (HostAddress.Address.ToString() == "0.0.0.0") //.Address == 0)
                {
                    if (HostAddress.Port > 0)
                        return String.Format("{0}:{1}",GetLocalHostName(),HostAddress.Port);
                    else
                        return "Anyone";
                }
                return String.Format("{0}:{1}", GetHostEntryName(HostAddress.Address.ToString()) ,HostAddress.Port);
            }
            catch
            {
                return HostAddress.ToString();
            }
        }

        private delegate IPHostEntry GetHostEntryHandler(string ip);
        //DNS response time in sec
        private static int DnsWaitTimeSec = 3;

        public static string GetLocalHostName()
        {
            //IPGlobalProperties.GetIPGlobalProperties().DomainName +"." + IPGlobalProperties.GetIPGlobalProperties().HostName
            return GetHostEntryName("localhost");
        }

        private static HostNameCurcularBuffer _HostNames = new HostNameCurcularBuffer();
        //private static object _SyncHostName = new object();
        public static string GetHostEntryName(string hostNameOrAddress)
        {
            foreach (var pair in _HostNames.ToArray())
            {
                if (pair.Key == hostNameOrAddress)
                    return pair.Value;
            }

            var newHostName = "unresolve";
            try
            {
                GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
                IAsyncResult result = callback.BeginInvoke(hostNameOrAddress, null, null);

                // Wait dns response certain amount of time
                if (result.AsyncWaitHandle.WaitOne(DnsWaitTimeSec * 1000, false))
                {
                    newHostName = callback.EndInvoke(result).HostName;
                }
                _HostNames.Add(new KeyValuePair<string, string>(hostNameOrAddress, newHostName));
                return newHostName;
            }
            catch (Exception)
            {
                _HostNames.Add(new KeyValuePair<string, string>(hostNameOrAddress, newHostName));
                return newHostName;
            }
        }

        // Together with GetHostName can generate duplications in cache.
        // It decrease efficiency of cache, but not generate an error.
        public static bool FillHostNameCache(string hostNameOrAddress)
        {
            foreach (var pair in _HostNames.ToArray())
            {
                if (pair.Key == hostNameOrAddress)
                    return false;
            }

            var newHostName = "unresolve";
            try
            {
                // Waiting time in sec
                int DnsWaitTimeSec = 3;
                GetHostEntryHandler callback = new GetHostEntryHandler(Dns.GetHostEntry);
                IAsyncResult result = callback.BeginInvoke(hostNameOrAddress, null, null);

                // Wait dns response while 1sec
                if (result.AsyncWaitHandle.WaitOne(DnsWaitTimeSec * 1000, false))
                {
                    newHostName = callback.EndInvoke(result).HostName;
                    _HostNames.Add(new KeyValuePair<string, string>(hostNameOrAddress, newHostName));
                    return true;
                }
                else
                {
                    _HostNames.Add(new KeyValuePair<string, string>(hostNameOrAddress, newHostName));
                    return false;
                }
            }
            catch (Exception)
            {
                _HostNames.Add(new KeyValuePair<string, string>(hostNameOrAddress, newHostName));
                return false;
            }
        }

        public static int CompareIPEndPoints(IPEndPoint first, IPEndPoint second)
        {
            int i;
            byte[] _first = first.Address.GetAddressBytes();
            byte[] _second = second.Address.GetAddressBytes();
            for (int j = 0; j < _first.Length; j++)
            {
                i = _first[j] - _second[j];
                if (i != 0)
                    return i;
            }
            i = first.Port - second.Port;
            if (i != 0)
                return i;
            return 0;
        }

        public static string GetProcessNameByPID(int processID)
        {
            //could be an error here if the process die before we can get his name
            try
            {
                Process p = Process.GetProcessById((int)processID);
                return p.ProcessName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return "Unknown";
            }
        }
        #endregion
    }
}