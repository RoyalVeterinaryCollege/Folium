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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EventSaucing.Aggregates;

namespace Folium.Api.Models.Messaging {
	public class MessagingAggregate : Aggregate {
		public MessagingAggregate(Guid id) {
			base.Id = id;
		}
        readonly Dictionary<int, Message> _messages = new Dictionary<int, Message>();

        public ReadOnlyCollection<Message> Messages => _messages.Values.ToList().AsReadOnly();

        private bool _isCreated;
		private bool _isRemoved;

		public void Create(int fromUserId, int toUserId, string body) {
			if (_isCreated || _isRemoved) return;
            RaiseEvent(new MessageCreated(fromUserId, toUserId, body, DateTime.UtcNow));
		}
		public int Reply(int fromUserId, int toUserId, string body) {
            var newId = _messages.Count + 1;
            RaiseEvent(new MessageReplyCreated(newId, fromUserId, toUserId, body, DateTime.UtcNow));
            return newId;
        }

		#region Events

		void Apply(MessageCreated @event) {
			_isCreated = true;
            _messages.Add(0, new Message {
                FromUserId = @event.FromUserId,
                ToUserId = @event.ToUserId,
                Body = @event.Body,
                CreatedAt = @event.CreatedAt
            });
        }
		
		void Apply(MessageReplyCreated @event) {
            _messages.Add(@event.Id, new Message {
                Id = @event.Id,
                FromUserId = @event.FromUserId,
                ToUserId = @event.ToUserId,
                Body = @event.Body,
                CreatedAt = @event.CreatedAt
            });
        }
		#endregion Events
	}
}      
