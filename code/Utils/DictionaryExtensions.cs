using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class DictionaryExtension
{
	public static TValue AddOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
	{
		if (!dictionary.ContainsKey(key))
			dictionary.Add(key, new TValue());
		return dictionary[key];
	}
}
