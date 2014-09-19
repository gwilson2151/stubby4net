using System;
using System.Collections.Generic;
using System.Linq;

namespace stubby.Domain
{
	internal class InvocationDb
	{
		private readonly IDictionary<string, IDictionary<string, IList<Invocation>>> _invocations = new Dictionary<string, IDictionary<string, IList<Invocation>>>();

		public void Add(Invocation invocation)
		{
			var method = invocation.Method.ToUpperInvariant();
			var url = invocation.Url.ToUpperInvariant();
			IDictionary<string, IList<Invocation>> verbInvocations;
			if (!_invocations.TryGetValue(method, out verbInvocations))
			{
				lock (_invocations)
				{
					if (!_invocations.TryGetValue(method, out verbInvocations))
					{
						verbInvocations = new Dictionary<string, IList<Invocation>>();
						verbInvocations[url] = new List<Invocation>();
						_invocations[method] = verbInvocations;
					}
				}
			}

			IList<Invocation> urlInvocations;
			if (!verbInvocations.TryGetValue(url, out urlInvocations))
			{
				lock (verbInvocations)
				{
					if (!verbInvocations.TryGetValue(url, out urlInvocations))
					{
						urlInvocations = new List<Invocation>();
						verbInvocations[url] = urlInvocations;
					}
				}
			}

			lock (urlInvocations)
				urlInvocations.Add(invocation);
		}

		private IList<Invocation> GetInvocations(Invocation incoming)
		{
			IDictionary<string, IList<Invocation>> verbInvocations;
			if (!_invocations.TryGetValue(incoming.Method.ToUpperInvariant(), out verbInvocations))
				return new List<Invocation>();

			IList<Invocation> invocations;
			if (!verbInvocations.TryGetValue(incoming.Url.ToUpperInvariant(), out invocations))
				return new List<Invocation>();

			return invocations;
		}

		public IList<Invocation> Find(Invocation incoming)
		{
			var invocations = GetInvocations(incoming);
			lock (invocations)
				return invocations.Where(i => IsMatched(i, incoming)).ToList();
		}

		private static bool IsMatched(Invocation invocation, Invocation incoming)
		{
			if (!IsMatched(invocation.Headers, incoming.Headers))
				return false;

			if (!IsMatched(invocation.Query, incoming.Query))
				return false;

			return String.IsNullOrEmpty(incoming.Post) ||
			       String.Equals(invocation.Post, incoming.Post, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsMatched(IDictionary<string, IList<string>> invocation, IDictionary<string, IList<string>> incoming)
		{
			var isInvocationEmpty = invocation == null || invocation.Count == 0;
			var isIncomingEmpty = incoming == null || incoming.Count == 0;

			if (isIncomingEmpty && isInvocationEmpty)
				return true;

			if (isIncomingEmpty != isInvocationEmpty)
				return false;

			foreach (var incomingEntry in incoming)
			{
				var invocationEntry = invocation
					.Select(x => new { x.Key, x.Value })
					.FirstOrDefault(x => String.Equals(x.Key, incomingEntry.Key, StringComparison.OrdinalIgnoreCase));
				if (invocationEntry == null)
					return false;

				if (!incomingEntry.Value.Any())
					continue;

				if (!incomingEntry.Value.All(x => invocationEntry.Value.Any(y => String.Equals(x, y, StringComparison.OrdinalIgnoreCase))))
					return false;
			}

			return true;
		}
	}
}
