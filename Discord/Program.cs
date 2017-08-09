using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Libs.Redmine;
using System.Threading;
using Redmine.Net.Api.Types;
using System.Configuration;

namespace Discord
{
	class Program
	{
		private DiscordSocketClient _client;
		private Client _redmineManager;
		private List<ulong> _channelIds = new List<ulong>();
		private List<Issue> _sendedIssues = new List<Issue>();
		private Timer _timer;

		public static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
			Console.WriteLine("Start app");
			try
			{
				#region Старт дискорда

				_client = new DiscordSocketClient();

				_client.MessageReceived += MessageReceived;

				string token = ConfigurationManager.AppSettings.Get("DiscordToken");
				await _client.LoginAsync(TokenType.Bot, token);
				await _client.StartAsync();

				#endregion

				#region Запуск проверки тикетов

				this._redmineManager = new Client();
				var autoEvent = new AutoResetEvent(false);

				this._timer = new Timer(new TimerCallback(SendIssuesToDiscrod),
										autoEvent, 20 * 1000, 2 * 60 * 1000);

				#endregion

				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		/// <summary>
		/// Эвент для получения сообщения в дискорд
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task MessageReceived(SocketMessage message)
		{
			if (message.Content == "start tickets bot")
			{
				this._channelIds.Add(message.Channel.Id);
				await message.Channel.SendMessageAsync($"Канал {message.Channel.Name} подписан на рассылку уведомлений о задачах на саппорте с высоким приоритетом.");
			}

			if (message.Content == "stop tickets bot")
			{
				if (this._channelIds.Contains(message.Channel.Id))
				{
					this._channelIds.Remove(message.Channel.Id);
					await message.Channel.SendMessageAsync($"Канал {message.Channel.Name} больше не подписан на рассылку.");
				}
				else
				{
					await message.Channel.SendMessageAsync($"Канал не был подписан на рассылку.");
				}
			}
		}

		private async void SendIssuesToDiscrod(Object state)
		{
			var issues = this.GetNewIssuesForSending();

			foreach (Issue issue in issues)
			{
				foreach (ulong channelId in this._channelIds)
				{
					var channel = _client.GetChannel(channelId) as SocketTextChannel;
					this._sendedIssues.Add(issue);
					await channel.SendMessageAsync($"Тикет на саппорте {issue.Subject}, {issue.Priority.Name} от {issue.Author.Name}" +
						$"\n http://helpdesk.nemo.travel/issues/{issue.Id.ToString()}");
				}
			}
		}

		private List<Issue> GetNewIssuesForSending()
		{
			Console.WriteLine("request to redmine");

			var issues = this._redmineManager.GetSupportIssues();
			var result = issues.Where(issue => 
				!this._sendedIssues.Contains(issue) 
				&& (issue.Priority.Name == "High" || issue.Priority.Name == "Critical" || issue.Priority.Name == "Blocker")).ToList();

			#region Очищение тикетов, которые больше не актуальны

			List<Issue> issuesToRemove = new List<Issue>();
			foreach (Issue sendedIssue in this._sendedIssues)
			{
				if (!issues.Contains(sendedIssue))
				{
					issuesToRemove.Add(sendedIssue);
				}
			}

			foreach (Issue issueToRemove in issuesToRemove)
			{
				this._sendedIssues.Remove(issueToRemove);
			}

			#endregion

			return result;
		}
	}
}
