﻿using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Communication;

namespace ChatAppClient
{
	public class Client : TcpClient
	{
		private object _consoleAccess = new object();
		
		public Client(string hostname, int port) : base(hostname, port)
		{
			Console.WriteLine("Connection established");
		}

		
		public void Start()
		{
			new Thread(ReceiveMessagesFromServer).Start();

			var inputBuilder = new StringBuilder();
			
			
			while (true)
			{
				bool commandSubmitted = false;
				
				Console.SetCursorPosition(0, Console.BufferHeight - 1);
				Console.Write("$ ");

				

				while (!commandSubmitted)
				{
					ConsoleKeyInfo key;
					lock (_consoleAccess)
					{
						key = Console.ReadKey(false);
					}

					if (key.Key == ConsoleKey.Enter)
					{
						commandSubmitted = true;
					}
					else
					{
						inputBuilder.Append(key.KeyChar.ToString());
					}
				}
				
				var command = Command.Prepare(inputBuilder.ToString());
				inputBuilder.Clear();

				// Do not send commands which are known to contain an error
				if (command.Error != null)
				{
					Console.WriteLine(command);
					continue;
				}
				
				Net.SendCommand(GetStream(), command);
			}
		}


		public void ReceiveMessagesFromServer()
		{
			while (true)
			{
				var previousLeft = Console.CursorLeft;
				var previousTop = Console.CursorTop;
				
				// Simulate receiving message from server
				Console.SetCursorPosition(0, 0);
        Console.WriteLine("Hello");
        Console.SetCursorPosition(previousLeft, previousTop);
        
        Thread.Sleep(100);
			}
		}
	}
}