using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using Redmine.Net.Api.Async;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Configuration;

namespace Discord.Libs.Redmine
{
	public class Client
	{
		public RedmineManager Manager { get; set; }

		public Client()
		{
			this.Manager = new RedmineManager(ConfigurationManager.AppSettings.Get("RedmineHost"), ConfigurationManager.AppSettings.Get("RedmineKey"));
		}

		public List<Issue> GetSupportIssues()
		{
			var parameters = new NameValueCollection { };
			parameters.Add(RedmineKeys.ASSIGNED_TO_ID, "123");
			// 123 support reception
			// 343 fimchenko
			return this.Manager.GetObjects<Issue>(parameters);
		}
	}
}