﻿/*
 * Copyright 2014, 2015 James Geall
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Autofac;
using IdentityServer.Core.MongoDb;
using MongoDB.Driver;
using Thinktecture.IdentityServer.Core.Configuration;
using Thinktecture.IdentityServer.Core.Services;

namespace Core.MongoDb.Tests
{
    public class PersistenceTestFixture
    {
        private readonly StoreSettings _settings;
        private readonly MongoDatabase _database;
        private readonly Factory _factory;

        public PersistenceTestFixture()
        {
            _settings = StoreSettings.DefaultSettings();
            _settings.Database = "testidentityserver";
            var registrations = new ServiceFactory(null, _settings);
            var client = new MongoClient(_settings.ConnectionString);
            var server = client.GetServer();
            _database = server.GetDatabase(_settings.Database);
            _factory = new Factory(registrations);
        }


        public MongoDatabase Database
        {
            get { return _database; }
        }

        public StoreSettings Settings
        {
            get { return _settings; }
        }

        public Factory Factory
        {
            get { return _factory; }
        }
    }

    public class Factory
    {
        private readonly IContainer _container;

        public Factory(ServiceFactory config)
        {
            var cb = new ContainerBuilder();
            Register(cb, config.AuthorizationCodeStore);
            Register(cb, config.ClientStore);
            Register(cb, config.ConsentStore);
            Register(cb, config.RefreshTokenStore);
            Register(cb, config.ScopeStore);

            Register(cb, config.TokenHandleStore);
            foreach (var registration in config.Registrations)
            {
                Register(cb, registration);
            }
            _container = cb.Build();
        }

        private void Register(ContainerBuilder cb, Registration registration, string name = null)
        {
            if (registration.Instance != null)
            {
                var reg = cb.Register(ctx => registration.Instance).SingleInstance();
                if (name != null)
                {
                    reg.Named(name, registration.DependencyType);
                } else
                {
                    reg.As(registration.DependencyType);
                }
            } else if (registration.Type != null)
            {
                var reg = cb.RegisterType(registration.Type);
                if (name != null)
                {
                    reg.Named(name, registration.DependencyType);
                } else
                {
                    reg.As(registration.DependencyType);
                }
            } else if (registration.Factory != null)
            {
                var reg = cb.Register(ctx => registration.Factory(new AutofacDependencyResolver(ctx)));
                if (name != null)
                {
                    reg.Named(name, registration.DependencyType);
                } else
                {
                    reg.As(registration.DependencyType);
                }
            } else
            {
                var message = "No type or factory found on registration " + registration.GetType().FullName;
                throw new InvalidOperationException(message);
            }
        }

        private class AutofacDependencyResolver : IDependencyResolver
        {
            private readonly IComponentContext _ctx;

            public AutofacDependencyResolver(IComponentContext ctx)
            {
                _ctx = ctx;
            }

            public T Resolve<T>(string name = null)
            {
                if (name == null)
                    return _ctx.Resolve<T>();
                return _ctx.ResolveNamed<T>(name);
            }
        }

        public T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
    }
}