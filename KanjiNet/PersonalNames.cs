using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace KanjiNet;

public static class PersonalNames
{
	private static readonly UnicodeHelpers.RangeTable Jinmei = BuildJinmei();

	public static bool IsForPersonalNames(int scalar)
		=> RegularUse.IsRegularUse(scalar) || Jinmei.IsIn(scalar);

	public static bool IsNotForPersonalNames(int scalar)
		=> UnicodeHelpers.IsHan(scalar) && !IsForPersonalNames(scalar);

	private static UnicodeHelpers.RangeTable BuildJinmei()
	{
		// golden_jinmei.txt のセクション1（!!!で囲まれたブロック）にすべての対象文字が並ぶ
		var path = Path.Combine(AppContext.BaseDirectory, "testdata", "golden_jinmei.txt");
		if (!File.Exists(path)) return new UnicodeHelpers.RangeTable();
		var scalars = new HashSet<int>();
		using var sr = new StreamReader(path);
		string? line = sr.ReadLine();
		if (line is null || !line.StartsWith("!!!")) return new UnicodeHelpers.RangeTable();
		while ((line = sr.ReadLine()) != null)
		{
			if (line.StartsWith("!!!")) break;
			foreach (var ch in line)
			{
				if (ch == '‐') continue;
				scalars.Add(char.ConvertToUtf32(ch.ToString(), 0));
			}
		}
		var r16 = ToRanges16(scalars.Where(x => x <= 0xFFFF)).ToArray();
		var r32 = ToRanges32(scalars.Where(x => x > 0xFFFF)).ToArray();
		return new UnicodeHelpers.RangeTable(r16, r32);
	}

	private static IEnumerable<UnicodeHelpers.Range16> ToRanges16(IEnumerable<int> codes)
	{
		var sorted = codes.Select(x => (ushort)x).Distinct().OrderBy(x => x).ToArray();
		if (sorted.Length == 0) yield break;
		ushort start = sorted[0];
		ushort prev = sorted[0];
		for (int i = 1; i < sorted.Length; i++)
		{
			var cur = sorted[i];
			if (cur == prev + 1) { prev = cur; continue; }
			yield return new UnicodeHelpers.Range16(start, prev, 1);
			start = prev = cur;
		}
		yield return new UnicodeHelpers.Range16(start, prev, 1);
	}

	private static IEnumerable<UnicodeHelpers.Range32> ToRanges32(IEnumerable<int> codes)
	{
		var sorted = codes.Select(x => (uint)x).Distinct().OrderBy(x => x).ToArray();
		if (sorted.Length == 0) yield break;
		uint start = sorted[0];
		uint prev = sorted[0];
		for (int i = 1; i < sorted.Length; i++)
		{
			var cur = sorted[i];
			if (cur == prev + 1) { prev = cur; continue; }
			yield return new UnicodeHelpers.Range32(start, prev, 1);
			start = prev = cur;
		}
		yield return new UnicodeHelpers.Range32(start, prev, 1);
	}
}
