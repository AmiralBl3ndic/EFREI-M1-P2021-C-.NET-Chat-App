﻿using ChatAppServer.Models;
using ChatAppServer.Services;
using Communication;

namespace ChatAppServer
{
	public partial class ClientHandler
	{
		/// <summary>
		/// Handle the "say" command
		/// </summary>
		/// <param name="command">Command to parse and execute</param>
		/// <param name="response">Message object to send to the user</param>
		private void HandleSayCommand(Command command, Message response)
		{
			if (_user == null)
			{
				response.Type = MessageType.Error;
				response.Content = "You must be logged in to say something";
				Net.SendMessage(_tcpClient.GetStream(), response);
				return;
			}
			
			var topic = command.Arguments[0];

			// Check if topic exists
			if (!TopicsService.Exists(new Topic {Name = topic}))
			{
				response.Type = MessageType.Error;
				response.Content = $"Topic {topic} does not exist, you can create it with: create-topic {topic}";
				Net.SendMessage(_tcpClient.GetStream(), response);
				return;
			}
			
			// Check if user should be able to talk in the topic
			if (!_user.Topics.Contains(topic))
			{
				response.Type = MessageType.Error;
				response.Content = $"You cannot send messages in {topic} since you haven't joined it, you can join it with: join {topic}";
				Net.SendMessage(_tcpClient.GetStream(), response);
				return;
			}

			// Build message
			response.Content = $"[{_user.Username}@{topic}] - {command.Arguments[1]}";

			// Explore list of all connected clients 
			foreach (var (connectedUser, tcpClient) in ConnectedClients)
			{
				if (connectedUser.Topics.Contains(topic))  // Check if the user we are checking is subscribed to the concerned topic
				{
					Net.SendMessage(tcpClient.GetStream(), response);
				}
			}
		}

		/// <summary>
		/// Handle the "create-topic" command
		/// </summary>
		/// <param name="command">Command to parse and execute</param>
		/// <param name="response">Message object to send to the user</param>
		private void HandleCreateTopicCommand(Command command, Message response)
		{
			// Check if user is logged in
			if (_user == null)
			{
				response.Type = MessageType.Error;
				response.Content = "You must be logged in to create topics";
				return;
			}

			var topic = new Topic {Name = command.Arguments[0]};

			// Check if topic already exists
			if (TopicsService.Exists(topic))
			{
				response.Type = MessageType.Error;
				response.Content = $"Topic {command.Arguments[0]} already exists, consider joining it.";
				return;
			}

			// Actually create the topic
			TopicsService.Create(topic);
			
			// Add the topic to the list of joined topics
			_user.Topics.Add(topic.Name);
			UserService.Update(_user.Id, _user);

			response.Type = MessageType.Info;
			response.Content = $"Topic {topic.Name} created and joined.";
		}

		/// <summary>
		/// Handle the "join" command
		/// </summary>
		/// <param name="command">Command to parse and execute</param>
		/// <param name="response">Message object to send to the user</param>
		private void HandleJoinCommand(Command command, Message response)
		{
			if (_user == null)
			{
				response.Type = MessageType.Error;
				response.Content = "You must be logged in to join a topic";
				return;
			}

			var topic = new Topic {Name = command.Arguments[0]};

			if (!TopicsService.Exists(topic))
			{
				response.Type = MessageType.Error;
				response.Content = $"Topic {topic.Name} does not exist, you can create it with: create-topic {topic.Name}";
				return;
			}
			
			// Update user's list of topics
			_user.Topics.Add(topic.Name);
			UserService.Update(_user.Id, _user);

			response.Type = MessageType.Info;
			response.Content = $"Joined topic {topic.Name}";
		}

		/// <summary>
		/// Handle the "leave" command
		/// </summary>
		/// <param name="command">Command to parse and execute</param>
		/// <param name="response">Message object to send to the user</param>
		private void HandleLeaveCommand(Command command, Message response)
		{
			if (_user == null)
			{
				response.Type = MessageType.Error;
				response.Content = "You must be logged in to leave a topic";
				return;
			}
			
			var topic = new Topic {Name = command.Arguments[0]};
			
			if (!TopicsService.Exists(topic))
			{
				response.Type = MessageType.Error;
				response.Content = $"Topic {topic.Name} does not exist, you can create it with: create-topic {topic.Name}";
				return;
			}
			
			// Update user's list of topics
			_user.Topics.Remove(topic.Name);
			UserService.Update(_user.Id, _user);

			response.Type = MessageType.Info;
			response.Content = $"Left topic {topic.Name}";
		}
		
		/// <summary>
		/// Handle the "leave" command
		/// </summary>
		/// <param name="command">Command to parse and execute</param>
		/// <param name="response">Message object to send to the user</param>
		private void HandleDmCommand(Command command, Message response)
		{
			if (_user == null)
			{
				response.Type = MessageType.Error;
				response.Content = "You must be logged in to send private messages";
				return;
			}

			var receiverUsername = command.Arguments[0];
			var messageContent = command.Arguments[1];


			// Send message to 
			foreach (var (connectedUser, tcpClient) in ConnectedClients)
			{
				if (connectedUser.Username != receiverUsername) continue;
				
				response.Type = MessageType.Message;
					
				// Build and send message to receiver
				response.Content = $"[From: {_user.Username}] - {messageContent}";
				Net.SendMessage(tcpClient.GetStream(), response);
					
				// Build and send message feedback to sender
				response.Content = $"[To: {receiverUsername}] - {messageContent}";
				Net.SendMessage(_tcpClient.GetStream(), response);
				return;
			}

			// From this point, the message has not been sent to the receiver
			
			// Check if receiver exists
			if (UserService.GetByUsername(receiverUsername) == null)
			{
				response.Type = MessageType.Error;
				response.Content = $"No user found with username \"{receiverUsername}\"";
				Net.SendMessage(_tcpClient.GetStream(), response);
				return;
			}
			
			// TODO: store private message in database (receiver exists but is not connected)
		}
	}
}