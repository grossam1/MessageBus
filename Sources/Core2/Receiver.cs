﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using MessageBus.Core.API;

using RabbitMQ.Client;

namespace MessageBus.Core
{
    public class Receiver : IReceiver
    {
        private readonly ConcurrentDictionary<Type, MessageFilterInfo> _nameMappings = new ConcurrentDictionary<Type, MessageFilterInfo>();

        private readonly IModel _model;
        private readonly SubscriptionHelper _helper;

        private readonly IMessageHelper _messageHelper;
        private readonly ISerializerHelper _serializerHelper;
        private readonly IErrorSubscriber _errorSubscriber;

        private readonly bool _receiveSelfPublish;
        private readonly string _queue;
        private readonly string _busId;

        public Receiver(IModel model, string busId, string exchange, string queue, IMessageHelper messageHelper, ISerializerHelper serializerHelper, IErrorSubscriber errorSubscriber, bool receiveSelfPublish)
        {
            _model = model;
            _busId = busId;

            _queue = queue;
            _messageHelper = messageHelper;
            _serializerHelper = serializerHelper;
            _errorSubscriber = errorSubscriber;
            _receiveSelfPublish = receiveSelfPublish;

            _helper = new SubscriptionHelper((type, filterInfo, handler) =>
            {
                if (_nameMappings.TryAdd(type, filterInfo))
                {
                    _model.QueueBind(_queue, exchange, filterInfo);

                    return true;
                }

                return false;
            });
        }

        public void Dispose()
        {
            _model.Dispose();
        }

        public void Open()
        {
        }

        public void Close()
        {
        }

        public bool Subscribe<TData>(bool hierarchy = false, IEnumerable<BusHeader> filter = null)
        {
            if (hierarchy)
            {
                return _helper.SubscribeHierarchy(typeof(TData), null, filter);
            }
            
            return _helper.Subscribe(typeof (TData), null, filter);
        }

        public TData Receive<TData>()
        {
            BusMessage<TData> message = ReceiveBusMessage<TData>();

            if (message == null) return default(TData);

            return message.Data;
        }
        
        public BusMessage<TData> ReceiveBusMessage<TData>()
        {
            BasicGetResult result = _model.BasicGet(_queue, true);

            IBasicProperties basicProperties = result.BasicProperties;

            DataContractKey dataContractKey = basicProperties.GetDataContractKey();

            var subscription =
                _nameMappings.Where(p => p.Value.ContractKey.Equals(dataContractKey)).Select(
                    pair =>
                        new
                        {
                            DataType = pair.Key
                        }).FirstOrDefault();

            if (subscription == null) return null;

            object data;
            
            try
            {
                data = _serializerHelper.Deserialize(dataContractKey, subscription.DataType, result.Body);
            }
            catch (Exception ex)
            {
                RawBusMessage rawBusMessage = _messageHelper.ConstructMessage(dataContractKey, basicProperties, (object)result.Body);

                _errorSubscriber.MessageDeserializeException(rawBusMessage, ex);

                return null;
            }

            BusMessage<TData> message = _messageHelper.ConstructMessage(dataContractKey, basicProperties, (TData)data);
            
            if (!_receiveSelfPublish && _busId.Equals(message.BusId))
            {
                RawBusMessage rawBusMessage = _messageHelper.ConstructMessage(dataContractKey, basicProperties, (object)result.Body);

                _errorSubscriber.MessageFilteredOut(rawBusMessage);

                return null;
            }
            
            return message;
        }
    }
}