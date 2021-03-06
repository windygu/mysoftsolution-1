﻿using System;
using System.Net.Sockets;
using MySoft.Communication.Scs.Client;
using MySoft.Communication.Scs.Communication;
using MySoft.Communication.Scs.Communication.EndPoints.Tcp;
using MySoft.Communication.Scs.Communication.Messages;
using MySoft.IoC.Messages;
using MySoft.Logger;
using MySoft.IoC.Services;

namespace MySoft.IoC
{
    /// <summary>
    /// 服务请求类
    /// </summary>
    public class ServiceRequest : IDisposable
    {
        /// <summary>
        /// 数据回调
        /// </summary>
        public event EventHandler<ServiceMessageEventArgs> OnCallback;

        /// <summary>
        /// 错误回调
        /// </summary>
        public event EventHandler<ErrorMessageEventArgs> OnError;

        /// <summary>
        /// This event is raised when client disconnected from server.
        /// </summary>
        public event EventHandler Disconnected;
        private RequestMessage request;
        private IScsClient client;
        private ILog logger;
        private string node;
        private string ip;
        private int port;

        /// <summary>
        /// 实例化ServiceMessage
        /// </summary>
        /// <param name="node"></param>
        /// <param name="logger"></param>
        public ServiceRequest(ServerNode node, ILog logger, bool isTimeoutDisconnect)
        {
            this.logger = logger;
            this.node = node.Key;
            this.ip = node.IP;
            this.port = node.Port;

            this.client = ScsClientFactory.CreateClient(new ScsTcpEndPoint(ip, port));
            this.client.ConnectTimeout = 5000;
            this.client.IsTimeoutDisconnect = isTimeoutDisconnect;
            this.client.Disconnected += client_Disconnected;
            this.client.MessageReceived += client_MessageReceived;
            this.client.MessageSent += client_MessageSent;
            this.client.MessageError += client_MessageError;
            this.client.WireProtocol = new CustomWireProtocol(node.Compress, node.Encrypt);
        }

        void client_Disconnected(object sender, EventArgs e)
        {
            //输出错误信息
            if (Disconnected != null)
                Disconnected(sender, e);
            else
                this.logger.Write(new SocketException((int)SocketError.ConnectionReset));
        }

        void client_MessageError(object sender, ErrorEventArgs e)
        {
            //输出错误信息
            if (OnError != null)
                OnError(sender, new ErrorMessageEventArgs { Request = request, Error = e.Error });
        }

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected
        {
            get { return client.CommunicationState == CommunicationStates.Connected; }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            client.Disconnect();
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        private void ConnectServer(RequestMessage reqMsg)
        {
            //如果连接断开，直接抛出异常
            if (!IsConnected)
            {
                try
                {
                    //连接到服务器
                    client.Connect();

                    //发送客户端信息到服务端
                    var clientInfo = new AppClient
                    {
                        AppPath = AppDomain.CurrentDomain.BaseDirectory,
                        AppName = reqMsg.AppName,
                        IPAddress = reqMsg.IPAddress,
                        HostName = reqMsg.HostName
                    };

                    client.SendMessage(new ScsClientMessage(clientInfo));
                }
                catch (Exception e)
                {
                    throw new WarningException(string.Format("Can't connect to server ({0}:{1})！Server node : {2} -> {3}", ip, port, node, e.Message));
                }
            }
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="reqMsg"></param>
        /// <returns></returns>
        public void SendMessage(RequestMessage reqMsg)
        {
            this.request = reqMsg;

            //如果未连接上服务
            if (!IsConnected) ConnectServer(reqMsg);

            client.SendMessage(new ScsResultMessage(reqMsg, reqMsg.TransactionId.ToString()));
        }

        #region Socket消息委托

        void client_MessageSent(object sender, MessageEventArgs e)
        {
            //暂不作处理
        }

        void client_MessageReceived(object sender, MessageEventArgs e)
        {
            var message = new ServiceMessageEventArgs
            {
                Client = client,
                Request = request
            };

            //不是指定消息不处理
            if (e.Message is ScsCallbackMessage)
            {
                //消息类型转换
                var data = e.Message as ScsCallbackMessage;
                message.Result = data.MessageValue;
            }
            else if (e.Message is ScsResultMessage)
            {
                //消息类型转换
                var data = e.Message as ScsResultMessage;
                message.Result = data.MessageValue;
            }

            //把数据发送到客户端
            if (OnCallback != null) OnCallback(this, message);
        }

        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            client.Disconnect();
            client.Dispose();
        }
    }
}
