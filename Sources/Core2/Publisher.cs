﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using MessageBus.Core;
using MessageBus.Core.API;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;

namespace MessageBus.Core
{
    public class Publisher : IPublisher
    {
        private readonly ConcurrentDictionary<Type, DataContractKey> _nameMappings = new ConcurrentDictionary<Type, DataContractKey>();

        private readonly IModel _model;

        private readonly IMessageHelper _messageHelper;
        private readonly ISerializerHelper _serializerHelper;

        private readonly string _busId;
        private readonly string _exchange;
        private readonly PublisherConfigurator _configuration;

        public Publisher(IModel model, string busId, string exchange, PublisherConfigurator configuration, IMessageHelper messageHelper, ISerializerHelper serializerHelper)
        {
            _model = model;
            _exchange = exchange;
            _configuration = configuration;
            _messageHelper = messageHelper;
            _serializerHelper = serializerHelper;
            _busId = busId;

            _model.BasicReturn += ModelOnBasicReturn;
        }

        private void ModelOnBasicReturn(IModel model, BasicReturnEventArgs args)
        {
            DataContractKey dataContractKey = args.BasicProperties.GetDataContractKey();

            Type dataType = _nameMappings.Where(pair => pair.Value.Equals(dataContractKey)).Select(pair => pair.Key).FirstOrDefault();

            if (dataType == null)
            {
                dataContractKey = DataContractKey.BinaryBlob;
            }

            object data = _serializerHelper.Deserialize(dataContractKey, dataType, args.Body);

            RawBusMessage message = _messageHelper.ConstructMessage(dataContractKey, args.BasicProperties, data);

            _configuration.ErrorHandler.DeliveryFailed(args.ReplyCode, args.ReplyText, message);
        }

        public void Dispose()
        {
            _model.BasicReturn -= ModelOnBasicReturn;

            _model.Close();
        }

        public void Send<TData>(TData data)
        {
            Send(new BusMessage<TData> {Data = data});
        }

        public void Send<TData>(BusMessage<TData> busMessage)
        {
            DataContractKey contractKey;
            Type type = busMessage.Data.GetType();

            if (!_nameMappings.TryGetValue(type, out contractKey))
            {
                DataContract contract = new DataContract(busMessage.Data);

                _nameMappings.TryAdd(type, contract.Key);

                contractKey = contract.Key;
            }

            BasicProperties basicProperties = new BasicProperties
            {
                AppId = _busId,
                Timestamp = DateTime.Now.ToAmqpTimestamp(),
                Type = contractKey.Name,
                Headers = new Dictionary<string, object>
                {
                    {MessagingConstants.HeaderNames.Name, contractKey.Name},
                    {MessagingConstants.HeaderNames.NameSpace, contractKey.Ns}
                }
            };
            
            foreach (BusHeader header in busMessage.Headers)
            {
                basicProperties.Headers.Add(header.Name, header.Value);
            }

            byte[] bytes = _serializerHelper.Serialize(busMessage.Data);
            
            _model.BasicPublish(_exchange, "", _configuration.MandatoryDelivery, false, basicProperties, bytes);
        }
    }
}