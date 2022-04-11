using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

internal static class Extensions
{
	public static TValue AddOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
	{
		if (!dictionary.ContainsKey(key))
			dictionary.Add(key, new TValue());
		return dictionary[key];
	}

	public static Vector3 RotateAroundPoint(this Vector3 vec, Vector3 pivot, Rotation rot)
	{
		return rot * (vec - pivot) + pivot;
	}

	public static float NormalDot(this Vector3 a, Vector3 b)
	{
		return Vector3.Dot(a.Normal, b.Normal).Clamp(-1f, 1f);
	}

	// Helper for getting clients from a string.
	// Currently accepts, in descending priority:
	//  - [any number] to get clients by network ident
	//  - !me, !self, or !player for current player
	//  - !picker for player under crosshairs
	//  - !all for all players
	//  - !bots for all bots
	//  - !humans or !players for all humans
	//  - Multiple partial name matches
	public static IEnumerable<Client> TryGetClients(this Client cl, string str)
	{
		if (string.IsNullOrEmpty(str))
			return null;

		const char token = '!';

		List<Client> list = new();

		string specifier = str.Remove(0, 1);
		int possibleNum = str.ToInt(-1);

		if (possibleNum > 0)
			list.AddRange(Client.All.Where(x => x.NetworkIdent == possibleNum));
		else if (str[0] == token)
		{
			switch (specifier)
			{
				case "me":
				case "self":
				case "player":
					return new[] { cl };
				case "picker":
					if (!cl.IsValid() || !cl.Pawn.IsValid())
						break;

					var tr = Trace.Ray(cl.Pawn.EyePosition, cl.Pawn.EyePosition + cl.Pawn.EyeRotation.Forward * Int16.MaxValue).Ignore(cl.Pawn).Run();

					if (tr.Hit && tr.Entity.IsValid() && !tr.Entity.IsWorld && tr.Entity.Client.IsValid())
						list.Add(tr.Entity.Client);
					break;
				case "all":
					return Client.All;
				case "bots":
					return Client.All.Where(x => x.IsBot);
				case "humans":
				case "players":
					return Client.All.Where(x => !x.IsBot);
			}
		}

		if (list.Count == 0)
		{
			// try by partial name
			list.AddRange(Client.All.Where(x => x.Name.Contains(str, StringComparison.InvariantCultureIgnoreCase)));
		}

		return list;
	}

	public static Client TryGetClient(this Client cl, string str)
	{
		return TryGetClients(cl, str).FirstOrDefault();
	}
}
