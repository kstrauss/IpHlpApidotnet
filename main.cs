using System.Text;
using System.Net;
using System.Collections.Generic;

namespace IpHlpApidotnet
{
    public enum Protocol { TCP, UDP, None };

    public class SortConnections : IComparer<TCPUDPConnection>
    {
        /// <summary>
        /// Method is used to compare two <seealso cref="TCPUDPConnection"/>. 
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public virtual int CompareConnections(TCPUDPConnection first, TCPUDPConnection second)
        {
            int i = Utils.CompareIPEndPoints(first.Local, second.Local);
            if (i != 0)
                    return i;
            if (first.Protocol == Protocol.TCP &&
                second.Protocol == Protocol.TCP)
            {
                i = Utils.CompareIPEndPoints(first.Remote, second.Remote);
                if (i != 0)
                    return i;
            }
            i = first.PID - second.PID;
            if (i != 0)
                return i;
            if (first.Protocol == second.Protocol)
                return 0;
            if (first.Protocol == Protocol.TCP)
                return -1;
            else
                return 1;
        }

        #region IComparer<TCPUDPConnection> Members

        public int Compare(TCPUDPConnection x, TCPUDPConnection y)
        {
            return this.CompareConnections(x, y);
        }

        #endregion
    }
}
