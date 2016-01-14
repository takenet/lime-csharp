using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Lime.Transport.Tcp
{
    public static class TcpClientExtensions
    {
        public static TcpState GetState(this TcpClient tcpClient)
        {                        
            // http://stackoverflow.com/questions/1387459/how-to-check-if-tcpclient-connection-is-closed/29151608
            var connection = TcpConnectionInformationCache.GetTcpConnectionInformation(
                (IPEndPoint)tcpClient.Client.LocalEndPoint, (IPEndPoint)tcpClient.Client.RemoteEndPoint);
            return connection != null ? connection.State : TcpState.Unknown;
       }
    }
}