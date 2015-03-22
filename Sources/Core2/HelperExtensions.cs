﻿using System;
using System.Collections.Generic;
using System.Text;
using MessageBus.Core;
using MessageBus.Core.API;
using RabbitMQ.Client;

namespace MessageBus.Core
{
    public static class HelperExtensions
    {
        public static void QueueBind(this IModel model, string queue, string exchange, MessageFilterInfo filterInfo)
        {
            IDictionary<string, object> arguments = new Dictionary<string, object>();

            arguments.Add(MessagingConstants.HeaderNames.Name, filterInfo.ContractKey.Name);
            arguments.Add(MessagingConstants.HeaderNames.NameSpace, filterInfo.ContractKey.Ns);

            foreach (BusHeader header in filterInfo.FilterHeaders)
            {
                arguments.Add(header.Name, header.Value);
            }

            model.QueueBind(queue, exchange, "", arguments);
        }

        public static string GetHeaderValue(this IBasicProperties basicProperties, string key)
        {
            string name = "";

            if (basicProperties.Headers.ContainsKey(key))
            {
                object o = basicProperties.Headers[key];

                name = Encoding.ASCII.GetString((byte[])o);

                basicProperties.Headers.Remove(key);
            }

            return name;
        }

        public static DataContractKey GetDataContractKey(this IBasicProperties properties)
        {
            return new DataContractKey(properties.GetHeaderValue(MessagingConstants.HeaderNames.Name),
                                        properties.GetHeaderValue(MessagingConstants.HeaderNames.NameSpace));
        }

        public static DateTime GetDateTime(this AmqpTimestamp timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timestamp.UnixTime);
        }

        public static AmqpTimestamp ToAmqpTimestamp(this DateTime dateTime)
        {
            return new AmqpTimestamp(Convert.ToInt64((dateTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds));
        }
    }
}