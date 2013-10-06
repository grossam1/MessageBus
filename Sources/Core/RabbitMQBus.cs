﻿using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using MessageBus.Binding.RabbitMQ;
using MessageBus.Core.API;

namespace MessageBus.Core
{
    public class RabbitMQBus : Bus
    {
        protected readonly string _host;
        protected readonly RabbitMQBinding _binding;

        private IChannelFactory<IOutputChannel> _channelFactory;

        public RabbitMQBus()
            : this(Guid.NewGuid().ToString())
        {
        }
        
        public RabbitMQBus(string busId, string host = "localhost", string exchange = "amq.fanout", bool exactlyOnce = false, IErrorSubscriber errorSubscriber = null)
            : base(busId, errorSubscriber)
        {
            _host = host;

            _binding = new RabbitMQBinding
                {
                    ApplicationId = busId,
                    AutoBindExchange = exchange,
                    OneWayOnly = true,
                    ExactlyOnce = exactlyOnce,
                    PersistentDelivery = false,
                    // TODO: Config
                    //MessageFormat = MessageFormat.MTOM,
                    //ReaderQuotas = new XmlDictionaryReaderQuotas
                    //    {
                    //        MaxArrayLength = 10 * 1024 * 1024
                    //    }
                };
        }

        protected override MessageVersion MessageVersion
        {
            get { return _binding.MessageVersion; }
        }

        protected override IOutputChannel CreateOutputChannel()
        {
            if (_channelFactory == null)
            {
                _channelFactory = _binding.BuildChannelFactory<IOutputChannel>();

                _channelFactory.Open();
            }

            Uri toAddress = new Uri(string.Format("amqp://{0}/{1}", _host, _binding.AutoBindExchange));

            return _channelFactory.CreateChannel(new EndpointAddress(toAddress));
        }

        protected override IInputChannel CreateInputChannel()
        {
            Uri listenUriBaseAddress = new Uri(string.Format("amqp://{0}/", _host));

            IChannelListener<IInputChannel> listener = _binding.BuildChannelListener<IInputChannel>(listenUriBaseAddress);

            listener.Open();

            try
            {
                return listener.AcceptChannel();
            }
            finally
            {
                listener.Close();
            }
        }

        public override void Dispose()
        {
            if (_channelFactory != null)
            {
                _channelFactory.Close();
            }
        }
    }
}