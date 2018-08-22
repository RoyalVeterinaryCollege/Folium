/** 
 * Copyright 2017 The Royal Veterinary College, jbullock AT rvc.ac.uk
 * 
 * This file is part of Folium.
 * 
 * Folium is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Folium is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Folium.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using Folium.Api.Models;
using CommonDomain.Persistence;
using Folium.Api.Dtos;
using Folium.Api.Models.Messaging;

namespace Folium.Api.Services {
    public interface IMessagingService {
        MessageDto CreateMessage(User user, MessageDto messageDto);
        MessageDto CreateMessageReply(User user, MessageDto messageDto);
    }
    public class MessagingService : IMessagingService {
        private readonly IDbService _dbService;
		private readonly IConstructAggregates _factory;
		private readonly IRepository _repository;
		public MessagingService(
			IDbService dbService,
			IConstructAggregates factory,
			IRepository repository) {
            _dbService = dbService;
			_factory = factory;
			_repository = repository;
		}

        public MessageDto CreateMessage(User user, MessageDto messageDto) {
			var id = Guid.NewGuid();
			var messageAggregate = (MessagingAggregate)_factory.Build(typeof(MessagingAggregate), id, null);
            messageAggregate.OnFirstCreated();
            messageAggregate.Create(user.Id, messageDto.ToUserId, messageDto.Body);
			_repository.Save(messageAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
	        messageDto.Id = id;
            messageDto.CreatedAt = DateTime.UtcNow;
            return messageDto;
		}

        public MessageDto CreateMessageReply(User user, MessageDto messageDto) {
            var messageAggregate = _repository.GetById<MessagingAggregate>(messageDto.Id);
            var replyIndex = messageAggregate.Reply(user.Id, messageDto.ToUserId, messageDto.Body);
            _repository.Save(messageAggregate, commitId: Guid.NewGuid(), updateHeaders: null);
            messageDto.ReplyIndex = replyIndex;
            messageDto.CreatedAt = DateTime.UtcNow;
            return messageDto;
        }
    }
}