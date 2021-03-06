﻿using Flurl;
using neurUL.Common.Domain.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ei8.Cortex.Graph.Client;
using ei8.Avatar.Domain.Model;
using ei8.EventSourcing.Client.Out;
using static ei8.Avatar.Application.Constants;

namespace ei8.Avatar.Application
{
    public class AuthorApplicationService : IAuthorApplicationService
    {
        private readonly ISettingsService settingsService;
        private readonly INotificationClient notificationClient;
        private readonly INeuronGraphQueryClient neuronGraphQueryClient;
        private readonly IUserRepository userRepository;
        private readonly IRegionPermitRepository regionPermitRepository;

        public AuthorApplicationService(ISettingsService settingsService, INotificationClient notificationClient, INeuronGraphQueryClient neuronGraphQueryClient, IUserRepository userRepository, IRegionPermitRepository regionPermitRepository)
        {
            this.settingsService = settingsService;
            this.notificationClient = notificationClient;
            this.neuronGraphQueryClient = neuronGraphQueryClient;
            this.userRepository = userRepository;
            this.regionPermitRepository = regionPermitRepository;
        }

        public async Task<ValidationResult> ValidateWrite(Guid neuronId, Guid regionId, Guid subjectId, CancellationToken token = default)
        {
            AssertionConcern.AssertArgumentValid(g => g != Guid.Empty, neuronId, Constants.Messages.Exception.InvalidId, nameof(neuronId));
            AssertionConcern.AssertArgumentValid(g => g != Guid.Empty, subjectId, Constants.Messages.Exception.InvalidId, nameof(subjectId));

            var cortexGraphOutUrl = this.settingsService.CortexGraphOutBaseUrl + "/";
            var eventSourcingOutUrl = this.settingsService.EventSourcingOutBaseUrl + "/";
            var author = await this.GetAuthorBySubjectId(subjectId, token);
            
            // Ensure that Neuron Id is equal to AuthorId if first Neuron is being created
            if ((await this.notificationClient.GetNotificationLog(eventSourcingOutUrl, string.Empty)).NotificationList.Count == 0)
                AssertionConcern.AssertArgumentValid(m => m == author.User.NeuronId, neuronId, "Author Neuron is expected.", nameof(neuronId));
            // Ensure that Neuron Id is not equal to AuthorId if non-first Neuron is being created
            else
                AssertionConcern.AssertArgumentValid(m => m != author.User.NeuronId, neuronId, "Author Neuron not expected .", nameof(neuronId));

            // if region was specified, check if it exists
            if (regionId != Guid.Empty)
            {
                // ensure that layer is a valid neuron
                var region = await this.neuronGraphQueryClient.GetNeuronById(
                    cortexGraphOutUrl,
                    regionId.ToString(),
                    token: token
                    );
                AssertionConcern.AssertStateTrue(region != null, "Invalid region specified");
            }

            // get reference to neuron being modified
            var neuron = (await this.neuronGraphQueryClient.GetNeuronById(
                cortexGraphOutUrl.ToString(),
                neuronId.ToString(),
                token: token
                )).First();

            // get write permit of author user for region
            var permit = author.Permits.SingleOrDefault(l => l.RegionNeuronId == regionId && l.WriteLevel > 0);

            // does author user have a write permit
            AssertionConcern.AssertStateTrue(
                permit != null,
                string.Format(Messages.Exception.UnauthorizedLayerWriteTemplate, neuron.RegionTag)
                );

            // TODO: test
            // does neuron already exist
            if (neuron != null)
            {                
                AssertionConcern.AssertArgumentValid(r => r.ToString() == neuron.RegionId, regionId, "Specified RegionId does not match RegionId of specified Neuron.", nameof(regionId));

                // does author user have an admin write access, or author user is the author of this neuron
                AssertionConcern.AssertStateTrue(
                    permit.WriteLevel == 2 || neuron.AuthorId == author.User.NeuronId.ToString(),
                    string.Format(Messages.Exception.UnauthorizedNeuronWriteTemplate, neuron.Tag)
                    );
            }

            return new ValidationResult(new string[0], true);
        }

        public async Task<Author> GetAuthorBySubjectId(Guid subjectId, CancellationToken token = default)
        {
            AssertionConcern.AssertArgumentValid(g => g != Guid.Empty, subjectId, Constants.Messages.Exception.InvalidId, nameof(subjectId));

            await this.userRepository.Initialize();
            User user = await this.userRepository.GetBySubjectId(subjectId);
            
            AssertionConcern.AssertStateTrue(user != null, Constants.Messages.Exception.UnauthorizedUserAccess);

            // TODO: check if null if neuron is inactive or deactivated, if so, should throw exception
            var userNeuron = (await this.neuronGraphQueryClient.GetNeuronById(
                this.settingsService.CortexGraphOutBaseUrl + "/",
                user.NeuronId.ToString(),
                token: token
                )).First();
            AssertionConcern.AssertStateTrue(userNeuron != null, Constants.Messages.Exception.NeuronNotFound);

            await this.regionPermitRepository.Initialize();
            var permits = await this.regionPermitRepository.GetAllByUserNeuronId(user.NeuronId);
            var author = new Author(
                user,
                permits
                );
            return author;
        }
    }
}
