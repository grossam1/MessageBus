﻿using System.ServiceModel.Channels;
using MessageBus.Core.API;

namespace MessageBus.Core
{
    public class PublisherConfigurator : IPublisherConfigurator
    {
        private BufferManager _bufferManager;
        private IPublishingErrorHandler _errorHandler;
        private ISerializer _serializer;
        private bool _mandatoryDelivery;
        private bool _persistentDelivery;

        public BufferManager BufferManager
        {
            get { return _bufferManager; }
        }

        public IPublishingErrorHandler ErrorHandler
        {
            get
            {
                return _errorHandler ?? new NullPublishingErrorHandler();
            }
        }

        public bool MandatoryDelivery
        {
            get { return _mandatoryDelivery; }
        }

        public bool PersistentDelivery
        {
            get { return _persistentDelivery; }
        }

        public ISerializer Serializer
        {
            get { return _serializer ?? new JsonSerializer(); }
        }

        public IPublisherConfigurator UseBufferManager(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            return this;
        }

        public IPublisherConfigurator UseErrorHandler(IPublishingErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;

            return this;
        }

        public IPublisherConfigurator SetMandatoryDelivery()
        {
            _mandatoryDelivery = true;

            return this;
        }

        public IPublisherConfigurator SetPersistentDelivery()
        {
            _persistentDelivery = true;

            return this;
        }

        public IPublisherConfigurator UseSoapSerializer()
        {
            _serializer = new SoapSerializer();
            
            return this;
        }

        public IPublisherConfigurator UseCustomSerializer(ISerializer serializer)
        {
            _serializer = serializer;

            return this;
        }
    }
}