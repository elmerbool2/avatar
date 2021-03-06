﻿using neurUL.Common.Domain.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ei8.Avatar.Domain.Model;

namespace ei8.Avatar.Application
{
    public class ResourceApplicationService : IResourceApplicationService
    {
        private readonly IResourceRepository resourceRepository;
        public ResourceApplicationService(IResourceRepository resourceRepository)
        {
            this.resourceRepository = resourceRepository;
        }

        public async Task<Resource> GetByPath(string path, CancellationToken token = default)
        {
            AssertionConcern.AssertArgumentNotEmpty(path, Constants.Messages.Exception.PathInvalid, nameof(path));

            await this.resourceRepository.Initialize();

            return await this.resourceRepository.GetByPath(path);
        }
    }
}
